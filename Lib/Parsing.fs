namespace ETL

open System
open ETL.Models

module Parsing =
    /// <summary>Funções auxiliares para parsing de campos vindos de CSV ou entrada textual.</summary>
    
    /// <summary>Converte uma string em `int option`.</summary>
    /// <param name="s">String a ser convertida.</param>
    /// <returns>`Some int` se o parse for bem sucedido; `None` caso contrário.</returns>
    let parseInt (s: string): int option =
        match Int32.TryParse s with
        | true, value -> Some value
        | _ -> None

    /// <summary>Converte uma string em `float option`.</summary>
    /// <param name="s">String a ser convertida.</param>
    /// <returns>`Some float` se o parse for bem sucedido; `None` caso contrário.</returns>
    let parseFloat (s: string): float option =
        match Double.TryParse s with
        | true, value -> Some value
        | _ -> None

    /// <summary>Converte uma string em `DateTime option`.</summary>
    /// <param name="s">String que contém a data/hora.</param>
    /// <returns>`Some DateTime` se o parse for bem sucedido; `None` caso contrário.</returns>
    let parseDateTime (s: string): DateTime option =
        match DateTime.TryParse s with
        | true, value -> Some value
        | _ -> None

    /// <summary>Faz o parse de uma string para o tipo discriminado `Status`.</summary>
    /// <param name="s">String representando o status (ex.: "pending", "complete", "cancelled").</param>
    /// <returns>`Some Status` se houver correspondência; `None` caso contrário.</returns>
    let parseStatus (s: string): Status option =
        match s.ToLowerInvariant() with
        | "pending" | "pend" -> Some Pending
        | "complete" | "cmpl" -> Some Complete
        | "cancelled" | "canc" -> Some Cancelled
        | _ -> None

    /// <summary>Faz o parse de uma string para o tipo discriminado `Origin`.</summary>
    /// <param name="s">String representando a origem (ex.: "physical", "online").</param>
    /// <returns>`Some Origin` se houver correspondência; `None` caso contrário.</returns>
    let parseOrigin (s: string): Origin option =
        match s.ToLowerInvariant() with
        | "physical" | "p" -> Some Physical
        | "online" | "o" -> Some Online
        | _ -> None

    /// <summary>
    /// Tenta fazer o parse dos campos brutos de uma linha de `Order`.
    /// </summary>
    /// <param name="orderId">Campo `id` como string.</param>
    /// <param name="clientId">Campo `client_id` como string.</param>
    /// <param name="orderDate">Campo `order_date` como string.</param>
    /// <param name="status">Campo `status` como string.</param>
    /// <param name="origin">Campo `origin` como string.</param>
    /// <returns>Tupla `(OrderId option, ClientId option, OrderDate option, Status option, Origin option)`.</returns>
    let parseOrder (orderId: string, clientId: string, orderDate: string, status: string, origin: string) =
        orderId |> parseInt,
        clientId |> parseInt,
        orderDate |> parseDateTime,
        status |> parseStatus,
        origin |> parseOrigin

    /// <summary>Cria um `Order` a partir de valores tipados.</summary>
    /// <returns>Registro `Order` preenchido.</returns>
    let makeOrder (orderId: Id, clientId: Id, orderDate: DateTime, status: Status, origin: Origin) =
        { OrderId = orderId
          ClientId = clientId
          OrderDate = orderDate
          Status = status
          Origin = origin }

    /// <summary>
    /// Constrói um `Order` a partir de uma tupla de `option`.
    /// </summary>
    /// <returns>`Some Order` se todos os campos estiverem presentes; `None` caso contrário.</returns>
    let tryMakeOrder = function
        | Some orderId, Some clientId, Some orderDate, Some status, Some origin ->
            Some (makeOrder (orderId, clientId, orderDate, status, origin))
        | _ -> None

    /// <summary>
    /// Tenta fazer o parse dos campos brutos de uma linha de `OrderItem`.
    /// </summary>
    /// <returns>Tupla `(OrderId option, ProductId option, Quantity option, Price option, Tax option)`.</returns>
    let parseOrderItem (orderId: string, productId: string, quantity: string, price: string, tax: string) =
        orderId |> parseInt, 
        productId |> parseInt, 
        quantity |> parseInt, 
        price |> parseFloat, 
        tax |> parseFloat

    /// <summary>Cria um `OrderItem` a partir de valores tipados.</summary>
    /// <returns>Registro `OrderItem` preenchido.</returns>
    let makeOrderItem (orderId: Id, productId: Id, quantity: Quantity, price: Price, tax: Tax) =
        { OrderId = orderId
          ProductId = productId
          Quantity = quantity
          Price = price
          Tax = tax }

    /// <summary>
    /// Constrói um `OrderItem` a partir de uma tupla de `option`.
    /// </summary>
    /// <returns>`Some OrderItem` se todos os campos estiverem presentes; `None` caso contrário.</returns>
    let tryMakeOrderItem = function
        | Some orderId, Some productId, Some quantity, Some price, Some tax ->
            Some (makeOrderItem (orderId, productId, quantity, price, tax))
        | _ -> None

    /// <summary>
    /// Constrói um `OrderItemWithOrderInfo` combinando um `Order` com um `OrderItem`.
    /// </summary>
    /// <param name="order">Registro `Order` com informações do pedido.</param>
    /// <param name="orderItem">Registro `OrderItem` contendo dados do item.</param>
    /// <returns>Registro `OrderItemWithOrderInfo` contendo campos combinados.</returns>
    let makeOrderItemWithInfo (order: Order) (orderItem: OrderItem) : OrderItemWithOrderInfo =
        { ProductId = orderItem.ProductId
          Quantity = orderItem.Quantity
          Price = orderItem.Price
          Tax = orderItem.Tax
          OrderId = order.OrderId
          ClientId = order.ClientId
          OrderDate = order.OrderDate
          Status = order.Status
          Origin = order.Origin }