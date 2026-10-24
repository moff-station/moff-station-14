using System.Linq;
using Content.Shared._Moffstation.Chitter;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._Moffstation.Chitter;

public sealed partial class ClientChitterClientSystem : ChitterClientSystem
{
    [Dependency] private ChitterServerSystem _server = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public IEnumerable<(ChitterAvatarPrototype Proto, Texture Tex)> GetSelectableAvatars() =>
        ProtoMan.EnumeratePrototypes<ChitterAvatarPrototype>()
            .Where(proto => !proto.NotSelectable)
            .Select(proto => (proto, _sprite.Frame0(proto.Icon)));

    public Texture? GetAvatarTexture(ProtoId<ChitterAvatarPrototype> protoId) =>
        ProtoMan.Resolve(protoId, out var proto) ? _sprite.Frame0(proto.Icon) : null;

    public IEnumerable<(string Name, NetEntity Ent)> Get(Entity<ChitterClientComponent?> entity) =>
        GetClientOrNull(entity) is { } client
            ? _server.GetDiscoverableServers(client).Select(it => (Name(it), GetNetEntity(it)))
            : [];
}

public static class ChitterClientExt
{
    extension(ProtoId<ChitterAvatarPrototype> protoId)
    {
        /// Sets <paramref name="control"/>'s <see cref="TextureRect.Texture"/> to the avatar indicated by
        /// <paramref name="protoId"/>, or hides the control if the avatar texture cannot be resolved.
        public void SetToOrHide(TextureRect control, ClientChitterClientSystem chitter)
        {
            if (chitter.GetAvatarTexture(protoId) is { } texture)
            {
                control.Texture = texture;
                control.Visible = true;
            }
            else
            {
                control.Visible = false;
            }
        }
    }
}
