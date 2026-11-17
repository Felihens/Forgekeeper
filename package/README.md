# Forgekeeper

Your production stations look after themselves. Charcoal kilns, smelters, blast furnaces, eitr refineries, spinning wheels, windmills and every other station that turns one item into another take what they need from the chests next to them. What they make goes back into those chests.

Client-side: install it on your own game. It looks after the stations near you.

## What it does

Every few seconds, for each built station near you:

- **Output.** Finished goods (ingots, coal, eitr, linen, flour...) go into the nearby chests instead of piling up or dropping on the ground. A chest that already holds that item gets it first.
- **Fuel.** Coal, wood or soft tissue from the chests tops the station up.
- **Input.** Ore, scrap, wood, sap, flax, barley and the rest fill the station's queue.

The hot tub and the frost kiln only burn fuel and make nothing, so they are left alone unless you turn on **FuelOnlyStations**.

## Done safely

- Items go into a station the same way as when a player puts them in, so the station really gets them.
- In multiplayer, the game lets only one player's game change a given station or chest at a time. Forgekeeper only touches stations and chests that your game runs, so items are never lost or doubled. A chest another player has open is left alone.
- It never uses private chests that aren't yours or chests inside someone else's ward.
- Carts, ships and animal saddlebags are never used.
- The kiln never burns fine wood, core wood and similar woods unless you allow it in **NeverFeed**.
- Every step is checked right after it is done: the chests lost what was taken, the station got it, the chests got what was stored. Anything that doesn't add up is written to the BepInEx log as a PROBLEM, and what a station didn't take goes back into the chests.

## Requirements

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- Client-side. Other players and the server don't need it.

## Installation

**r2modman / Thunderstore Mod Manager:** click *Install*. To install a downloaded zip: Settings → *Import local mod*.

**Manual:**
1. Extract the zip.
2. Copy the `plugins/Forgekeeper` folder into `Valheim/BepInEx/plugins/`.
   Result: `Valheim/BepInEx/plugins/Forgekeeper/Forgekeeper.dll`.
3. Launch the game.

To update, delete the old `Forgekeeper` folder before copying the new one.

## Settings (`BepInEx/config/felih.Forgekeeper.cfg`)

Press **F1** in game (requires Configuration Manager) or edit the file.

| Setting | Default | What it does |
|---|---|---|
| Enabled | true | Turns the mod on or off |
| StationRange | 40 | Stations within this many metres of you are looked after |
| ChestRange | 30 | How far from a station (in metres) its chests may stand |
| Interval | 5 | Seconds between checks |
| KeepInChests | Wood:100,Stone:100,Flax:40,Barley:40,Sap:0,Softtissue:40 | The least of an item that always stays in the chests. The station only takes what is above it, so with Wood:100 the kiln only makes coal while there is more than 100 wood. Items not listed, such as ores, scrap and coal, may be used up |
| AddFuel / AddInput / StoreOutput | true | Each part on its own |
| NeverFeed | FineWood,RoundLog,ElderBark,YggdrasilWood,Blackwood | Items never put in to be turned into something else |
| FuelOnlyStations | false | Also keeps the hot tub (wood) and the frost kiln (ice) fueled. They make nothing, so they are left alone unless you turn this on |
| SkipStations | (empty) | Stations to leave alone, for example `charcoal_kiln,windmill` |
| DetailedLog | false | Writes every step, and why a station is left alone, to the BepInEx log |

---

# Forgekeeper (Português)

Suas estações de produção cuidam de si mesmas. Kiln de carvão, fornalha, alto-forno, refinaria de eitr, roda de fiar, moinho e qualquer outra estação que transforma um item em outro pegam o que precisam dos baús ao lado. O que elas produzem volta para esses baús.

Client-side: instale no seu jogo. O mod cuida das estações perto de você.

## O que faz

A cada poucos segundos, para cada estação construída perto de você:

- **Produto.** Lingotes, carvão, eitr, linho, farinha e afins vão para os baús em vez de se acumular ou cair no chão. O baú que já tem aquele item recebe primeiro.
- **Combustível.** Carvão, madeira ou tecido macio dos baús completam a estação.
- **Entrada.** Minério, sucata, madeira, seiva, linho, cevada e o resto enchem a fila da estação.

A banheira e a fogueira de gelo só queimam combustível e não produzem nada, então ficam de fora, a não ser que você ligue **FuelOnlyStations**.

## Com segurança

- Os itens entram na estação do mesmo jeito que quando um jogador coloca, então ela realmente recebe.
- No multiplayer, o jogo deixa só um jogo por vez alterar cada estação ou baú. O Forgekeeper só mexe nas estações e baús que o seu jogo controla, então nada some nem duplica. Um baú aberto por outro jogador fica de fora.
- Nunca usa baús privados de outra pessoa nem baús dentro da ward de outra pessoa.
- Nunca usa carroças, navios nem alforjes de animais.
- O kiln nunca queima madeira fina, madeira de núcleo e afins, a não ser que você libere em **NeverFeed**.
- Cada passo é conferido logo depois de feito: os baús perderam o que foi tirado, a estação recebeu, os baús receberam o que foi guardado. O que não bater vai para o log do BepInEx como PROBLEM, e o que a estação não aceitou volta para os baús.

## Requisitos

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- Só no cliente. Os outros jogadores e o servidor não precisam do mod.

## Instalação

**r2modman / Thunderstore Mod Manager:** clique em *Install*. Para instalar um zip baixado: Settings → *Import local mod*.

**Manual:**
1. Extraia o zip.
2. Copie a pasta `plugins/Forgekeeper` para `Valheim/BepInEx/plugins/`.
   Resultado: `Valheim/BepInEx/plugins/Forgekeeper/Forgekeeper.dll`.
3. Abra o jogo.

Para atualizar, apague a pasta `Forgekeeper` antiga antes de copiar a nova.

## Configuração

Aperte **F1** no jogo (precisa do Configuration Manager) ou edite `BepInEx/config/felih.Forgekeeper.cfg`. As opções estão na tabela em inglês acima.
