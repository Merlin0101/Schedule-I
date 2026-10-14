# LucroDoDia

Adiciona o app **Lucro** ao celular do Schedule I para acompanhar o dinheiro do dia:

- **Entradas**: tudo que entrou hoje (vendas, dealers, lavagem...).
- **Investimento**: tudo que saiu hoje (sementes, ingredientes, equipamentos, aluguel...).
- **Lucro**: entradas menos investimento, em verde ou vermelho.
- **Saldo**: dinheiro em maos + banco.
- **Ultimos dias**: lucro de cada um dos 7 dias anteriores.

Os dados ficam salvos dentro do proprio save do jogo.

## Instalacao
- **r2modman / Thunderstore Mod Manager:** clique em *Install*. MelonLoader e S1API vem juntos como dependencias.
- **Manual:** instale MelonLoader 0.7.0 (ou 0.7.2+) e a [S1API Forked](https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/), depois copie `LucroDoDia.dll` para a pasta `Mods/` do jogo.

Funciona na versao padrao da Steam (IL2CPP) e na branch `alternate` (Mono) com a mesma DLL.

## Como a conta e feita
O app compara o saldo total (em maos + banco) a cada meio segundo: subiu, e entrada; desceu, e investimento.
Por isso, depositar ou sacar no caixa eletronico nao altera o lucro. Ja colocar dinheiro numa empresa para lavar
conta como investimento, e o valor que volta limpo conta como entrada.
