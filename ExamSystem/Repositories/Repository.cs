using System.Linq.Expressions;
using ExamSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ExamSystemDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(ExamSystemDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public async Task<List<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = DbSet.AsNoTracking();
        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.ToListAsync();
    }

    public async Task<T?> GetByIdAsync(object id, params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = DbSet;
        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(entity => EF.Property<object>(entity, "Id").Equals(id));
    }

    public Task AddAsync(T entity) => DbSet.AddAsync(entity).AsTask();

    public void Update(T entity) => DbSet.Update(entity);

    public void Remove(T entity) => DbSet.Remove(entity);

    public Task<int> SaveChangesAsync() => Context.SaveChangesAsync();
}
