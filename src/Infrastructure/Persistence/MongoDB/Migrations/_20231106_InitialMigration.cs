using System.Threading.Tasks;
using MongoDB.Driver;

namespace DDDExample.Infrastructure.Persistence.MongoDB.Migrations
{
    public class _20231106_InitialMigration : MongoMigrationBase
    {
        public override int Version => 1;
        public override string Name => "Initial migration - Create Categories collection with indexes";

        public override async Task Up(IMongoDatabase database)
        {
            // Create Categories collection if it doesn't exist
            await database.CreateCollectionAsync("Categories");
            
            // Create indexes
            var categories = database.GetCollection<Domain.Entities.Category>("Categories");
            
            // Create index on Name field (unique)
            var nameIndex = new CreateIndexModel<Domain.Entities.Category>(
                Builders<Domain.Entities.Category>.IndexKeys.Ascending("Name"),
                new CreateIndexOptions { Unique = true, Name = "IX_Categories_Name" });
                
            // Create index on IsActive field
            var isActiveIndex = new CreateIndexModel<Domain.Entities.Category>(
                Builders<Domain.Entities.Category>.IndexKeys.Ascending("IsActive"),
                new CreateIndexOptions { Name = "IX_Categories_IsActive" });
            
            await categories.Indexes.CreateManyAsync(new[] { nameIndex, isActiveIndex });
        }
    }
}
