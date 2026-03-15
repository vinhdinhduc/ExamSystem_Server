using System.Linq.Expressions;

namespace ExamSystem.Repositories;

public interface IRepository<T> where T : class
{
    Task<List<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);
    Task<T?> GetByIdAsync(object id, params Expression<Func<T, object>>[] includes);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync();
}
