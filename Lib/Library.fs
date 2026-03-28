namespace ETL

open ETL.Models
open ETL.Parsing
open ETL.Utils

module Report =


    /// <summary>
    /// Função curried que realiza um `inner join` entre listas de `Order` e `OrderItem`.
    /// </summary>
    /// <remarks>Retorna uma função que recebe listas de `orders` e `orderItems` e produz uma lista de `OrderItemWithOrderInfo`.</remarks>
    let joinOrderItems =
        innerJoin
            (fun (o: Order) -> o.OrderId)
            (fun (i: OrderItem) -> i.OrderId)
            (fun (o: Order) (i: OrderItem) -> makeOrderItemWithInfo o i)
            

    /// <summary>
    /// Calcula o total (valor) e total de impostos para uma lista de `OrderItemWithOrderInfo`.
    /// </summary>
    /// <param name="joinedOrderItems">Lista de itens associados a um pedido.</param>
    /// <returns>Tupla `(totalAmount, totalTaxes)`.</returns>
    /// <remarks>Multiplica `Price * Quantity` para cada item e soma valores e impostos.</remarks>
    let calcOrderTotals (joinedOrderItems: OrderItemWithOrderInfo list): Price * Tax = 
        joinedOrderItems
        |> List.map (fun orderItem -> 
                let itemAmount = orderItem.Price * float orderItem.Quantity
                (itemAmount, itemAmount * orderItem.Tax)
            )
        |> List.fold (fun (accAmount, accTaxAmount) (itemAmount, itemTaxAmount) -> 
                (accAmount + itemAmount, accTaxAmount + itemTaxAmount)
            ) (0.0, 0.0)


    /// <summary>Gera o relatório de totais por pedido.</summary>
    /// <param name="joinedOrderItems">Lista de `OrderItemWithOrderInfo` já combinados com o `Order`.</param>
    /// <returns>Lista de `OrderTotalsReport` com totais por `OrderId`.</returns>
    let reportOrderTotals (joinedOrderItems: OrderItemWithOrderInfo list) =
        joinedOrderItems
        |> List.groupBy (fun item -> item.OrderId)
        |> List.map (fun (orderId, joinedOrderItemsOfOrder) ->
            let totalAmount, totalTax = calcOrderTotals joinedOrderItemsOfOrder
            { OrderId = orderId
              TotalAmount = totalAmount
              TotalTaxes = totalTax })


    /// <summary>Gera o relatório de média mensal por pedido.</summary>
    /// <param name="joinedOrderItems">Lista de `OrderItemWithOrderInfo`.</param>
    /// <returns>Lista de `MonthlyAverageReport` agrupada por ano e mês.</returns>
    /// <remarks>
    /// Para cada mês calcula-se a média dos totais dos pedidos (soma dos itens por pedido / número de pedidos no mês).
    /// </remarks>
    let reportMonthlyAverage (joinedOrderItems: OrderItemWithOrderInfo list)=
        joinedOrderItems
        |> List.groupBy (fun oI -> (oI.OrderDate.Year, oI.OrderDate.Month))
        |> List.map (fun ((year, month), joinedOrderItems) ->
            let MonthlyOrderTotals =
                joinedOrderItems
                |> List.groupBy (fun oi -> oi.OrderId)
                |> List.map (fun (_, joinedOrderItemsOfOrder) -> calcOrderTotals joinedOrderItemsOfOrder)
            let count = float (List.length MonthlyOrderTotals)
            let sumAmount, sumTax =
                MonthlyOrderTotals
                |> List.fold (fun (accAmount, accTax) (orderAmount, orderTax) -> 
                    (accAmount + orderAmount, accTax + orderTax)
                ) (0.0, 0.0)
            let avgAmount = if count = 0.0 then 0.0 else sumAmount / count
            let avgTax = if count = 0.0 then 0.0 else sumTax / count
            { Year = year
              Month = month
              AverageAmount = avgAmount
              AverageTaxes = avgTax })