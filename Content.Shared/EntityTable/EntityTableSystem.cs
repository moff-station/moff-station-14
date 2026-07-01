using System.Diagnostics.CodeAnalysis;
using Content.Shared.EntityTable.EntitySelectors;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityTable;

// Moffstation - Begin - Rewrite table selectors with visitors. Early merge of https://github.com/Space-Wizards-Federation/space-station-14/pull/177
/// <summary>
/// Provides operations on <see cref="EntityTableSelector"/>s.
/// </summary>
/// <remarks>
/// Operations which "traverse" a tree of <see cref="EntityTableSelector"/>s should be implemented using an
/// <see cref="IEntityTableVisitor{TArgs,TResult}"/>. This keeps the implementation details encapsulated within the
/// visitor class where it can reuse common portions and keeps those details cleanly away from the table data's
/// definitions.
/// </remarks>
/// <seealso cref="GetSpawns(EntityTableSelector?, IRobustRandom?, EntityTableContext?)"/>
// Moffstation - End
public sealed partial class EntityTableSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IRobustRandom _random = default!;

    // Moffstation - Begin - Rewrite table selectors with visitors. Early merge of https://github.com/Space-Wizards-Federation/space-station-14/pull/177
    // public IEnumerable<EntProtoId> GetSpawns(EntityTablePrototype entTableProto, IRobustRandom? rand = null, EntityTableContext? ctx = null)
    // {
    //     // convenient
    //     return GetSpawns(entTableProto.Table, rand, ctx);
    // }
    //
    // public IEnumerable<EntProtoId> GetSpawns(EntityTableSelector? table, IRobustRandom? rand = null, EntityTableContext? ctx = null)
    // {
    //     if (table == null)
    //         return new List<EntProtoId>();
    //
    //     rand ??= _random;
    //     ctx ??= new EntityTableContext();
    //     return table.GetSpawns(rand, EntityManager, _prototypeManager, ctx);
    // }
    //
    // public IEnumerable<(EntProtoId spawn, double)> ListSpawns(EntityTablePrototype entTableProto, EntityTableContext? ctx = null)
    // {
    //     return ListSpawns(entTableProto.Table, ctx);
    // }
    //
    // /// <summary>
    // /// Builds a list of all the spawns in an EntityTable as keys, and their modified weights as values.
    // /// </summary>
    // /// <param name="table">Table we're examining</param>
    // /// <param name="ctx">Optional extra context</param>
    // public IEnumerable<(EntProtoId spawn, double)> ListSpawns(EntityTableSelector? table, EntityTableContext? ctx = null)
    // {
    //     if (table == null)
    //         return new List<(EntProtoId spawn, double)>();
    //
    //     ctx ??= new EntityTableContext();
    //     return table.ListSpawns(EntityManager, _prototypeManager, ctx);
    // }
    //
    // /// <inheritdoc cref="AverageSpawns(EntityTableSelector?,EntityTableContext?)"/>
    // public IEnumerable<(EntProtoId spawn, double)> AverageSpawns(EntityTablePrototype entTableProto, EntityTableContext? ctx = null)
    // {
    //     return AverageSpawns(entTableProto.Table, ctx);
    // }
    //
    // /// <summary>
    // /// Returns the average expected spawns of a specific entity table.
    // /// </summary>
    // /// <param name="table">The entity table we want the spawns of</param>
    // /// <param name="ctx">Optional EntityTableContext</param>
    // /// <returns></returns>
    // public IEnumerable<(EntProtoId spawn, double)> AverageSpawns(EntityTableSelector? table, EntityTableContext? ctx = null)
    // {
    //     if (table == null)
    //         return new List<(EntProtoId spawn, double)>();
    //
    //     ctx ??= new EntityTableContext();
    //     return table.AverageSpawns(EntityManager, _prototypeManager, ctx);
    // }
    // Moffstation - End
}

/// <summary>
/// Context used by selectors and conditions to evaluate in generic gamestate information.
/// </summary>
public sealed class EntityTableContext
{
    private readonly Dictionary<string, object> _data = new();

    public EntityTableContext()
    {

    }

    public EntityTableContext(Dictionary<string, object> data)
    {
        _data = data;
    }

    /// <summary>
    /// Retrieves an arbitrary piece of data from the context based on a provided key.
    /// </summary>
    /// <param name="key">A string key that corresponds to the value we are searching for. </param>
    /// <param name="value">The value we are trying to extract from the context object</param>
    /// <typeparam name="T">The type of <see cref="value"/> that we are trying to retrieve</typeparam>
    /// <returns>If <see cref="key"/> has a corresponding value of type <see cref="T"/></returns>
    [PublicAPI]
    public bool TryGetData<T>([ForbidLiteral] string key, [NotNullWhen(true)] out T? value)
    {
        value = default;
        if (!_data.TryGetValue(key, out var valueData) || valueData is not T castValueData)
            return false;

        value = castValueData;
        return true;
    }
}
