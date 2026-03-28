namespace ETL

open System.IO
open FSharp.Data

open ETL.Models

module CSVHandler =
    let buildPath (relativePath: string) =
        Path.Combine(Directory.GetCurrentDirectory(), relativePath) |> Path.GetFullPath

    let readCSV (filePath: string) =
        let csv = CsvFile.Load filePath
        csv.Rows

    let unpackOrderCSVRow (row: CsvRow) =
        row.GetColumn "id",
        row.GetColumn "client_id",
        row.GetColumn "order_date",
        row.GetColumn "status",
        row.GetColumn "origin"

    let unpackOrderItemCSVRow (row: CsvRow) =
        row.GetColumn "order_id",
        row.GetColumn "product_id",
        row.GetColumn "quantity",
        row.GetColumn "price",
        row.GetColumn "tax"

    let writeCSV (columns: string list) (filePath: string) (mapper: 'T -> string list) (entries: 'T list) =
        
        let header = String.concat "," columns
        let template = CsvFile.Parse(header + "\n", hasHeaders = true)

        let rows =
            entries
            |> List.map (fun record ->
                let row = record |> mapper |> List.toArray
                CsvRow(template, row)
            )

        let csv = template.Append rows 
        csv.Save filePath

    let saveOrderTotalsReportOnCSV (filePath: string) (report: OrderTotalsReport list) =
        writeCSV 
            ["order_id"; "total_amount"; "total_taxes"] 
            filePath 
            (fun (r: OrderTotalsReport) -> [r.OrderId.ToString(); r.TotalAmount.ToString(); r.TotalTaxes.ToString()]) 
            report

    let saveMonthlyAverageReportOnCSV (filePath: string) (report: MonthlyAverageReport list) =
        writeCSV 
            ["year"; "month"; "average_amount"; "average_taxes"] 
            filePath 
            (fun (r: MonthlyAverageReport) -> [r.Year.ToString(); r.Month.ToString(); r.AverageAmount.ToString(); r.AverageTaxes.ToString()]) 
            report