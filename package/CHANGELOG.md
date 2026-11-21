# Changelog

## 0.1.1
- Output dropped on the ground is picked up within 6 m of the station's output instead of 2 m, so the charcoal kiln's coal that rolls away goes into the chests too. / O que cai no chão é recolhido a até 6 m da saída da estação em vez de 2 m, então o carvão do forno que rola pra longe também vai para os baús.
- A pile on the ground that doesn't fit in one chest is spread over several; what still doesn't fit stays on the ground. / Uma pilha no chão que não cabe num baú só é dividida entre vários; o que ainda não couber fica no chão.

## 0.1.0
- First release. / Primeira versão.
- Every production station near you (charcoal kiln, smelter, blast furnace, eitr refinery, spinning wheel, windmill, and any later one) takes fuel and input from the chests within 30 m, and what it makes is stored in those chests. / Toda estação de produção perto de você (kiln, fornalha, alto-forno, refinaria de eitr, roda de fiar, moinho e as que vierem depois) pega combustível e material dos baús a até 30 m, e o que ela produz é guardado nesses baús.
- KeepInChests: a least amount that always stays in the chests (Wood 100, Stone 100, Flax/Barley/Soft tissue 40 by default). Ores may be used up. / KeepInChests: quantidade mínima que sempre fica nos baús (Madeira 100, Pedra 100, Linho/Cevada/Tecido macio 40 por padrão). Minérios podem ir a zero.
- Never takes from your inventory; never uses carts, ships, saddlebags, other players' private chests or warded chests. / Nunca pega do seu inventário; nunca usa carroças, navios, alforjes, baús privados de outros nem baús em ward alheia.
- The hot tub and the frost kiln only burn fuel and are left alone unless FuelOnlyStations is turned on. Every step is checked, and anything that doesn't add up is written to the log. / A banheira e a fogueira de gelo só queimam combustível e ficam de fora, a não ser que FuelOnlyStations esteja ligado. Cada passo é conferido, e o que não bater vai para o log.
