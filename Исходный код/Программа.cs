```csharp


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;



public abstract class Document
{
    public string Id { get; } = Guid.NewGuid().ToString();
    protected List<Document> _children = new();

    public IReadOnlyList<Document> Children => _children;

    public virtual void Add(Document child)
    {
        _children.Add(child);
        Log(nameof(Add));
    }

    public virtual void Remove(Document child)
    {
        _children.Remove(child);
        Log(nameof(Remove));
    }

    public virtual IEnumerable<Document> GetChildren() => _children;

    public abstract string GetText();

    protected void Log(string method) =>
        Console.WriteLine($"Выполнен метод {method} класса {GetType().Name}");
}



public class Letter : Document
{
    public char Value { get; }
    public bool IsHighlighted { get; set; }
    public bool HasCursor { get; set; }

    public Letter(char value)
    {
        Value = value;
        Log(nameof(Letter));
    }

    public override string GetText()
    {
        Log(nameof(GetText));
        return Value.ToString();
    }
}



public class Word : Document
{
    public bool IsHighlighted { get; set; }

    public Word(string text)
    {
        SetText(text);
        Log(nameof(Word));
    }

    public void SetText(string text)
    {
        _children.Clear();
        foreach (var c in text)
            Add(new Letter(c));
        Log(nameof(SetText));
    }

    public override string GetText()
    {
        Log(nameof(GetText));
        return string.Concat(_children.Select(c => c.GetText()));
    }
}

public class Sentence : Document
{
    public void AddWord(Word w) => Add(w);

    public override string GetText()
    {
        Log(nameof(GetText));
        return string.Join(" ", _children.Select(c => c.GetText()));
    }
}

public class Paragraph : Document
{
    public void AddSentence(Sentence s) => Add(s);

    public override string GetText()
    {
        Log(nameof(GetText));
        return string.Join(" ", _children.Select(c => c.GetText()));
    }
}


public class RootDocument : Document
{
    public string Name { get; }
    public Cursor Cursor { get; } = new();

    public RootDocument(string name)
    {
        Name = name;
        Log(nameof(RootDocument));
    }

    public void AddParagraph(Paragraph p) => Add(p);

    public override string GetText()
    {
        Log(nameof(GetText));
        return string.Join("\n\n", _children.Select(c => c.GetText()));
    }

    public void HighlightElement(string id)
    {
        foreach (var node in Traverse())
        {
            if (node.Id == id)
            {
                if (node is Word w) w.IsHighlighted = true;
                if (node is Letter l) l.IsHighlighted = true;
            }
        }
        Log(nameof(HighlightElement));
    }

    public void MoveCursorTo(string id)
    {
        Cursor.MoveTo(id);

        foreach (var node in Traverse())
            if (node is Letter l) l.HasCursor = false;

        foreach (var node in Traverse())
            if (node.Id == id && node is Letter l)
                l.HasCursor = true;

        Log(nameof(MoveCursorTo));
    }

    public IEnumerable<Document> Traverse()
    {
        foreach (var n in InternalTraverse(this))
            yield return n;
    }

    private IEnumerable<Document> InternalTraverse(Document node)
    {
        yield return node;
        foreach (var c in node.GetChildren())
            foreach (var x in InternalTraverse(c))
                yield return x;
    }
}


public class Cursor
{
    public string CurrentElementId { get; private set; }

    public void MoveTo(string id)
    {
        CurrentElementId = id;
        Console.WriteLine($"Выполнен метод MoveTo класса Cursor");
    }
}



public class CompositeManager
{
    public Paragraph CreateParagraph(string text)
    {
        var p = new Paragraph();
        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var raw in sentences)
        {
            var cleaned = raw.Trim();
            if (cleaned.Length == 0) continue;

            var s = new Sentence();
            foreach (var w in cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                s.AddWord(new Word(w));

            p.AddSentence(s);
        }

        Console.WriteLine("Выполнен метод CreateParagraph класса CompositeManager");
        return p;
    }

    public void AddText(RootDocument doc, string text)
    {
        var p = CreateParagraph(text);
        doc.AddParagraph(p);
        Console.WriteLine("Выполнен метод AddText класса CompositeManager");
    }

    public void ReplaceText(RootDocument doc, string oldText, string newText)
    {
        foreach (var node in doc.Traverse())
        {
            if (node is Word w)
            {
                var t = w.GetText();
                if (t.Contains(oldText))
                    w.SetText(t.Replace(oldText, newText));
            }
        }
        Console.WriteLine("Выполнен метод ReplaceText класса CompositeManager");
    }

    public IEnumerable<Word> Find(RootDocument doc, string substring)
    {
        foreach (var node in doc.Traverse())
        {
            if (node is Word w && w.GetText().Contains(substring))
                yield return w;
        }
        Console.WriteLine("Выполнен метод Find класса CompositeManager");
    }

    public int Count(RootDocument doc, Type t)
    {
        int count = doc.Traverse().Count(x => x.GetType() == t);
        Console.WriteLine("Выполнен метод Count класса CompositeManager");
        return count;
    }
}



public class HighlightToken
{
    public string ElementId { get; }
    public string Type { get; }
    public int Start { get; }
    public int Length { get; }

    public HighlightToken(string id, string type, int start, int length)
    {
        ElementId = id;
        Type = type;
        Start = start;
        Length = length;
    }
}

public class SyntaxError
{
    public int Position { get; }
    public string Message { get; }

    public SyntaxError(int pos, string msg)
    {
        Position = pos;
        Message = msg;
    }
}

public class SyntaxHighlighter
{
    public IEnumerable<HighlightToken> FindMatches(RootDocument doc, string pattern)
    {
        int pos = 0;
        foreach (var node in doc.Traverse())
        {
            if (node is Word w)
            {
                var text = w.GetText();
                int idx = text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                    yield return new HighlightToken(w.Id, "match", pos + idx, pattern.Length);

                pos += text.Length + 1;
            }
        }

        Console.WriteLine("Выполнен метод FindMatches класса SyntaxHighlighter");
    }

    public IEnumerable<SyntaxError> Validate(RootDocument doc)
    {
        int pos = 0;
        foreach (var node in doc.Traverse())
        {
            if (node is Word w)
            {
                if (string.IsNullOrWhiteSpace(w.GetText()))
                    yield return new SyntaxError(pos, "Empty word");

                pos += w.GetText().Length + 1;
            }
        }

        Console.WriteLine("Выполнен метод Validate класса SyntaxHighlighter");
    }
}



public interface IExportAdapter
{
    string Format { get; }
    void Export(RootDocument doc, string path);
}

public class JsonExportAdapter : IExportAdapter
{
    public string Format => "json";

    public void Export(RootDocument doc, string path)
    {
        var data = doc.Traverse().Select(n => n.GetText()).ToList();
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(path, json);
        Console.WriteLine("Выполнен метод Export класса JsonExportAdapter");
    }
}

public class PdfExportAdapter : IExportAdapter
{
    public string Format => "pdf";

    public void Export(RootDocument doc, string path)
    {
        var text = doc.GetText();
        System.IO.File.WriteAllText(path, "PDF CONTENT:\n\n" + text);
        Console.WriteLine("Выполнен метод Export класса PdfExportAdapter");
    }
}

public class ExportManager
{
    private readonly Dictionary<string, IExportAdapter> _adapters;

    public ExportManager(IEnumerable<IExportAdapter> adapters)
    {
        _adapters = adapters.ToDictionary(a => a.Format.ToLower());
        Console.WriteLine("Выполнен метод ExportManager класса ExportManager");
    }

    public void Export(RootDocument doc, string format, string path)
    {
        if (_adapters.TryGetValue(format.ToLower(), out var a))
            a.Export(doc, path);
        else
            throw new Exception("Unsupported format");

        Console.WriteLine("Выполнен метод Export класса ExportManager");
    }
}



public class PrintSettings
{
    public string PrinterName { get; }
    public int Copies { get; }
    public string PageRange { get; }  
    public bool Duplex { get; }           
    public string Orientation { get; }    

    public PrintSettings(string printerName, int copies = 1, string pageRange = "", bool duplex = false, string orientation = "Portrait")
    {
        PrinterName = printerName;
        Copies = copies;
        PageRange = pageRange;
        Duplex = duplex;
        Orientation = orientation;
    }
}



public class PrintSettings
{
    public string PrinterName { get; }
    public int Copies { get; }
    public string PageRange { get; } 
    public bool Duplex { get; }  
    public string Orientation { get; } 

    public PrintSettings(string printerName, int copies = 1, string pageRange = null, bool duplex = false, string orientation = "Portrait")
    {
        PrinterName = printerName;
        Copies = copies;
        PageRange = pageRange;
        Duplex = duplex;
        Orientation = orientation;
    }
}

public class PrintManager
{
    private const int PageSize = 1000; 

    public void ShowPreview(RootDocument doc, PrintSettings settings, string path)
    {
        var pages = SplitIntoPages(doc.GetText());
        var selectedPages = SelectPages(pages, settings.PageRange);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Preview for printer: {settings.PrinterName}");
        sb.AppendLine($"Orientation: {settings.Orientation}");
        sb.AppendLine($"Duplex: {settings.Duplex}");
        sb.AppendLine($"Copies: {settings.Copies}");
        sb.AppendLine("=== Pages Preview ===");
        foreach (var (i, content) in selectedPages.Select((p, idx) => (idx + 1, p)))
        {
            sb.AppendLine($"--- Page {i} ---");
            sb.AppendLine(content);
        }

        System.IO.File.WriteAllText(path, sb.ToString());
        Console.WriteLine($"Preview saved to {path}");
    }

    public void Print(RootDocument doc, PrintSettings settings)
    {
        var pages = SplitIntoPages(doc.GetText());
        var selectedPages = SelectPages(pages, settings.PageRange);

        for (int copy = 1; copy <= settings.Copies; copy++)
        {
            Console.WriteLine($"Printing copy {copy} to printer {settings.PrinterName}");
            Console.WriteLine($"Orientation: {settings.Orientation}, Duplex: {settings.Duplex}");
            
            int pageNum = 1;
            foreach (var content in selectedPages)
            {
                if (settings.Duplex)
                {
                    Console.WriteLine($"Printing page {pageNum} (Duplex front/back)...");
                    pageNum++;
                    Console.WriteLine($"Printing page {pageNum} (Duplex back)...");
                }
                else
                {
                    Console.WriteLine($"Printing page {pageNum}...");
                }

                pageNum++;
            }

            Console.WriteLine("Copy finished.\n");
        }
    }

    private List<string> SplitIntoPages(string text)
    {
        var pages = new List<string>();
        for (int i = 0; i < text.Length; i += PageSize)
        {
            pages.Add(text.Substring(i, Math.Min(PageSize, text.Length - i)));
        }
        return pages;
    }

    private List<string> SelectPages(List<string> pages, string pageRange)
    {
        if (string.IsNullOrEmpty(pageRange))
            return pages;

        var result = new List<string>();
        var ranges = pageRange.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var r in ranges)
        {
            if (r.Contains('-'))
            {
                var parts = r.Split('-');
                if (int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                {
                    for (int i = start; i <= end && i <= pages.Count; i++)
                        result.Add(pages[i - 1]);
                }
            }
            else
            {
                if (int.TryParse(r, out int page) && page <= pages.Count)
                    result.Add(pages[page - 1]);
            }
        }

        return result;
    }
}



public class Editor
{
    private readonly CompositeManager _manager;
    private readonly SyntaxHighlighter _highlighter;
    private readonly ExportManager _export;
    private readonly PrintManager _print;

    public RootDocument Document { get; private set; }

    public Editor(CompositeManager m, SyntaxHighlighter sh, ExportManager ex, PrintManager pm)
    {
        _manager = m;
        _highlighter = sh;
        _export = ex;
        _print = pm;
        Log(nameof(Editor));
    }

    public RootDocument NewDocument(string name)
    {
        Document = new RootDocument(name);
        Log(nameof(NewDocument));
        return Document;
    }

    public void Insert(string text)
    {
        _manager.AddText(Document, text);
        Log(nameof(Insert));
    }

    public void Replace(string oldText, string newText)
    {
        _manager.ReplaceText(Document, oldText, newText);
        Log(nameof(Replace));
    }

    public IEnumerable<HighlightToken> Highlight(string pattern)
    {
        Log(nameof(Highlight));
        return _highlighter.FindMatches(Document, pattern);
    }

    public IEnumerable<SyntaxError> Validate()
    {
        Log(nameof(Validate));
        return _highlighter.Validate(Document);
    }

    public void Export(string format, string path)
    {
        _export.Export(Document, format, path);
        Log(nameof(Export));
    }

    public void Preview(PrintSettings settings, string path)
    {
        _print.ShowPreview(Document, settings, path);
        Log(nameof(Preview));
    }

    public void Print(PrintSettings settings)
    {
        _print.Print(Document, settings);
        Log(nameof(Print));
    }

    private void Log(string m)
    {
        Console.WriteLine($"Выполнен метод {m} класса Editor");
    }
}



```
