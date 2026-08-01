moff-stain-examine-main-text = It's stained with {INDEFINITE($desc)} [color={$color}]{$colorName} {$desc}[/color] { $chemCount ->
    [1] chemical.
    *[other] mixture of chemicals.
}

moff-stain-examine-recognizable = You can recognize {$recognizedString} in the stains.

moff-stain-examine-volume = The stains are { $fillLevel ->
    [exact] made of [color=white]{$current}/{$max}u[/color].
    *[other] [bold]{ -moff-stain-vague-fill-level(fillLevel: $fillLevel) }[/bold].
}

-moff-stain-vague-fill-level = { $fillLevel ->
    [full] [color=white]sopping[/color]
    [mostlyfull] [color=#DFDFDF]sopping[/color]
    [halffull] [color=#C8C8C8]heavy[/color]
    [halfempty] [color=#C8C8C8]light[/color]
    *[mostlyempty] [color=#A4A4A4]faint[/color]
    [empty] unnoticeable
}
