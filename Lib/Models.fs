namespace ETL

open System

module Models =

    type Id = int
    type Quantity = int
    type Price = float
    type Tax = float

    type Status =
        | Pending
        | Complete
        | Cancelled

    type Origin =
        | Physical
        | Online

    type Order = {
        OrderId: Id
        ClientId: Id
        OrderDate: DateTime
        Status: Status
        Origin: Origin
    }

    type OrderItem = {
        OrderId: Id
        ProductId: Id
        Quantity: Quantity
        Price: Price
        Tax: Tax
    }

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

    type OrderTotalsReport = {
        OrderId: Id
        TotalAmount: Price
        TotalTaxes: Tax
    }

    type MonthlyAverageReport = {
        Year: int
        Month: int
        AverageAmount: Price
        AverageTaxes: Tax
    }