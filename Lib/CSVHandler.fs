namespace ETL

open System.IO
open FSharp.Data

open ETL.Models
/// <summary>
/// Funções utilitárias para leitura e escrita de arquivos CSV.
/// </summary>
/// <remarks>Utiliza `FSharp.Data.CsvFile` para parsing simples e construção de relatórios CSV.</remarks>
module CSVHandler =

    /// <summary>
    /// Constrói um caminho absoluto a partir de um caminho relativo ao diretório atual.
    /// </summary>
    /// <param name="relativePath">Caminho relativo a ser combinado com o `CurrentDirectory`.</param>
    /// <returns>Caminho absoluto como `string`.</returns>
    let buildPath (relativePath: string) =
        Path.Combine(Directory.GetCurrentDirectory(), relativePath) |> Path.GetFullPath

    /// <summary>
    /// Carrega um CSV e retorna as linhas como `CsvRow`.
    /// </summary>
    /// <param name="filePath">Caminho do arquivo CSV a ser lido.</param>
    /// <returns>Sequência de `CsvRow` representando cada linha do arquivo.</returns>
    /// <exception cref="System.IO.FileNotFoundException">Se o arquivo não for encontrado no caminho fornecido.</exception>
    /// <exception cref="System.IO.IOException">Se ocorrer um erro de I/O ao ler o arquivo.</exception>
    /// <exception cref="System.ArgumentException">Se o caminho for inválido ou o CSV estiver mal formado para o parser.</exception>
    let readCSV (filePath: string) =
        let csv = CsvFile.Load filePath
        csv.Rows

    /// <summary>
    /// Extrai as colunas esperadas de uma linha do CSV de pedidos.
    /// </summary>
    /// <remarks>As colunas esperadas são: `id`, `client_id`, `order_date`, `status`, `origin`.</remarks>
    /// <param name="row">Linha do CSV.</param>
    /// <exception cref="System.ArgumentException">Se alguma coluna esperada estiver ausente.</exception>
    /// <returns>Tupla `(id, client_id, order_date, status, origin)` como strings.</returns>
    let unpackOrderCSVRow (row: CsvRow) =
        row.GetColumn "id",
        row.GetColumn "client_id",
        row.GetColumn "order_date",
        row.GetColumn "status",
        row.GetColumn "origin"

    /// <summary>
    /// Extrai as colunas esperadas de uma linha do CSV de itens de pedido.
    /// </summary>
    /// <remarks>As colunas esperadas são: `order_id`, `product_id`, `quantity`, `price`, `tax`.</remarks>
    /// <param name="row">Linha do CSV.</param>
    /// <exception cref="System.ArgumentException">Se alguma coluna esperada estiver ausente.</exception>
    /// <returns>Tupla `(order_id, product_id, quantity, price, tax)` como strings.</returns>
    let unpackOrderItemCSVRow (row: CsvRow) =
        row.GetColumn "order_id",
        row.GetColumn "product_id",
        row.GetColumn "quantity",
        row.GetColumn "price",
        row.GetColumn "tax"

    /// <summary>
    /// Escreve uma lista de registros em CSV usando um mapeador para colunas.
    /// </summary>
    /// <param name="columns">Lista de nomes de colunas do CSV.</param>
    /// <param name="filePath">Caminho do arquivo CSV de saída.</param>
    /// <param name="mapper">Função que mapeia um Record para a lista de colunas (strings).</param>
    /// <param name="entries">Entradas a serem gravadas.</param>
    /// <remarks>O mapeador deve garantir a ordem e contagem correta de colunas.</remarks>
    /// <exception cref="System.ArgumentException">Se o mapeador produzir um número incorreto de colunas ou valores inválidos.</exception>
    /// <exception cref="System.IO.IOException">Se ocorrer erro ao gravar o arquivo resultante.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Se o processo não tiver permissão para gravar no caminho.</exception>
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

    /// <summary>Salva o relatório de totais por pedido em CSV.</summary>
    /// <param name="filePath">Arquivo de saída CSV.</param>
    /// <param name="report">Lista de `OrderTotalsReport`.</param>
    /// <exception cref="System.IO.IOException">Se ocorrer erro ao gravar o arquivo CSV.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Se não houver permissão para gravar no caminho.</exception>
    /// <exception cref="System.ArgumentException">Se houver problema de mapeamento de colunas.</exception>
    let saveOrderTotalsReportOnCSV (filePath: string) (report: OrderTotalsReport list) =
        writeCSV 
            ["order_id"; "total_amount"; "total_taxes"] 
            filePath 
            (fun (r: OrderTotalsReport) -> [r.OrderId.ToString(); r.TotalAmount.ToString(); r.TotalTaxes.ToString()]) 
            report

    /// <summary>Salva o relatório de média mensal em CSV.</summary>
    /// <param name="filePath">Arquivo de saída CSV.</param>
    /// <param name="report">Lista de `MonthlyAverageReport`.</param>
    /// <exception cref="System.IO.IOException">Se ocorrer erro ao gravar o arquivo CSV.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Se não houver permissão para gravar no caminho.</exception>
    /// <exception cref="System.ArgumentException">Se houver problema de mapeamento de colunas.</exception>
    let saveMonthlyAverageReportOnCSV (filePath: string) (report: MonthlyAverageReport list) =
        writeCSV 
            ["year"; "month"; "average_amount"; "average_taxes"] 
            filePath 
            (fun (r: MonthlyAverageReport) -> [r.Year.ToString(); r.Month.ToString(); r.AverageAmount.ToString(); r.AverageTaxes.ToString()]) 
            report