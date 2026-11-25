namespace LibrarySystem;

public class Reader
{
    public int Id { get; }
    public string FullName { get; set; }
    private readonly List<Book> _borrowedBooks = [];

    public IReadOnlyList<Book> BorrowedBooks => _borrowedBooks;

    public Reader(int id, string fullName)
    {
        Id = id;
        FullName = fullName;
    }

    public void AddBook(Book book)
    {
        _borrowedBooks.Add(book);
    }

    public bool RemoveBook(Book book)
    {
        return _borrowedBooks.Remove(book);
    }

    public override string ToString()
    {
        return $"[{Id}] {FullName} (книг на руках: {_borrowedBooks.Count})";
    }
}