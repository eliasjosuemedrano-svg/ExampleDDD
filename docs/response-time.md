# Guía de Implementación: Métricas de Tiempo de Respuesta con Almacenamiento en MongoDB

## Requisitos Previos
- .NET 8.0 SDK o superior
- Visual Studio 2022 o VS Code
- MongoDB Server (local o remoto)
- Paquetes NuGet:
  - `Microsoft.AspNetCore.Http.Abstractions`
  - `Microsoft.Extensions.Logging.Abstractions`
  - `Microsoft.Extensions.Options`
  - `MongoDB.Driver`
  - `Microsoft.Extensions.Configuration`

## Configuración de MongoDB

### 1. Configuración en appsettings.json

Asegúrate de tener la siguiente configuración en tu archivo `appsettings.json`:

```json
{
  "MongoDBSettings": {
    "ConnectionString": "mongodb://usuario:contraseña@localhost:27017",
    "DatabaseName": "tu_base_de_datos"
  }
}
```

## Implementación del Middleware con Almacenamiento en MongoDB

### 1. Entidad ResponseTimeLog

```csharp
#nullable enable
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DDDExample.Domain.Entities
{
    public class ResponseTimeLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        public string? Path { get; set; }
        public string? Method { get; set; }
        public string? QueryString { get; set; }
        public long DurationMs { get; set; }
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? ClientIp { get; set; }
        public string? UserAgent { get; set; }
        public bool IsSlowRequest { get; set; }
    }
}
```

### 2. Interfaz IResponseTimeLogRepository

```csharp
#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using DDDExample.Domain.Entities;

namespace DDDExample.Domain.Repositories
{
    public interface IResponseTimeLogRepository
    {
        Task AddAsync(ResponseTimeLog log);
        Task<IEnumerable<ResponseTimeLog>> GetLogsAsync(
            string? path = null, 
            string? method = null, 
            int? minDurationMs = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null,
            int limit = 100);
    }
}
```

