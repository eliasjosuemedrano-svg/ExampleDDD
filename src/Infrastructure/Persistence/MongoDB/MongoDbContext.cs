using DDDExample.Domain.Entities;
using DDDExample.Infrastructure.Persistence.MongoDB.Migrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace DDDExample.Infrastructure.Persistence.MongoDB;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoMigrationRunner>? _logger;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
        : this(settings, null)
    {
    }

    public MongoDbContext(
        IOptions<MongoDbSettings> settings,
        ILogger<MongoMigrationRunner>? logger)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
        _logger = logger;
        
        // Run migrations if logger is available (not available in some contexts like design-time)
        if (_logger != null)
        {
            var migrationRunner = new MongoMigrationRunner(_database, _logger);
            migrationRunner.RunMigrations().GetAwaiter().GetResult();
        }
    }

    public IMongoCollection<Category> Categories => _database.GetCollection<Category>("Categories");
}

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
}
