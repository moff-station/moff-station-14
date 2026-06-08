using System.Linq;
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

    private List<PlayerBook> _cachedBookRepo = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LibraryRepoComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<LibraryRepoComponent> ent, ref MapInitEvent args)
    {
        RefreshRepoCache();
    }

    private void RefreshRepoCache()
    {
        _cachedBookRepo.Clear();
        _cachedBookRepo = GetBookRepo();
    }

    private List<PlayerBook> GetBookRepo()
    {
        var results = _db.GetLibraryRepo(true, null, false).GetAwaiter().GetResult();
        return results.Select(ToPlayerBook).ToList();
    }

    private static PlayerBook ToPlayerBook(MoffModel.MoffLibraryEntry entry)
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
