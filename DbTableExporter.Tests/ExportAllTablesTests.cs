using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DbTableExporter;

namespace DbTableExporter.Tests
{
    public class FakeDbConnection : IDbConnection
    {
        private readonly List<DataTable> _tables;
        private ConnectionState _state = ConnectionState.Closed;
        public FakeDbConnection(IEnumerable<DataTable> tables)
        {
            _tables = tables.ToList();
        }
        public IEnumerable<DataTable> Tables => _tables;
        public string ConnectionString { get; set; } = string.Empty;
        public int ConnectionTimeout => 0;
        public string Database => "Fake";
        public ConnectionState State => _state;
        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) { }
        public void Close() { _state = ConnectionState.Closed; }
        public IDbCommand CreateCommand() => new FakeDbCommand(this);
        public void Open() { _state = ConnectionState.Open; }
        public void Dispose() { Close(); }
    }

    public class FakeDbCommand : IDbCommand
    {
        private readonly FakeDbConnection _connection;
        public FakeDbCommand(FakeDbConnection connection) { _connection = connection; }
        public string CommandText { get; set; } = string.Empty;
        public int CommandTimeout { get; set; }
        public CommandType CommandType { get; set; } = CommandType.Text;
        public IDbConnection Connection { get => _connection; set { } }
        public IDataParameterCollection Parameters => new List<IDataParameter>();
        public IDbTransaction Transaction { get; set; }
        public UpdateRowSource UpdatedRowSource { get; set; }
        public void Cancel() { }
        public IDbDataParameter CreateParameter() => throw new NotImplementedException();
        public void Dispose() { }
        public int ExecuteNonQuery() => throw new NotImplementedException();
        public IDataReader ExecuteReader() => ExecuteReader(CommandBehavior.Default);
        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            if (CommandText.StartsWith("SELECT TABLE_NAME", StringComparison.OrdinalIgnoreCase))
            {
                var dt = new DataTable();
                dt.Columns.Add("TABLE_NAME");
                foreach (var t in _connection.Tables)
                {
                    dt.Rows.Add(t.TableName);
                }
                return dt.CreateDataReader();
            }
            else if (CommandText.StartsWith("SELECT * FROM", StringComparison.OrdinalIgnoreCase))
            {
                var parts = CommandText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var name = parts.Last().Trim('[', ']', '"');
                var table = _connection.Tables.First(t => t.TableName == name);
                return table.CreateDataReader();
            }
            throw new InvalidOperationException($"Unsupported query: {CommandText}");
        }
        public object ExecuteScalar() => throw new NotImplementedException();
        public void Prepare() { }
    }

    public class ExportAllTablesTests
    {
        [Fact]
        public void ExportAllTables_WritesCsv()
        {
            var table = new DataTable("Customers");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Rows.Add(1, "Alice");
            table.Rows.Add(2, "Bob");

            using var conn = new FakeDbConnection(new[] { table });
            var output = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            int count = DatabaseExporter.ExportAllTables(conn, "oracle", output);

            Assert.Equal(1, count);
            string file = Path.Combine(output, "Customers.csv");
            Assert.True(File.Exists(file));
            var lines = File.ReadAllLines(file);
            Assert.Contains("\"Alice\"", lines[1]);
            Assert.Contains("\"Bob\"", lines[2]);
        }
    }
}
