using System.Threading.Tasks;
using MongoDB.Driver;

namespace DDDExample.Infrastructure.Persistence.MongoDB.Migrations
{
    public interface IMongoMigration
    {
        int Version { get; }
        string Name { get; }
        Task Up(IMongoDatabase database);
        Task Down(IMongoDatabase database);
    }
}
