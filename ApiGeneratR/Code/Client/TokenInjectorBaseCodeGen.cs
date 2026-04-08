using System;
using ApiGeneratR.Builder;
using ApiGeneratR.Code.Builder;

namespace ApiGeneratR.Code.Client;

public static class TokenInjectorBaseCodeGen
{
    public static SourceCodeFile Create(Language targetLanguage, string projectNamespace)
    {
        switch (targetLanguage)
        {
            case Language.CSharpWeb:
                return CreateCSharpTokenInjectorBaseClass(projectNamespace + ".Generated");
            case Language.CSharpTranspiled:
                return CreateCSharpTokenInjectorBaseClass(TranspilerBuilder.TranspilerNamespace + ".Generated");
            default:
                throw new NotSupportedException(
                    $"Language {targetLanguage} is not supported for ApiContainer generation.");
        }
    }

    private static SourceCodeFile CreateCSharpTokenInjectorBaseClass(string projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetNamespace(projectNamespace);

        scb.StartScope("public class TokenInjection");
        scb.AddLine("private string _token = string.Empty;");
        scb.AddLine("protected string Token => _token;");
        scb.AddLine();
        scb.StartScope("public void SetToken(string token)");
        scb.AddLine("_token = token;");
        scb.EndScope();
        scb.EndScope();

        return new SourceCodeFile("TokenInjection.g.cs", scb.ToString());
    }
}