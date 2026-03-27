open System
open Argu

open ETL.CSVHandler
open ETL.Models
open ETL.Parsing
open ETL.Report

// 1. Definimos nossos argumentos como uma Discriminated Union
type CliArguments =
    | [<MainCommand>] Input of order:string * orderItem:string
    | [<AltCommandLine("-o")>] Output of output:string
    | [<AltCommandLine("-ma")>] Monthly_Average
    | [<AltCommandLine("-s")>] Status of status:string
    | [<AltCommandLine("-ori")>] Origin of origin:string
    with
        // A interface IArgParserTemplate gera o texto de "Help" (--help) automaticamente
        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Input _ -> "Especifica os arquivos de entrada para os pedidos e itens de pedido."
                | Output _ -> "Especifica o arquivo de saída para o relatório gerado (Default: report_output.csv)."
                | Monthly_Average -> "Calcula a média mensal dos pedidos."
                | Origin _ -> "Filtra por um ou mais origens."
                | Status _ -> "Filtra por um ou mais status."

type ReportType =
    | OrderTotals
    | MonthlyAverage

[<EntryPoint>]
let main argv =
    let errorHandler = ProcessExiter(
        colorizer = function 
        | ErrorCode.HelpText -> None 
        | _ -> Some ConsoleColor.Red)

    let parser = ArgumentParser.Create<CliArguments>(programName = "orderReport", errorHandler = errorHandler)

    try
        let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)

        let orderPath, orderItemPath =  results.GetResult <@ Input @>

        let reportType = 
            if results.Contains <@ Monthly_Average @> 
                then MonthlyAverage
                else OrderTotals

        let outputPath = 
            buildPath (if results.Contains <@ Output @> 
                then results.GetResult <@ Output @>
                else "report_output.csv")

        let origins = 
            results.GetResults <@ Origin @>
            |> List.choose parseOrigin

        let statuses =
            results.GetResults <@ Status @>
            |> List.choose parseStatus

        let orders = 
            readCSV (buildPath orderPath)
            |> Seq.choose (unpackOrderCSVRow >> parseOrder >> tryMakeOrder)
            |> Seq.toList

        let orderItems = 
            readCSV (buildPath orderItemPath)
            |> Seq.choose (unpackOrderItemCSVRow >> parseOrderItem >> tryMakeOrderItem)
            |> Seq.toList

        let filteredJoinedOrderItems = 
            joinOrderItems orders orderItems
            |> List.filter (fun oi -> 
                (origins.IsEmpty || List.contains oi.Origin origins) &&
                (statuses.IsEmpty || List.contains oi.Status statuses))
    
        match reportType with
        | OrderTotals ->
            filteredJoinedOrderItems |> reportOrderTotals |> saveOrderTotalsReport outputPath
        | MonthlyAverage ->
            filteredJoinedOrderItems |> reportMonthlyAverage |> saveMonthlyAverageReport outputPath

        printfn "Relatório gerado com sucesso em: %s" outputPath

        0 
        
    with e ->
        printfn "%s" e.Message
        1