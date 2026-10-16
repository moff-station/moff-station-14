using Content.Shared.Access.Components;
using Content.Shared.PDA;

namespace Content.Shared._Moffstation.PDA;

/// Raised on an entity with <seealso cref="PdaComponent"/> when <paramref name="Id"/> is inserted into its ID slot.
[ByRefEvent] public readonly record struct IdInsertedIntoEvent(Entity<IdCardComponent> Id);

/// Raised on an entity with <seealso cref="PdaComponent"/> when <paramref name="Id"/> is removed from its ID slot.
[ByRefEvent] public readonly record struct IdRemovedFromEvent(Entity<IdCardComponent> Id);

/// Raised on an entity with <seealso cref="IdCardComponent"/> when it is inserted into <paramref name="Pda"/>'s ID slot.
[ByRefEvent] public readonly record struct InsertedIntoPdaEvent(Entity<PdaComponent> Pda);

/// Raised on an entity with <seealso cref="IdCardComponent"/> when it is removed from <paramref name="Pda"/>'s ID slot.
[ByRefEvent] public readonly record struct RemovedFromPdaEvent(Entity<PdaComponent> Pda);
