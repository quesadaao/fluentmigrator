using System.Data;

using FluentMigrator.Expressions;
using FluentMigrator.Model;
using FluentMigrator.Runner.Generators.Hana;

using NUnit.Framework;
using NUnit.Should;

namespace FluentMigrator.Tests.Unit.Generators.Hana
{
    [TestFixture]
    [Category("Hana")]
    public class HanaGeneratorTests
    {
        protected HanaGenerator Generator;

        [SetUp]
        public void Setup()
        {
            Generator = new HanaGenerator();
        }

        [Test]
        public void CanCreateAutoIncrementColumnForInt64()
        {
            var expression = GeneratorTestHelper.GetCreateTableWithAutoIncrementExpression();
            expression.Columns[0].Type = DbType.Int64;

            var result = Generator.Generate(expression);
            result.ShouldBe("CREATE COLUMN TABLE \"TestTable1\" (\"TestColumn1\" BIGINT GENERATED ALWAYS AS IDENTITY, \"TestColumn2\" INTEGER);");
        }

        [Test]
        public void CanCreateTableWithBinaryColumnWithSize()
        {
            var expression = GeneratorTestHelper.GetCreateTableExpression();
            expression.Columns[0].Type = DbType.Binary;
            expression.Columns[0].Size = 10000;

            var result = Generator.Generate(expression);
            result.ShouldBe("CREATE COLUMN TABLE \"TestTable1\" (\"TestColumn1\" VARBINARY(10000), \"TestColumn2\" INTEGER);");
        }

        [Test]
        public void CanCreateTableWithBlobColumn()
        {
            var expression = GeneratorTestHelper.GetCreateTableExpression();
            expression.Columns[0].Type = DbType.Binary;

            var result = Generator.Generate(expression);
            result.ShouldBe("CREATE COLUMN TABLE \"TestTable1\" (\"TestColumn1\" BLOB, \"TestColumn2\" INTEGER);");
        }

        [Test]
        public void CanCreateTableWithBlobColumnWithObjectType()
        {
            var expression = GeneratorTestHelper.GetCreateTableExpression();
            expression.Columns[0].Type = DbType.Object;

            var result = Generator.Generate(expression);
            result.ShouldBe("CREATE COLUMN TABLE \"TestTable1\" (\"TestColumn1\" BLOB, \"TestColumn2\" INTEGER);");
        }

        [Test]
        public void CanCreateTableWithBoolDefaultValue()
        {
            var expression = GeneratorTestHelper.GetCreateTableExpression();
            expression.Columns[0].DefaultValue = true;

            var result = Generator.Generate(expression);
            result.ShouldBe("CREATE COLUMN TABLE \"TestTable1\" (\"TestColumn1\" NVARCHAR(255) DEFAULT 1, \"TestColumn2\" INTEGER);");
        }

        [Test]
        public void CanUseSystemMethodCurrentUTCDateTimeAsADefaultValueForAColumn()
        {
            const string tableName = "NewTable";
            var columnDefinition = new ColumnDefinition { Name = "NewColumn", Size = 5, Type = DbType.String, DefaultValue = SystemMethods.CurrentUTCDateTime };
            var expression = new CreateColumnExpression { Column = columnDefinition, TableName = tableName };

            var result = Generator.Generate(expression);

            result.ShouldBe("ALTER TABLE \"NewTable\" ADD (\"NewColumn\" NVARCHAR(5) DEFAULT CURRENT_UTCTIMESTAMP);");
        }

        [Test]
        public void CanUseSystemMethodCurrentCDateTimeAsADefaultValueForAColumn()
        {
            const string tableName = "NewTable";
            var columnDefinition = new ColumnDefinition { Name = "NewColumn", Size = 5, Type = DbType.String, DefaultValue = SystemMethods.CurrentDateTime };
            var expression = new CreateColumnExpression { Column = columnDefinition, TableName = tableName };

            var result = Generator.Generate(expression);

            result.ShouldBe("ALTER TABLE \"NewTable\" ADD (\"NewColumn\" NVARCHAR(5) DEFAULT CURRENT_TIMESTAMP);");
        }

        [Test]
        public void CanAlterColumnAndSetAsNullable()
        {
            var expression = new AlterColumnExpression
            {
                Column = new ColumnDefinition { Type = DbType.String, Name = "TestColumn1", IsNullable = true },
                TableName = "TestTable1"
            };

            var result = Generator.Generate(expression);

            result.ShouldBe("ALTER TABLE \"TestTable1\" ALTER (\"TestColumn1\" NVARCHAR(255) NULL);");
        }

        [Test]
        public void CanAlterColumnAndSetAsNotNullable()
        {
            var expression = new AlterColumnExpression
            {
                Column = new ColumnDefinition { Type = DbType.String, Name = "TestColumn1", IsNullable = false },
                SchemaName = "TestSchema",
                TableName = "TestTable1"
            };

            var result = Generator.Generate(expression);
            result.ShouldBe("ALTER TABLE \"TestTable1\" ALTER (\"TestColumn1\" NVARCHAR(255) NOT NULL);");
        }
    }
}
