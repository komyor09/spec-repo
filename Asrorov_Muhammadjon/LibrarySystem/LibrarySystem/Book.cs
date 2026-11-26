namespace LibrarySystem;

public class Book
{
    public int Id { get; }
    public string Title { get; set; }
    public string Author { get; set; }
    public bool IsAvailable { get; private set; } = true;

    public Book(int id, string title, string author)
    {
        Id = id;
        Title = title;
        Author = author;
    }

    public bool Borrow()
    {
        if (!IsAvailable)
        {
            return false;
        }

        IsAvailable = false;
        return true;
    }

    public void ReturnBook()
    {
        IsAvailable = true;
    }

    public override string ToString()
    {
        string status = IsAvailable ? "Доступна" : "Выдана";
        return $"[{Id}] \"{Title}\" — {Author} ({status})";
    }
}