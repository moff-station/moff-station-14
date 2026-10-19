using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client.CrewManifest.UI;

public sealed partial class CrewManifestListing : BoxContainer
{
    [Dependency] private IEntitySystemManager _entitySystem = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    private readonly SpriteSystem _spriteSystem;

    public CrewManifestListing()
    {
        IoCManager.InjectDependencies(this);
        _spriteSystem = _entitySystem.GetEntitySystem<SpriteSystem>();
    }

    public void AddCrewManifestEntries(CrewManifestEntries entries)
    {
        var entryDict = new Dictionary<DepartmentPrototype, List<CrewManifestEntry>>();

        foreach (var entry in entries.Entries)
        {
            foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
            {
                // this is a little expensive, and could be better
                if (department.Roles.Contains(entry.JobPrototype))
                {
                    entryDict.GetOrNew(department).Add(entry);
                }
            }
        }

        var entryList = new List<(DepartmentPrototype section, List<CrewManifestEntry> entries)>();

        foreach (var (section, listing) in entryDict)
        {
            entryList.Add((section, listing));
        }

        entryList.Sort((a, b) => DepartmentUIComparer.Instance.Compare(a.section, b.section));

        var firstSection = true; // Moff - improved manifest
        foreach (var item in entryList)
        {
            // Moff start - improved manifest
            if (firstSection)
                firstSection = false;
            else
                AddChild(new Control { MinSize = new Vector2(0, 12) });
            // Moff end

            AddChild(new CrewManifestSection(item.section, item.entries));
        }
    }
}
