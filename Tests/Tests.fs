module Tests

open System
open Xunit

open Etl.Models
open Etl.Utils 
open Etl.Parsing   
open Etl.Report

// Data padrão usada nos testes
let defaultDate = DateTime(2026, 1, 1)

// Instâncias globais para facilitar manutenção dos testes
let order1 = makeOrder 1 10 defaultDate Complete Online
let order2_pending = makeOrder 2 20 defaultDate Pending Physical
let order2_complete = makeOrder 2 20 defaultDate Complete Physical
let order3 = makeOrder 3 30 defaultDate Complete Online

let itemA = makeItem 1 100 2 50.0 0.10
let itemB = makeItem 1 101 1 30.0 0.10
let itemC = makeItem 2 200 3 20.0 0.10
let itemD = makeItem 1 101 1 100.0 0.20
let itemE = makeItem 2 200 4 25.0 0.10
let itemF = makeItem 1 100 1 100.0 0.10
let itemG = makeItem 1 100 3 10.0 1.0
let itemH = makeItem 1 102 3 30.0 0.10

// ── innerJoin – contagem e presença ───────────────────────────────────────

[<Fact>]
let ``innerJoin retorna combinações corretas`` () =
    let orders = [ order1; order2_pending ]

    let items = [ itemA; itemB; itemC ]

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
    Assert.Contains((2, 200), result)

[<Fact>]
let ``innerJoin exclui registros sem correspondência`` () =
    let orders = [ makeOrder 1 10 defaultDate Complete Online ]
    let items = [ makeItem 99 100 1 10.0 0.10 ]

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
    let items = [ makeItem 1 100 1 10.0 0.10 ]

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

    let items = [ itemA; itemB; itemH ]

    let result =
        innerJoin (fun (o: Order) -> o.OrderId) (fun (i: OrderItem) -> i.OrderId) (fun o i -> i.ProductId) orders items

    Assert.Equal(3, result.Length)
    Assert.Contains(100, result)
    Assert.Contains(101, result)
    Assert.Contains(102, result)

[<Fact>]
let ``innerJoin omite pedidos sem nenhum item`` () =
    // pedido 3 não tem itens → não deve aparecer no resultado
    let orders = [ order1; order2_pending; order3 ]

    let items = [ itemA; itemC ]

    let result =
        innerJoin (fun (o: Order) -> o.OrderId) (fun (i: OrderItem) -> i.OrderId) (fun o i -> o.OrderId) orders items

    Assert.DoesNotContain(3, result)
    Assert.Equal(2, result.Length)

[<Fact>]
let ``innerJoin propaga campos corretos da função de junção`` () =
    let order = makeOrder 7 42 defaultDate Complete Physical
    let item = makeItem 7 999 5 15.50 0.10

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
    let orders = [ 1..10 ] |> List.map (fun i -> makeOrder i i defaultDate Complete Online)
    // 3 itens por pedido
    let items =
        [ 1..10 ]
        |> List.collect (fun i ->
            [ makeItem i (i * 100) 1 10.0 0.10
              makeItem i (i * 100 + 1) 2 20.0 0.10
              makeItem i (i * 100 + 2) 3 30.0 0.10 ])

    let result =
        innerJoin
            (fun (o: Order) -> o.OrderId)
            (fun (i: OrderItem) -> i.OrderId)
            (fun o i -> (o.OrderId, i.ProductId))
            orders
            items

    Assert.Equal(30, result.Length)

// ── calcOrderTotals ────────────────────────────────────────────────────────

[<Fact>]
let ``calcOrderTotals calcula total e imposto médio ponderado corretamente`` () =
    // item1: 2 × 50.0 = 100.0, tax 10 % → taxAmt = 10.0
    // item2: 1 × 100.0 = 100.0, tax 20 % → taxAmt = 20.0
    // totalAmount = 200.0, avgTax = 30.0 / 200.0 = 0.15
    let order = order1

    let items =
        [ toOrderItemWithInfo order itemA
          toOrderItemWithInfo order itemD ]

    let totalAmount, avgTax = calcOrderTotals items

    Assert.Equal(200.0, totalAmount, 6)
    Assert.Equal(0.15, avgTax, 6)

[<Fact>]
let ``calcOrderTotals com imposto uniforme retorna o mesmo percentual`` () =
    let order = makeOrder 1 10 defaultDate Complete Online

    let items =
        [ toOrderItemWithInfo order (makeItem 1 100 2 50.0 0.10)
          toOrderItemWithInfo order (makeItem 1 101 3 20.0 0.10) ]

    let totalAmount, avgTax = calcOrderTotals items

    // 2×50 + 3×20 = 160.0
    Assert.Equal(160.0, totalAmount, 6)
    Assert.Equal(0.10, avgTax, 6)

[<Fact>]
let ``calcOrderTotals com item único`` () =
    let order = makeOrder 1 10 defaultDate Complete Online
    let items = [ toOrderItemWithInfo order (makeItem 1 100 4 25.0 0.10) ]

    let totalAmount, avgTax = calcOrderTotals items

    Assert.Equal(100.0, totalAmount, 6)
    Assert.Equal(0.10, avgTax, 6)

