namespace ETL

open ETL.Models
open ETL.Utils

module Report =

    let toOrderItemWithInfo (order: Order) (orderItem: OrderItem) : OrderItemWithOrderInfo =
        { ProductId = orderItem.ProductId
          Quantity = orderItem.Quantity
          Price = orderItem.Price
          Tax = orderItem.Tax
          OrderId = order.OrderId
          ClientId = order.ClientId
          OrderDate = order.OrderDate
          Status = order.Status
          Origin = order.Origin }

    let joinOrderItems =
        innerJoin
            (fun (o: Order) -> o.OrderId)
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