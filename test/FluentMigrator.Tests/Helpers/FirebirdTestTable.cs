using System;
using System.Collections.Generic;
using System.Text;
using FirebirdSql.Data.FirebirdClient;
using FluentMigrator.Runner.Generators.Firebird;
using FluentMigrator.Runner.Processors.Firebird;

namespace FluentMigrator.Tests.Helpers
{
    public class FirebirdTestTable : IDisposable
    {
        private readonly FirebirdQuoter quoter = new FirebirdQuoter(false);
        private readonly FirebirdProcessor processor;

        public FbConnection Connection
        {
            get { return (FbConnection)processor.Connection; }
        }

        public string Name { get; set; }

        public FbTransaction Transaction
        {
            get { return (FbTransaction)processor.Transaction; }
        }

        public FirebirdTestTable(FirebirdProcessor processor, string schemaName, params string[] columnDefinitions)
        {
            this.processor = processor;
            string guid = Guid.NewGuid().ToString("N");
            Name = "Table" + guid.Substring(0, Math.Min(guid.Length, 16));
            Init(processor, columnDefinitions);
        }

        public FirebirdTestTable(string tableName, FirebirdProcessor processor, string schemaName, params string[] columnDefinitions)
        {
            this.processor = processor;
            Name = tableName;
            Init(processor, columnDefinitions);
        }

        private void Init(FirebirdProcessor processor, IEnumerable<string> columnDefinitions)
        {
            Create(columnDefinitions);
        }

        public void Dispose()
        {
            if (Connection.State == System.Data.ConnectionState.Open && !processor.WasCommitted)
                Drop();
        }

        public void Create(IEnumerable<string> columnDefinitions)
        {
            var sb = new StringBuilder();

            sb.Append("CREATE TABLE ");

            sb.Append(quoter.QuoteTableName(Name, string.Empty));
            sb.Append(" (");

            foreach (string definition in columnDefinitions)
            {
                sb.Append(definition);
                sb.Append(", ");
            }

            sb.Remove(sb.Length - 2, 2);
            sb.Append(")");

            var s = sb.ToString();

            using (var command = new FbCommand(s, Connection, Transaction))
                command.ExecuteNonQuery();

            processor.AutoCommit();

            processor.LockTable(Name);

        }

        public void Drop()
        {
            processor.CheckTable(Name);
            var sb = new StringBuilder();
            sb.AppendFormat("DROP TABLE {0}", quoter.QuoteTableName(Name, null));

            using (var command = new FbCommand(sb.ToString(), Connection, Transaction))
                command.ExecuteNonQuery();

            processor.AutoCommit();

        }
    }
}
