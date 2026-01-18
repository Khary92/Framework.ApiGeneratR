using Framework.Contract.Repository;

namespace Framework.Example.Entities;

public class TestRepository : IRepository<User>
{
    public List<User> Users { get; set; } = new List<User>();
    public Task AddAsync(User entity)
    {
        throw new NotImplementedException();
    }

    public Task<User> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<List<User>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(User entity)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}