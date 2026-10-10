gauss-fabricator-window-title = Gaussian Fabrication Unit
gauss-fabricator-window-draw-rate = Target Draw Rate:
gauss-fabricator-window-received = Receiving:
gauss-fabricator-window-progress = Output Progress:
gauss-fabricator-window-output-rate = {$rate}/min
gauss-fabricator-window-temperature = Target Temp (K)
gauss-fabricator-window-pressure = Target Pressure (kPa)
gauss-fabricator-window-no-data = N/A
gauss-fabricator-window-adjust-decrease = -{ TOSTRING($value, "F0") } kW
gauss-fabricator-window-adjust-increase = +{ TOSTRING($value, "F0") } kW
gauss-fabricator-window-on = On
gauss-fabricator-window-off = Off

gauss-fabricator-examine-draw-rate = Its draw rate is set to [color=yellow]{ POWERWATTS($rate) }[/color].
gauss-fabricator-examine-temperature = Its current temperature is { $band ->
    [Optimal] [color=green]optimal[/color]
    [Acceptable] [color=yellow]suboptimal[/color]
   *[Bad] [color=red]unsuitable[/color]
}.

gauss-fabricator-examine-pressure = Its current pressure is { $band ->
    [Optimal] [color=green]optimal[/color]
    [Acceptable] [color=yellow]suboptimal[/color]
   *[Bad] [color=red]unsuitable[/color]
}.
