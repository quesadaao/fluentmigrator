using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using FluentMigrator.Builders.Execute;
using FluentMigrator.Expressions;
using FluentMigrator.Model;
using FluentMigrator.Runner.Generators.Firebird;
using FluentMigrator.Runner.Helpers;
using FluentMigrator.Runner.Models;

namespace FluentMigrator.Runner.Processors.Firebird
{
    public class FirebirdProcessor : GenericProcessorBase
    {
        private readonly Lazy<Version> _firebirdVersionFunc;
        protected readonly FirebirdTruncator truncator;
        private readonly FirebirdQuoter quoter;
        public FirebirdOptions FBOptions { get; private set; }
        public bool IsFirebird3 => _firebirdVersionFunc.Value >= new Version(3, 0);
        public new IMigrationGenerator Generator { get { return base.Generator; } }
        public new IAnnouncer Announcer { get { return base.Announcer; } }
        protected List<string> DDLCreatedTables;
        protected Dictionary<string, List<string>> DDLCreatedColumns;
        protected List<string> DDLTouchedTables;
        protected Dictionary<string, List<string>> DDLTouchedColumns;

        public override string DatabaseType
        {
            get { return "Firebird"; }
        }

        public override IList<string> DatabaseTypeAliases { get; } = new List<string>();

        public FirebirdProcessor(IDbConnection connection, IMigrationGenerator generator, IAnnouncer announcer, IMigrationProcessorOptions options, IDbFactory factory, FirebirdOptions fbOptions)
            : base(connection, factory, generator, announcer, options)
        {
            if (fbOptions == null)
                throw new ArgumentNullException("fbOptions");
            _firebirdVersionFunc = new Lazy<Version>(GetFirebirdVersion);
            quoter = new FirebirdQuoter(fbOptions.ForceQuote);
            FBOptions = fbOptions;
            truncator = new FirebirdTruncator(FBOptions.TruncateLongNames, FBOptions.PackKeyNames);
            ClearLocks();
            ClearDDLFollowers();
        }

        private Version GetFirebirdVersion()
        {
            EnsureConnectionIsOpen();
            try
            {
                var result = Read("SELECT rdb$get_context('SYSTEM', 'ENGINE_VERSION') from rdb$database");
                var version = (string)result.Tables[0].Rows[0].ItemArray[0];
                var versionRegEx = new Regex(@"\d+\.\d+(\.\d+)?");
                var match = versionRegEx.Match(version);
                if (match.Success)
                {
                    return new Version(match.Value);
                }

                return new Version(2, 1);
            }
            catch
            {
                // Fehler ignorieren - Älter als Version 2.1
                return new Version(2, 0);
            }
        }

        #region Schema checks

        public override bool SchemaExists(string schemaName)
        {
            //No schema support in firebird
            return true;
        }

        public override bool TableExists(string schemaName, string tableName)
        {
            CheckTable(schemaName);
            return Exists("select rdb$relation_name from rdb$relations where (rdb$flags IS NOT NULL) and (lower(rdb$relation_name) = lower('{0}'))", FormatToSafeName(tableName));
        }

        public override bool ColumnExists(string schemaName, string tableName, string columnName)
        {
            CheckTable(tableName);
            CheckColumn(tableName, columnName);
            return Exists("select rdb$field_name from rdb$relation_fields where (lower(rdb$relation_name) = lower('{0}')) and (lower(rdb$field_name) = lower('{1}'))", FormatToSafeName(tableName), FormatToSafeName(columnName));
        }

        public override bool ConstraintExists(string schemaName, string tableName, string constraintName)
        {
            CheckTable(tableName);
            return Exists("select rdb$constraint_name from rdb$relation_constraints where (lower(rdb$relation_name) = lower('{0}')) and (lower(rdb$constraint_name) = lower('{1}'))", FormatToSafeName(tableName), FormatToSafeName(constraintName));
        }

