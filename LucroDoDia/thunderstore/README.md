# LucroDoDia

Adiciona o app **Lucro** ao celular do Schedule I para acompanhar o dinheiro do dia:

- **Entradas**: tudo que entrou hoje (vendas, dealers, lavagem...).
- **Investimento**: tudo que saiu hoje (sementes, ingredientes, equipamentos, aluguel...).
- **Lucro**: entradas menos investimento, em verde ou vermelho.
- **Por hora de jogo**: quanto o dia esta rendendo por hora.
- **Meta do dia**: barra de progresso e notificacao no jogo quando voce bate a meta.
- **Saldo**: em maos e no banco, separados.
- **Grafico**: lucro de hoje e dos 6 dias anteriores (verde = lucro, vermelho = prejuizo).

## Configuracao
Em `UserData/MelonPreferences.cfg` (no r2modman: *Config editor* > MelonPreferences):

```toml
[LucroDoDia]
MetaDiaria = 10000.0     # meta de lucro por dia em $; 0 desliga
NotificarMeta = true     # notificacao no jogo ao bater a meta
```

Os dados ficam salvos dentro do proprio save do jogo.

## Instalacao
- **r2modman / Thunderstore Mod Manager:** clique em *Install*. MelonLoader e S1API vem juntos como dependencias.
- **Manual:** instale MelonLoader 0.7.0 (ou 0.7.2+) e a [S1API Forked](https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/), depois copie `LucroDoDia.dll` para a pasta `Mods/` do jogo.

Funciona na versao padrao da Steam (IL2CPP) e na branch `alternate` (Mono) com a mesma DLL.

## Como a conta e feita
O app compara o saldo total (em maos + banco) a cada meio segundo: subiu, e entrada; desceu, e investimento.
Por isso, depositar ou sacar no caixa eletronico nao altera o lucro. Ja colocar dinheiro numa empresa para lavar
conta como investimento, e o valor que volta limpo conta como entrada.
