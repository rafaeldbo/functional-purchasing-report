module Tests

open System
open Xunit

open ETL.Models
open ETL.Utils 
open ETL.Parsing   
open ETL.Report

// Data padrão usada nos testes
let defaultDate = DateTime(2026, 1, 1)

// Instâncias globais para facilitar manutenção dos testes
let order1 = makeOrder (1, 10, defaultDate, Complete, Online)
let order2_pending = makeOrder (2, 20, defaultDate, Pending, Physical)
let order2_complete = makeOrder (2, 20, defaultDate, Complete, Physical)
let order3 = makeOrder (3, 30, defaultDate, Complete, Online)

let itemO1_Q2_P50_T10 = makeOrderItem (1, 100, 2, 50.0, 0.10)
let itemO1_Q1_P30_T10 = makeOrderItem (1, 101, 1, 30.0, 0.10)
let itemO1_Q3_P20_T10 = makeOrderItem (1, 101, 3, 20.0, 0.10)
let itemO1_Q1_P100_T20 = makeOrderItem (1, 102, 1, 100.0, 0.20)
let itemO1_Q1_P100_T10 = makeOrderItem (1, 103, 1, 100.0, 0.10)
let itemO1_Q3_P10_T100 = makeOrderItem (1, 104, 3, 10.0, 1.0)
let itemO1_Q3_P30_T10 = makeOrderItem (1, 105, 3, 30.0, 0.10)
let itemO1_Q2_P50_T0 = makeOrderItem (1, 106, 2, 50.0, 0.0)
let itemO1_Q1_P20_T0 = makeOrderItem (1, 107, 1, 20.0, 0.0)
let itemO2_Q4_P25_T10 = makeOrderItem (2, 100, 4, 25.0, 0.10)
let itemO2_Q3_P20_T10 = makeOrderItem (2, 101, 3, 20.0, 0.10)

// Globais adicionais para evitar construções inline nos testes
let orders_many = [ 1..10 ] |> List.map (fun i -> makeOrder (i, i, defaultDate, Complete, Online))
let items_many =
        [ 1..10 ]
        |> List.collect (fun i ->
                [ makeOrderItem (i, i * 100, 1, 10.0, 0.10)
                  makeOrderItem (i, i * 100 + 1, 2, 20.0, 0.10)
                  makeOrderItem (i, i * 100 + 2, 3, 30.0, 0.10) ])
let many_items_order1 = [ 1..100 ] |> List.map (fun i -> toOrderItemWithInfo order1 (makeOrderItem (1, i, 1, 1.0, 0.10)))

// ── innerJoin – contagem e presença ───────────────────────────────────────

[<Fact>]
let ``innerJoin retorna combinações corretas`` () =
    let orders = [ order1; order2_pending ]

    let items = [ itemO1_Q2_P50_T10; itemO1_Q1_P30_T10; itemO2_Q3_P20_T10 ]

    let result =
        innerJoin
            (fun (o: Order) -> o.OrderId)
            (fun (i: OrderItem) -> i.OrderId)
            (fun (o: Order) (i: OrderItem) -> (o.OrderId, i.ProductId))
            orders
            items

    Assert.Equal(3, result.Length)
    Assert.Contains((1, 100), result)
    Assert.Contains((1, 101), result)
    Assert.Contains((2, 101), result)

[<Fact>]
let ``innerJoin exclui registros sem correspondência`` () =
    let orders = [ order1 ]
    let items = [ itemO2_Q4_P25_T10 ]

    let result =
        innerJoin
            (fun (o: Order) -> o.OrderId)
            (fun (i: OrderItem) -> i.OrderId)
            (fun (o: Order) (i: OrderItem) -> (o.OrderId, i.ProductId))
            orders
            items

    Assert.Empty(result)

[<Fact>]
let ``innerJoin com listas vazias retorna lista vazia`` () =
    let result =
        innerJoin (fun (o: Order) -> o.OrderId) (fun (i: OrderItem) -> i.OrderId) (fun o i -> (o.OrderId, i.ProductId)) [] []

    Assert.Empty(result)

