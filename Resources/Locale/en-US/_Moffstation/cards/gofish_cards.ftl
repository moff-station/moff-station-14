gofish-card-name-reverse = gofish card
gofish-card-desc-reverse = You can't tell what is on the other side of that fish card.

gofish-card-suit-name = { $suit ->
    [gofishblue] Blue
    [gofishgreen] Green
    [gofishred] Red
    [gofishyellow] Yellow
   *[other] {$suit}
}

gofish-card-name = { gofish-card-value-name } Card

gofish-card-desc =
    The border of this card is { gofish-card-suit-name }.
    It belongs to the { gofish-card-group-name } group of cards!

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


# THIS PART IS BROKEN, I DONT KNOW HOW TO DO THIS REFERENCE SHIT IN FTL.

gofish-card-group-name = { $card ->
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

   *[other] Misc
}

gofish-card-rules = Rules