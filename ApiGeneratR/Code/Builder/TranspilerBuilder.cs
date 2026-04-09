using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ApiGeneratR.Mapper;

namespace ApiGeneratR.Code.Builder;

public class TranspilerBuilder
{
    public const string TranspilerNamespace = "ApiGeneratR.Transpiler";
    private const string Pattern = @"global::(?:[\w\.]+\.)([\w]+)";

    private readonly List<string> _toBeReplacedNameSpaces = [];

    public TranspilerBuilder(GlobalOptions options)
    {
        _toBeReplacedNameSpaces.Add(options.DefinitionsProject);
        _toBeReplacedNameSpaces.Add(options.HandlerProject);
        _toBeReplacedNameSpaces.AddRange(options.ClientProjects);
    }

    private readonly Dictionary<string, string> _files = new();

    public void AddFile(SourceCodeFile code) => _files.Add(code.FileName, code.Content);
    public void AddFiles(List<SourceCodeFile> code) => code.ForEach(AddFile);

    public SourceCodeFile GetStaticSourceCode()
    {
        SourceCodeBuilder scb = new();
        scb.SetUsings([
            "System",
            "System.Collections.Generic",
            "System.IO",
        ]);

        scb.SetNamespace("Api.Definitions.Generated");
        scb.StartScope("public static class GeneratedTranspiler");

        scb.StartScope("private static readonly Dictionary<string, string> _files = new()");

        foreach (var sourceFile in _files)
        {
            var cleanedContent = Regex.Replace(sourceFile.Value, Pattern, "$1");

            foreach (var toBeReplacedNameSpace in _toBeReplacedNameSpaces)
            {
                if (cleanedContent.Contains(toBeReplacedNameSpace))
                {
                    cleanedContent = cleanedContent.Replace(toBeReplacedNameSpace, TranspilerNamespace);
                }
            }

            scb.AddLine($"[\"{sourceFile.Key}\"] = \"\"\"");
            foreach (var line in cleanedContent.Split(["\n", "\r"], StringSplitOptions.None)) scb.AddLine(line);
            scb.AddLine("\"\"\",");
            scb.AddLine();
        }

        scb.EndScope(";");

        scb.StartScope("public static void EmitToPath(string path)");
        scb.AddLine("if (!Directory.Exists(path)) Directory.CreateDirectory(path);");
        scb.StartScope("foreach (var file in _files)");
        scb.AddLine("File.WriteAllText(Path.Combine(path, file.Key), file.Value);");
        scb.EndScope();
        scb.EndScope();

        scb.EndScope();
        
        return new SourceCodeFile("GeneratedTranspiler.g.cs", scb.ToString());
    }
}