[<Fact>]
let ``innerJoin com lista esquerda vazia retorna lista vazia`` () =
    let items = [ itemO1_Q2_P50_T10 ]

    let result =
        innerJoin (fun (o: Order) -> o.OrderId) (fun (i: OrderItem) -> i.OrderId) (fun o i -> (o.OrderId, i.ProductId)) [] items

    Assert.Empty(result)

[<Fact>]
let ``innerJoin com lista direita vazia retorna lista vazia`` () =
    let orders = [ order1 ]

    let result =
        innerJoin
            (fun (o: Order) -> o.OrderId)
            (fun (i: OrderItem) -> i.OrderId)
            (fun (o: Order) (i: OrderItem) -> (o.OrderId, i.ProductId))
            orders
            []

    Assert.Empty(result)

[<Fact>]
let ``innerJoin produz produto cartesiano quando há múltiplos matches`` () =
    let orders = [ order1 ]

    let items = [ itemO1_Q2_P50_T10; itemO1_Q1_P30_T10; itemO1_Q3_P30_T10 ]

    let result =
        innerJoin (fun (o: Order) -> o.OrderId) (fun (i: OrderItem) -> i.OrderId) (fun o i -> i.ProductId) orders items

    Assert.Equal(3, result.Length)
    Assert.Contains(100, result)
    Assert.Contains(101, result)
    Assert.Contains(105, result)

[<Fact>]
let ``innerJoin omite pedidos sem nenhum item`` () =
    // pedido 3 não tem itens → não deve aparecer no resultado
    let orders = [ order1; order2_pending; order3 ]

    let items = [ itemO1_Q2_P50_T10; itemO2_Q3_P20_T10 ]

    let result =
        innerJoin (fun (o: Order) -> o.OrderId) (fun (i: OrderItem) -> i.OrderId) (fun o i -> o.OrderId) orders items

    Assert.DoesNotContain(3, result)
    Assert.Equal(2, result.Length)

[<Fact>]
let ``innerJoin propaga campos corretos da função de junção`` () =
    let order = order1
    let item = itemO1_Q2_P50_T10

    let result = joinOrderItems [ order ] [ item ]

    Assert.Equal(1, result.Length)
    let r = result.[0]
    Assert.Equal(order.ClientId, r.ClientId)
    Assert.Equal(order.Status, r.Status)
    Assert.Equal(order.Origin, r.Origin)
    Assert.Equal(item.ProductId, r.ProductId)
    Assert.Equal(item.Quantity, r.Quantity)
    Assert.Equal(item.Price, r.Price)
    Assert.Equal(item.Tax, r.Tax)

[<Fact>]
let ``innerJoin com muitos pedidos e muitos itens retorna contagem correta`` () =
    let orders = orders_many
    // 3 itens por pedido
    let items = items_many

    let result =
        innerJoin
            (fun (o: Order) -> o.OrderId)
            (fun (i: OrderItem) -> i.OrderId)
            (fun o i -> (o.OrderId, i.ProductId))
            orders
            items

    Assert.Equal(30, result.Length)

// ── calcOrderTotals ───────────────────────────────────────────────────────-

[<Fact>]
let ``calcOrderTotals calcula total e imposto médio ponderado corretamente`` () =
    let order = order1

    let items =
        [ toOrderItemWithInfo order itemO1_Q2_P50_T10
          toOrderItemWithInfo order itemO1_Q1_P100_T20 ]

    let totalAmount, totalTax = calcOrderTotals items

    Assert.Equal(200.0, totalAmount, 6)
    Assert.Equal(30.0, totalTax, 6)

[<Fact>]
let ``calcOrderTotals com imposto uniforme retorna o mesmo percentual`` () =
    let order = order1

    let items =
        [ toOrderItemWithInfo order itemO1_Q2_P50_T10
          toOrderItemWithInfo order itemO2_Q3_P20_T10 ]

    let totalAmount, totalTax = calcOrderTotals items

    // 2×50 + 3×20 = 160.0
    Assert.Equal(160.0, totalAmount, 6)
    Assert.Equal(16.0, totalTax, 6)

[<Fact>]
let ``calcOrderTotals com item único`` () =
    let order = order1
    let items = [ toOrderItemWithInfo order itemO2_Q4_P25_T10 ]

    let totalAmount, totalTax = calcOrderTotals items

    Assert.Equal(100.0, totalAmount, 6)
    Assert.Equal(10.0, totalTax, 6)

