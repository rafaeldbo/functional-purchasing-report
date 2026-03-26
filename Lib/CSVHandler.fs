namespace ETL

open System.IO
open FSharp.Data

module CSVHandler =

    let buildPath (relativePath: string) =
        Path.Combine(__SOURCE_DIRECTORY__, relativePath) |> Path.GetFullPath

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
        
        // 1. Montamos o template de cabeçalho unindo as colunas com vírgulas
        let header = String.concat "," columns
        let template = CsvFile.Parse(header + "\n", hasHeaders = true)

        // 2. Iteramos sobre a lista de records
        let rows =
            entries
            |> Seq.map (fun record ->
                let row = record |> mapper |> List.toArray
                CsvRow(template, row)
            )

        let csvFinal = template.Append rows 
        csvFinal.Save filePath