### 3. Implementación de MongoResponseTimeLogRepository

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DDDExample.Domain.Entities;
using DDDExample.Domain.Repositories;
using DDDExample.Infrastructure.Persistence.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DDDExample.Infrastructure.Repositories.MongoDB
{
    public class MongoResponseTimeLogRepository : IResponseTimeLogRepository
    {
        private readonly IMongoCollection<ResponseTimeLog> _logsCollection;

        public MongoResponseTimeLogRepository(IOptions<MongoDbSettings> settings)
        {
            if (settings?.Value == null)
            {
                throw new ArgumentNullException(nameof(settings), "MongoDB settings are not configured.");
            }

            if (string.IsNullOrEmpty(settings.Value.ConnectionString))
            {
                throw new ArgumentException("MongoDB connection string is not configured.", nameof(settings));
            }

            if (string.IsNullOrEmpty(settings.Value.DatabaseName))
            {
                throw new ArgumentException("MongoDB database name is not configured.", nameof(settings));
            }

            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _logsCollection = database.GetCollection<ResponseTimeLog>("responseTimeLogs");
            
            var indexKeysDefinition = Builders<ResponseTimeLog>.IndexKeys.Descending(x => x.Timestamp);
            _logsCollection.Indexes.CreateOne(new CreateIndexModel<ResponseTimeLog>(indexKeysDefinition));
        }

        public async Task AddAsync(ResponseTimeLog log)
        {
            await _logsCollection.InsertOneAsync(log);
        }

        public async Task<IEnumerable<ResponseTimeLog>> GetLogsAsync(
            string? path = null, 
            string? method = null, 
            int? minDurationMs = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null,
            int limit = 100)
        {
            var query = _logsCollection.AsQueryable();

            if (!string.IsNullOrEmpty(path))
                query = query.Where(x => x.Path != null && x.Path.Contains(path));
                
            if (!string.IsNullOrEmpty(method))
                query = query.Where(x => x.Method != null && x.Method.Equals(method, StringComparison.OrdinalIgnoreCase));
                
            if (minDurationMs.HasValue)
                query = query.Where(x => x.DurationMs >= minDurationMs.Value);
                
            if (startDate.HasValue)
                query = query.Where(x => x.Timestamp >= startDate.Value);
                
            if (endDate.HasValue)
                query = query.Where(x => x.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(x => x.Timestamp)
                .Take(limit)
                .ToListAsync();
        }
    }
}
```

### 4. ResponseTimeMiddleware.cs

```csharp
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using DDDExample.Domain.Entities;
using DDDExample.Domain.Repositories;

namespace DDDExample.Middleware
{
    public class ResponseTimeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ResponseTimeMiddleware> _logger;
        private readonly IResponseTimeLogRepository _logRepository;
        private readonly long _thresholdMs;
        private readonly bool _logSlowRequests;

        public ResponseTimeMiddleware(
            RequestDelegate next, 
            ILogger<ResponseTimeMiddleware> logger,
            IResponseTimeLogRepository logRepository,
            long thresholdMs = 500,
            bool logSlowRequests = true)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
            _thresholdMs = thresholdMs;
            _logSlowRequests = logSlowRequests;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/health") || 
                context.Request.Path.StartsWithSegments("/swagger") ||
                context.Request.Path.StartsWithSegments("/_framework") ||
                context.Request.Path.StartsWithSegments("/favicon.ico"))
            {
                await _next(context);
                return;
            }

            var watch = Stopwatch.StartNew();
            
            try
            {
                context.Response.OnStarting(state =>
                {
                    var httpContext = (HttpContext)state;
                    httpContext.Response.Headers["X-Response-Time"] = $"{watch.ElapsedMilliseconds}ms";
                    return Task.CompletedTask;
                }, context);

                await _next(context);
                
                watch.Stop();
                var responseTime = watch.ElapsedMilliseconds;
                var isSlow = responseTime > _thresholdMs;

                if (_logSlowRequests && isSlow)
                {
                    _logger.LogWarning($"Slow Request Detected: {context.Request.Method} {context.Request.Path} took {responseTime}ms");
                }
                else
                {
                    _logger.LogInformation($"Request: {context.Request.Method} {context.Request.Path} - {responseTime}ms");
                }

                try
                {
                    var logEntry = new ResponseTimeLog
                    {
                        Path = context.Request.Path,
                        Method = context.Request.Method,
                        QueryString = context.Request.QueryString.Value,
                        DurationMs = responseTime,
                        StatusCode = context.Response.StatusCode,
                        ClientIp = context.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = context.Request.Headers["User-Agent"],
                        IsSlowRequest = isSlow,
                        Timestamp = DateTime.UtcNow
                    };

                    _ = _logRepository.AddAsync(logEntry).ContinueWith(t => 
                    {
                        if (t.IsFaulted)
                        {
                            _logger.LogError(t.Exception, "Failed to log response time to MongoDB");
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error logging response time");
                }
            }
            catch (Exception ex)
            {
                watch.Stop();
                _logger.LogError(ex, "Error processing request {Method} {Path} after {Elapsed}ms",
                    context.Request.Method,
                    context.Request.Path,
                    watch.ElapsedMilliseconds);
                
                if (!context.Response.HasStarted)
                {
                    throw;
                }
                
                _logger.LogWarning("Could not handle error properly because response has already started.");
            }
        }
    }
}
```

### 2. MiddlewareExtensions.cs

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace DDDExample.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IServiceCollection AddResponseTimeMiddleware(this IServiceCollection services)
        {
            services.Configure<ResponseTimeOptions>(options =>
            {
                options.ThresholdMs = 500;
                options.LogSlowRequests = true;
            });
            
            // Register MongoDB repository as singleton since it's used in middleware
            services.AddSingleton<IResponseTimeLogRepository, MongoResponseTimeLogRepository>();
            
            return services.AddScoped<ResponseTimeMiddleware>();
        }

        public static IServiceCollection AddResponseTimeMiddleware(this IServiceCollection services, 
            Action<ResponseTimeOptions> configureOptions)
        {
            services.Configure(configureOptions);
            
            return services.AddResponseTimeMiddleware();
        }

        public static IApplicationBuilder UseResponseTimeMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ResponseTimeMiddleware>();
        }
    }

    public class ResponseTimeOptions
    {
        public long ThresholdMs { get; set; } = 500;
        public bool LogSlowRequests { get; set; } = true;
    }
}
```

## Pasos de Instalación

1. **Crear el proyecto de middleware** (si no existe):
   ```bash
   dotnet new classlib -n DDDExample.Middleware
   cd DDDExample.Middleware
   dotnet add package Microsoft.AspNetCore.Http.Abstractions
   dotnet add package Microsoft.Extensions.Logging.Abstractions
   dotnet add package Microsoft.Extensions.Options
   ```

2. **Agregar referencia al proyecto de middleware** desde el proyecto API:
   ```bash
   dotnet add reference ../DDDExample.Middleware/DDDExample.Middleware.csproj
   ```

3. **Configurar el middleware** en `Program.cs`:
   ```csharp
   // Agregar antes de builder.Build()
   builder.Services.AddResponseTimeMiddleware(options =>
   {
       options.ThresholdMs = 500; // Umbral para registrar solicitudes lentas
       options.LogSlowRequests = true;
   });
   
   // ... otros servicios ...
   
   var app = builder.Build();
   
   // Agregar después de UseRouting() y antes de UseAuthorization()
   app.UseResponseTimeMiddleware();
   ```

## Configuración

### Opciones de configuración

El middleware acepta las siguientes opciones:

```csharp
public class ResponseTimeOptions
{
    public long ThresholdMs { get; set; } = 500;
    public bool LogSlowRequests { get; set; } = true;
}
```

### Rutas excluidas por defecto

El middleware excluye automáticamente las siguientes rutas:
- `/health`
- `/swagger`
- `/_framework`
- `/favicon.ico`

## Verificación

1. Inicia la aplicación:
   ```bash
   dotnet run --project src/DDDExample.API
   ```

2. Realiza peticiones a los endpoints de la API

3. Verifica los logs para ver los tiempos de respuesta:
   ```
   warn: DDDExample.Middleware.ResponseTimeMiddleware[0]
         Request GET /api/products took 650ms (Threshold: 500ms)
   ```

4. Verifica el encabezado de respuesta `X-Response-Time`

## Solución de Problemas

- **No se registran tiempos**:
  - Verifica que el middleware esté registrado después de `UseRouting()`
  - Asegúrate de que la ruta no esté en la lista de rutas excluidas

- **Tiempos incorrectos**:
  - Verifica que no haya otros middlewares que puedan afectar el flujo
  - Revisa si hay operaciones asíncronas que no estén siendo esperadas correctamente

- **Errores 500**:
  - Verifica que todos los servicios requeridos estén registrados
  - Revisa los logs de la aplicación para mensajes de error detallados