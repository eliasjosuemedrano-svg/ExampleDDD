# Guía de Instalación, Configuración y Construcción

Esta guía te ayudará a configurar, construir y ejecutar el proyecto localmente siguiendo los principios de Domain-Driven Design (DDD).

Esta guía te ayudará a configurar y ejecutar el proyecto localmente.

## Estructura del Proyecto

El proyecto sigue una arquitectura limpia (Clean Architecture) con separación clara de responsabilidades:

```
src/
├── DDDExample.API/               # Capa de presentación (Web API)
│   ├── Controllers/             # Controladores de la API
│   ├── DependencyInjection.cs   # Configuración de inyección de dependencias
│   └── Program.cs               # Punto de entrada de la aplicación
├── DDDExample.Application/       # Capa de aplicación
│   ├── DTOs/                    # Objetos de transferencia de datos
│   ├── Interfaces/              # Interfaces de servicios
│   ├── Mappings/                # Configuración de AutoMapper
│   └── Services/                # Implementación de servicios
├── DDDExample.Domain/           # Capa de dominio
│   ├── Entities/                # Entidades del dominio
│   ├── Repositories/            # Interfaces de repositorios
│   └── Common/                  # Clases base y utilidades
└── DDDExample.Infrastructure/   # Capa de infraestructura
    ├── Persistence/            # Configuración de MongoDB
    └── Repositories/           # Implementación de repositorios
```

## Flujo de la Aplicación

1. **Capa de API**: Recibe las peticiones HTTP a través de los controladores
2. **Capa de Aplicación**: Orquesta el flujo de la aplicación usando servicios
3. **Capa de Dominio**: Contiene la lógica de negocio y reglas del dominio
4. **Capa de Infraestructura**: Maneja la persistencia de datos y servicios externos

## Requisitos Previos

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) o superior
- [Visual Studio 2022](https://visualstudio.microsoft.com/es/vs/) o [Visual Studio Code](https://code.visualstudio.com/)
- [MongoDB Atlas](https://www.mongodb.com/cloud/atlas) (cuenta gratuita)
- [Git](https://git-scm.com/) (opcional)

## Construcción de la Aplicación

### 1. Configuración de la Solución

La solución está organizada en 4 proyectos principales:

1. **DDDExample.API**: Proyecto web que expone los endpoints REST
2. **DDDExample.Application**: Contiene la lógica de la aplicación
3. **DDDExample.Domain**: Define el modelo de dominio y sus reglas
4. **DDDExample.Infrastructure**: Implementa la persistencia y servicios externos

### 2. Configuración de Dependencias

Las dependencias principales son:

- **API**:
  - `Microsoft.AspNetCore.OpenApi` para documentación de la API
  - `AutoMapper.Extensions.Microsoft.DependencyInjection` para mapeo de objetos
  - `MongoDB.Driver` para la conexión con MongoDB
  - `Microsoft.EntityFrameworkCore.SqlServer` para el proveedor de SQL Server
  - `Microsoft.EntityFrameworkCore.Tools` para herramientas de migración de EF Core

- **Application**:
  - `AutoMapper` para mapeo entre entidades y DTOs
  - `Microsoft.EntityFrameworkCore` para Entity Framework Core

- **Infrastructure**:
  - `MongoDB.Driver` para operaciones con MongoDB
  - `Microsoft.Extensions.DependencyInjection.Abstractions` para inyección de dependencias
  - `Microsoft.EntityFrameworkCore.SqlServer` para la conexión con SQL Server
  - `Microsoft.EntityFrameworkCore.Design` para herramientas de diseño de EF Core

### 3. Configuración de Cadenas de Conexión

La cadena de conexión se configura en `appsettings.json`:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb+srv://<username>:<password>@<cluster-address>/test?retryWrites=true&w=majority",
    "DatabaseName": "DDDExampleDB"
  },
  "ConnectionStrings": {
    "SqlServer": "Server=<server-name>;Database=<database-name>;User Id=<user>;Password=<password>;TrustServerCertificate=True;"
  }
}
```

### 4. Configuración de SQL Server con Entity Framework Core

1. **Configuración del DbContext**

Crea una clase que herede de `DbContext` en el proyecto de Infraestructura:

```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Agrega tus DbSet para cada entidad
    // public DbSet<MiEntidad> MiEntidad { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configuraciones de las entidades
        base.OnModelCreating(modelBuilder);
    }
}
```

2. **Configuración en Program.cs**

Agrega el servicio de Entity Framework en el método `ConfigureServices` de `Startup.cs` o directamente en `Program.cs`:

```csharp
// En Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
```

3. **Migraciones**

Para crear y aplicar migraciones, ejecuta los siguientes comandos en la Consola del Administrador de Paquetes:

```bash
# Crear una migración
Add-Migration InitialCreate -Project DDDExample.Infrastructure -StartupProject DDDExample.API

# Aplicar la migración a la base de datos
Update-Database -Project DDDExample.Infrastructure -StartupProject DDDExample.API
```

### 5. Patrones y Buenas Prácticas

- **Repository Pattern**: Aislamos el acceso a datos mediante repositorios
- **Dependency Injection**: Todas las dependencias se inyectan en los constructores
- **DTOs**: Separación clara entre entidades de dominio y objetos de transferencia
- **Async/Await**: Operaciones asíncronas para mejor rendimiento
- **Inmutabilidad**: Las entidades de dominio son inmutables una vez creadas

### 6. Ejecutar la Aplicación

```bash
dotnet run
```
