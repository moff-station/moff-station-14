# Moffstation Contributing Guidelines
Thanks for contributing to Moffstation! In order to avoid any nasty merge conflicts or confusion when working on the project, please follow these guidelines.

Our guidelines are generally mirrored from [Harmony's Contribution Guidelines](https://github.com/ss14-harmony/ss14-harmony/blob/master/CONTRIBUTING.md), with some minor tweaks to fit our project.

As a base, we expect you to follow the [Space Station 14 Contribution Guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html) as well.

> [!WARNING]
> Do not make any changes in the GitHub web editor (webedits), as we want you to test your changes in-game before submitting a pull request. This holds true even for small YAML balance tweaks.

> [!TIP]
> It's highly recommended to set up a development environment when working on the project. A general step-by-step guide is available in the upstream docs [here](https://docs.spacestation14.com/en/general-development/setup/setting-up-a-development-environment.html).
>
>We highly recommend you use an IDE like [Jetbrains Rider](https://www.jetbrains.com/rider/). It's free for non-commercial use, and it basically holds your hand.

## Moffstation-exclusive content
Space Station 14 allows separate content to be added to the game that is not part of the upstream project in a clean and easy way. Separate content is placed in a new subfolder namespace, `_Moffstation`. This is to avoid conflicts with upstream content.

This includes new prototypes, new sprites, new sounds, new maps, and so on. If you are adding new content to the game that is different from upstream, it should be placed in the `_Moffstation` namespace.

Examples:

- `Content.Server/_Moffstation/Speech/EntitySystems/IlleismAccentSystem.cs`
- `Resources/Prototypes/_Moffstation/game_presets.yml`
- `Resources/Audio/_Moffstation/Lobby/harmonilla.ogg`
- `Resources/Textures/_Moffstation/Clothing/Shoes/Misc/ducky-galoshes.rsi`
- `Resources/Locale/en-US/_Moffstation/game-ticking/game-presets/preset-deathmatchpromod.ftl`

Try to mirror the original file structure of the game as much as possible when adding new content in our custom namespace. This makes it easier for others to find and understand your changes.

## Changes to upstream files
If you make changes to files that are part of the upstream project, you must comment them respectively. This is to make it easier for us to keep track of what changes we have made to the upstream project (for merge conflict resolution)

Overall, the comment should state that the code modified is a Moffstation modification, and the reason why.

If you delete any content from upstream, do not delete the content! Instead, comment it out, and comment why you did so above the commented out section.

If you changed any content from upstream (for example, for balance or for disabling unwanted behavior), comment why you did so on the same line as the change.

Fluent (.ftl) files, commonly used for localization, do not support same-line comments. Always comment above the line you are changing.

If you're adding a lot of C# code to upstream files, you should put it in a [partial class](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods) in a new file in the `_Moffstation` namespace. This makes it easier to keep track of what we've changed.

## Comment formatting
When adding comments to upstream files, use the following formats.
Generally, you should state the reason why you modified the code.

For modifying single lines of YAML:
```yaml
# Moffstation - (reason)
```

For multi-line YAML changes:
```yaml
# Moffstation - Begin - (reason)
(content)
(content)
# Moffstation - End
```

Example:
```yaml
# Moffstation - Begin - Make warops admin only
#  inhand:
#  - NukeOpsDeclarationOfWar
# Moffstation - End
```


For modifying single lines of C#:
```csharp
public const int LowPressureDamage = 4; // Moffstation - Revert to original value for a better MRP experience
```

For multi-line C# changes:
```csharp
public EntityUid SpawnPlayerMob(
        EntityCoordinates coordinates,
        ProtoId<JobPrototype>? job,
        HumanoidCharacterProfile? profile,
        EntityUid? station,
        EntityUid? entity = null)
    {

[rest of file]

        if (_randomizeCharacters)
        {
            profile = HumanoidCharacterProfile.RandomWithSpecies(speciesId);
        }

        // Moffstation - Begin - Clown/Borg/Mime loadout names (Moved from lower in file, separated from ID processing)
        if (profile != null)
        {
            _humanoidSystem.LoadProfile(entity.Value, profile);
            _metaSystem.SetEntityName(entity.Value, profile.Name);

            if (profile.FlavorText != "" && _configurationManager.GetCVar(CCVars.FlavorText))
            {
                AddComp<DetailExaminableComponent>(entity.Value).Content = profile.FlavorText;
            }
        }
        // Moffstation - End - Clown/Borg/Mime loadout names

        if (loadout != null)
        {
            EquipRoleLoadout(entity.Value, loadout, roleProto!);
        }

[rest of file]

}
```
For easier organization, you can also choose to wrap large sections of Moffstation-specific upstream changes in their own `#region`. Make sure to keep the comments at the top and bottom of the region, as they are used to identify the changes when searching for them.

```csharp
#region Moffstation - (reason)
// Moffstation - Begin - (reason)
(content)
(content)
// Moffstation - End
#endregion
```

### Cherry-picking future upstream changes early
We allow cherry-picking future upstream changes (open, not yet merged PRs) early under specific circumstances.

If you are cherry-picking a change that is **certain** to be merged, you do not have to follow the guidelines for changes to upstream files.

> [!CAUTION]
> We highly recommend you do not cherry-pick changes that are uncertain to be merged upstream without following the guidelines for changes to upstream files. If the change is not merged upstream, it will have to be reverted, and you will be asked to follow the guidelines for our fork.

## Porting content from other forks
> [!CAUTION]
> Be sure that the content you are porting is licensed under the same license as Moffstation, or a compatible one. Content that comes from AGPL-licensed forks is not allowed to be ported to Moffstation, as it is not compatible with our license.

When porting content from other forks, make sure to separate ported content into its own namespace. This makes it easier to keep track of what content is ported from other forks, and what content is original to Moffstation. For example, porting content over from Sector Umbra would be placed in the `_Umbra` namespace, and porting content from Harmony would be placed in the `_Harmony` namespace.

If the ported content doesn't come in a namespace folder, create a new namespace folder with the fork name and place the content in there.

## Mapping
We're happy to accept new, unique maps for Moffstation!

We generally follow the [upstream mapping guidelines](https://docs.spacestation14.com/en/space-station-14/mapping/guidelines.html) for new maps.

New maps should be mapped in a development environment running on Moffstation, or at least a game server running Moffstation. This is to ensure that the map works with our custom content and doesn't break anything.

Remember that Moffstation-exclusive content should be placed in the `_Moffstation` namespace, as stated above.

### Modifying upstream maps
Modifying upstream maps is explicitly not allowed unless you're willing to maintain the map with upstream changes.

Maintaining an upstream map means that you will have to manually merge (map) upstream changes into the map. This is a lot of work, and we don't want to burden you with it, so we much prefer you to create new maps unique to Moffstation instead.

## Modifying art and sprites
We're happy to accept cool art and sprites for Moffstation!

We don't have any specific guidelines for new art and sprites other than:
- Does it look good?
- Does it fit the general theme of Upstream/Moffstation?

Art is voted on by the community of Moffstation, and if it is accepted, it will be added to the game.

## Before submitting a pull request
Before submitting a pull request, make sure to:
- Test your changes in a development environment running Moffstation.
  - Be sure to play around with your feature more than "if it works" as it could cause weird behavior when interacting with other features.
- Double-check your diff on GitHub to make sure you didn't accidentally include any changes you didn't mean to.
  - Similarly, make sure you're PRing to the right place, otherwise you'll accidentally include a morbillion commits and pipe bomb someone else.
- Revert any unnecessary whitespace changes in your pull request.

## HELP I ACCIDENTALLY INCLUDED ROBUSTTOOLBOX IN MY CHANGES
```commandline
git checkout upstream/master RobustToolbox
git submodule update --init --recursive
```
This will revert the changes to the RobustToolbox submodule to the upstream version. You can then commit this change and push it to your branch.

Note that `upstream` in this instance might be different depending on your git setup. If you have a different name for the upstream repository, use that instead.

Upstream in this context is referring to https://github.com/moff-station/moff-station-14/.

