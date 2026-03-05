namespace Etl

open System
open Etl.Models

module CoreFunctions =

    let innerJoin (getLeftKey: 'a -> 'key) (getRightKey: 'b -> 'key) (joinFun: 'a -> 'b -> 'c) (leftTable: 'a list) (rightTable: 'b list): 'c list=
        // Obtem um Map cuja as chaves são os valores (únicos) da coluna ON
        // e os valores todos os itens que possuem aquele valor na coluna ON
        let groupedRightItemsByKey = 
            rightTable 
            |> List.groupBy getRightKey 
            |> Map.ofList
        // Para cada item de Left, pega alista de rightItems com a mesma key
        // E cria uma nova de 'c para cada rightItem
        // OBS.: o resultado vai ser ('c list list), mas o List.collect achata para ('c list)
        leftTable 
        |> List.collect (fun leftItem ->
            let key = getLeftKey leftItem
            match Map.tryFind key groupedRightItemsByKey with
            | Some matchingRightItems -> 
                matchingRightItems 
                |> List.map (joinFun leftItem) // (fun rightItem -> joinFun leftItem rightItem)
            | None -> []
        )

    let makeOrder id clientId status origin =
        { Id = id
          ClientId = clientId
          OrderDate = DateTime(2026, 1, 1)
          Status = status
          Origin = origin }

    let makeItem orderId productId qty price tax =
        { OrderId = orderId
          ProductId = productId
          Quantity = qty
          Price = price
          Tax = tax }

    let toOrderItemWithInfo (order: Order) (orderItem: OrderItem) : OrderItemWithOrderInfo =
        { ProductId = orderItem.ProductId
          Quantity = orderItem.Quantity
          Price = orderItem.Price
          Tax = orderItem.Tax
          OrderId = order.Id
          ClientId = order.ClientId
          OrderDate = order.OrderDate
          Status = order.Status
          Origin = order.Origin }

    let joinOrderItems =
        innerJoin
            (fun (o: Order) -> o.Id)
            (fun (i: OrderItem) -> i.OrderId)
            (fun (o: Order) (i: OrderItem) -> toOrderItemWithInfo o i)
            

    let calcOrderTotals (orderItems: OrderItemWithOrderInfo list): Price * Tax = 
        orderItems
        |> List.map (fun orderItem -> 
                let itemAmount = orderItem.Price * float orderItem.Quantity
                (itemAmount, itemAmount * orderItem.Tax)
            )
        |> List.fold (fun (accAmount, accTaxAmount) (itemAmount, itemTaxAmount) -> 
                (accAmount + itemAmount, accTaxAmount + itemTaxAmount)
            ) (0.0, 0.0)
        |> fun (totalAmount, totalTaxAmount) -> (totalAmount, totalTaxAmount / totalAmount)

    let reportOrderTotals (status: Status) (date: DateTime) (orders: Order list) (items: OrderItem list) =
        joinOrderItems orders items
        |> List.filter (fun orderItem -> orderItem.Status = status && orderItem.OrderDate = date)
        |> List.groupBy (fun item -> item.OrderId)
        |> List.map (fun (orderId, orderItems) ->
            let totalAmount, totalTax = calcOrderTotals orderItems
            { OrderId = orderId
              TotalAmount = totalAmount
              TotalTaxes = totalTax })