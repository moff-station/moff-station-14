namespace Content.Server._Moffstation.Database;

public sealed record PlayerBookRecord(
    int Id,
    string Name,
    string Description,
    string Author,
    string Content,
    string Type
);
