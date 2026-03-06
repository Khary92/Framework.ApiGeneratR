using Microsoft.CodeAnalysis;

namespace ApiGeneratR.CodeGen.Generators.PostInitialization;

[Generator(LanguageNames.CSharp)]
public class AttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.GenerateRequestHandlerAttribute();
        context.GenerateRequestAttribute();
        context.GenerateHttpMethodEnum();
        context.GenerateApiConsumerAttribute();
        context.GenerateRequestTypeAttribute();
        context.GenerateEventAttributeAttribute();
    }
}