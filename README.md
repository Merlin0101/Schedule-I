# Mods de Schedule I (MelonLoader + Thunderstore)

| Mod | O que faz |
| --- | --- |
| [GrowFaster](GrowFaster/) | Multiplicador configuravel no tempo de crescimento das plantas |

## Pre-requisitos (no seu PC)
- [.NET SDK 8](https://dotnet.microsoft.com/download) (compila os alvos net6.0 e net472).
- Schedule I + [MelonLoader](https://github.com/LavaGang/MelonLoader/releases) **0.7.0 ou 0.7.2+** (a 0.7.1 tem bugs conhecidos com o jogo).

Nao precisa ter as DLLs do jogo para compilar: os tipos do jogo sao
encontrados por reflection em tempo de execucao, e MelonLoader/Harmony vem
do NuGet so como referencia.

## Compilar e empacotar
```bash
cd schedule1-mods
./pack-thunderstore.sh            # gera dist/GrowFaster-x.y.z.zip e dist/GrowFaster_Mono-x.y.z.zip
```
No Windows sem bash: `dotnet build GrowFaster -c Release` e copie a DLL de
`bin/Release/net6.0/` (IL2CPP) ou `bin/Release/net472/` (Mono).

## Testar no jogo
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
Copie a pasta `GrowFaster`, renomeie o `.csproj`/`AssemblyName`/namespace e
rode `./pack-thunderstore.sh NomeDoMod`.
