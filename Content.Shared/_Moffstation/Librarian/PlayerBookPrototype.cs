namespace Content.Shared._Moffstation.Librarian;

/// <summary>
/// This is a prototype for storing player made books in the database
/// </summary>
[Serializable]
public struct PlayerBook
{
    public string Name = default!;
    public string Description = default!;
    public string Author = default!;
    public string Content = default!;

    internal PlayerBook(string name, string description, string author, string content)
    {
        Name = name;
        Description = description;
        Author = author;
        Content = content;
    }
}