        public override bool IndexExists(string schemaName, string tableName, string indexName)
        {
            CheckTable(tableName);
            return Exists("select rdb$index_name from rdb$indices where (lower(rdb$relation_name) = lower('{0}')) and (lower(rdb$index_name) = lower('{1}')) and (rdb$system_flag <> 1 OR rdb$system_flag IS NULL) and (rdb$foreign_key IS NULL)", FormatToSafeName(tableName), FormatToSafeName(indexName));
        }

        public override bool SequenceExists(string schemaName, string sequenceName)
        {
            return Exists("select rdb$generator_name from rdb$generators where lower(rdb$generator_name) = lower('{0}')", FormatToSafeName(sequenceName));
        }

        public virtual bool TriggerExists(string schemaName, string tableName, string triggerName)
        {
            CheckTable(tableName);
            return Exists("select rdb$trigger_name from rdb$triggers where (lower(rdb$relation_name) = lower('{0}')) and (lower(rdb$trigger_name) = lower('{1}'))", FormatToSafeName(tableName), FormatToSafeName(triggerName));
        }

        public override DataSet ReadTableData(string schemaName, string tableName)
        {
            CheckTable(tableName);
            return Read("SELECT * FROM {0}", quoter.QuoteTableName(tableName, schemaName));
        }

        public override DataSet Read(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            //Announcer.Sql(String.Format(template,args));

            using (var command = Factory.CreateCommand(String.Format(template, args), Connection, Transaction, Options))
            using (var reader = command.ExecuteReader())
            {
                return reader.ReadDataSet();
            }
        }

        public override bool DefaultValueExists(string schemaName, string tableName, string columnName, object defaultValue)
        {
            return false;
        }

        public override bool Exists(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            using (var command = Factory.CreateCommand(String.Format(template, args), Connection, Transaction, Options))
            using (var reader = command.ExecuteReader())
            {
                return reader.Read();
            }
        }

        #endregion


        #region Transaction handling

        public override void BeginTransaction()
        {
            base.BeginTransaction();
            ClearLocks();
            ClearDDLFollowers();
        }

        public override void CommitTransaction()
        {
            base.CommitTransaction();
            EnsureConnectionIsClosed();
            ClearLocks();
        }

        public override void RollbackTransaction()
        {
            base.RollbackTransaction();
            EnsureConnectionIsClosed();
            ClearLocks();
        }

        public virtual void CommitRetaining()
        {
            if (IsRunningOutOfMigrationScope())
            {
                EnsureConnectionIsClosed();
                return;
            }

            Announcer.Say("Committing and Retaining Transaction");

            CommitTransaction();
            BeginTransaction();
        }

        public virtual void AutoCommit()
        {
            if (FBOptions.TransactionModel == FirebirdTransactionModel.AutoCommit)
                CommitRetaining();
        }

        public bool IsRunningOutOfMigrationScope()
        {
            return Transaction == null;
        }

        #endregion


        #region DDL Tracking

        protected void ClearDDLFollowers()
        {
            DDLCreatedTables = new List<string>();
            DDLCreatedColumns = new Dictionary<string, List<string>>();
        }

        protected void RegisterTableCreation(string tableName)
        {
            if (!DDLCreatedTables.Contains(tableName))
                DDLCreatedTables.Add(tableName);
        }

        protected void RegisterColumnCreation(string tableName, string columnName)
        {
            if (!DDLCreatedColumns.ContainsKey(tableName))
            {
                DDLCreatedColumns.Add(tableName, new List<string>() { columnName });
            }
            else if (!DDLCreatedColumns[tableName].Contains(columnName))
            {
                DDLCreatedColumns[tableName].Add(columnName);
            }
        }

        protected bool IsTableCreated(string tableName)
        {
            return DDLCreatedTables.Contains(tableName);
        }

        protected bool IsColumnCreated(string tableName, string columnName)
        {
            return DDLCreatedColumns.ContainsKey(tableName) && DDLCreatedColumns[tableName].Contains(columnName);
        }

        #endregion


        #region Virtual Lock

        protected void ClearLocks()
        {
            DDLTouchedTables = new List<string>();
            DDLTouchedColumns = new Dictionary<string, List<string>>();
        }

