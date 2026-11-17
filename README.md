# Forgekeeper

Your production stations look after themselves. Charcoal kilns, smelters, blast furnaces, eitr refineries, spinning wheels, windmills and every other station that turns one item into another take what they need from the chests next to them. What they make goes back into those chests.

Client-side: install it on your own game. It looks after the stations near you.

## What it does

Every few seconds, for each built station near you:

- **Output.** Finished goods (ingots, coal, eitr, linen, flour...) go into the nearby chests instead of piling up or dropping on the ground. A chest that already holds that item gets it first.
- **Fuel.** Coal, wood or soft tissue from the chests tops the station up.
- **Input.** Ore, scrap, wood, sap, flax, barley and the rest fill the station's queue.

## Done safely

- Items go into a station the same way as when a player puts them in, so the station really gets them.
- In multiplayer, the game lets only one player's game change a given station or chest at a time. Forgekeeper only touches stations and chests that your game runs, so items are never lost or doubled. A chest another player has open is left alone.
- It never uses private chests that aren't yours or chests inside someone else's ward.
- Carts, ships and animal saddlebags are never used.
- The kiln never burns fine wood, core wood and similar woods unless you allow it in **NeverFeed**.

## Settings (`BepInEx/config/felih.Forgekeeper.cfg`)

| Setting | Default | What it does |
|---|---|---|
| Enabled | true | Turns the mod on or off |
| StationRange | 40 | Stations within this many metres of you are looked after |
| ChestRange | 8 | How far from a station (in metres) its chests may stand |
| Interval | 5 | Seconds between checks |
| KeepInChests | Wood:100,Stone:100,Flax:20,Barley:20,Sap:0,Softtissue:20 | The least of an item that always stays in the chests. The station only takes what is above it, so with Wood:100 the kiln only makes coal while there is more than 100 wood. Items not listed, such as ores, scrap and coal, may be used up |
| AddFuel / AddInput / StoreOutput | true | Each part on its own |
| NeverFeed | FineWood,RoundLog,ElderBark,YggdrasilWood,Blackwood | Items never put in to be turned into something else |
| SkipStations | (empty) | Stations to leave alone, for example `charcoal_kiln,windmill` |

---

# Forgekeeper (Português)

Suas estações de produção cuidam de si mesmas. Kiln de carvão, fornalha, alto-forno, refinaria de eitr, roda de fiar, moinho e qualquer outra estação que transforma um item em outro pegam o que precisam dos baús ao lado. O que elas produzem volta para esses baús.

Client-side: instale no seu jogo. O mod cuida das estações perto de você.

## O que faz

A cada poucos segundos, para cada estação construída perto de você:

- **Produto.** Lingotes, carvão, eitr, linho, farinha e afins vão para os baús em vez de se acumular ou cair no chão. O baú que já tem aquele item recebe primeiro.
- **Combustível.** Carvão, madeira ou tecido macio dos baús completam a estação.
- **Entrada.** Minério, sucata, madeira, seiva, linho, cevada e o resto enchem a fila da estação.

## Com segurança

- Os itens entram na estação do mesmo jeito que quando um jogador coloca, então ela realmente recebe.
- No multiplayer, o jogo deixa só um jogo por vez alterar cada estação ou baú. O Forgekeeper só mexe nas estações e baús que o seu jogo controla, então nada some nem duplica. Um baú aberto por outro jogador fica de fora.
- Nunca usa baús privados de outra pessoa nem baús dentro da ward de outra pessoa.
- Nunca usa carroças, navios nem alforjes de animais.
- O kiln nunca queima madeira fina, madeira de núcleo e afins, a não ser que você libere em **NeverFeed**.
