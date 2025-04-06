# Personal Item Guidelines
At Moffstation, players are able to create a cosmetic personal item for their characters.
This is meant to be a fun way to express your character's personality, style, or story.
The following guidelines are in place to ensure that personal items are appropriate for the game and do not interfere with or complicate gameplay.

Personal items are limited to each character, and you can only have one personal item per character.

These goals and guidelines are similar in vain to Sector Umbra's personal items.

## Core Item Guidelines
1. **Personal items are limited in their functionality.**
   1. They cannot be weapons, or have any combat-related functionality.
      - Examples include reskinned cybersun pens, combat/survival knives, shovels, or any other item that could be used as a weapon.
   2. They cannot be tools, or have any tool-related functionality.
      - Examples include reskinned screwdrivers, wrenches, flashlights, or any other item that could be used as a tool.
   3. They cannot be protective gear, or have any protective functionality.
      - Examples include reskinned helmets, armor, or any other item that could be used as protective gear.
      - Items like masks or winter coats are allowed as long as they remain mechanically the same as the original item it's based off of.
2. **Personal items are limited in their visuals.**
   1. They cannot be visually conflicting with any job clothing.
      - People should be able to recognize you and not mistake you for other jobs or characters.
      - For example, if you can be mistaken for a security officer or command member, you should not be wearing it.
   2. They cannot mimic any emergency response (ERT) clothing.
      - This includes any clothing that is similar to the ERT's clothing, such as the ERT's armor or helmets.
   3. They cannot have any unrealistic or special effects.
      - Examples include using the ninja stealth effect (`StealthComponent`), or jitter effect.
      - Unshaded elements like flashing lights on the sprite are allowed.
3. **Personal item descriptions are limited.**
   1. Descriptions should be in-character and not break immersion.
      - For example, OOC details about a long story are not allowed. These questions should be asked to the character in-game through IC visual cues.
        - Ex. **Not allowed**: "A guitar that (charname) used to pass the time during the Corporate Wars. It was a gift from (charname)'s father, who was a famous musician."
        - Ex. **Allowed**: "A guitar that looks like it has been used for a long time. The finish is long gone, with the frets worn down and the tuners stained. A small NT insignia is carved into the body, with a signature on the back."
      - Try to always be _physically descriptive_ about the item.
   2. Descriptions should not be excessively long or a wall of text.
      - Try to make a brief, 1–2 sentence description of the item. Sometimes even one sentence about the item's appearance is good.
      - Large descriptions of the item should be put under a `DetailExaminable` component.
4. **Personal items cannot be illegal or cause security trouble.**
   - They cannot have any contraband status, like being restricted contraband or syndicate contraband.
     - The exception to this is job-related personal items, which can have contraband status if it is a job-related item that is normally contraband.
   - They cannot be interpreted as anything illegal, such as a stealth item, syndicate item, or anything that could generate a grey-area and cause security a headache.
     - This is done in mind to avoid security making hypocritical exceptions to items normally considered illegal.

### Examples
- **Allowed**: A small, black, leather notebook with a red ribbon bookmark. It is writable, or it can have papers stored inside it, similar to a folder.
- **Allowed**: A small pendant with a picture of a cat on it. It can be opened or closed using a `ToggleItem` interaction.
- **Allowed**: A keychain that has a small miniature AI core on it. The sprite flashes with activity, and it can be turned on and off using a `ToggleItem` interaction. The AI core is not inhabitable by any ghost or player.
- **Allowed**: Any custom instrument (not a Digital Audio Workstation or similar).
  - This is **disallowed** if it is a reskinned instrument that has any extra functionality or bypasses checks, like not requiring you to have it in your primary hand, unless that behavior is default.
- **Allowed**: Any custom plushie normally available in-game.
- **Allowed**: Any toy that is normally available in-game.
- **Allowed**: Mantles for your character that fit the style or color theme of your character.
  - This is **disallowed** if it looks like a higher rank or command mantle.
- **Disallowed**: A reskinned pen parented off of the Cybersun pen, which can be used as a screwdriver, does damage, has a contraband status, and can rewrite papers.
- **Disallowed**: A syndicate scarf. This is normally an illegal item.


## Job-related Item Guidelines
Players can create personal items related to their character's job. These personal items have relaxed restrictions as to what they can do.

These personal items are locked to the job they are oriented around. A character can only use the personal item if they're playing as the job it was created for.

1. **Functional personal items are restricted to what they start with roundstart, or what they can very easily get as part of their job roundstart.**
   - Functional personal items cannot be used to acquire items that are not normally available to the job at roundstart.
2. **Functional personal items cannot add any extra functionality.**
   - For example, a personal item that is a belt cannot have any extra items in it besides the items that are normally available to the job at roundstart.
   - A good way to easily enforce this is to simply parent to the item you want to mimic, and add a custom name and description. This makes it easy if we ever change item fills.