[<Fact>]
let ``calcOrderTotals com tax zero retorna imposto zero`` () =
    let order = order1

    let items = [ toOrderItemWithInfo order itemO1_Q2_P50_T0
                  toOrderItemWithInfo order itemO1_Q1_P20_T0 ]

    let totalAmount, totalTax = calcOrderTotals items

    Assert.Equal(120.0, totalAmount, 6)
    Assert.Equal(0.0, totalTax, 6)

[<Fact>]
let ``calcOrderTotals com tax 100% retorna totalTax igual ao totalAmount`` () =
    let order = order1
    let items = [ toOrderItemWithInfo order itemO1_Q3_P10_T100 ]

    let totalAmount, totalTax = calcOrderTotals items

    Assert.Equal(30.0, totalAmount, 6)
    Assert.Equal(30.0, totalTax, 6)

[<Fact>]
let ``calcOrderTotals itens com quantities diferentes são ponderados corretamente`` () =
    let order = order1
    let items = [ toOrderItemWithInfo order itemO2_Q3_P20_T10
                  toOrderItemWithInfo order itemO1_Q1_P100_T20 ]

    let totalAmount, totalTax = calcOrderTotals items

    Assert.Equal(160.0, totalAmount, 6)
    Assert.Equal(26.0, totalTax, 6)

[<Fact>]
let ``calcOrderTotals com muitos itens acumula corretamente`` () =
    let items = many_items_order1

    let totalAmount, totalTax = calcOrderTotals items

    Assert.Equal(100.0, totalAmount, 6)
    Assert.Equal(10.0, totalTax, 6)

// ── integração: reportOrderTotals ─────────────────────────────────────────

[<Fact>]
let ``reportOrderTotals gera totais corretos para múltiplos pedidos`` () =
    let orders = [ order1; order2_complete ]
    let items = [ itemO1_Q2_P50_T10; itemO1_Q1_P100_T20; itemO2_Q4_P25_T10 ]

    let joined = joinOrderItems orders items
    let result =
        reportOrderTotals joined
        |> List.sortBy (fun r -> r.OrderId)

    Assert.Equal(2, result.Length)

    let r1 = result.[0]
    Assert.Equal(1, r1.OrderId)
    Assert.Equal(200.0, r1.TotalAmount, 6)
    Assert.Equal(30.0, r1.TotalTaxes, 6)

    let r2 = result.[1]
    Assert.Equal(2, r2.OrderId)
    Assert.Equal(100.0, r2.TotalAmount, 6)
    Assert.Equal(10.0, r2.TotalTaxes, 6)

[<Fact>]
let ``reportOrderTotals exclui pedido sem itens`` () =
    let orders = [ order1; order2_complete ]
    let items = [ itemO1_Q1_P100_T10 ]

    let joined = joinOrderItems orders items

    let result = reportOrderTotals joined

    Assert.Equal(1, result.Length)
    Assert.Equal(1, result.[0].OrderId)

[<Fact>]
let ``reportOrderTotals filtra pedidos por status`` () =
    let orders = [ order1; order2_pending ]
    let items = [ itemO1_Q2_P50_T10; itemO2_Q3_P20_T10 ]

    let filteredOrders = orders |> List.filter (fun o -> o.Status = Complete)
    let joined = joinOrderItems filteredOrders items

    let result = reportOrderTotals joined

    Assert.Equal(1, result.Length)
    Assert.Equal(1, result.[0].OrderId)

[<Fact>]
let ``reportOrderTotals filtra pedidos por data`` () =
    let orders = [ order1 ]
    let items = [ itemO1_Q2_P50_T10 ]

    let filteredOrders = orders |> List.filter (fun o -> o.OrderDate = DateTime(2027, 1, 1))
    let joined = joinOrderItems filteredOrders items

    let result = reportOrderTotals joined

    Assert.Empty(result)

[<Fact>]
let ``reportOrderTotals com listas vazias retorna lista vazia`` () =
    let result = joinOrderItems [] [] |> reportOrderTotals
    Assert.Empty(result)
