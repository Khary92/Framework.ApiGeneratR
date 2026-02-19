using Core.Domain.Entities;

namespace Core.Application.Ports;

public interface IUnitOfWork
{
    List<Message> Messages { get; set; }
    List<User> Users { get; set; }
    Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action, CancellationToken ct = default);
}