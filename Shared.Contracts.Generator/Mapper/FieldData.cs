namespace Shared.Contract.Generator.Mapper;

public class FieldData(
    string name,
    string type)
{
    public string Name { get; } = name;
    public string Type { get; } = type;
}