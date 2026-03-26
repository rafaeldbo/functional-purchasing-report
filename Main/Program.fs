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

let report = reportOrderTotals orders orderItems

writeCSV 
    ["order_id"; "total_amount"; "total_taxes"] 
    (buildPath "report_order_totals.csv") 
    (fun (r: OrderTotalsReport) -> [r.OrderId.ToString(); r.TotalAmount.ToString(); r.TotalTaxes.ToString()]) 
    report
printfn "Order Totals Report: %A" report

let monthlyAverageReport = reportMonthlyAverage orders orderItems

writeCSV 
    ["year"; "month"; "average_amount"; "average_taxes"] 
    (buildPath "report_monthly_average.csv") 
    (fun (r: MonthlyAverageReport) -> [r.Year.ToString(); r.Month.ToString(); r.AverageAmount.ToString(); r.AverageTaxes.ToString()]) 
    monthlyAverageReport
printfn "Monthly Average Report: %A" monthlyAverageReport