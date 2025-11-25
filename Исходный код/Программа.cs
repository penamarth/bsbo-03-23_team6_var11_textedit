```csharp

using System;
using System.Collections.Generic;


class Letter
{
    public char Value;
    public bool IsHighlighted = false;
    public bool HasCursor = false;
    
    public Letter(char value)
    {
        Value = value;
        Console.WriteLine($"Letter создан: {value}");
    }
}

class Word
{
    public List<Letter> Letters = new();
    public bool IsHighlighted = false;

    public Word(string text)
    {
        Console.WriteLine($"Word создан: {text}");
        foreach (var ch in text)
        {
            Letters.Add(new Letter(ch));
        }
    }

    public string GetText()
    {
        Console.WriteLine("Word.GetText вызван");
        string result = "";
        foreach (var l in Letters) result += l.Value;
        return result;
    }

    public void SetText(string newText)
    {
        Console.WriteLine($"Word.SetText вызван: {newText}");
        Letters.Clear();
        foreach (var ch in newText)
            Letters.Add(new Letter(ch));
    }
}

class Sentence
{
    public List<Word> Words = new();

    public void Add(Word w)
    {
        Console.WriteLine("Sentence.Add(Word) вызван");
        Words.Add(w);
    }

    public string GetText()
    {
        Console.WriteLine("Sentence.GetText вызван");
        string result = "";
        foreach (var w in Words)
            result += w.GetText() + " ";
        return result.Trim();
    }
}

class Paragraph
{
    public List<Sentence> Sentences = new();

    public void Add(Sentence s)
    {
        Console.WriteLine("Paragraph.Add(Sentence) вызван");
        Sentences.Add(s);
    }

    public string GetText()
    {
        Console.WriteLine("Paragraph.GetText вызван");
        string result = "";
        foreach (var s in Sentences)
            result += s.GetText() + "\n";
        return result.Trim();
    }
}

class Cursor
{
    public string CurrentElementId;

    public void MoveToNext()
    {
        Console.WriteLine("Cursor.MoveToNext вызван");
    }

    public void MoveToPrevious()
    {
        Console.WriteLine("Cursor.MoveToPrevious вызван");
    }
}

class Document
{
    public string Name;
    public List<Paragraph> Paragraphs = new();
    public Cursor Cursor = new Cursor();

    public Document(string name)
    {
        Name = name;
        Console.WriteLine($"Document создан: {name}");
    }

    public void Add(Paragraph p)
    {
        Console.WriteLine("Document.Add(Paragraph) вызван");
        Paragraphs.Add(p);
    }

    public string GetText()
    {
        Console.WriteLine("Document.GetText вызван");
        string result = "";
        foreach (var p in Paragraphs)
            result += p.GetText() + "\n";
        return result.Trim();
    }
}


class CompositeManager
{
    public void Add(Document doc, string text)
    {
        Console.WriteLine("CompositeManager.Add вызван");
        // Простейшее разбиение на Paragraph -> Sentence -> Word
        var paragraph = new Paragraph();
        var sentence = new Sentence();
        foreach (var wordText in text.Split(' '))
        {
            var word = new Word(wordText);
            sentence.Add(word);
        }
        paragraph.Add(sentence);
        doc.Add(paragraph);
    }

    public void Traverse(Document doc, Action<Word> action)
    {
        Console.WriteLine("CompositeManager.Traverse вызван");
        foreach (var p in doc.Paragraphs)
            foreach (var s in p.Sentences)
                foreach (var w in s.Words)
                    action(w);
    }

    public void ReplaceText(Document doc, string oldText, string newText)
    {
        Console.WriteLine("CompositeManager.ReplaceText вызван");
        Traverse(doc, w =>
        {
            if (w.GetText() == oldText)
                w.SetText(newText);
        });
    }
}


class HighlightToken
{
    public string ElementId;
    public string Type;
    public int Start;
    public int Length;
}

class SyntaxError
{
    public int Position;
    public string Message;
}

class SyntaxHighlighter
{
    public List<HighlightToken> FindMatches(Document doc, string pattern)
    {
        Console.WriteLine("SyntaxHighlighter.FindMatches вызван");
        return new List<HighlightToken>();
    }

    public List<SyntaxError> Validate(string content)
    {
        Console.WriteLine("SyntaxHighlighter.Validate вызван");
        return new List<SyntaxError>();
    }
}


class PrintSettings
{
    public string PrinterName;
    public int Copies;
    public string PageRange;
    public bool Duplex;
    public string Orientation;
}

class PrintPreview
{
    public void Generate(Document doc, PrintSettings settings)
    {
        Console.WriteLine("PrintPreview.Generate вызван");
    }
}

class PrintManager
{
    public void ShowPreview(Document doc, PrintSettings settings)
    {
        Console.WriteLine("PrintManager.ShowPreview вызван");
        var preview = new PrintPreview();
        preview.Generate(doc, settings);
    }

    public void Print(Document doc, PrintSettings settings)
    {
        Console.WriteLine("PrintManager.Print вызван");
    }
}


class PdfAdapter
{
    public void ExportFromDocx(string docxPath, string outputPath)
    {
        Console.WriteLine($"PdfAdapter.ExportFromDocx вызван: {outputPath}");
    }
}

class JsonAdapter
{
    public void ExportFromDocx(string docxPath, string outputPath)
    {
        Console.WriteLine($"JsonAdapter.ExportFromDocx вызван: {outputPath}");
    }
}

class ExportManager
{
    PdfAdapter pdfAdapter = new PdfAdapter();
    JsonAdapter jsonAdapter = new JsonAdapter();

    public void Export(Document doc, string format, string path)
    {
        Console.WriteLine($"ExportManager.Export вызван: {format} -> {path}");
        // получаем текст через CompositeManager
        string text = doc.GetText();

        if (format == "pdf")
            pdfAdapter.ExportFromDocx("temp.docx", path);
        else if (format == "json")
            jsonAdapter.ExportFromDocx("temp.docx", path);
        else if (format == "docx")
            Console.WriteLine("Сохраняем DOCX");
    }
}


class Editor
{
    public Document CurrentDoc;
    public CompositeManager Manager = new CompositeManager();
    public SyntaxHighlighter Highlighter = new SyntaxHighlighter();
    public PrintManager PrintManager = new PrintManager();
    public ExportManager ExportManager = new ExportManager();

    public Document NewDocument(string name)
    {
        Console.WriteLine($"Editor.NewDocument вызван: {name}");
        CurrentDoc = new Document(name);
        return CurrentDoc;
    }

    public void InsertText(string text)
    {
        Console.WriteLine($"Editor.InsertText вызван: {text}");
        Manager.Add(CurrentDoc, text);
    }

    public void ReplaceAll(string oldText, string newText)
    {
        Console.WriteLine($"Editor.ReplaceAll вызван: {oldText} -> {newText}");
        Manager.ReplaceText(CurrentDoc, oldText, newText);
    }

    public void HighlightSyntax()
    {
        Console.WriteLine("Editor.HighlightSyntax вызван");
        Highlighter.FindMatches(CurrentDoc, "pattern");
        Highlighter.Validate(CurrentDoc.GetText());
    }

    public void Print(PrintSettings settings)
    {
        Console.WriteLine("Editor.Print вызван");
        PrintManager.Print(CurrentDoc, settings);
    }

    public void ShowPreview(PrintSettings settings)
    {
        Console.WriteLine("Editor.ShowPreview вызван");
        PrintManager.ShowPreview(CurrentDoc, settings);
    }

    public void Export(string format, string path)
    {
        Console.WriteLine($"Editor.Export вызван: {format} -> {path}");
        ExportManager.Export(CurrentDoc, format, path);
    }
}


class MainApp
{
    public Editor editor = new Editor();

    public void Run()
    {
        Console.WriteLine("Main.Run вызван");

        // Создание документа
        editor.NewDocument("MyDoc");

        // Ввод текста
        editor.InsertText("Hello world!");

        // Замена текста
        editor.ReplaceAll("Hello", "Hi");

        // Подсветка синтаксиса
        editor.HighlightSyntax();

        // Экспорт
        editor.Export("docx", "mydoc.docx");
        editor.Export("pdf", "mydoc.pdf");
        editor.Export("json", "mydoc.json");

        // Печать
        var settings = new PrintSettings() { PrinterName = "Printer1", Copies = 1 };
        editor.ShowPreview(settings);
        editor.Print(settings);
    }
}


class Program
{
    static void Main(string[] args)
    {
        var app = new MainApp();
        app.Run();
    }
}


```
