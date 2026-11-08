using System.Text.Json;
using Bogkatalog.Api.Contracts;
using Bogkatalog.Api.Models;
using Bogkatalog.Api.Validation;

namespace Bogkatalog.Api.Services;

// Forberedt hjælp til dag 7. Én API-proces ejer filen.
// Alle opslag og ændringer bruger samme lock i denne proces.
public sealed class BookStore
{
    private readonly object gate = new();
    private readonly string filePath;
    private readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private List<Book> books;
    private int nextId;

    public BookStore(IConfiguration configuration, IHostEnvironment environment,
                     ILogger<BookStore> logger)
    {
        filePath = Path.GetFullPath(configuration["BookStorage:Path"]
            ?? Path.Combine(environment.ContentRootPath, "App_Data", "books.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        if (File.Exists(filePath))
        {
            // En beskadiget fil skal give en opstartsfejl, ikke nye startdata.
            var document = JsonSerializer.Deserialize<StoredBooks>(
                File.ReadAllText(filePath), json)
                ?? throw new InvalidDataException("Bogfilen indeholder null.");
            ValidateDocument(document);
            books = document.Books.ToList();
            nextId = document.NextId;
        }
        else
        {
            books = [new(1, "Hobbitten", "J.R.R. Tolkien", 310),
                     new(2, "Frankenstein", "Mary Shelley", 280),
                     new(3, "Øvelsesbog A", "Undervisningseksempel", 100),
                     new(4, "Øvelsesbog B", "Undervisningseksempel", 110),
                     new(5, "Øvelsesbog C", "Undervisningseksempel", 120),
                     new(6, "Øvelsesbog D", "Undervisningseksempel", 130)];
            nextId = 7;
            WriteFile(books, nextId);
        }
        logger.LogInformation("Bogdata: {Path}. Indlæste {Count} bøger.", filePath, books.Count);
    }

    public Book[] GetAll()
    {
        lock (gate) return books.ToArray();
    }

    public Book? GetById(int id)
    {
        lock (gate) return books.Find(book => book.Id == id);
    }

    public Book Add(BookInput input)
    {
        lock (gate)
        {
            var book = new Book(nextId, input.Title?.Trim() ?? "",
                                input.Author?.Trim() ?? "", input.Pages);
            var changed = new List<Book>(books) { book };
            Commit(changed, checked(nextId + 1));
            return book;
        }
    }

    public Book? Update(int id, BookInput input)
    {
        lock (gate)
        {
            int index = books.FindIndex(book => book.Id == id);
            if (index < 0) return null;
            var book = new Book(id, input.Title?.Trim() ?? "",
                               input.Author?.Trim() ?? "", input.Pages);
            var changed = new List<Book>(books);
            changed[index] = book;
            Commit(changed, nextId);
            return book;
        }
    }

    public bool Delete(int id)
    {
        lock (gate)
        {
            if (!books.Any(book => book.Id == id)) return false;
            var changed = books.Where(book => book.Id != id).ToList();
            Commit(changed, nextId);
            return true;
        }
    }

    private void Commit(List<Book> changed, int newNextId)
    {
        // Ved skrivefejl bevares den tidligere liste i hukommelsen.
        WriteFile(changed, newNextId);
        books = changed;
        nextId = newNextId;
    }

    private void WriteFile(List<Book> changed, int newNextId)
    {
        // Midlertidig fil i samme mappe før erstatning af den gamle fil.
        // Dette er ikke en database eller beskyttelse mod flere API-processer.
        string temporary = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew,
                       FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, new StoredBooks(newNextId, changed.ToArray()), json);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, filePath, overwrite: true);
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static void ValidateDocument(StoredBooks document)
    {
        if (document.Books is null || document.NextId < 1
            || document.Books.Any(book => book is null || book.Id < 1
                || BookRules.Validate(new BookInput { Title = book.Title, Author = book.Author, Pages = book.Pages }).Count > 0)
            || document.Books.Select(book => book.Id).Distinct().Count() != document.Books.Length
            || document.NextId <= document.Books.Select(book => book.Id).DefaultIfEmpty(0).Max())
            throw new InvalidDataException("Bogfilens indhold er ugyldigt. Filen er ikke overskrevet.");
    }

    private sealed record StoredBooks(int NextId, Book[] Books);
}
