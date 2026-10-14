# GrowFaster

Faz as plantas do Schedule I crescerem mais rapido (ou mais devagar) com um multiplicador configuravel.

## Instalacao
- **r2modman / Thunderstore Mod Manager:** clique em *Install* - o MelonLoader vem junto como dependencia.
- **Manual:** instale o [MelonLoader](https://github.com/LavaGang/MelonLoader) 0.7.0 (ou 0.7.2+; evite 0.7.1) e copie `GrowFaster.dll` para a pasta `Mods/` do jogo.

Existem dois pacotes:
- **GrowFaster** - versao padrao da Steam (IL2CPP).
- **GrowFaster_Mono** - branch `alternate` da Steam (Mono).

## Configuracao
Depois de abrir o jogo uma vez, edite `UserData/MelonPreferences.cfg`:

```toml
[GrowFaster]
Enabled = true
GrowthMultiplier = 2.0   # 2 = 2x mais rapido, 0.5 = 2x mais devagar (0.1 a 100)
```

A mudanca vale para plantas plantadas depois de reiniciar o jogo.

## Compatibilidade
Se uma atualizacao do jogo mudar a classe de plantas, o mod apenas mostra um aviso no console do MelonLoader e fica inativo - ele nao quebra o save.
