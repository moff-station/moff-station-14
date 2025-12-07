using System.Linq;
using System.Numerics;

namespace Content.Shared._Moffstation.Extensions;

public static class SpriteExt
{
    public static PrototypeLayerData With(
        this PrototypeLayerData layer,
        string? shader = null,
        string? texturePath = null,
        string? rsiPath = null,
        string? state = null,
        Vector2? scale = null,
        Angle? rotation = null,
        Vector2? offset = null,
        bool? visible = null,
        Color? color = null,
        HashSet<string>? mapKeys = null,
        LayerRenderingStrategy? renderingStrategy = null,
        PrototypeCopyToShaderParameters? copyToShaderParameters = null,
        bool? cycle = null,
        bool? loop = null
    ) => new()
    {
        Shader = shader ?? layer.Shader,
        TexturePath = texturePath ?? layer.TexturePath,
        RsiPath = rsiPath ?? layer.RsiPath,
        State = state ?? layer.State,
        Scale = scale ?? layer.Scale,
        Rotation = rotation ?? layer.Rotation,
        Offset = offset ?? layer.Offset,
        Visible = visible ?? layer.Visible,
        Color = color ?? layer.Color,
        MapKeys = mapKeys ?? layer.MapKeys,
        RenderingStrategy = renderingStrategy ?? layer.RenderingStrategy,
        CopyToShaderParameters = copyToShaderParameters ?? layer.CopyToShaderParameters,
        Cycle = cycle ?? layer.Cycle,
        Loop = loop ?? layer.Loop,
    };

    public static PrototypeLayerData[] With(
        this IEnumerable<PrototypeLayerData> layers,
        string? shader = null,
        string? texturePath = null,
        string? rsiPath = null,
        string? state = null,
        Vector2? scale = null,
        Angle? rotation = null,
        Vector2? offset = null,
        bool? visible = null,
        Color? color = null,
        HashSet<string>? mapKeys = null,
        LayerRenderingStrategy? renderingStrategy = null,
        PrototypeCopyToShaderParameters? copyToShaderParameters = null,
        bool? cycle = null,
        bool? loop = null
    ) => layers.Select(l => l.With(
                shader,
                texturePath,
                rsiPath,
                state,
                scale,
                rotation,
                offset,
                visible,
                color,
                mapKeys,
                renderingStrategy,
                copyToShaderParameters,
                cycle,
                loop
            )
        )
        .ToArray();

    public static PrototypeLayerData WithUnlessAlreadySpecified(
        this PrototypeLayerData layer,
        string? shader = null,
        string? texturePath = null,
        string? rsiPath = null,
        string? state = null,
        Vector2? scale = null,
        Angle? rotation = null,
        Vector2? offset = null,
        bool? visible = null,
        Color? color = null,
        HashSet<string>? mapKeys = null,
        LayerRenderingStrategy? renderingStrategy = null,
        PrototypeCopyToShaderParameters? copyToShaderParameters = null
    ) => new()
    {
        Shader = layer.Shader ?? shader,
        TexturePath = layer.TexturePath ?? texturePath,
        RsiPath = layer.RsiPath ?? rsiPath,
        State = layer.State ?? state,
        Scale = layer.Scale ?? scale,
        Rotation = layer.Rotation ?? rotation,
        Offset = layer.Offset ?? offset,
        Visible = layer.Visible ?? visible,
        Color = layer.Color ?? color,
        MapKeys = layer.MapKeys ?? mapKeys,
        RenderingStrategy = layer.RenderingStrategy ?? renderingStrategy,
        CopyToShaderParameters = layer.CopyToShaderParameters ?? copyToShaderParameters,
    };

    public static PrototypeLayerData[] WithUnlessAlreadySpecified(
        this IEnumerable<PrototypeLayerData> layers,
        string? shader = null,
        string? texturePath = null,
        string? rsiPath = null,
        string? state = null,
        Vector2? scale = null,
        Angle? rotation = null,
        Vector2? offset = null,
        bool? visible = null,
        Color? color = null,
        HashSet<string>? mapKeys = null,
        LayerRenderingStrategy? renderingStrategy = null,
        PrototypeCopyToShaderParameters? copyToShaderParameters = null
    ) => layers.Select(l => l.WithUnlessAlreadySpecified(
                shader,
                texturePath,
                rsiPath,
                state,
                scale,
                rotation,
                offset,
                visible,
                color,
                mapKeys,
                renderingStrategy,
                copyToShaderParameters
            )
        )
        .ToArray();
}
