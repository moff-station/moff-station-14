using System.Linq;

namespace Content.Shared._MACRO.StrangeMoods;

public abstract class SharedStrangeMoodsSystem : EntitySystem
{
    public static SharedMood DeepCopy(SharedMood sharedMood) => new()
    {
        Count = sharedMood.Count,
        Dataset = sharedMood.Dataset,
        Moods = sharedMood.Moods.Select(DeepCopy).ToList(),
        UniqueId = sharedMood.UniqueId,
    };

    public static StrangeMood DeepCopy(StrangeMood strangeMood) => new()
    {
        ProtoId = strangeMood.ProtoId,
        MoodName = strangeMood.MoodName,
        MoodDesc = strangeMood.MoodDesc,
        Conflicts = strangeMood.Conflicts.ToHashSet(),
        MoodVars = strangeMood.MoodVars.ToDictionary(),
    };

    public static StrangeMoodDefinition DeepCopy(StrangeMoodDefinition strangeMoodDef) => new()
    {
        ProtoId = strangeMoodDef.ProtoId,
        SharedMoodPrototype = strangeMoodDef.SharedMoodPrototype,
        Datasets = strangeMoodDef.Datasets.ToDictionary(),
        Moods = strangeMoodDef.Moods.Select(DeepCopy).ToList(),
        MoodsChangedMessage = strangeMoodDef.MoodsChangedMessage,
        MoodsChangedSound = strangeMoodDef.MoodsChangedSound,
        MoodsChangedColor = strangeMoodDef.MoodsChangedColor,
        ActionViewMoods = strangeMoodDef.ActionViewMoods,
    };
}
