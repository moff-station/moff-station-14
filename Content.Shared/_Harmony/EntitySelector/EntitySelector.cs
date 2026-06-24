using Robust.Shared.Utility;

namespace Content.Shared._Harmony.EntitySelector;

[ImplicitDataDefinitionForInheritors]
public abstract partial class EntitySelector
{
    [Dependency] protected IEntityManager EntityManager = default!;

    public bool Initialized { get; private set; }

    [DataField]
    public List<EntitySelector> SubSelectors = new();
    
    // Moffstation - Start - Add bounds for prototype selectors.
    /// <summary>
    /// Optional grid-local area that an entity must be inside to match this selector.
    /// When null, this selector can match entities anywhere on the grid.
    /// </summary>
    [DataField]
    public Box2? Bounds;
    // Moffstation - End

    /// <summary>
    /// One-time initialization of an entity selector.
    /// Recursively initializes all sub-selectors.
    /// </summary>
    [MustCallBase]
    internal virtual void Initialize()
    {
        DebugTools.Assert(!Initialized, "Tried to initialize an entity selector twice.");

        IoCManager.InjectDependencies(this);

        Initialized = true;

        foreach (var subSelector in SubSelectors)
        {
            if (!subSelector.Initialized)
                subSelector.Initialize();
        }
    }

    /// <summary>
    /// Checks if the entity should get selected by the entity selector.
    /// </summary>
    [MustCallBase]
    public virtual bool Matches(EntityUid entity)
    {
        if (!Initialized)
            Initialize();

        // Moffstation - Start - Add bounds for prototype selectors.
        // If bounds were configured for this selector, require the entity to have a transform so we can
        // compare its current grid-local position against those bounds.
        if (Bounds != null)
        {
            if (!EntityManager.TryGetComponent<TransformComponent>(entity, out var transform))
                return false;

            // Map modifications iterate over entities parented to the target grid, so LocalPosition is the
            // position within that grid. Only entities inside the configured bounds are eligible to match.
            if (!Bounds.Value.Contains(transform.LocalPosition))
                return false;
        }
        // Moffstation - End

        if (SubSelectors.Count == 0)
            return true;
        
        foreach (var subSelector in SubSelectors)
        {
            if (subSelector.Matches(entity))
                return true;
        }

        return false;
    }
}