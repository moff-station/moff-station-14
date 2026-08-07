#Moff - We are renaming them to Gorlex Marauders, with the team being referred to as a "Gorlex Marauder Strike Team"
#Most of this file is going to be rewritten
nukeops-title = Gorlex Marauder Strike Team
nukeops-description = The Gorlex Marauders have targeted the station. Try to keep them from arming and detonating the nuke by protecting the nuke disk!

nukeops-welcome =
    You are a Gorlex Marauder. Follow your commander to victory, your target is {$station}, and ensure that it is nothing but a pile of rubble. Your bosses, the Syndicate, have provided you with the tools you'll need for the task.
    Operation {$name} is a go! Death to Nanotrasen!
nukeops-briefing = Your objectives are simple. Deliver the payload and get out before the payload detonates. Begin mission.

nukeops-opsmajor = [color=crimson]Syndicate major victory![/color]
nukeops-opsminor = [color=crimson]Syndicate minor victory![/color]
nukeops-neutral = [color=yellow]Neutral outcome![/color]
nukeops-crewminor = [color=green]Crew minor victory![/color]
nukeops-crewmajor = [color=green]Crew major victory![/color]

nukeops-cond-nukeexplodedoncorrectstation = The Gorlex Marauders managed to blow up the station.
nukeops-cond-nukeexplodedonnukieoutpost = The nuclear operative outpost was destroyed by a nuclear blast!
nukeops-cond-nukeexplodedonincorrectlocation = The nuclear bomb detonated off-station.
nukeops-cond-nukeactiveinstation = The nuclear bomb was left armed on-station.
nukeops-cond-nukeactiveatcentcom = The nuclear bomb was armed and delivered to Central Command!
nukeops-cond-nukediskoncentcom = The crew escaped with the nuclear authentication disk.
nukeops-cond-nukedisknotoncentcom = The crew left the nuclear authentication disk behind.
nukeops-cond-nukiesabandoned = The Gorlex Marauders were abandoned.
nukeops-cond-allnukiesdead = All Gorlex Marauders have died.
nukeops-cond-somenukiesalive = Some Gorlex Marauders died.
nukeops-cond-allnukiesalive = No Gorlex Marauders died.

nukeops-disk-location-title = Final location of Disk:
nukeops-disk-carried-by = {" "}carried by [color=White]{$name}[/color], [color=orange]{$job}[/color], {$location} { $user ->
    [unknown] { "" }
    *[other] ([color=gray]{$user}[/color])
}

storage-hierarchy-list = { $items-left ->
  [0] { $existing-text } { $item },
  *[other] { $existing-text } { $item }, in
}

nukeops-list-start = The nuclear operatives were:
nukeops-list-name = - [color=White]{$name}[/color]
nukeops-list-name-user = - [color=White]{$name}[/color] ([color=gray]{$user}[/color])
nukeops-not-enough-ready-players = Not enough players readied up for the game! There were {$readyPlayersCount} players readied up out of {$minimumPlayers} needed. Can't start Nukeops.
nukeops-no-one-ready = No players readied up! Can't start Nukeops.

nukeops-role-commander = Commander
nukeops-role-agent = Corpsman
nukeops-role-operator = Operator
