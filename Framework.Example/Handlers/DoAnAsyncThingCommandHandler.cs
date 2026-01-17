using Framework.Contract.Mediator;
using Framework.Example.Commands;

namespace Framework.Example.Handlers;

public class DoAnAsyncThingCommandHandler : IRequestHandler<DoAnAsyncThingCommand, CommandResponse>
{
    public Task<CommandResponse> HandleAsync(DoAnAsyncThingCommand request)
    {
        return Task.FromResult(new CommandResponse(true));
    }
}