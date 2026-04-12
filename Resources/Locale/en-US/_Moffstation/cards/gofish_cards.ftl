gofish-card-name-reverse = gofish card
gofish-card-desc-reverse = You can't tell what is on the other side of that fish card.

gofish-card-name = { gofish-card-value-name } Card
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

gofish-card-desc = {$id ->
    [rules] { gofish-card-desc-rules }
   *[other] { gofish-card-desc-regular }
}

gofish-card-desc-regular =
    The border of this card is { $suit }.
    It belongs to the { gofish-card-group-name } group of cards!

gofish-card-suit-name = { $suit ->
    [gofishblue] Blue
    [gofishgreen] Green
    [gofishred] Red
    [gofishyellow] Yellow
   *[other] {$suit}   
}

gofish-card-group-name = { $id ->
    [carp] Carp
    [magic] Carp
    [holo] Carp
    [rainbowcarp] Carp
    [acmeco] Cigarette
    [dromedaryco] Cigarette
    [nomads] Cigarette
    [spessman] Cigarette
    [ian] Corgi
    [lisa] Corgi
    [puppy] Corgi
    [oldian] Corgi
    [appledonut] Donut
    [bungodonut] Donut
    [chocolatedonut] Donut
    [pinkdonut] Donut
    [ertengineer] ERT
    [ertleader] ERT
    [ertmedic] ERT
    [ertsecurity] ERT
    [bingus] Cat
    [exception] Cat
    [floppa] Cat
    [runtime] Cat
    [apple] Fruit
    [banana] Fruit
    [grapes] Fruit
    [orange] Fruit
    [brown] Mice
    [grey] Mice
    [real] Mice
    [white] Mice
    [deathshead] Mothroach
    [moproach] Mothroach
    [mothroach] Mothroach
    [rosy] Mothroach
    [nukieelite] Nukie
    [nukiejuggernaut] Nukie
    [nukiemedic] Nukie
    [nukieoperative] Nukie
    [drazil] Plushie
    [lizard] Plushie
    [rainbowlizard] Plushie
    [spacelizard] Plushie
    [fourteenloko] Soda
    [grape] Soda
    [smitecranberry] Soda
    [spacecola] Soda
    [bloodbag] Topical
    [bruisepack] Topical
    [gauze] Topical
    [ointment] Topical
    [clown] Troublemaker
    [mime] Troublemaker
    [passenger] Troublemaker
    [skeleton] Troublemaker
   *[other] !!Brother you should not be seeing this...!!
}

gofish-card-desc-rules = Rules of the game!
   
   Players are dealt 5 cards from a shuffled deck. Remaining cards are left face down in a deck forming the fish pond/draw pile.
   The dealer will go first, and they get to ask a single player if they currently hold a card from a specific group.
   For example, "Do you have any Mothroach Cards?"
   You can examine the card to see what group it belongs to if you are unsure based on the image.
   The asking player can only ask another player for a card from a specific group, if they hold one of those cards in their hand. They cannot ask for a Mothroach card if they do not hold any Mothroach cards.
   If the asked player has a Mothroach card, they must surrender the card to the asking player. The asking player will then get to repeat their turn.
   If the asked player does not have a card from that group, they will reply with "Go Fish!", in which the asking player must draw a card from the fish pond/draw pile, and the next person will get their turn.
   Once a player collects all four cards from a specific group, that player removes those four cards from their hand, laying them face up on the table and collecting one point.
   If a player has no cards, they must immediately end their turn and draw a card from the fishpond/drawpile.
   The game repeats until there are no more cards left in the fish pond/draw pile, and all card groups have been united.
   Whoever has the most points at the end will be declared the winner!
