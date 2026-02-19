using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared.Contract.Generator.Builder;

public class MarkdownBuilder
{
    private const int IndentSize = 4;
    private readonly StringBuilder _sb = new();

    public void AddHeader(string text, int level = 1)
    {
        level = Math.Max(1, Math.Min(level, 6));
        _sb.AppendLine($"{new string('#', level)} {text}");
        _sb.AppendLine();
    }

    public void AddLine(string line = "", int indentLevel = 3)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            _sb.AppendLine();
            return;
        }

        _sb.Append(' ', indentLevel * IndentSize);
        _sb.AppendLine(line);
    }

    public void AddRawLine(string line)
    {
        _sb.AppendLine(line);
    }

    public void AddParagraph(string text)
    {
        _sb.AppendLine(text);
        _sb.AppendLine();
    }

    public void StartCodeBlock(string language = "csharp")
    {
        _sb.AppendLine($"```{language}");
    }

    public void EndCodeBlock()
    {
        _sb.AppendLine("```");
        _sb.AppendLine();
    }

    public void AddListItem(string text, int indentLevel = 0)
    {
        _sb.Append(' ', indentLevel * 2);
        _sb.AppendLine($"- {text}");
    }

    public void AddTable(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
    {
        var headerList = headers.ToList();
        _sb.AppendLine($"| {string.Join(" | ", headerList)} |");
        _sb.AppendLine($"| {string.Join(" | ", headerList.Select(_ => "---"))} |");

        foreach (var row in rows) _sb.AppendLine($"| {string.Join(" | ", row)} |");
        _sb.AppendLine();
    }

    public void AddHorizontalRule()
    {
        _sb.AppendLine("---");
        _sb.AppendLine();
    }

    public override string ToString()
    {
        return _sb.ToString();
    }
}