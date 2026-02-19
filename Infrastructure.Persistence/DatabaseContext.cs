using Core.Application.Ports;
using Core.Domain.Entities;

namespace Infrastructure.Persistence;

public class DatabaseContext : IUnitOfWork
{
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public DatabaseContext()
    {
        Users.Add(new User(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "Han",
            "Han",
            "Solo"));

        Users.Add(new User(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Luke",
            "Luke",
            "Skywalker"
        ));

        Users.Add(new User(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "Leia",
            "Leia",
            "Organa"
        ));

        Users.Add(new User(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            "Padme",
            "Padme",
            "Amidala"
        ));

        Users.Add(new User(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            "Obi-Wan",
            "Obi-Wan",
            "Kenobi"
        ));
    }

    public List<Message> Messages { get; set; } = new();
    public List<User> Users { get; set; } = new();

    public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action, CancellationToken ct = default)
    {
        await Lock.WaitAsync(ct);
        try
        {
            var result = await action();

            await SaveChangesAsync(ct);

            return result;
        }
        finally
        {
            Lock.Release();
        }
    }

    private Task SaveChangesAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}