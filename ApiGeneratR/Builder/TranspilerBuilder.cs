using System;
using System.Collections.Generic;

namespace ApiGeneratR.Builder;

public class TranspilerBuilder
{
    private readonly Dictionary<string, string> _files = new();

    public void AddFile(string fileName, string code) => _files.Add(fileName, code);

    public string GetStaticSourceFile()
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
            scb.AddLine($"[\"{sourceFile.Key}\"] = \"\"\"");
            foreach (var line in sourceFile.Value.Split(["\n", "\r"], StringSplitOptions.None)) scb.AddLine(line);
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
        return scb.ToString();
    }
}