using System.Collections.Generic;
using System.Linq.Expressions;
using DDDExample.Domain.Common;
using DDDExample.Domain.Entities;
using DDDExample.Domain.Repositories;
using DDDExample.Infrastructure.Persistence.MongoDB;
using MongoDB.Driver;

namespace DDDExample.Infrastructure.Repositories.MongoDB;

public class MongoCategoryRepository : IRepository<Category, string>
{
    private readonly MongoDbContext _context;
    private readonly IMongoCollection<Category> _collection;

    public MongoCategoryRepository(MongoDbContext context)
    {
        _context = context;
        _collection = _context.Categories;
    }

    public async Task<Category?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await _collection.Find(_ => true).ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }

    public async Task<IReadOnlyList<Category>> FindAsync(Expression<Func<Category, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _collection.Find(predicate).ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }

    public async Task<Category> AddAsync(Category entity, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Category entity, CancellationToken cancellationToken = default)
    {
        await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Category entity, CancellationToken cancellationToken = default)
    {
        await _collection.DeleteOneAsync(x => x.Id == entity.Id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Expression<Func<Category, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(predicate).AnyAsync(cancellationToken);
    }
}
