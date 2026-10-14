# LucroDoDia — Daily Profit phone app

Adds a **"Lucro"** app to your in-game phone that tracks your money for the day:

- **Income** — everything that came in today (sales, dealers, laundering...).
- **Dealers, live** — your share of each dealer sale counts the moment it happens, plus how much cash is waiting to be collected. Collecting it is just a transfer, so it's never counted twice.
- **Investment** — everything that went out today (seeds, ingredients, equipment, rent...).
- **Profit** — income minus investment, in green or red.
- **Per in-game hour** — how much the day is earning per hour actually played.
- **Daily goal** — progress bar plus an in-game notification when you hit it.
- **Balance** — cash and bank, shown separately.
- **Chart** — profit for today and the previous 6 days (green = profit, red = loss).
- **Dealer ranking** — who sold the most today and how much cash each dealer is holding for you, so you know who to visit first.

History is stored inside your save file. One DLL works on both the default Steam
build (IL2CPP) and the `alternate` branch (Mono). The app's text is in Portuguese (pt-BR).

## Installation
- **r2modman / Thunderstore Mod Manager:** click *Install*; MelonLoader and S1API are installed as dependencies.
- **Manual:** install MelonLoader 0.7.0 (or 0.7.2+, avoid 0.7.1) and [S1API Forked](https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/), then copy `LucroDoDia.dll` into the game's `Mods/` folder.

## Configuration
`UserData/MelonPreferences.cfg` (in r2modman: *Config editor* → MelonPreferences):

```toml
[LucroDoDia]
MetaDiaria = 10000.0     # daily profit goal in $; 0 disables it
NotificarMeta = true     # in-game notification when the goal is reached
```

## How it's calculated
The app samples your total money (cash + bank + uncollected dealer cash) twice per
second: up is income, down is investment. Depositing or withdrawing at the ATM doesn't change profit.
Putting cash into a business to launder counts as investment, and the clean money
that comes back counts as income, so the final profit is still correct.

---

## Português

Adiciona o app **Lucro** ao celular do jogo: entradas, investimento e lucro do dia,
vendas dos dealers ao vivo (e quanto há a coletar), lucro por hora de jogo, meta diária com notificação, saldo em mãos e no banco, e
gráfico dos últimos 7 dias e ranking dos dealers (quem mais vendeu hoje e quanto
cada um tem a coletar). Os dados ficam salvos no próprio save. Funciona em IL2CPP
e Mono com a mesma DLL.

Instalação: pelo r2modman/Thunderstore Mod Manager (as dependências vêm juntas) ou
copiando `LucroDoDia.dll` para `Mods/` com MelonLoader e S1API instalados.

Código-fonte: https://github.com/Merlin0101/Schedule-I