[<Fact>]
let ``calcOrderTotals com tax zero retorna imposto zero`` () =
    let order = makeOrder 1 10 defaultDate Complete Online

    let items =
        [ toOrderItemWithInfo order (makeItem 1 100 2 50.0 0.0)
          toOrderItemWithInfo order (makeItem 1 101 1 30.0 0.0) ]

    let totalAmount, avgTax = calcOrderTotals items

    Assert.Equal(130.0, totalAmount, 6)
    Assert.Equal(0.0, avgTax, 6)

[<Fact>]
let ``calcOrderTotals com tax 100% retorna totalTax igual ao totalAmount`` () =
    let order = order1
    let items = [ toOrderItemWithInfo order itemG ]

    let totalAmount, avgTax = calcOrderTotals items

    Assert.Equal(30.0, totalAmount, 6)
    Assert.Equal(1.0, avgTax, 6)

[<Fact>]
let ``calcOrderTotals itens com quantities diferentes são ponderados corretamente`` () =
    // item1: 10 × 10.0 = 100.0, tax 5 %  → taxAmt = 5.0
    // item2: 1  × 100.0 = 100.0, tax 20 % → taxAmt = 20.0
    // avgTax = 25.0 / 200.0 = 0.125
    let order = makeOrder 1 10 defaultDate Complete Online

    let items =
        [ toOrderItemWithInfo order (makeItem 1 100 10 10.0 0.05)
          toOrderItemWithInfo order (makeItem 1 101 1 100.0 0.20) ]

    let totalAmount, avgTax = calcOrderTotals items

    Assert.Equal(200.0, totalAmount, 6)
    Assert.Equal(0.125, avgTax, 6)

[<Fact>]
let ``calcOrderTotals com muitos itens acumula corretamente`` () =
    let order = makeOrder 1 10 defaultDate Complete Online
    // 100 itens, cada um: qty=1, price=1.0, tax=10% → total = 100.0, avgTax = 0.10
    let items =
        [ 1..100 ]
        |> List.map (fun i -> toOrderItemWithInfo order (makeItem 1 i 1 1.0 0.10))

    let totalAmount, avgTax = calcOrderTotals items

    Assert.Equal(100.0, totalAmount, 6)
    Assert.Equal(0.10, avgTax, 6)

// ── integração: reportOrderTotals ─────────────────────────────────────────

let reportDate = DateTime(2026, 1, 1)

[<Fact>]
let ``reportOrderTotals gera totais corretos para múltiplos pedidos`` () =
    let orders = [ makeOrder 1 10 defaultDate Complete Online; makeOrder 2 20 defaultDate Complete Physical ]

    let items = [ itemA; itemD; itemE ]

    let result =
        reportOrderTotals Complete reportDate orders items
        |> List.sortBy (fun r -> r.OrderId)

    Assert.Equal(2, result.Length)

    let r1 = result.[0]
    Assert.Equal(1, r1.OrderId)
    Assert.Equal(200.0, r1.TotalAmount, 6)
    Assert.Equal(0.15, r1.TotalTaxes, 6)

    let r2 = result.[1]
    Assert.Equal(2, r2.OrderId)
    Assert.Equal(100.0, r2.TotalAmount, 6)
    Assert.Equal(0.10, r2.TotalTaxes, 6)

[<Fact>]
let ``reportOrderTotals exclui pedido sem itens`` () =
    let orders = [ makeOrder 1 10 defaultDate Complete Online; makeOrder 2 20 defaultDate Complete Physical ] // pedido 2 não tem itens

    let items = [ itemF ]

    let result = reportOrderTotals Complete reportDate orders items

    Assert.Equal(1, result.Length)
    Assert.Equal(1, result.[0].OrderId)

[<Fact>]
let ``reportOrderTotals filtra pedidos por status`` () =
    let orders = [ makeOrder 1 10 defaultDate Complete Online; makeOrder 2 20 defaultDate Pending Physical ] // status diferente

    let items = [ makeItem 1 100 1 50.0 0.10; makeItem 2 200 1 50.0 0.10 ]

    let result = reportOrderTotals Complete reportDate orders items

    Assert.Equal(1, result.Length)
    Assert.Equal(1, result.[0].OrderId)

[<Fact>]
let ``reportOrderTotals filtra pedidos por data`` () =
    // makeOrder usa DateTime(2026,1,1) — consultar outra data não retorna nada
    let orders = [ makeOrder 1 10 defaultDate Complete Online ]
    let items = [ makeItem 1 100 1 50.0 0.10 ]

    let outroDate = DateTime(2025, 6, 15)
    let result = reportOrderTotals Complete outroDate orders items

    Assert.Empty(result)

[<Fact>]
let ``reportOrderTotals com listas vazias retorna lista vazia`` () =
    let result = reportOrderTotals Complete reportDate [] []
    Assert.Empty(result)
