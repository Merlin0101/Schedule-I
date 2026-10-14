# Mods de Schedule I (MelonLoader + Thunderstore)

Mods para o [Schedule I](https://store.steampowered.com/app/3164500/Schedule_I/) feitos com MelonLoader,
publicados no Thunderstore.

| Mod | O que faz |
| --- | --- |
| [LucroDoDia](LucroDoDia/) | App "Lucro" no celular do jogo: entradas, investimento e lucro do dia + historico de 7 dias |
| [SmartRestockFix](SmartRestockFix/) | Complemento do SmartRestock: repoe armarios sem abri-los e lembra o produto de cada slot entre sessoes |
| [GrowFaster](GrowFaster/) | Multiplicador configuravel no tempo de crescimento das plantas |

## Pre-requisitos (no seu PC)
- [.NET SDK 8](https://dotnet.microsoft.com/download) (compila os alvos net6.0 e net472).
- Schedule I + [MelonLoader](https://github.com/LavaGang/MelonLoader/releases) **0.7.0 ou 0.7.2+** (a 0.7.1 tem bugs conhecidos com o jogo).

Nao precisa ter as DLLs do jogo para compilar: MelonLoader, Harmony, S1API e
Unity vem do NuGet so como referencia (o jogo ja tem tudo isso instalado).

- **LucroDoDia** usa a [S1API](https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/)
  e por isso uma unica DLL (`netstandard2.1`) roda em IL2CPP e Mono.
- **GrowFaster** acha os tipos do jogo por reflection e gera uma DLL por build
  (`net6.0` = IL2CPP, `net472` = Mono).

## Compilar e empacotar
```bash
./pack-thunderstore.sh              # todos os mods -> dist/*.zip
./pack-thunderstore.sh LucroDoDia   # so um mod
dotnet test LucroDoDia.Tests        # testes da logica de lucro
dotnet test SmartRestockFix.Tests   # testes da memoria de produtos
```
Os pacotes de cada mod sao definidos em `<Mod>/thunderstore/packages.tsv`.
No Windows sem bash: `dotnet build GrowFaster -c Release` e copie a DLL de
`bin/Release/net6.0/` (IL2CPP) ou `bin/Release/net472/` (Mono).

## Testar no jogo
### LucroDoDia
1. Instale a S1API Forked (pelo r2modman ou manualmente).
2. Copie `LucroDoDia.dll` para `<pasta do jogo>/Mods/` (ou importe o zip pelo r2modman).
3. Carregue um save, abra o celular e toque no app **Lucro**. Venda ou compre algo e veja os numeros mudarem.

### GrowFaster
1. Copie `GrowFaster.dll` para `<pasta do jogo>/Mods/`.
   - Versao padrao da Steam = IL2CPP -> `net6.0`.
   - Branch `alternate` (Steam > Propriedades > Betas) = Mono -> `net472`.
2. Abra o jogo e veja no console do MelonLoader:
   `Patch aplicado em ...Plant.Initialize`.
   Se aparecer um aviso "nao encontrado", o jogo renomeou algo: abra
   `Schedule I_Data/Managed/Assembly-CSharp.dll` (Mono) no dnSpy/ILSpy e
   ajuste os nomes em `GrowFaster/src/PlantPatches.cs`.
3. Ajuste `UserData/MelonPreferences.cfg` -> secao `[GrowFaster]`.

## Publicar no Thunderstore
1. Crie uma conta e um *team* em <https://thunderstore.io> (o nome do team
   vira o prefixo do pacote, ex: `SeuTeam-GrowFaster`).
2. *Upload* -> comunidade **Schedule I** -> envie cada zip de `dist/`.
3. A cada versao nova: suba `<Version>` no `.csproj`, `ModVersion` em
   `src/Core.cs` e adicione uma entrada no `thunderstore/CHANGELOG.md`.

## Criar um mod novo
Copie a pasta de um mod parecido (`LucroDoDia` se usar S1API, `GrowFaster` se
usar so Harmony), renomeie o `.csproj`/`AssemblyName`/namespace, ajuste
`thunderstore/packages.tsv` e rode `./pack-thunderstore.sh NomeDoMod`.
