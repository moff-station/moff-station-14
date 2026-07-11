# Verbs
modularhud-verb-insert-module = Insert module
modularhud-verb-insert-module-message = Inserts { THE($module) } into { THE($hud) }.
modularhud-verb-insert-module-error-fails-requirements = This { $module } {CONJUGATE-BE(module)} incompatible with { THE($hud) }.
modularhud-verb-insert-module-error-slots-full = { CAPITALIZE(THE($hud)) } {CONJUGATE-BE(hud)} full.
modularhud-verb-remove-modules = Remove modules
modularhud-verb-remove-modules-message = Removes the modules from { THE($hud) }.
modularhud-verb-remove-modules-error-missing-tool-quality = You need something capable of {$quality} to do that.
modularhud-verb-remove-modules-error-no-modules-to-remove = { CAPITALIZE(THE($hud)) } {CONJUGATE-BE(hud)} already empty.

# Examine
modularhud-examine-capacity = It has room for { $capacity ->
  [1] [bold]one[/bold] module
  *[other] [bold]{ $capacity }[/bold] modules
} in total.
modularhud-examine-no-modules = It contains no modules.
modularhud-examine-modules-header = It contains:
modularhud-examine-module-item = - [bold]{$module}[/bold]

# Module requirements
modular-hud-requirement-full-coverage = Requires eyewear which fully covers all eyes

# Labels for premade HUDs
modularhud-premade-thirst = Beer
modularhud-premade-chemistry = Chemical Analysis
modularhud-premade-command = Command
modularhud-premade-food = Chef
modularhud-premade-diagnostic = Diagnostic
modularhud-premade-medchem = "Valkyrie" MedChem
modularhud-premade-medical = Medical
modularhud-premade-medsec = MedSec
modularhud-premade-noir = Noir-tech
modularhud-premade-security = Security
