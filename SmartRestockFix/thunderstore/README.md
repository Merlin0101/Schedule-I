# SmartRestockFix

Complemento para o **SmartRestock** (uplusion23, testado com a 1.3.2).

## O problema
O SmartRestock salva quais slots tem auto-restock ligado, mas so repoe um armario
depois que voce o abre pelo menos uma vez na sessao. Ao reabrir o jogo, armarios
que voce nao abriu nao sao repostos, mesmo aparecendo como ligados.

## A correcao
Quando um item sai de um slot, este mod verifica no arquivo salvo do SmartRestock
se aquele slot esta ligado e o registra antes do SmartRestock decidir se repoe,
exatamente como aconteceria ao abrir o armario. Nao altera o SmartRestock, nem
seus arquivos de configuracao.

## Instalacao
Junto com o SmartRestock: importe o zip no r2modman / Thunderstore Mod Manager,
ou copie `SmartRestockFix.dll` para a pasta `Mods/`.

No console do MelonLoader deve aparecer:
`Correcao aplicada: armarios configurados repoem mesmo sem abrir.`

Se o SmartRestock mudar por dentro numa versao futura, o complemento se desativa
sozinho e avisa no console, sem afetar o jogo.
