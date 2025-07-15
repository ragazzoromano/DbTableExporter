using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Oracle.ManagedDataAccess.Client;

namespace DbTableExporter
{
    public static class DatabaseExporter
    {
        public static int ExportAllTables(string connectionString, string dbType, string outputFolder, Action<string> log = null)
        {
            IDbConnection connection;
            if (dbType.ToLower().Contains("sql"))
                connection = new SqlConnection(connectionString);
            else if (dbType.ToLower().Contains("oracle"))
                connection = new OracleConnection(connectionString);
            else
                throw new ArgumentException("Unsupported dbType.");

            Directory.CreateDirectory(outputFolder);

            using (connection)
            {
                connection.Open();
                var tableNames = GetTableNames(connection, dbType);

                int count = 0;
                foreach (var table in tableNames)
                {
                    string filePath = Path.Combine(outputFolder, $"{table}.csv");
                    try
                    {
                        ExportTable(connection, table, filePath);
                        count++;
                        log?.Invoke($"Exported {table} ({filePath})");
                    }
                    catch (Exception ex)
                    {
                        log?.Invoke($"Failed to export {table}: {ex.Message}");
                    }
                }

                return count;
            }
        }

        private static List<string> GetTableNames(IDbConnection conn, string dbType)
        {
            var tables = new List<string>();
            string sql = dbType.ToLower().Contains("sql")
                ? "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'"
                : "SELECT TABLE_NAME FROM USER_TABLES";
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        tables.Add(reader.GetString(0));
            }
            return tables;
        }

        private static void ExportTable(IDbConnection conn, string tableName, string filePath)
        {
            using (var cmd = conn.CreateCommand())
            {
                if (conn is SqlConnection)
                    cmd.CommandText = $"SELECT * FROM [{tableName}]";
                else
                    cmd.CommandText = $"SELECT * FROM \"{tableName}\"";
                using (var reader = cmd.ExecuteReader())
                using (var writer = new StreamWriter(filePath))
                {
                    // Header
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (i > 0) writer.Write(",");
                        writer.Write(reader.GetName(i));
                    }
                    writer.WriteLine();

                    // Rows
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (i > 0) writer.Write(",");
                            string value = reader.IsDBNull(i) ? "" : reader[i].ToString().Replace("\"", "\"\"");
                            writer.Write($"\"{value}\"");
                        }
                        writer.WriteLine();
                    }
                }
            }
        }
    }
}