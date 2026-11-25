using LibrarySystem;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Library library = new();

library.AddBook(new Book(1, "Чистый код", "Роберт Мартин"));
library.AddBook(new Book(2, "C# 12 и .NET 8", "Марк Дж. Прайс"));
library.AddBook(new Book(3, "Грокаем алгоритмы", "Адитья Бхаргава"));

library.RegisterReader(new Reader(101, "Асроров Мухаммаджон"));
library.RegisterReader(new Reader(102, "Алишер Каримов"));

library.IssueBook(1, 101);
library.IssueBook(2, 101);

library.PrintCatalog();
Console.WriteLine();
library.PrintReaders();

Console.WriteLine("\nВозвращаем книгу [1]...");
library.AcceptBook(1, 101);
Console.WriteLine();
library.PrintCatalog();