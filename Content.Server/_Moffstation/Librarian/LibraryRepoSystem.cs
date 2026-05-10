using Content.Server.Database;
using Content.Shared._Moffstation.Librarian;
using Robust.Shared.Network;

namespace Content.Server._Moffstation.Librarian;

/// <summary>
/// This handles...
/// </summary>
public sealed class LibraryRepoSystem : SharedLibraryRepoSystem
{
    [Dependency] private readonly IServerDbManager _db = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LibraryRepoComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<LibraryRepoComponent> ent, ref MapInitEvent args)
    {
        var results = _db.GetLibraryRepo(true, null, false);
    }

    public PlayerBook ToPlayerBook(MoffModel.MoffLibraryEntry entry)
    {
        return new PlayerBook
        {
            Name = entry.Name,
            Description = entry.Description,
            Author = entry.Author,
            Content = entry.Content,
            Type = entry.Type,
        };
    }
}
