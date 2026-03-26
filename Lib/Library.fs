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
        |> fun (totalAmount, totalTaxAmount) -> (totalAmount, totalTaxAmount)


    let reportOrderTotals (orders: Order list) (items: OrderItem list) =
        joinOrderItems orders items
        |> List.groupBy (fun item -> item.OrderId)
        |> List.map (fun (orderId, orderItems) ->
            let totalAmount, totalTax = calcOrderTotals orderItems
            { OrderId = orderId
              TotalAmount = totalAmount
              TotalTaxes = totalTax })


    let reportMonthlyAverage (orders: Order list) (items: OrderItem list) =
        joinOrderItems orders items
        |> List.groupBy (fun oI -> (oI.OrderDate.Year, oI.OrderDate.Month))
        |> List.map (fun ((year, month), orderItems) ->
            let MonthlyOrderTotals =
                orderItems
                |> List.groupBy (fun oi -> oi.OrderId)
                |> List.map (fun (_, itemsOfOrder) -> calcOrderTotals itemsOfOrder)
            let count = float (List.length MonthlyOrderTotals)
            let sumAmount = MonthlyOrderTotals |> List.sumBy fst
            let sumTax = MonthlyOrderTotals |> List.sumBy snd
            let avgAmount = if count = 0.0 then 0.0 else sumAmount / count
            let avgTax = if count = 0.0 then 0.0 else sumTax / count
            { Year = year
              Month = month
              AverageAmount = avgAmount
              AverageTaxes = avgTax })