namespace ETL

open System
open ETL.Models

module Parsing =

    let parseInt (s: string): int option =
        match Int32.TryParse s with
        | true, value -> Some value
        | _ -> None

    let parseFloat (s: string): float option =
        match Double.TryParse s with
        | true, value -> Some value
        | _ -> None

    let parseDateTime (s: string): DateTime option =
        match DateTime.TryParse s with
        | true, value -> Some value
        | _ -> None

    let parseStatus (s: string): Status option =
        match s.ToLowerInvariant() with
        | "pending" | "pend" -> Some Pending
        | "complete" | "cmpl" -> Some Complete
        | "cancelled" | "canc" -> Some Cancelled
        | _ -> None

    let parseOrigin (s: string): Origin option =
        match s.ToLowerInvariant() with
        | "physical" | "p" -> Some Physical
        | "online" | "o" -> Some Online
        | _ -> None

    let parseOrder (orderId: string, clientId: string, orderDate: string, status: string, origin: string) =
        orderId |> parseInt,
        clientId |> parseInt,
        orderDate |> parseDateTime,
        status |> parseStatus,
        origin |> parseOrigin

    let makeOrder (orderId: Id, clientId: Id, orderDate: DateTime, status: Status, origin: Origin) =
        { OrderId = orderId
          ClientId = clientId
          OrderDate = orderDate
          Status = status
          Origin = origin }

    let tryMakeOrder = function
        | Some orderId, Some clientId, Some orderDate, Some status, Some origin ->
            Some (makeOrder (orderId, clientId, orderDate, status, origin))
        | _ -> None

    let parseOrderItem (orderId: string, productId: string, quantity: string, price: string, tax: string) =
        orderId |> parseInt, 
        productId |> parseInt, 
        quantity |> parseInt, 
        price |> parseFloat, 
        tax |> parseFloat

    let makeOrderItem (orderId: Id, productId: Id, quantity: Quantity, price: Price, tax: Tax) =
        { OrderId = orderId
          ProductId = productId
          Quantity = quantity
          Price = price
          Tax = tax }

    let tryMakeOrderItem = function
        | Some orderId, Some productId, Some quantity, Some price, Some tax ->
            Some (makeOrderItem (orderId, productId, quantity, price, tax))
        | _ -> None