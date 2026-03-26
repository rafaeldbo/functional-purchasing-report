open System

open ETL.CSVHandler
open ETL.Models
open ETL.Parsing
open ETL.Report


let orders = readCSV (buildPath "../Data/order.csv")
            |> Seq.map (unpackOrderCSVRow >> parseOrder >> tryMakeOrder)
            |> Seq.choose id
            |> Seq.toList

let orderItems = readCSV (buildPath "../Data/order_item.csv")
                |> Seq.map (unpackOrderItemCSVRow >> parseOrderItem >> tryMakeOrderItem)
                |> Seq.choose id
                |> Seq.toList

let report = reportOrderTotals Pending Online orders orderItems

writeCSV 
    ["order_id"; "total_amount"; "total_taxes"] 
    (buildPath "report.csv") 
    (fun (r: PurchasingReport) -> [r.OrderId.ToString(); r.TotalAmount.ToString(); r.TotalTaxes.ToString()]) 
    report