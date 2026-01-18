using Framework.Contract.Attributes;
using Framework.Contract.Mediator;

namespace Framework.Example.Commands;

[ApiDefinition("/create-user", false)]
public class CreateUserCommand(string name) : IRequest<CommandResponse>
{
    public string Name { get; set; } = name;
}