        public void LockTable(string tableName)
        {
            if (!DDLTouchedTables.Contains(tableName))
                DDLTouchedTables.Add(tableName);
        }

        public void LockColumn(string tableName, IEnumerable<string> columns)
        {
            columns.ToList().ForEach(x => LockColumn(tableName, x));
        }

        public void LockColumn(string tableName, string columnName)
        {
            if (!DDLTouchedColumns.ContainsKey(tableName))
            {
                DDLTouchedColumns.Add(tableName, new List<string>() { columnName });
            }
            else if (!DDLTouchedColumns[tableName].Contains(columnName))
            {
                DDLTouchedColumns[tableName].Add(columnName);
            }
        }

        public void CheckTable(string tableName)
        {
            if (DDLTouchedTables.Contains(tableName))
            {
                if (FBOptions.TransactionModel == FirebirdTransactionModel.AutoCommitOnCheckFail)
                {
                    CommitRetaining();
                    return;
                }

                if (FBOptions.VirtualLock)
                    throw new InvalidOperationException(String.Format("Table {0} is locked", tableName));
            }
        }

        public void CheckColumn(string tableName, IEnumerable<string> columns)
        {
            columns.ToList().ForEach(x => CheckColumn(tableName, x));
        }

        public void CheckColumn(string tableName, string columnName)
        {
            CheckTable(tableName);
            if (DDLTouchedColumns.Any(x => x.Key == tableName && x.Value.Contains(columnName)))
            {
                if (FBOptions.TransactionModel == FirebirdTransactionModel.AutoCommitOnCheckFail)
                {
                    CommitRetaining();
                    return;
                }

                if (FBOptions.VirtualLock)
                    throw new InvalidOperationException(String.Format("Column {0} in table {1} is locked", columnName, tableName));
            }
        }

        #endregion


        #region DDL expressions

        public override void Process(Expressions.CreateColumnExpression expression)
        {
            truncator.Truncate(expression);
            CheckColumn(expression.TableName, expression.Column.Name);
            LockColumn(expression.TableName, expression.Column.Name);
            RegisterColumnCreation(expression.TableName, expression.Column.Name);
            InternalProcess(Generator.Generate(expression));

            if (expression.Column.IsIdentity)
            {
                CreateSequenceForIdentity(expression.TableName, expression.Column.Name);
            }

            /*if (FBOptions.TransactionModel == FirebirdTransactionModel.AutoCommitOnCheckFail)
                CommitRetaining();*/
            if (FBOptions.TransactionModel != FirebirdTransactionModel.None)
                CommitRetaining();
        }

