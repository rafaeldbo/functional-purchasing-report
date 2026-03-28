namespace ETL

open System
/// <summary>
/// Modelos de domínio usados no processamento de pedidos e geração de relatórios.
/// </summary>
module Models =

    /// <summary>Identificador numérico usado por entidades do domínio.</summary>
    /// <remarks>Alias para `int`. Usado para representar IDs como `OrderId` e `ProductId`.</remarks>
    type Id = int

    /// <summary>Quantidade de itens de um pedido.</summary>
    /// <remarks>Alias para `int`.</remarks>
    type Quantity = int

    /// <summary>Valor monetário associado a preços no domínio.</summary>
    /// <remarks>Alias para `float`.</remarks>
    type Price = float

    /// <summary>Valor de imposto aplicado a um item.</summary>
    /// <remarks>Alias para `float`, representando proporção (ex.: `0.10` para 10%).</remarks>
    type Tax = float

    /// <summary>Estado do pedido.</summary>
    /// <remarks>Valores possíveis: `Pending`, `Complete`, `Cancelled`.</remarks>
    type Status =
        | Pending
        | Complete
        | Cancelled

    /// <summary>Origem do pedido.</summary>
    /// <remarks>Valores possíveis: `Physical` (loja física) ou `Online` (venda online).</remarks>
    type Origin =
        | Physical
        | Online

    /// <summary>Representa um pedido realizado por um cliente.</summary>
    /// <param name="OrderId">Identificador do pedido.</param>
    /// <param name="ClientId">Identificador do cliente.</param>
    /// <param name="OrderDate">Data e hora em que o pedido foi realizado.</param>
    /// <param name="Status">Estado atual do pedido.</param>
    /// <param name="Origin">Origem do pedido.</param>
    type Order = {
        OrderId: Id
        ClientId: Id
        OrderDate: DateTime
        Status: Status
        Origin: Origin
    }

    /// <summary>Representa um item de um pedido.</summary>
    /// <param name="OrderId">ID do pedido ao qual o item pertence.</param>
    /// <param name="ProductId">ID do produto.</param>
    /// <param name="Quantity">Quantidade do item.</param>
    /// <param name="Price">Preço unitário do produto.</param>
    /// <param name="Tax">Percentual de imposto aplicado (ex.: `0.10` para 10%).</param>
    type OrderItem = {
        OrderId: Id
        ProductId: Id
        Quantity: Quantity
        Price: Price
        Tax: Tax
    }

    /// <summary>
    /// Estrutura que combina informações do `OrderItem` com dados do `Order`.
    /// </summary>
    /// <param name="ProductId">ID do produto.</param>
    /// <param name="Quantity">Quantidade do item.</param>
    /// <param name="Price">Preço unitário.</param>
    /// <param name="Tax">Imposto aplicado.</param>
    /// <param name="OrderId">ID do pedido.</param>
    /// <param name="ClientId">ID do cliente.</param>
    /// <param name="OrderDate">Data do pedido.</param>
    /// <param name="Status">Estado do pedido.</param>
    /// <param name="Origin">Origem do pedido.</param>
    type OrderItemWithOrderInfo = {
        ProductId: Id
        Quantity: Quantity
        Price: Price
        Tax: Tax

        OrderId: Id
        ClientId: Id
        OrderDate: DateTime
        Status: Status
        Origin: Origin
    }

    /// <summary>Relatório com totais por pedido.</summary>
    /// <param name="OrderId">ID do pedido.</param>
    /// <param name="TotalAmount">Soma dos valores dos itens do pedido.</param>
    /// <param name="TotalTaxes">Soma dos impostos do pedido.</param>
    type OrderTotalsReport = {
        OrderId: Id
        TotalAmount: Price
        TotalTaxes: Tax
    }

    /// <summary>Relatório com médias mensais de valor e imposto.</summary>
    /// <param name="Year">Ano do agrupamento.</param>
    /// <param name="Month">Mês do agrupamento.</param>
    /// <param name="AverageAmount">Valor médio por pedido no mês.</param>
    /// <param name="AverageTaxes">Média de impostos por pedido no mês.</param>
    type MonthlyAverageReport = {
        Year: int
        Month: int
        AverageAmount: Price
        AverageTaxes: Tax
    }