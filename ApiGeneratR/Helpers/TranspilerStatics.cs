namespace ApiGeneratR;

public static class TranspilerStatics
{
    public const string EmitApiMethodSignature = "public static void EmitApi(string path)";
    public const string EmitApiMethodCall = "EmitApi(path);";
    public const string EmitDtoMethodSignature = "public static void EmitDto(string path)";
    public const string EmitDtoMethodCall = "EmitDto(path);";
    public const string PartialBaseDtoMethodSignature = "public static partial void EmitDto(string path);";
    public const string PartialBaseApiMethodSignature = "public static partial void EmitApi(string path);";
    
    public const string TranspilerNamespace = "ApiGeneratR.Transpiler";
    public const string TranspilerClassSignature  = "public static partial class TranspilerEmitter";
}