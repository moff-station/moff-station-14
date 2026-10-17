using Content.Shared._Moffstation.Extensions;
using System.Linq;

// ReSharper disable once CheckNamespace // Partial addition to existing system.
namespace Content.Shared.CartridgeLoader;

public sealed partial class CartridgeLoaderSystem
{
    [Dependency] private EntityQuery<CartridgeLoaderComponent> _cartridgeLoaderQuery;

    /// Tries to return the <see cref="CartridgeLoaderComponent"/> which has loaded <paramref name="cartridge."/>
    public Entity<CartridgeLoaderComponent>? GetLoaderOrNull(EntityUid cartridge)
    {
        if (Transform(cartridge).ParentUid is not { Valid: true } parent ||
            GetAllPrograms(parent).All(loaded => loaded != cartridge))
            return null;

        return _cartridgeLoaderQuery.ResolveOrNull(parent);
    }
}
