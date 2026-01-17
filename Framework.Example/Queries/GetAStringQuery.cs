using Framework.Contract.Attributes;
using Framework.Contract.Mediator;

namespace Framework.Example.Queries;

[ApiDefinition("/test", false)]
public class GetAStringQuery : IRequest<string>;
