namespace LibrarySystem;

public class Library
{
    private readonly List<Book> _books = [];
    private readonly List<Reader> _readers = [];

    public void AddBook(Book book)
    {
        _books.Add(book);
    }

    public void RegisterReader(Reader reader)
    {
        _readers.Add(reader);
    }

    public bool IssueBook(int bookId, int readerId)
    {
        Book? book = _books.FirstOrDefault(b => b.Id == bookId);
        Reader? reader = _readers.FirstOrDefault(r => r.Id == readerId);

        if (book is null || reader is null || !book.Borrow())
        {
            return false;
        }

        reader.AddBook(book);
        return true;
    }

    public bool AcceptBook(int bookId, int readerId)
    {
        Book? book = _books.FirstOrDefault(b => b.Id == bookId);
        Reader? reader = _readers.FirstOrDefault(r => r.Id == readerId);

        if (book is null || reader is null || !reader.RemoveBook(book))
        {
            return false;
        }

        book.ReturnBook();
        return true;
    }

    public void PrintCatalog()
    {
        Console.WriteLine("=== Каталог книг ===");
        foreach (Book book in _books)
        {
            Console.WriteLine(book);
        }
    }

    public void PrintReaders()
    {
        Console.WriteLine("=== Читатели ===");
        foreach (Reader reader in _readers)
        {
            Console.WriteLine(reader);
        }
    }
}