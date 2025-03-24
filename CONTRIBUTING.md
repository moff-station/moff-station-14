# Moffstation Contributing Guidelines
Thanks for contributing to Moffstation! In order to avoid any nasty merge conflicts or confusion when working on the project, please follow these guidelines.

Our guidelines are generally mirrored from [Harmony's Contribution Guidelines](https://github.com/ss14-harmony/ss14-harmony/blob/master/CONTRIBUTING.md), with some minor tweaks to fit our project.

As a base, we expect you to follow the [Space Station 14 Contribution Guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html) as well.

Importantly, do not make any changes in the GitHub web editor (webedits), as we want you to test your changes in-game before submitting a pull request. This holds true even for small YAML balance tweaks.

> [!TIP]
> It's highly recommended to set up a development environment when working on the project. A general step-by-step guide is available in the upstream docs [here](https://docs.spacestation14.com/en/general-development/setup/setting-up-a-development-environment.html). We highly recommend you use an IDE fully like [Jetbrains Rider](https://www.jetbrains.com/rider/) (which is free for non-commercial use!).

## Moffstation-exclusive Content
Space Station 14 allows separate content to be added to the game that is not part of the upstream project in a clean and easy way. Separate content is placed in a new subfolder namespace, `_Moffstation`. This is to avoid conflicts with upstream content.

This includes new prototypes, new sprites, new sounds, new maps, and so on. If you are adding new content to the game that is different from upstream, it should be placed in the `_Moffstation` namespace.

Examples:

- `Content.Server/_Moffstation/Speech/EntitySystems/IlleismAccentSystem.cs`
- `Resources/Prototypes/_Moffstation/game_presets.yml`
- `Resources/Audio/_Moffstation/Lobby/harmonilla.ogg`
- `Resources/Textures/_Moffstation/Clothing/Shoes/Misc/ducky-galoshes.rsi`
- `Resources/Locale/en-US/_Moffstation/game-ticking/game-presets/preset-deathmatchpromod.ftl`

Try to mirror the original structure of the game as much as possible when adding new content in our custom namespace. This makes it easier for others to find and understand your changes.

## Changes to upstream files
If you make changes to files that are part of the upstream project, you must comment them respectively. This is to make it easier for us to keep track of what changes we have made to the upstream project (for merge conflict resolution)

Overall, the comment should state that the code modified is a Moffstation modification, and the reason why.

If you delete any content from upstream, comment why you did so above the commented out section.

If you changed any content from upstream (for example, for balance or for disabling unwanted behavior), comment why you did so on the same line as the change.

Fluent (.ftl) files, commonly used for localization, do not support same-line comments. Always comment above the line you are changing.

If you're adding a lot of C# code to upstream files, you should put it in a [partial class](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods) in a new file in the `_Moffstation` namespace. This makes it easier to keep track of what we've changed.

### Cherry-picking upstream changes early
In order to keep changes clean, we have a special policy for cherry-picking upstream changes.

If you are cherry-picking a change that is **certain** to be merged, you do not have to follow the guidelines for changes to upstream files. Any requested changes that could be made on the upstream PR will be made and merged, and everything will flow cleanly.

> [!CAUTION]
> We highly recommend you do not cherry-pick changes that are uncertain to be merged upstream without following the guidelines for changes to upstream files. If the change is not merged upstream, it will be reverted and you will be asked to follow the guidelines for our fork.
