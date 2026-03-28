# functional-purchasing-report

## Objetivo do Projeto
O sistema implementa um pipeline ETL que lê entidades `Order` e `OrderItem` a partir de CSV, normaliza os dados e gera relatórios agregados prontos para dashboards de acompanhamento.
O desenvolvimento enfatiza o paradigma funcional por meio de imutabilidade, composição e funções puras em módulos dedicados para manter previsibilidade.


## Estrutura do Projeto
A separação entre funções puras e efeitos colaterais aparece explicitamente na organização de pastas abaixo, reduzindo o acoplamento e facilitando testes.
- `Lib/`: biblioteca funcional com módulos puros (`Models`, `Parsing`, `Utils`, `Report`) e adaptadores de I/O (`CSVHandler`, `SQLHandler`). `Library.fs` concentra regras de negócio e agregações.
- `Main/`: aplicação CLI (`Program.fs`) que orquestra parsing de argumentos Argu, leitura/gravação de arquivos e interação com SQLite.
- `Tests/`: testes xUnit que cobre `innerJoin`, `calcOrderTotals`, `reportOrderTotals` e cenários de borda usando dados sintéticos.
- `Data/`: amostras `order.csv` e `order_item.csv` usadas como entrada durante demonstrações.

## Tecnologias e Bibliotecas
- F# 9 e .NET SDK 9.0 para compilação cross-platform e suporte a recursos modernos da linguagem.
- `FSharp.Data` para leitura e escrita de CSV com tipagem estática leve.
- `Argu` para parsing de CLI com mensagens de ajuda automáticas.
- `Microsoft.Data.Sqlite` para persistir relatórios em bancos locais.
- `xUnit` para testes

## Como Utilizar o CLI
Execute os comandos a partir da raiz do repositório para gerar relatórios filtrados por **status** e **origin**.
- **Comando base** (gera CSV padrão `report_output.csv`):

```bash
dotnet run --project Main -- Data/order.csv Data/order_item.csv --status pending --origin online
```

- **Persistência em SQLite + média mensal**:

```bash
dotnet run --project Main -- Data/order.csv Data/order_item.csv -ma -db database.db -stt cmpl -ori p
```

Use múltiplos `--status` ou `--origin` para aplicar filtros combinados; se nenhum valor for informado o pipeline processa todo o dataset. O parâmetro `-ma/--monthly-average` troca o modo de saída para médias mensais, preservando o mesmo conjunto de filtros e destinos (CSV ou SQLite).


## Relatório de Desenvolvimento
O desenvolvimento seguiu o fluxo clássico Extração → Transformação → Carga para atender ao enunciado da disciplina.
- **Extração**: leitura de CSV (`CSVHandler.readCSV`) + parsing (`Parsing.parseOrder*`) convertendo texto em tipos fortes antes de qualquer regra de negócio.
- **Transformação**: `Utils.innerJoin` combina pedidos e itens; `Report` aplica `map`, `groupBy` e `fold` para consolidar medidas.
- **Carga**: resultados são enviados para `saveOrderTotalsReportOnCSV`/`saveMonthlyAverageReportOnCSV` ou para as rotinas SQLite equivalentes.

A segregação entre módulos puros (`Lib/`) e adaptadores de I/O (`CSVHandler`, `SQLHandler`, `Program`) reduz impacto de mudanças e mantém previsibilidade nas funções críticas.


### Esse relatório foi escrito com auxílio de IA generativa.