### Examples
- **Allowed**: Custom medical webbing for a Paramedic that has a standard medical belt fill.
  - This is **disallowed** if any extra items are added to the webbing not normally in a belt fill, or if the webbing can take any item that a medical belt cannot normally take.
- **Allowed**: A custom salvage mask for a Salvager.
- **Allowed**: A custom engraved gun for a Security Officer.
  - This is **disallowed** if the gun has any extra functionality, such as being able to be used as a melee weapon or having any extra items in it that are not normally available to the job at roundstart.
- **Allowed**: A custom helmet for a Security Officer.
  - This is **disallowed** if the helmet has any extra functionality, such as having a light.
- **Allowed**: A custom knife for a Chef or Security Officer
- **Allowed**: A custom worn shaker for a Bartender.
- **Allowed**: A custom nibbled mantle for a Captain who is a moth.
- **Allowed**: A custom worn cloak for a Chief Engineer who has worked for a long time.
- **Disallowed**: A custom welding mask for a Scientist. Welding masks available to Scientists are limited at roundstart (there's only one in a vending machine usually) and are not available to them easily at roundstart.
- **Disallowed**: An industrial, advanced, or experimental welding tool for an Engineer. They only have access to the standard welding tool at roundstart.
- **Disallowed**: Any cloak or mantle that signifies a higher rank (like a command rank) unless you are a command member.
  - Example: a custom cloak for a regular Engineer/Senior Engineer that looks like a prestigious command cloak or mantle.

## PR Guidelines
When creating your personal items, be sure to follow the following guidelines.

Personal items content goes in the `_Moffstation` namespace under a `PersonalItems` folder. The item should be sectioned based on the type of item. For example:
   - If the item is a pen, it should be in `_Moffstation/PersonalItems/Items/...`
   - If the item is primarily a worn cosmetic, it should be in `_Moffstation/PersonalItems/Wearables/...`

Personal item content is organized in this structure. Be sure to separate textures and the item YAML like so:

- `Resources/Textures/_Moffstation/PersonalItems/Items/(playername)/(charactername)/`
  - `ItemName.rsi` (the item RSI folder)
- `Resources/Prototypes/_Moffstation/PersonalItems/Items/(playername)/(charactername)/`
  - `ItemName.yml` (the item YAML)

For example:
- `Resources/Textures/_Moffstation/PersonalItems/Wearables/FrostWinters/Frost_Winters/`
  - `frostmedicalwebbing.rsi` (the item RSI folder)
- `Resources/Prototypes/_Moffstation/PersonalItems/Wearables/FrostWinters/Frost_Winters/`
  - `frostmedicalwebbing.yml` (the item YAML)

When writing YAML for your item, write the prototype for your item first, and then the custom loadout group for your item.

The beginning of the YAML should state the player username and the character name. For example:

```yaml
# Player FrostWinters - Character: Frost Winters

- type: entity
  parent: ClothingBeltMedicalFilled
  id: PersonalItemFrostMedicalWebbing
  name: "Winters' medical rig"
  description: "Winters' chest rig brought from home, modified to fit medical supplies. Looks oddly familiar to some with certain backgrounds."
  suffix: PersonalItem, Filled
  components:
  - type: Sprite
    sprite: _Moffstation/PersonalItems/Wearables/frostmedicalwebbing.rsi
  - type: Clothing
    sprite: _Moffstation/PersonalItems/Wearables/frostmedicalwebbing.rsi

- type: loadout
  id: PersonalItemFrostMedicalWebbing
  equipment:
    belt: PersonalItemFrostMedicalWebbing
  effects:
  - !type:PersonalItemLoadoutEffect
    character:
    - Frost Winters
```

Be sure to follow upstream guidelines when working on the project.

### Shared Personal Items
If you want to create a personal item shared between multiple characters (including characters that are not yours),
you can create a shared personal item.

If the item is shared between multiple characters which all belong to you (you play these characters),
it should be in the `_Moffstation/PersonalItems/Wearables/(playername)/Shared` folder.

If the item is shared between multiple characters, and some of those characters do not belong to you (you do not play some of these characters)
it should be in the `_Moffstation/PersonalItems/Wearables/Shared` folder.

Shared personal items count as one personal item for each character that uses it.

### Job-related Items
Personal items mimicking a job-related item should be parented to the item you want to mimic.
This is done to ensure that the item behaves like the original item, and does not have any extra functionality.

This also makes upstream item balancing trickle down to functional personal items,
so items don't have to be manually re-balanced.

## Approval
Personal items are subject to approval by any Moffstation maintainer or admin. Personal item guidelines are guidelines, not explicitly rules, so if you're unclear if your item would be allowed, please ask a maintainer or admin for clarification.
