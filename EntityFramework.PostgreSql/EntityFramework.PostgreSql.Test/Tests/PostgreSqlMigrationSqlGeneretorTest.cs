using System;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.Migrations.Sql;
using System.Data.Entity.Spatial;
using System.Globalization;
using System.Threading;
using EntityFramework.PostgreSql.Utilities;
using NUnit.Framework;

namespace EntityFramework.PostgreSql.Test.Tests
{

    [TestFixture]
    public class PostgreSqlMigrationSqlGeneretorTest
    {

        [TestCase(null, "", "NOT NULL")]
        [TestCase(false, "SET NOT NULL", "DROP NOT NULL")]
        [TestCase(true, "DROP NOT NULL", "SET NOT NULL")]
        public void GenerateAlterDecimalColumn(bool? nullable, string contain, string notContain)
        {

            var migrationProvider = new PostgreSqlMigrationSqlGeneratorNew();

            var column = new ColumnModel(PrimitiveTypeKind.Decimal)
            {
                Name = "C",
                IsNullable = nullable
            };

            var alterColumnOperation = new AlterColumnOperation("dbo.T", column, false);

            var sql = migrationProvider.Generate(new[] { alterColumnOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            const string returnSqlBase = "ALTER TABLE \"dbo\".\"T\" ALTER COLUMN \"C\" ";

            Assert.IsNotNull(sql);
            Assert.True(sql.Contains(returnSqlBase));
            Assert.True(sql.Contains(returnSqlBase + "TYPE decimal"));
            Assert.True(sql.Contains(returnSqlBase + contain));
            Assert.False(sql.Contains(returnSqlBase + notContain));

        }


        [TestCase(null, "", "NOT NULL")]
        [TestCase(false, "SET NOT NULL", "DROP NOT NULL")]
        [TestCase(true, "DROP NOT NULL", "SET NOT NULL")]
        public void GenerateAlterColumn(bool? nullable, string contain, string notContain)
        {

            var migrationProvider = new PostgreSqlMigrationSqlGeneratorNew();

            var column = new ColumnModel(PrimitiveTypeKind.String)
                {
                    Name = "C",
                    MaxLength = 120,
                    IsNullable = nullable
                };

            var alterColumnOperation = new AlterColumnOperation("dbo.T", column, false);

            var sql = migrationProvider.Generate(new[] { alterColumnOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            const string returnSqlBase = "ALTER TABLE \"dbo\".\"T\" ALTER COLUMN \"C\" ";

            Assert.IsNotNull(sql);
            Assert.True(sql.Contains(returnSqlBase));
            Assert.True(sql.Contains(returnSqlBase + "TYPE varchar(120)"));
            Assert.True(sql.Contains(returnSqlBase + contain));
            Assert.False(sql.Contains(returnSqlBase + notContain));

        }

        [Test]
        public void GenerateShouldOutputInvariantDecimalsWhenNonInvariantCulture()
        {
            var migrationProvider = new PostgreSqlMigrationSqlGeneratorNew();

            var addColumnOperation
                = new AddColumnOperation(
                    "T",
                    new ColumnModel(PrimitiveTypeKind.Binary)
                    {
                        Name = "C",
                        DefaultValue = 123.45m
                    });

            var lastCulture = Thread.CurrentThread.CurrentCulture;

            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");

                var sql = migrationProvider.Generate(new[] { addColumnOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

                Assert.True(sql.Contains("ALTER TABLE \"T\" ADD \"C\" bytea DEFAULT 123,45"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = lastCulture;
            }
        }

        [Test]
        public void GenerateCanOutputAddTimestampColumnOperation()
        {
            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var addColumnOperation
                = new AddColumnOperation(
                    "T",
                    new ColumnModel(PrimitiveTypeKind.Binary)
                    {
                        IsNullable = false,
                        Name = "C",
                        IsTimestamp = true
                    });

            var sql = migrationSqlGenerator.Generate(new[] { addColumnOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.True(sql.Contains("ALTER TABLE \"T\" ADD \"C\" bytea NOT NULL"));
        }

        [Test]
        public void GenerateCanOutputAddTimestampStoreTypeColumnOperation()
        {
            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var addColumnOperation
                = new AddColumnOperation(
                    "T",
                    new ColumnModel(PrimitiveTypeKind.Binary)
                    {
                        IsNullable = false,
                        Name = "C",
                        StoreType = "timestamp"
                    });

            var sql = migrationSqlGenerator.Generate(new[] { addColumnOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.True(sql.Contains("ALTER TABLE \"T\" ADD \"C\" timestamp NOT NULL"));
        }

        [Test]
        public void GenerateCanOutputDropIndexOperation()
        {
            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var dropIndexOperation = new DropIndexOperation
            {
                Table = "dbo.Custumers"
            };

            dropIndexOperation.Columns.Add("Id");

            var sql = migrationSqlGenerator.Generate(new[] { dropIndexOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.True(sql.Contains("DROP INDEX \"dbo\".\"IX_dbo.Custumers_Id\""));
        }

        [Test]
        public void GenerateCanOutputDropPrimaryKeyOperation()
        {
            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var dropPrimaryKeyOperation = new DropPrimaryKeyOperation
            {
                Table = "T"
            };

            var sql = migrationSqlGenerator.Generate(new[] { dropPrimaryKeyOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.True(sql.Contains("ALTER TABLE \"T\" DROP CONSTRAINT \"PK_T\""));
        }

        [Test]
        public void GenerateCanOutputAddPrimaryKeyOperation()
        {
            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var addPrimaryKeyOperation = new AddPrimaryKeyOperation
            {
                Table = "T"
            };

            addPrimaryKeyOperation.Columns.Add("c1");
            addPrimaryKeyOperation.Columns.Add("c2");

            var sql = migrationSqlGenerator.Generate(new[] { addPrimaryKeyOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.True(sql.Contains("ALTER TABLE \"T\" ADD CONSTRAINT \"PK_T\" PRIMARY KEY (\"c1\", \"c2\")"));
        }

        [Test]
        public void GenerateCanOutputDropColumn()
        {
            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var dropColumnOperation = new DropColumnOperation("Customers", "Foo");

            var sql = migrationSqlGenerator.Generate(new[] { dropColumnOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.True(sql.Contains("ALTER TABLE \"Customers\" DROP COLUMN \"Foo\""));
        }

        [Test]
        public void GenerateCanOutputTimestampColumn()
        {
            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var createTableOperation = new CreateTableOperation("Customers");
            var column = new ColumnModel(PrimitiveTypeKind.Binary)
            {
                Name = "Version",
                IsTimestamp = true
            };
            createTableOperation.Columns.Add(column);

            var sql = migrationSqlGenerator.Generate(new[] { createTableOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.True(sql.Contains("\"Version\" bytea"));
        }

        [Test]
        public void GenerateCanOutputCustomSqlOperation()
        {
            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var sql = migrationSqlGenerator.Generate(new[] { new SqlOperation("insert into foo") }, "9.2").Join(
                s => s.Sql, Environment.NewLine);

            Assert.True(sql.Contains(@"insert into foo"));
        }

        [Test]
        public void GenerateCanOutputCreateTableStatement()
        {
            var createTableOperation = new CreateTableOperation("dbo.Customers");
            var idColumn = new ColumnModel(PrimitiveTypeKind.Int32)
            {
                Name = "Id",
                IsNullable = true,
                IsIdentity = true
            };
            createTableOperation.Columns.Add(idColumn);
            createTableOperation.Columns.Add(
                new ColumnModel(PrimitiveTypeKind.String)
                {
                    Name = "Name",
                    IsNullable = false
                });
            createTableOperation.PrimaryKey = new AddPrimaryKeyOperation();
            createTableOperation.PrimaryKey.Columns.Add(idColumn.Name);

            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var sql = migrationSqlGenerator.Generate(new[] { createTableOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.True(
                sql.Contains("CREATE TABLE \"dbo\".\"Customers\" (\r\n    \"Id\" serial,\r\n    \"Name\" text NOT NULL,\r\n    CONSTRAINT \"PK_dbo.Customers\" PRIMARY KEY (\"Id\")\r\n)"));
        }

        [Test]
        public void GenerateCanOutputCreateIndexStatement()
        {
            var createTableOperation = new CreateTableOperation("Customers");
            var idColumn = new ColumnModel(PrimitiveTypeKind.Int32)
            {
                Name = "Id",
                IsNullable = true,
                IsIdentity = true
            };
            createTableOperation.Columns.Add(idColumn);
            createTableOperation.Columns.Add(
                new ColumnModel(PrimitiveTypeKind.String)
                {
                    Name = "Name",
                    IsNullable = false
                });
            createTableOperation.PrimaryKey = new AddPrimaryKeyOperation();
            createTableOperation.PrimaryKey.Columns.Add(idColumn.Name);

            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var createIndexOperation = new CreateIndexOperation
            {
                Table = createTableOperation.Name,
                IsUnique = true
            };

            createIndexOperation.Columns.Add(idColumn.Name);

            var sql
                = migrationSqlGenerator.Generate(
                    new[]
                        {
                            createIndexOperation
                        },
                    "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.True(
                sql.Contains(
                    @"CREATE UNIQUE INDEX ""IX_Customers_Id"" ON ""Customers""(""Id"")"));
        }

        [Test]
        public void GenerateCanOutputAddFkStatement()
        {
            var addForeignKeyOperation = new AddForeignKeyOperation
            {
                PrincipalTable = "Customers",
                DependentTable = "Orders",
                CascadeDelete = true
            };
            addForeignKeyOperation.PrincipalColumns.Add("CustomerId");
            addForeignKeyOperation.DependentColumns.Add("CustomerId");

            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var sql = migrationSqlGenerator.Generate(new[] { addForeignKeyOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.True(
                sql.Contains(
                    "ALTER TABLE \"Orders\" ADD CONSTRAINT \"FK_Orders_Customers_CustomerId\" FOREIGN KEY (\"CustomerId\") REFERENCES \"Customers\" (\"CustomerId\") ON DELETE CASCADE"));
        }

        [Test]
        public void GenerateCanOutputDropTableStatement()
        {
            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var sql = migrationSqlGenerator.Generate(new[] { new DropTableOperation("dbo.Customers") }, "9.2").Join(
                s => s.Sql, Environment.NewLine);

            Assert.True(sql.Contains("DROP TABLE \"dbo\".\"Customers\""));
        }
        /*
        [Test]
        public void GenerateCanOutputDeleteHistoryOperation()
        {
            var migrationSqlGenerator = new PostgreSqlMigrationSqlGenerator();

            var sql = migrationSqlGenerator.Generate(new[] { new DeleteHistoryOperation("__MigrationHistory", "201212120818356_AddHouses") }, "9.2").Join(
                s => s.Sql, Environment.NewLine);

            Assert.True(sql.Contains("DELETE FROM \"dbo\".\"__MigrationHistory\" WHERE \"MigrationId\" = '201212120818356_AddHouses'"));
        }
        */
        [TestCase(PrimitiveTypeKind.Guid, "uuid")]
        [TestCase(PrimitiveTypeKind.Int16, "smallserial")]
        [TestCase(PrimitiveTypeKind.Int32, "serial")]
        [TestCase(PrimitiveTypeKind.Int64, "bigserial")]
        [TestCase(PrimitiveTypeKind.String, "text")]
        public void GenerateCanOutputAddColumnStatement(PrimitiveTypeKind type, string typeName)
        {
            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var column = new ColumnModel(type)
            {
                Name = "Bar",
                IsIdentity = true
            };
            var addColumnOperation = new AddColumnOperation("Foo", column);

            var sql = migrationSqlGenerator.Generate(
                new[] { addColumnOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.True(sql.Contains(string.Format("ALTER TABLE \"Foo\" ADD \"Bar\" {0}", typeName)));
        }
        
        [Test]
        public void GenerateCanOutputAddColumnStatementWithCustomStoreType()
        {
            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var column = new ColumnModel(PrimitiveTypeKind.String)
            {
                Name = "Bar",
                StoreType = "character",
                MaxLength = 15
            };
            var addColumnOperation = new AddColumnOperation("Foo", column);

            var sql = migrationSqlGenerator.Generate(new[] { addColumnOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.True(sql.Contains("ALTER TABLE \"Foo\" ADD \"Bar\" character(15)"));
        }

        [Test]
        public void GenerateCanOutputAddGeometryColumnOperationWithDefaultValue()
        {
            var operation
                = new AddColumnOperation(
                    "T",
                    new ColumnModel(PrimitiveTypeKind.Geometry)
                    {
                        IsNullable = false,
                        Name = "C",
                        DefaultValue = DbGeometry.FromText("POINT (8 9)")
                    });

            var sql = new PostgreSqlMigrationSqlGeneratorNew().Generate(new[] { operation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.AreEqual("ALTER TABLE \"T\" ADD \"C\" point NOT NULL DEFAULT 'SRID=0;POINT (8 9)'", sql);
        }

        [Test]
        public void GenerateCanOutputAddGeometryColumnOperationWithImplicitDefaultValue()
        {
            var operation
                = new AddColumnOperation(
                    "T",
                    new ColumnModel(PrimitiveTypeKind.Geometry)
                    {
                        IsNullable = false,
                        Name = "C"
                    });

            var sql = new PostgreSqlMigrationSqlGeneratorNew().Generate(new[] { operation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.AreEqual("ALTER TABLE \"T\" ADD \"C\" point NOT NULL DEFAULT 'SRID=0;POINT (0 0)'", sql);
        }

        [Test]
        public void GenerateCanOutputAddGeometryColumnOperationWithSqlDefaultValue()
        {
            var operation
                = new AddColumnOperation(
                    "T",
                    new ColumnModel(PrimitiveTypeKind.Geometry)
                    {
                        IsNullable = false,
                        Name = "C",
                        DefaultValueSql = "'POINT (8 9)'"
                    });

            var sql = new PostgreSqlMigrationSqlGeneratorNew().Generate(new[] { operation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.AreEqual("ALTER TABLE \"T\" ADD \"C\" point NOT NULL DEFAULT 'POINT (8 9)'", sql);
        }

        [Test]
        public void GenerateCanOutputAlterGeometryColumnOperationWithNoDefaultValue()
        {
            var operation
                = new AlterColumnOperation(
                    "T",
                    new ColumnModel(PrimitiveTypeKind.Geometry)
                    {
                        IsNullable = false,
                        Name = "C",
                    },
                    isDestructiveChange: false);

            var sql = new PostgreSqlMigrationSqlGeneratorNew().Generate(new[] { operation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.AreEqual(
               "ALTER TABLE \"T\" ALTER COLUMN \"C\" TYPE point;ALTER TABLE \"T\" ALTER COLUMN \"C\" SET NOT NULL;", sql);
        }

        [Test]
        public void GenerateCanOutputAddColumnStatementWithExplicitDefaultValue()
        {
            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var column = new ColumnModel(PrimitiveTypeKind.Guid)
            {
                Name = "Bar",
                IsNullable = false,
                DefaultValue = 42
            };
            var addColumnOperation = new AddColumnOperation("Foo", column);

            var sql = migrationSqlGenerator.Generate(new[] { addColumnOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.True(sql.Contains("ALTER TABLE \"Foo\" ADD \"Bar\" uuid NOT NULL DEFAULT 42"));
        }

        [Test]
        public void GenerateCanOutputAddColumnStatementWithExplicitDefaultValueSql()
        {
            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var column = new ColumnModel(PrimitiveTypeKind.Guid)
            {
                Name = "Bar",
                IsNullable = false,
                DefaultValueSql = "42"
            };
            var addColumnOperation = new AddColumnOperation("Foo", column);

            var sql = migrationSqlGenerator.Generate(new[] { addColumnOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.True(sql.Contains("ALTER TABLE \"Foo\" ADD \"Bar\" uuid NOT NULL DEFAULT 42"));
        }

        [Test]
        public void GenerateCanOutputAddColumnStatementWhenNonNullableAndNoDefaultProvided()
        {
            var migrationSqlGenerator = new PostgreSqlMigrationSqlGeneratorNew();

            var column = new ColumnModel(PrimitiveTypeKind.Int32)
            {
                Name = "Bar",
                IsNullable = false
            };
            var addColumnOperation = new AddColumnOperation("Foo", column);

            var sql = migrationSqlGenerator.Generate(new[] { addColumnOperation }, "9.2").Join(s => s.Sql, Environment.NewLine);

            Assert.True(sql.Contains("ALTER TABLE \"Foo\" ADD \"Bar\" integer NOT NULL DEFAULT 0"));
        }
    }
}
