using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared._Funkystation.Chaplain;

// <summary>
// Finds all altar styles for <see cref="AltarSwappingComponent"/>
// </summary>
public static class AltarStyles
{
    private const string AltarBasePrototype = "AltarBase";

    public static IEnumerable<EntityPrototype> GetAvailableStyles(IPrototypeManager prototypes, AltarSwappingComponent component)
    {
        foreach (var prototype in prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.Abstract)
                continue;

            if (component.Exclusions.Contains(prototype.ID))
                continue;

            if (!InheritsFromAltarBase(prototypes, prototype))
                continue;

            if (!prototype.Components.ContainsKey("Sprite"))
                continue;

            yield return prototype;
        }
    }

    public static bool IsAvailableStyle(IPrototypeManager prototypes, AltarSwappingComponent component, EntProtoId prototypeId)
    {
        // If the prototype is excluded by <see cref="AltarSwappingComponent.Exclusions"/> don't.
        if (component.Exclusions.Contains(prototypeId))
            return false;

        if (!prototypes.TryIndex(prototypeId, out EntityPrototype? prototype))
            return false;

        if (prototype.Abstract)
            return false;

        return prototypes
            .EnumerateAllParents<EntityPrototype>(prototype.ID)
            .Any(parent => parent.id == "AltarBase");
    }

    private static bool InheritsFromAltarBase(IPrototypeManager prototypes, EntityPrototype prototype)
    {
        return prototypes
            .EnumerateAllParents<EntityPrototype>(prototype.ID)
            .Any(parent => parent.id == AltarBasePrototype);
    }
}