        public override void Process(Expressions.AlterColumnExpression expression)
        {
            truncator.Truncate(expression);
            CheckColumn(expression.TableName, expression.Column.Name);
            FirebirdSchemaProvider schema = new FirebirdSchemaProvider(this, quoter);
            FirebirdTableSchema table = schema.GetTableSchema(expression.TableName);
            ColumnDefinition colDef = table.Definition.Columns.FirstOrDefault(x => x.Name == quoter.ToFbObjectName(expression.Column.Name));

            var generator = (FirebirdGenerator) Generator;

            var tableName = expression.Column.TableName ?? expression.TableName;

            //Change nullable constraint
            if (colDef == null || colDef.IsNullable != expression.Column.IsNullable)
            {
                string nullConstraintCommand;
                if (IsFirebird3)
                {
                    nullConstraintCommand = generator.GenerateSetNull3(tableName, expression.Column);
                }
                else
                {
                    nullConstraintCommand = generator.GenerateSetNullPre3(tableName, expression.Column);
                }

                InternalProcess(nullConstraintCommand);
            }

            //Change default value
            if (colDef == null || !FirebirdGenerator.DefaultValuesMatch(colDef, expression.Column))
            {
                IMigrationExpression defaultConstraint;
                if (expression.Column.DefaultValue is ColumnDefinition.UndefinedDefaultValue)
                {
                    defaultConstraint = new DeleteDefaultConstraintExpression()
                    {
                        SchemaName = expression.SchemaName,
                        TableName = expression.TableName,
                        ColumnName = expression.Column.Name
                    };
                }
                else
                {
                    defaultConstraint = new AlterDefaultConstraintExpression()
                    {
                        ColumnName = expression.Column.Name,
                        DefaultValue = expression.Column.DefaultValue,
                        TableName = expression.TableName,
                        SchemaName = expression.SchemaName
                    };
                }

                if (defaultConstraint is DeleteDefaultConstraintExpression)
                {
                    InternalProcess(Generator.Generate(defaultConstraint as DeleteDefaultConstraintExpression));
                }
                else if (defaultConstraint is AlterDefaultConstraintExpression)
                {
                    InternalProcess(Generator.Generate(defaultConstraint as AlterDefaultConstraintExpression));
                }
                else
                {
                    throw new InvalidOperationException("No expression generated for alter default constraint");
                }
            }

            //Change type
            if (colDef == null || !FirebirdGenerator.ColumnTypesMatch(colDef, expression.Column))
            {
                InternalProcess(generator.GenerateSetType(tableName, expression.Column));
            }

            bool identitySequenceExists;
            try
            {
                identitySequenceExists = SequenceExists(String.Empty, GetSequenceName(expression.TableName, expression.Column.Name));
            }
            catch (ArgumentException)
            {
                identitySequenceExists = false;
            }


            //Adjust identity generators
            if (expression.Column.IsIdentity)
            {
                if (!identitySequenceExists)
                    CreateSequenceForIdentity(expression.TableName, expression.Column.Name);
            }
            else
            {
                if (identitySequenceExists)
                    DeleteSequenceForIdentity(expression.TableName, expression.Column.Name);
            }

        }

        public override void Process(Expressions.RenameColumnExpression expression)
        {
            truncator.Truncate(expression);
            CheckColumn(expression.TableName, expression.OldName);
            CheckColumn(expression.TableName, expression.NewName);
            LockColumn(expression.TableName, expression.OldName);
            LockColumn(expression.TableName, expression.NewName);
            InternalProcess(Generator.Generate(expression));
        }

        public override void Process(Expressions.DeleteColumnExpression expression)
        {
            truncator.Truncate(expression);
            CheckColumn(expression.TableName, expression.ColumnNames);
            LockColumn(expression.TableName, expression.ColumnNames);
            foreach (string columnName in expression.ColumnNames)
            {
                try
                {
                    if (SequenceExists(String.Empty, GetSequenceName(expression.TableName, columnName)))
                    {
                        DeleteSequenceForIdentity(expression.TableName, columnName);
                    }
                }
                catch (ArgumentException)
                {
                    continue;
                }
            }

            InternalProcess(Generator.Generate(expression));
        }

        public override void Process(Expressions.CreateTableExpression expression)
        {
            truncator.Truncate(expression);
            LockTable(expression.TableName);
            RegisterTableCreation(expression.TableName);
            InternalProcess(Generator.Generate(expression));
            foreach (ColumnDefinition colDef in expression.Columns)
            {
                if (colDef.IsIdentity)
                    CreateSequenceForIdentity(expression.TableName, colDef.Name);
            }

            /*if(FBOptions.TransactionModel == FirebirdTransactionModel.AutoCommitOnCheckFail)
                CommitRetaining();*/
            if (FBOptions.TransactionModel != FirebirdTransactionModel.None)
                CommitRetaining();

        }

        public override void Process(Expressions.AlterTableExpression expression)
        {
            truncator.Truncate(expression);
            CheckTable(expression.TableName);
            LockTable(expression.TableName);
            InternalProcess(Generator.Generate(expression));
        }

