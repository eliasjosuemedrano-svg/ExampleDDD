using System.Threading.Tasks;
using MongoDB.Driver;

namespace DDDExample.Infrastructure.Persistence.MongoDB.Migrations
{
    public abstract class MongoMigrationBase : IMongoMigration
    {
        public abstract int Version { get; }
        public abstract string Name { get; }
        
        public abstract Task Up(IMongoDatabase database);
        
        public virtual Task Down(IMongoDatabase database)
        {
            // Default implementation does nothing
            return Task.CompletedTask;
        }
    }
}
