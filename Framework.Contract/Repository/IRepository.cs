namespace Framework.Contract.Repository;

public interface IRepository<TEntity>
{
    Task AddAsync(TEntity entity);
    Task<TEntity> GetByIdAsync(Guid id);
    Task<List<TEntity>> GetAllAsync();
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}