        public override void Process(Expressions.RenameTableExpression expression)
        {
            truncator.Truncate(expression);
            FirebirdSchemaProvider schema = new FirebirdSchemaProvider(this, quoter);
            FirebirdTableDefinition firebirdTableDef = schema.GetTableDefinition(expression.OldName);
            firebirdTableDef.Name = expression.NewName;
            CreateTableExpression createNew = new CreateTableExpression()
            {
                TableName = expression.NewName,
                SchemaName = String.Empty
            };

            //copy column definitions (nb: avoid to copy key names, because in firebird they must be globally unique, so let it rename)
            firebirdTableDef.Columns.ToList().ForEach(x => createNew.Columns.Add(new ColumnDefinition()
            {
                Name = x.Name,
                DefaultValue = x.DefaultValue,
                IsForeignKey = x.IsForeignKey,
                IsIdentity = x.IsIdentity,
                IsIndexed = x.IsIndexed,
                IsNullable = x.IsNullable,
                IsPrimaryKey = x.IsPrimaryKey,
                IsUnique = x.IsUnique,
                ModificationType = x.ModificationType,
                Precision = x.Precision,
                Size = x.Size,
                Type = x.Type,
                CustomType = x.CustomType
            }));

            Process(createNew);

            int columnCount = firebirdTableDef.Columns.Count;
            string[] columns = firebirdTableDef.Columns.Select(x => x.Name).ToArray();
            InsertDataExpression data = new InsertDataExpression();
            data.TableName = firebirdTableDef.Name;
            data.SchemaName = firebirdTableDef.SchemaName;
            using (DataSet ds = ReadTableData(String.Empty, expression.OldName))
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    InsertionDataDefinition insert = new InsertionDataDefinition();
                    for (int i = 0; i < columnCount; i++)
                    {
                        insert.Add(new KeyValuePair<string, object>(columns[i], dr.ItemArray[i]));
                    }
                    data.Rows.Add(insert);
                }
            }
            Process(data);

