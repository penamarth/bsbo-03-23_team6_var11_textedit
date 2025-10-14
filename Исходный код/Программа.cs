```csharp

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

// Интерфейсы
public interface IComponent
{
    int Start { get; set; }
    int Length { get; set; }
    string GetText();
    void Action(SyntaxHighlighter highlighter);
}

public interface IExportable
{
    void Export(string path);
    string GetContent();
}

// Highlight/Error
public class HighlightToken
{
    public int Start { get; set; }
    public int Length { get; set; }
    public string Type { get; set; }
    public override string ToString() => $"Token[{Type}] @({Start},{Length})";
}

public class SyntaxError
{
    public int Position { get; set; }
    public string Message { get; set; }
    public override string ToString() => $"Error @ {Position}: {Message}";
}

// Composite
public class DocumentComponent : IComponent
{
    public List<IComponent> Components { get; } = new();
    public int Start { get; set; }
    public int Length { get; set; }
    public void Add(IComponent c) => Components.Add(c);
    public void Remove(IComponent c) => Components.Remove(c);
    public string GetText() => string.Concat(Components.Select(c => c.GetText()));
    public void Action(SyntaxHighlighter highlighter)
    {
        foreach (var c in Components) c.Action(highlighter);
    }
}

public class ParagraphComponent : IComponent
{
    public List<IComponent> Components { get; } = new();
    public int Start { get; set; }
    public int Length { get; set; }
    public void Add(IComponent c) => Components.Add(c);
    public void Remove(IComponent c) => Components.Remove(c);
    public string GetText() => string.Concat(Components.Select(c => c.GetText()));
    public void Action(SyntaxHighlighter highlighter)
    {
        foreach (var c in Components) c.Action(highlighter);
    }
}

public class SentenceComponent : IComponent
{
    public List<IComponent> Components { get; } = new();
    public int Start { get; set; }
    public int Length { get; set; }
    public void Add(IComponent c) => Components.Add(c);
    public void Remove(IComponent c) => Components.Remove(c);
    public string GetText() => string.Concat(Components.Select(c => c.GetText()));
    public void Action(SyntaxHighlighter highlighter)
    {
        foreach (var c in Components) c.Action(highlighter);
    }
}

public class WordComponent : IComponent
{
    public List<IComponent> Components { get; } = new();
    public string Value => GetText();
    public int Start { get; set; }
    public int Length { get; set; }
    public void Add(IComponent c) => Components.Add(c);
    public void Remove(IComponent c) => Components.Remove(c);
    public string GetText() => string.Concat(Components.Select(c => c.GetText()));
    public void Action(SyntaxHighlighter highlighter)
    {
        highlighter.ProcessWord(this);
        foreach (var c in Components) c.Action(highlighter);
    }
}

public class LetterComponent : IComponent
{
    public char Value { get; }
    public int Start { get; set; }
    public int Length { get; set; } = 1;
    public LetterComponent(char value) { Value = value; }
    public string GetText() => Value.ToString();
    public void Action(SyntaxHighlighter highlighter)
    {
        highlighter.ProcessLetter(this);
    }
}

// DocxDocument
public class DocxDocument : IExportable
{
    private string _content = string.Empty;
    public string Content { get => _content; set => _content = value ?? string.Empty; }

    public void Export(string path)
    {
        File.WriteAllText(path, Content, Encoding.UTF8);
    }

    public string GetContent() => Content;

    public void LoadFrom(string path)
    {
        Content = File.ReadAllText(path, Encoding.UTF8);
    }
}

// Adapters
public class PdfAdapter : IExportable
{
    private readonly DocxDocument _doc;
    public PdfAdapter(DocxDocument doc) { _doc = doc; }
    public string GetContent() => $"%PDF-HEADER%\n{_doc.GetContent()}\n%PDF-FOOTER%";
    public void Export(string path)
    {
        File.WriteAllText(path, GetContent(), Encoding.UTF8);
    }
}

public class JsonAdapter : IExportable
{
    private readonly DocxDocument _doc;
    public JsonAdapter(DocxDocument doc) { _doc = doc; }
    public string GetContent()
    {
        var obj = new { format = "json", text = _doc.GetContent() };
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
    }
    public void Export(string path)
    {
        File.WriteAllText(path, GetContent(), Encoding.UTF8);
    }
}

// Document
public class Document : IExportable
{
    public string Name { get; set; }
    public string Path { get; set; }
    public DocxDocument BaseDoc { get; set; } = new();
    public DocumentComponent RootComponent { get; set; } = new();
    public bool IsModified { get; set; }

    public static Document CreateNew(string name = "Untitled")
    {
        return new Document { Name = name, Path = "", BaseDoc = new DocxDocument() };
    }

    public void Load(string path)
    {
        Path = path;
        BaseDoc.LoadFrom(path);
        BuildCompositeFromContent(BaseDoc.GetContent());
        IsModified = false;
    }

    public void Save(string path = null)
    {
        if (!string.IsNullOrEmpty(path)) Path = path;
        BaseDoc.Content = GetText();
        BaseDoc.Export(Path ?? $"{Name}.docx");
        IsModified = false;
    }

    public void Export(string path) => BaseDoc.Export(path);
    public string GetContent() => BaseDoc.GetContent();

    public string GetText() => RootComponent?.GetText() ?? BaseDoc.GetContent();
    public void SetText(string content)
    {
        BaseDoc.Content = content;
        BuildCompositeFromContent(content);
        IsModified = true;
    }

    private void BuildCompositeFromContent(string content)
    {
        RootComponent = new DocumentComponent();
        int pos = 0;
        foreach (var paragraph in content.Split(new string[] { "\n\n" }, StringSplitOptions.None))
        {
            var p = new ParagraphComponent { Start = pos };
            foreach (var sentence in paragraph.Split('.', '!', '?'))
            {
                var s = new SentenceComponent { Start = pos };
                foreach (var word in sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var w = new WordComponent { Start = pos };
                    foreach (var ch in word)
                    {
                        var l = new LetterComponent(ch) { Start = pos++ };
                        w.Add(l);
                    }
                    s.Add(w);
                }
                p.Add(s);
            }
            RootComponent.Add(p);
        }
    }
}

// SyntaxHighlighter
public class SyntaxHighlighter
{
    public List<HighlightToken> Tokens { get; } = new();
    public List<SyntaxError> Errors { get; } = new();

    public void ApplyTo(IComponent root)
    {
        Tokens.Clear();
        Errors.Clear();
        root.Action(this);
        ValidateBalancedParentheses(root);
    }

    public void ProcessWord(WordComponent word)
    {
        string w = word.GetText();
        if (string.IsNullOrWhiteSpace(w)) return;
        string type = w.All(char.IsDigit) ? "NUMBER"
                   : w.All(char.IsUpper) ? "CONSTANT"
                   : "IDENTIFIER";
        Tokens.Add(new HighlightToken { Start = word.Start, Length = word.Length, Type = type });
    }

    public void ProcessLetter(LetterComponent letter)
    {
        if (letter.Value == '@')
            Errors.Add(new SyntaxError { Position = letter.Start, Message = "Unexpected symbol '@'" });
    }

    private void ValidateBalancedParentheses(IComponent root)
    {
        var text = root.GetText();
        var stack = new Stack<int>();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '(') stack.Push(i);
            else if (text[i] == ')')
            {
                if (stack.Count == 0)
                    Errors.Add(new SyntaxError { Position = i, Message = "Unmatched ')'" });
                else stack.Pop();
            }
        }
        while (stack.Count > 0)
            Errors.Add(new SyntaxError { Position = stack.Pop(), Message = "Unmatched '(' " });
    }
}

// Печать
public class PrintSettings
{
    public string PrinterName { get; set; } = "DefaultPrinter";
    public int Copies { get; set; } = 1;
}

public class PrintPreview
{
    public string PreviewText { get; set; }
    public static PrintPreview GeneratePreview(Document doc)
    {
        var text = doc.GetText();
        return new PrintPreview { PreviewText = $"--- Print Preview ---\n{text}\n----------------------" };
    }
}

public class PrintManager
{
    public PrintPreview ShowPreview(Document doc) => PrintPreview.GeneratePreview(doc);
    public bool Print(Document doc, PrintSettings settings)
    {
        var preview = ShowPreview(doc);
        Console.WriteLine($"=== Printing on {settings.PrinterName} ===");
        Console.WriteLine(preview.PreviewText);
        Console.WriteLine("=== Done ===");
        return true;
    }
}

// DocumentManager-
public class DocumentManager
{
    private readonly List<Document> _documents = new();
    public Document ActiveDocument { get; private set; }

    public void NewDocument(string name)
    {
        var doc = Document.CreateNew(name);
        _documents.Add(doc);
        ActiveDocument = doc;
    }

    public void OpenDocument(string path)
    {
        var doc = new Document();
        doc.Load(path);
        _documents.Add(doc);
        ActiveDocument = doc;
    }

    public void CloseDocument(string name)
    {
        var doc = _documents.FirstOrDefault(d => d.Name == name);
        if (doc != null)
        {
            _documents.Remove(doc);
            if (ActiveDocument == doc)
                ActiveDocument = _documents.LastOrDefault();
        }
    }

    public void SwitchTo(string name)
    {
        var doc = _documents.FirstOrDefault(d => d.Name == name);
        if (doc != null) ActiveDocument = doc;
    }

    public IEnumerable<Document> GetAllDocuments() => _documents.AsReadOnly();
}

// Editor
public class Editor
{
    public DocumentManager DocManager { get; } = new();
    public SyntaxHighlighter Highlighter { get; } = new();
    public PrintManager PrintManager { get; } = new();

    public void NewDocument(string name) => DocManager.NewDocument(name);
    public void OpenDocument(string path) => DocManager.OpenDocument(path);
    public void SwitchDocument(string name) => DocManager.SwitchTo(name);
    public void CloseDocument(string name) => DocManager.CloseDocument(name);
    public Document Current => DocManager.ActiveDocument;

    public void InsertText(string text)
    {
        if (Current == null) return;
        var newText = Current.GetText() + text;
        Current.SetText(newText);
    }

    public void ReplaceText(string text)
    {
        if (Current == null) return;
        Current.SetText(text);
    }

    public void Save(string path = null) => Current?.Save(path);
    public void Export(string format, string path)
    {
        if (Current == null) return;
        if (format == "pdf") Current.ExportAs(new PdfAdapter(Current.BaseDoc), path);
        else if (format == "json") Current.ExportAs(new JsonAdapter(Current.BaseDoc), path);
        else Current.Export(path);
    }

    public List<HighlightToken> Highlight() { Highlighter.ApplyTo(Current.RootComponent); return Highlighter.Tokens; }
    public List<SyntaxError> Validate() { Highlighter.ApplyTo(Current.RootComponent); return Highlighter.Errors; }
    public PrintPreview Preview() => PrintManager.ShowPreview(Current);
    public bool Print(PrintSettings s) => PrintManager.Print(Current, s);
}

// UI
public class ConsoleInterface
{
    private readonly Editor _editor;
    public ConsoleInterface(Editor editor) { _editor = editor; }

    public void ShowDocs()
    {
        Console.WriteLine("=== Open Documents ===");
        foreach (var d in _editor.DocManager.GetAllDocuments())
            Console.WriteLine($" - {d.Name}{(d == _editor.Current ? " [ACTIVE]" : "")}");
        Console.WriteLine("======================");
    }

    public void ShowDocument()
    {
        var doc = _editor.Current;
        Console.WriteLine($"\n--- {doc?.Name ?? "No Active"} ---");
        Console.WriteLine(doc?.GetText());
        Console.WriteLine("-------------------\n");
    }

    public void ShowTokens(List<HighlightToken> tokens)
    {
        Console.WriteLine("=== Tokens ===");
        foreach (var t in tokens) Console.WriteLine(t);
        Console.WriteLine("==============");
    }

    public void ShowErrors(List<SyntaxError> errs)
    {
        Console.WriteLine("=== Errors ===");
        if (errs.Count == 0) Console.WriteLine("No errors");
        else foreach (var e in errs) Console.WriteLine(e);
        Console.WriteLine("==============");
    }
}

// Program
public class Program
{
    public static void Main()
    {
        var editor = new Editor();
        var ui = new ConsoleInterface(editor);

        // Создаём первый документ
        editor.NewDocument("Doc1");
        editor.InsertText("HELLO world. This is first document.");
        ui.ShowDocs();
        ui.ShowDocument();

        // Создаём второй документ
        editor.NewDocument("Doc2");
        editor.InsertText("SECOND document (demo).");
        ui.ShowDocs();
        ui.ShowDocument();

        // Переключаемся на первый
        editor.SwitchDocument("Doc1");
        ui.ShowDocs();
        ui.ShowDocument();

        // Проверяем подсветку и ошибки
        var tokens = editor.Highlight();
        var errs = editor.Validate();
        ui.ShowTokens(tokens);
        ui.ShowErrors(errs);

        // Печать
        var printSettings = new PrintSettings { PrinterName = "DemoPrinter" };
        editor.Print(printSettings);

        Console.WriteLine("\n=== END OF DEMO ===");
    }
}


```
