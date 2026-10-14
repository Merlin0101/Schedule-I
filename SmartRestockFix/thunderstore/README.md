# SmartRestockFix

Complemento para o **SmartRestock** (uplusion23, testado com a 1.3.2).

## O problema
O SmartRestock salva quais slots tem auto-restock ligado, mas so repoe um armario
depois que voce o abre pelo menos uma vez na sessao. Ao reabrir o jogo, armarios
que voce nao abriu nao sao repostos, mesmo aparecendo como ligados.

O SmartRestock tambem so sabe o que comprar no momento em que o slot esvazia.
Se o jogo foi fechado com um slot ligado vazio, ele nunca mais era reposto.

## O que este complemento faz
1. **Repoe sem abrir:** quando um item sai de um slot, verifica no arquivo salvo do
   SmartRestock se aquele slot esta ligado e o registra antes do SmartRestock
   decidir, como aconteceria ao abrir o armario.
2. **Memoria de produto:** anota o produto de cada slot ligado
   (`UserData/SmartRestockFix/slot-items-N.json`, um por slot de save). Alguns
   segundos depois de carregar o save, slots ligados que estao vazios sao repostos
   com esse produto pelo proprio SmartRestock (mesmo preco, filtros, notificacoes e
   escolha entre dinheiro em maos ou banco).

Nao altera o SmartRestock nem os arquivos dele.

## Configuracao
```toml
[SmartRestockFix]
ReporSlotsVaziosAoCarregar = true
```

Um slot que nunca teve produto desde que o complemento foi instalado precisa de
1 unidade colocada manualmente uma vez; depois disso ele e lembrado.

## Instalacao
Junto com o SmartRestock: importe o zip no r2modman / Thunderstore Mod Manager,
ou copie `SmartRestockFix.dll` para a pasta `Mods/`.

No console do MelonLoader deve aparecer:
`Correcao aplicada: armarios repoem sem abrir e slots vazios sao lembrados entre sessoes.`
e, alguns segundos depois de carregar o save:
`Apos carregar (N slots vistos): X slot(s) vazio(s) reposto(s), Y produto(s) novo(s) memorizado(s).`

Credito: SmartRestock e de uplusion23. Este complemento so corrige o comportamento
descrito acima.

Se o SmartRestock mudar por dentro numa versao futura, o complemento se desativa
sozinho e avisa no console, sem afetar o jogo.