            DeleteTableExpression delTable = new DeleteTableExpression()
            {
                TableName = expression.OldName,
                SchemaName = String.Empty
            };
            Process(delTable);
        }

        public override void Process(Expressions.DeleteTableExpression expression)
        {
            truncator.Truncate(expression);
            CheckTable(expression.TableName);
            LockTable(expression.TableName);
            InternalProcess(Generator.Generate(expression));
        }

        public override void Process(Expressions.AlterDefaultConstraintExpression expression)
        {
            truncator.Truncate(expression);
            CheckColumn(expression.TableName, expression.ColumnName);
            LockColumn(expression.TableName, expression.ColumnName);
            InternalProcess(Generator.Generate(expression));
        }

        public override void Process(Expressions.DeleteDefaultConstraintExpression expression)
        {
            truncator.Truncate(expression);
            CheckColumn(expression.TableName, expression.ColumnName);
            LockColumn(expression.TableName, expression.ColumnName);
            InternalProcess(Generator.Generate(expression));
        }

        public override void Process(CreateIndexExpression expression)
        {
            truncator.Truncate(expression);
            CheckTable(expression.Index.TableName);
            CheckColumn(expression.Index.TableName, expression.Index.Columns.Select(x => x.Name));
            InternalProcess(Generator.Generate(expression));
        }

        public override void Process(DeleteIndexExpression expression)
        {
            truncator.Truncate(expression);
            CheckTable(expression.Index.TableName);
            CheckColumn(expression.Index.TableName, expression.Index.Columns.Select(x => x.Name));
            InternalProcess(Generator.Generate(expression));
        }

        public override void Process(CreateSchemaExpression expression)
        {
            truncator.Truncate(expression);
            InternalProcess(Generator.Generate(expression));
        }

        public override void Process(AlterSchemaExpression expression)
        {
            truncator.Truncate(expression);
            InternalProcess(Generator.Generate(expression));
        }

        public override void Process(DeleteSchemaExpression expression)
        {
            truncator.Truncate(expression);
            InternalProcess(Generator.Generate(expression));
        }

        public override void Process(CreateConstraintExpression expression)
        {
            truncator.Truncate(expression);
            InternalProcess(Generator.Generate(expression));
        }

        public override void Process(DeleteConstraintExpression expression)
        {
            truncator.Truncate(expression);
            InternalProcess(Generator.Generate(expression));
        }

        public override void Process(CreateForeignKeyExpression expression)
        {
            truncator.Truncate(expression);
            InternalProcess(Generator.Generate(expression));
        }

        public override void Process(DeleteForeignKeyExpression expression)
        {
            truncator.Truncate(expression);
            InternalProcess(Generator.Generate(expression));
        }

        public override void Process(CreateSequenceExpression expression)
        {
            truncator.Truncate(expression);
            InternalProcess(Generator.Generate(expression));

            if (expression.Sequence.StartWith != null)
                InternalProcess((Generator as FirebirdGenerator).GenerateAlterSequence(expression.Sequence));

        }

        public override void Process(DeleteSequenceExpression expression)
        {
            truncator.Truncate(expression);
            InternalProcess(Generator.Generate(expression));
        }

        #endregion


        #region DML expressions


        public override void Process(Expressions.InsertDataExpression expression)
        {
            truncator.Truncate(expression);
            CheckTable(expression.TableName);
            expression.Rows.ForEach(x => x.ForEach(y => CheckColumn(expression.TableName, y.Key)));
            var subExpression = new InsertDataExpression() { SchemaName = expression.SchemaName, TableName = expression.TableName };
            foreach (var row in expression.Rows)
            {
                subExpression.Rows.Clear();
                subExpression.Rows.Add(row);
                InternalProcess(Generator.Generate(subExpression));
            }
        }

        public override void Process(Expressions.DeleteDataExpression expression)
        {
            truncator.Truncate(expression);
            CheckTable(expression.TableName);
            var subExpression = new DeleteDataExpression()
            {
                SchemaName = expression.SchemaName,
                TableName = expression.TableName,
                IsAllRows = expression.IsAllRows
            };
            if (expression.IsAllRows)
            {
                InternalProcess(Generator.Generate(expression));
            }
            else
            {
                foreach (var row in expression.Rows)
                {
                    subExpression.Rows.Clear();
                    subExpression.Rows.Add(row);
                    InternalProcess(Generator.Generate(subExpression));
                }
            }
        }

        public override void Process(Expressions.UpdateDataExpression expression)
        {
            truncator.Truncate(expression);
            CheckColumn(expression.TableName, expression.Set.Select(x => x.Key));
            InternalProcess(Generator.Generate(expression));
        }


        #endregion


        #region Generic expressions

        public override void Execute(string template, params object[] args)
        {
            Process(string.Format(template, args));
        }

        public override void Process(PerformDBOperationExpression expression)
        {
            Announcer.Say("Performing DB Operation");

            if (Options.PreviewOnly)
                return;

            EnsureConnectionIsOpen();

            if (expression.Operation != null)
            {
                expression.Operation(Connection, Transaction);

                if (FBOptions.TransactionModel == FirebirdTransactionModel.AutoCommit)
                    CommitRetaining();

            }
        }

        protected void InternalProcess(string sql)
        {
            Announcer.Sql(sql);

            if (Options.PreviewOnly || string.IsNullOrEmpty(sql))
                return;

            EnsureConnectionIsOpen();

            using (var command = Factory.CreateCommand(sql, Connection, Transaction, Options))
            {
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    using (var message = new StringWriter())
                    {
                        message.WriteLine("An error occurred executing the following sql:");
                        message.WriteLine(sql);
                        message.WriteLine("The error was {0}", ex.Message);

                        throw new Exception(message.ToString(), ex);
                    }
                }
            }

            if (FBOptions.TransactionModel == FirebirdTransactionModel.AutoCommit)
                CommitRetaining();

        }

        protected override void Process(string sql)
        {
            InternalProcess(sql);
        }

        #endregion


        #region Helpers

        private string FormatToSafeName(string sqlName)
        {
            if (quoter.IsQuoted(sqlName))
                return FormatHelper.FormatSqlEscape(quoter.UnQuote(sqlName));
            else
                return FormatHelper.FormatSqlEscape(sqlName).ToUpper();
        }

        private string GetSequenceName(string tableName, string columnName)
        {
            return truncator.Truncate(String.Format("gen_{0}_{1}", tableName, columnName));
        }

        private string GetIdentityTriggerName(string tableName, string columnName)
        {
            return truncator.Truncate(String.Format("gen_id_{0}_{1}", tableName, columnName));
        }

        private void CreateSequenceForIdentity(string tableName, string columnName)
        {
            CheckTable(tableName);
            LockTable(tableName);
            string sequenceName = GetSequenceName(tableName, columnName);
            if (!SequenceExists(String.Empty, sequenceName))
            {
                CreateSequenceExpression sequence = new CreateSequenceExpression()
                {
                    Sequence = new SequenceDefinition() { Name = sequenceName }
                };
                Process(sequence);
            }
            string triggerName = GetIdentityTriggerName(tableName, columnName);
            string quotedColumn = quoter.Quote(columnName);
            string trigger = String.Format("as begin if (NEW.{0} is NULL) then NEW.{1} = GEN_ID({2}, 1); end", quotedColumn, quotedColumn, quoter.QuoteSequenceName(sequenceName, string.Empty));

            PerformDBOperationExpression createTrigger = CreateTriggerExpression(tableName, triggerName, true, TriggerEvent.Insert, trigger);
            Process(createTrigger);
        }

        private void DeleteSequenceForIdentity(string tableName, string columnName)
        {
            CheckTable(tableName);
            LockTable(tableName);

            string sequenceName;
            try{
                sequenceName = GetSequenceName(tableName, columnName);
            }
            catch (ArgumentException)
            {
                return;
            }

            DeleteSequenceExpression deleteSequence = null;
            if (SequenceExists(String.Empty, sequenceName))
            {
                deleteSequence = new DeleteSequenceExpression() { SchemaName = String.Empty, SequenceName = sequenceName };
            }
            string triggerName = GetIdentityTriggerName(tableName, columnName);
            PerformDBOperationExpression deleteTrigger = DeleteTriggerExpression(tableName, triggerName);
            Process(deleteTrigger);

            if (deleteSequence != null)
                Process(deleteSequence);

        }

        public PerformDBOperationExpression CreateTriggerExpression(string tableName, TriggerInfo trigger)
        {
            return CreateTriggerExpression(tableName, trigger.Name, trigger.Before, trigger.Event, trigger.Body);
        }

        public PerformDBOperationExpression CreateTriggerExpression(string tableName, string triggerName, bool onBefore, TriggerEvent onEvent, string triggerBody)
        {
            tableName = truncator.Truncate(tableName);
            triggerName = truncator.Truncate(triggerName);
            CheckTable(tableName);
            LockTable(tableName);
            PerformDBOperationExpression createTrigger = new PerformDBOperationExpression();
            createTrigger.Operation = (connection, transaction) =>
            {
                string triggerSql = String.Format(@"CREATE TRIGGER {0} FOR {1} ACTIVE {2} {3} POSITION 0
                    {4}
                    ", quoter.Quote(triggerName), quoter.Quote(tableName),
                     onBefore ? "before" : "after",
                     onEvent.ToString().ToLower(),
                     triggerBody
                     );
                Announcer.Sql(triggerSql);
                using (var cmd = Factory.CreateCommand(triggerSql, connection, transaction, Options))
                {
                    cmd.ExecuteNonQuery();
                }
            };
            return createTrigger;
        }

        public PerformDBOperationExpression DeleteTriggerExpression(string tableName, string triggerName)
        {
            tableName = truncator.Truncate(tableName);
            triggerName = truncator.Truncate(triggerName);
            CheckTable(tableName);
            LockTable(tableName);
            PerformDBOperationExpression deleteTrigger = new PerformDBOperationExpression();
            deleteTrigger.Operation = (connection, transaction) =>
            {
                string triggerSql = String.Format("DROP TRIGGER {0}", quoter.Quote(triggerName));
                Announcer.Sql(triggerSql);
                using (var cmd = Factory.CreateCommand(triggerSql, connection, transaction, Options))
                {
                    cmd.ExecuteNonQuery();
                }
            };
            return deleteTrigger;
        }

        #endregion


    }
}
