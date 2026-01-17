using Framework.Contract.Attributes;
using Framework.Contract.Mediator;

namespace Framework.Example.Commands;

[ApiDefinition("/do-an-async-thing", false)]
public class DoAnAsyncThingCommand() : IRequest<CommandResponse>;