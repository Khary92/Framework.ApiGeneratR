using Microsoft.CodeAnalysis;

namespace ApiGeneratR.Generators.PostInitialization;

[Generator(LanguageNames.CSharp)]
public class AttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.GenerateIRequestTaggingInterface();
        context.GenerateRequestHandlerAttribute();
        context.GenerateRequestAttribute();
        context.GenerateHttpMethodEnum();
        context.GenerateApiConsumerAttribute();
        context.GenerateRequestTypeAttribute();
        context.GenerateEventAttributeAttribute();
    }
}