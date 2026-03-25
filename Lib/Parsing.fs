namespace Etl

open System
open Etl.Models

module Parsing =

    let makeOrder orderId clientId orderDate status origin =
        { OrderId = orderId
          ClientId = clientId
          OrderDate = orderDate
          Status = status
          Origin = origin }

    let makeItem orderId productId qty price tax =
        { OrderId = orderId
          ProductId = productId
          Quantity = qty
          Price = price
          Tax = tax }