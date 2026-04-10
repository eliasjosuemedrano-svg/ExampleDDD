using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace DDDExample.Infrastructure.Persistence.MongoDB.Migrations
{
    public class MongoMigrationRunner
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<MongoMigrationRunner> _logger;
        private const string MigrationCollectionName = "__Migrations";

        public MongoMigrationRunner(IMongoDatabase database, ILogger<MongoMigrationRunner> logger)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunMigrations()
        {
            var migrationTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IMongoMigration).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .OrderBy(t => ((IMongoMigration)Activator.CreateInstance(t)).Version)
                .ToList();

            var appliedMigrations = await GetAppliedMigrations();
            var appliedMigrationVersions = new HashSet<int>(appliedMigrations.Select(m => m.Version));

            foreach (var migrationType in migrationTypes)
            {
                var migration = (IMongoMigration)Activator.CreateInstance(migrationType);
                
                if (!appliedMigrationVersions.Contains(migration.Version))
                {
                    _logger.LogInformation($"Applying migration {migration.Version}: {migration.Name}");
                    
                    try
                    {
                        await migration.Up(_database);
                        await LogMigration(migration);
                        _logger.LogInformation($"Successfully applied migration {migration.Version}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error applying migration {migration.Version}");
                        throw;
                    }
                }
            }
        }

        private async Task LogMigration(IMongoMigration migration)
        {
            var collection = _database.GetCollection<MigrationHistory>(MigrationCollectionName);
            var entry = new MigrationHistory
            {
                Version = migration.Version,
                Name = migration.Name,
                AppliedOn = DateTime.UtcNow
            };
            
            await collection.InsertOneAsync(entry);
        }

        private async Task<List<MigrationHistory>> GetAppliedMigrations()
        {
            var collection = _database.GetCollection<MigrationHistory>(MigrationCollectionName);
            var result = await collection.Find(_ => true).ToListAsync();
            return result;
        }
    }

    public class MigrationHistory
    {
        public int Version { get; set; }
        public required string Name { get; set; }
        public DateTime AppliedOn { get; set; }
    }
}
