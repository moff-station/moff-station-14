gofish-card-name-reverse = gofish card
gofish-card-desc-reverse = You can't tell what is on the other side of that fish card.

gofish-card-suit-name = { $suit ->
    [gofishblue] Blue
    [gofishgreen] Green
    [gofishred] Red
    [gofishyellow] Yellow
   *[other] Grey.
   Game Rules:
   Players are dealt 5 cards from a shuffled deck. Remaining cards are left face down in a deck forming the fish pond/draw pile.
   The dealer will go first, and they get to ask each player if they currently hold a card from a specific group.
   For example, "Do you have any Mothroach Cards?"
   You can examine the card to see what group it belongs to if you are unsure based on the image.
   The asking player can only ask another player for a card from a specific group, if they hold one of those cards in their hand. They cannot ask for a Mothroach card if they do not hold any Mothroach cards.
   If the asked player has a Mothroach card, they must surrender the card to the asking player. The asking player will then get to repeat their turn.
   If the asked player does not have a card from that group, they will reply with "Go Fish!", in which the asking player must draw a card from the fish pond/draw pile, and the next person will get their turn.
   Once a player collects all four cards from a specific group, that player removes those four cards from their hand, laying them face up on the table and collecting one point.
   If a player has no cards, they must immediately end their turn and draw a card from the fishpond/drawpile.
   The game repeats until there are no more cards left in the fish pond/draw pile, and all card groups have been united.
   Whoever has the most points at the end will be declared the winner
   
}

gofish-card-name = { gofish-card-value-name } Card

gofish-card-desc =
    The border of this card is { $suit }.
    { gofish-card-group-name }

gofish-rules-card-desc = Rules of the game!

gofish-card-value-name = { $card ->
    [rules] Rules
    [carp] Space Carp
    [magic] Magic Carp
    [holo] Holocarp
    [rainbowcarp] Rainbow Carp
    [acmeco] AcmeCo
    [dromedaryco] DromedaryCo
    [nomads] Nomads
    [spessman] Spessman
    [ian] Ian
    [lisa] Lisa
    [puppy] Puppy Ian
    [oldian] Old Ian
    [appledonut] Apple Donut
    [bungodonut] Bungo Donut
    [chocolatedonut] Chocolate Donut
    [pinkdonut] Pink Donut
    [ertengineer] ERT Engineer
    [ertleader] ERT Leader
    [ertmedic] ERT Medic
    [ertsecurity] ERT Security
    [bingus] Bingus
    [exception] Exception
    [floppa] Floppa
    [runtime] Runtime
    [apple] Apple
    [banana] Banana
    [grapes] Grapes
    [orange] Orange
    [brown] Brown Mouse
    [grey] Grey Mouse
    [real] Real Mouse
    [white] White Mouse
    [deathshead] Deathshead Mothroach
    [moproach] Moproach
    [mothroach] Regular Mothroach
    [rosy] Rosy Mothroach
    [nukieelite] Elite Nukie
    [nukiejuggernaut] Nukie Juggernaut
    [nukiemedic] Nukie Medic 
    [nukieoperative] Nukie Operative
    [drazil] Drazil Plushie
    [lizard] Lizard Plushie
    [rainbowlizard] Rainbow Lizard Plushie
    [spacelizard] Space Lizard Plushie
    [fourteenloko] Fourteen Loko
    [grape] Grape Soda
    [smitecranberry] Smite Cranberry Soda
    [spacecola] Space Cola
    [bloodbag] Blood Bag
    [bruisepack] Bruise Pack
    [gauze] Gauze
    [ointment] Ointment
    [clown] Clown
    [mime] Mime
    [passenger] Passenger
    [skeleton] Skeleton
   *[other] {$card}
}

gofish-card-group-name = { $id ->
    [carp] It belongs to the Carp group of cards!
    [magic] It belongs to the Carp group of cards!
    [holo] It belongs to the Carp group of cards!
    [rainbowcarp] It belongs to the Carp group of cards!
    [acmeco] It belongs to the Cigarette group of cards!
    [dromedaryco] It belongs to the Cigarette group of cards!
    [nomads] It belongs to the Cigarette group of cards!
    [spessman] It belongs to the Cigarette group of cards!
    [ian] It belongs to the Corgi group of cards!
    [lisa] It belongs to the Corgi group of cards!
    [puppy] It belongs to the Corgi group of cards!
    [oldian] It belongs to the Corgi group of cards!
    [appledonut] It belongs to the Donut group of cards!
    [bungodonut] It belongs to the Donut group of cards!
    [chocolatedonut] It belongs to the Donut group of cards!
    [pinkdonut] It belongs to the Donut group of cards!
    [ertengineer] It belongs to the ERT group of cards!
    [ertleader] It belongs to the ERT group of cards!
    [ertmedic] It belongs to the ERT group of cards!
    [ertsecurity] It belongs to the ERT group of cards!
    [bingus] It belongs to the Cat group of cards!
    [exception] It belongs to the Cat group of cards!
    [floppa] It belongs to the Cat group of cards!
    [runtime] It belongs to the Cat group of cards!
    [apple] It belongs to the Fruit group of cards!
    [banana] It belongs to the Fruit group of cards!
    [grapes] It belongs to the Fruit group of cards!
    [orange] It belongs to the Fruit group of cards!
    [brown] It belongs to the Mice group of cards!
    [grey] It belongs to the Mice group of cards!
    [real] It belongs to the Mice group of cards!
    [white] It belongs to the Mice group of cards!
    [deathshead] It belongs to the Mothroach group of cards!
    [moproach] It belongs to the Mothroach group of cards!
    [mothroach] It belongs to the Mothroach group of cards!
    [rosy] It belongs to the Mothroach group of cards!
    [nukieelite] It belongs to the Nukie group of cards!
    [nukiejuggernaut] It belongs to the Nukie group of cards!
    [nukiemedic] It belongs to the Nukie group of cards!
    [nukieoperative] It belongs to the Nukie group of cards!
    [drazil] It belongs to the Plushie group of cards!
    [lizard] It belongs to the Plushie group of cards!
    [rainbowlizard] It belongs to the Plushie group of cards!
    [spacelizard] It belongs to the Plushie group of cards!
    [fourteenloko] It belongs to the Soda group of cards!
    [grape] It belongs to the Soda group of cards!
    [smitecranberry] It belongs to the Soda group of cards!
    [spacecola] It belongs to the Soda group of cards!
    [bloodbag] It belongs to the Topical group of cards!
    [bruisepack] It belongs to the Topical group of cards!
    [gauze] It belongs to the Topical group of cards!
    [ointment] It belongs to the Topical group of cards!
    [clown] It belongs to the Troublemaker group of cards!
    [mime] It belongs to the Troublemaker group of cards!
    [passenger] It belongs to the Troublemaker group of cards!
    [skeleton] It belongs to the Troublemaker group of cards!
   *[other] This is the Rules card and does not belong to any group.
}

gofish-card-rules = Rules
