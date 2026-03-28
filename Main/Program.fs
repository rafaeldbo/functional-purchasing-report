open System
open Argu

open ETL.Models
open ETL.CSVHandler
open ETL.SQLHandler
open ETL.Parsing
open ETL.Report

/// <summary>
/// Argumentos de linha de comando aceitos pelo programa.
/// </summary>
/// <remarks>
/// Casos:
/// - <c>Input</c>: especifica os arquivos de entrada (`order`, `orderItem`).
/// - <c>Output</c>: define o caminho do arquivo CSV de saída.
/// - <c>SQL</c>: define o arquivo SQLite onde salvar o relatório.
/// - <c>Monthly_Average</c>: solicita cálculo da média mensal dos pedidos.
/// - <c>Status</c>: filtra por status do pedido.
/// - <c>Origin</c>: filtra por origem do pedido.
/// </remarks>
// 1. Definimos nossos argumentos como uma Discriminated Union
type CliArguments =
    | [<MainCommand>] Input of order:string * orderItem:string
    | [<AltCommandLine("-csv")>] Output of csv:string
    | [<AltCommandLine("-db")>] SQL of sql:string
    | [<AltCommandLine("-ma")>] Monthly_Average
    | [<AltCommandLine("-stt")>] Status of status:string
    | [<AltCommandLine("-ori")>] Origin of origin:string
    with
        // A interface IArgParserTemplate gera o texto de "Help" (--help) automaticamente
        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Input _ -> "Especifica os arquivos de entrada para os pedidos e itens de pedido."
                | Output _ -> "Especifica o arquivo CSV de saída para o relatório gerado (Default: report_output.csv)."
                | SQL _ -> "Especifica o arquivo SQL onde salvar o relatório (se informado, ignora --output)."
                | Monthly_Average -> "Calcula a média mensal dos pedidos."
                | Origin _ -> "Filtra por um ou mais origens. ('o' ou 'online' para vendas online; 'p' ou 'physical' para loja física)."
                | Status _ -> "Filtra por um ou mais status. ('pend' ou 'pending' para pedidos pendentes; 'cmpl' ou 'complete' para pedidos completos; 'canc' ou 'cancelled' para pedidos cancelados)."

type ReportType =
    | OrderTotals
    | MonthlyAverage

/// <summary>Tipo de relatório a ser gerado pelo programa.</summary>
/// <remarks><c>OrderTotals</c> gera totais por pedido; <c>MonthlyAverage</c> calcula médias mensais.</remarks>
/// <summary>
/// Ponto de entrada da aplicação CLI que processa arquivos CSV e gera relatórios.
/// </summary>
/// <param name="argv">Array de argumentos de linha de comando fornecidos pelo sistema.</param>
/// <returns>Código de saída: `0` em caso de sucesso; `1` em caso de erro.</returns>
/// <remarks>
/// O fluxo principal: parse de argumentos -> leitura de CSV -> aplicação de filtros -> geração do relatório
/// (OrderTotals ou MonthlyAverage) -> persistência em CSV ou SQLite conforme opções.
/// </remarks>
/// <exception cref="System.IO.IOException">Lançada quando há erro de I/O ao ler ou escrever arquivos.</exception>
/// <exception cref="Microsoft.Data.Sqlite.SqliteException">Lançada em caso de falha ao acessar o banco SQLite.</exception>
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

        let csvPath = 
            buildPath (if results.Contains <@ Output @> 
                then results.GetResult <@ Output @>
                else "report_output.csv")

        let dbPathOpt =
            results.TryGetResult <@ SQL @> |> Option.map buildSQLConnectionString

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
            let saveOrderTotalsReport = 
                match dbPathOpt with
                | Some dbPath -> saveOrderTotalsReportOnSQL dbPath
                | None -> saveOrderTotalsReportOnCSV csvPath
            filteredJoinedOrderItems |> reportOrderTotals |> saveOrderTotalsReport
        | MonthlyAverage ->
            let saveMonthlyAverageReport = 
                match dbPathOpt with
                | Some dbPath -> saveMonthlyAverageReportOnSQL dbPath
                | None -> saveMonthlyAverageReportOnCSV csvPath
            filteredJoinedOrderItems |> reportMonthlyAverage |> saveMonthlyAverageReport

        match dbPathOpt with
        | Some dbPath -> printfn "Relatório salvo com sucesso no banco de dados"
        | None -> printfn "Relatório gerado com sucesso em: %s" csvPath

        0 
        
    with e ->
        printfn "%s" e.Message
        1