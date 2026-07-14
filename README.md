<div class="header" align="center">
<img alt="Moff Station" height="400" src="https://raw.githubusercontent.com/moff-station/moff-station-14/021b361c1c512675ca61592108ec87093d1f28b0/Resources/Textures/_Moffstation/Logo/logo.png" />
</div>

Moffstation is a fork of upstream Space Station 14 (Wizard's Den) with a focus on silly antics and custom content, all in an MRP environment.

This is not the official Space Station 14 repository.

Space Station 14 is a remake of SS13 that runs on Robust Toolbox, a homegrown engine written in C#.

## Upstream Links

<div class="header" align="center">

[Website](https://spacestation14.com/) | [Discord](https://discord.ss14.io/) | [Forum](https://forum.spacestation14.com/) | [Mastodon](https://mastodon.gamedev.place/@spacestation14) | [Patreon](https://www.patreon.com/spacestation14) | [Steam](https://store.steampowered.com/app/1255460/Space_Station_14/) | [Standalone Download](https://spacestation14.com/about/nightlies/)

</div>

## Documentation/Wiki

Moffstation's [docs site](https://moff-station.github.io/moff-docs/) has documentation on Moffstation's content, engine, game design, and more.

Upstream's [docs site](https://docs.spacestation14.io/) has documentation on SS14s content, engine, game design and more.

## Contributing

We are happy to accept contributions from anybody. Note that because Moffstation is a fork of upstream Space Station 14, we will not accept any contributions that do not follow both upstream's [contribution guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html) and our own [contribution guidelines](CONTRIBUTING.md).

## Building

1. Clone this repo:
```shell
git clone https://github.com/space-wizards/space-station-14.git
```
2. Go to the project folder and run `RUN_THIS.py` to initialize the submodules and load the engine:
```shell
cd space-station-14
python RUN_THIS.py
```
3. Compile the solution:

Build the server using `dotnet build`.

[More detailed instructions on building the project.](https://docs.spacestation14.com/en/general-development/setup.html)

## License

All code for the content repository is licensed under [MIT](https://github.com/moff-station/moff-station-14/blob/master/LICENSE.TXT) unless stated otherwise, following the conditions within [LICENSE.TXT](https://raw.githubusercontent.com/moff-station/moff-station-14/refs/heads/master/LICENSE.TXT).

Most assets are licensed under [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) unless stated otherwise. Assets have their license and the copyright in the metadata file. [Example](https://github.com/space-wizards/space-station-14/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json).

Note that some assets are licensed under the non-commercial [CC-BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/) or similar non-commercial licenses and will need to be removed if you wish to use this project commercially.
