using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ApiGeneratR.Code;

public static class SourceProductionContextExtensions
{
    public static void AddFile(this SourceProductionContext ctx, SourceCodeFile file) =>
        ctx.AddSource(file.FileName, file.Content);


    public static void AddFiles(this SourceProductionContext ctx, List<SourceCodeFile> files) =>
        files.ForEach(file => ctx.AddSource(file.FileName, file.Content));
}