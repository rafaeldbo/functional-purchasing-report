namespace ETL

open System
open System.IO
open Microsoft.Data.Sqlite

open ETL.Models

module SQLHandler =

    let buildSQLConnectionString (relativePath: string) =
        "Data Source=" + (Path.Combine(Directory.GetCurrentDirectory(), relativePath) |> Path.GetFullPath)

    let private openConnection (connString: string) =
        let conn = new SqliteConnection(connString)
        conn.Open()
        conn

    let private createOrderTotalsTable (conn: SqliteConnection) =
        use cmd = conn.CreateCommand()
        cmd.CommandText <- """
        CREATE TABLE IF NOT EXISTS OrderTotals (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            OrderId INTEGER NOT NULL,
            TotalAmount REAL NOT NULL,
            TotalTaxes REAL NOT NULL,
            SavedAt TEXT NOT NULL
        );
        """
        cmd.ExecuteNonQuery() |> ignore

    let private createMonthlyAveragesTables (conn: SqliteConnection) =
        use cmd = conn.CreateCommand()
        cmd.CommandText <- """
        CREATE TABLE IF NOT EXISTS MonthlyAverages (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Year INTEGER NOT NULL,
            Month INTEGER NOT NULL,
            AverageAmount REAL NOT NULL,
            AverageTaxes REAL NOT NULL,
            SavedAt TEXT NOT NULL
        );
        """
        cmd.ExecuteNonQuery() |> ignore

    let saveOrderTotalsReportOnSQL (connString: string) (report: OrderTotalsReport list) =
        use conn = openConnection connString
        createOrderTotalsTable conn
        use tran = conn.BeginTransaction()
        for r in report do
            use cmd = conn.CreateCommand()
            cmd.CommandText <- "INSERT INTO OrderTotals (OrderId, TotalAmount, TotalTaxes, SavedAt) VALUES ($orderId, $totalAmount, $totalTaxes, $savedAt)"
            cmd.Parameters.AddWithValue("$orderId", r.OrderId) |> ignore
            cmd.Parameters.AddWithValue("$totalAmount", r.TotalAmount) |> ignore
            cmd.Parameters.AddWithValue("$totalTaxes", r.TotalTaxes) |> ignore
            cmd.Parameters.AddWithValue("$savedAt", DateTime.UtcNow.ToString("o")) |> ignore
            cmd.ExecuteNonQuery() |> ignore
        tran.Commit()

    let saveMonthlyAverageReportOnSQL (connString: string) (report: MonthlyAverageReport list) =
        use conn = openConnection connString
        createMonthlyAveragesTables conn
        use tran = conn.BeginTransaction()
        for r in report do
            use cmd = conn.CreateCommand()
            cmd.CommandText <- "INSERT INTO MonthlyAverages (Year, Month, AverageAmount, AverageTaxes, SavedAt) VALUES ($year, $month, $avgAmount, $avgTaxes, $savedAt)"
            cmd.Parameters.AddWithValue("$year", r.Year) |> ignore
            cmd.Parameters.AddWithValue("$month", r.Month) |> ignore
            cmd.Parameters.AddWithValue("$avgAmount", r.AverageAmount) |> ignore
            cmd.Parameters.AddWithValue("$avgTaxes", r.AverageTaxes) |> ignore
            cmd.Parameters.AddWithValue("$savedAt", DateTime.UtcNow.ToString("o")) |> ignore
            cmd.ExecuteNonQuery() |> ignore
        tran.Commit()
