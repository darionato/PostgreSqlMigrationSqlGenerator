using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Migrations.Utilities;
using System.Data.Metadata.Edm;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Data.Entity.Migrations.Model;
using Badlydone.Utilities;
using Npgsql;

// ReSharper disable CheckNamespace
namespace System.Data.Entity.Migrations.Sql
// ReSharper restore CheckNamespace
{
    public class PostgreSqlMigrationSqlGenerator : SqlServerMigrationSqlGenerator
    {

        private readonly HashSet<string> _generatedSchemas;

        public PostgreSqlMigrationSqlGenerator()
        {

            _generatedSchemas = new HashSet<string>();

        }

        protected override void Generate(DeleteHistoryOperation deleteHistoryOperation)
        {

            Contract.Requires(deleteHistoryOperation != null);

            using (var writer = Writer())
            {

                writer.Write("DELETE FROM \"dbo\".");

                writer.Write(Name(deleteHistoryOperation.Table));

                writer.Write(" WHERE ");

                writer.Write("{0} = ", Quote("MigrationId"));

                writer.Write(Generate(deleteHistoryOperation.MigrationId));


                Statement(writer);

            }

        }

        /// <summary>
        /// Must implement this method
        /// </summary>
        /// <param name="insertHistoryOperation"></param>
        protected override void Generate(InsertHistoryOperation insertHistoryOperation)
        {

            Contract.Requires(insertHistoryOperation != null);

            using (var writer = Writer())
            {

                writer.Write("INSERT INTO \"dbo\".");

                writer.Write(Name(insertHistoryOperation.Table));

                writer.Write(" ({0}, {1}, {2})", Quote("MigrationId"), Quote("Model"), Quote("ProductVersion"));

                writer.Write(" VALUES (");

                writer.Write(Generate(insertHistoryOperation.MigrationId));

                writer.Write(", ");

                writer.Write(Generate(insertHistoryOperation.Model));

                writer.Write(", ");

                writer.Write(Generate(insertHistoryOperation.ProductVersion));

                writer.Write(")");


                Statement(writer);

            }

        }

        protected override string Generate(bool defaultValue)
        {
            return (defaultValue ? "TRUE" : "FALSE");
        }

        protected override string Generate(byte[] defaultValue)
        {
            Contract.Requires(defaultValue != null);

            return "decode('" + defaultValue.ToHexString() + "', 'hex')";

        }

        protected override string Quote(string identifier)
        {
            return "\"" + identifier + "\"";
        }

        protected override void GenerateCreateSchema(string schema)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));

            // till EF 5, connection string is not available here
            // so the schema should be created manually
            // in Postgresql I have to execute a query to check if the schema already exists
            // because a function like "schema_id()" not exists

            /*
            using (var conn = CreateConnection())
            {

                
                conn.ConnectionString = 
                    @"Server=127.0.0.1;Port=5432;Database=ne;User Id=postgres;Password=Malfatti;CommandTimeout=20;Preload Reader = true;";

                conn.Open();

                using (var command = new NpgsqlCommand(
                    string.Format(
                        "SELECT schema_name FROM information_schema.schemata WHERE schema_name = '{0}';",
                        schema), (NpgsqlConnection) conn))
                {

                    var reader = command.ExecuteReader();

                    // if I have record, the schema exists, so I exit
                    var hasRows = reader.HasRows;

                    reader.Close();

                    if (hasRows) return;

                }

            }

            using (var writer = Writer())
            {

                writer.Write("CREATE SCHEMA ");
                writer.Write(Quote(schema));

                Statement(writer);

            }
             */

        }

        protected override void Generate(DropTableOperation dropTableOperation)
        {
            Contract.Requires(dropTableOperation != null);

            using (var writer = Writer())
            {
                writer.Write("DROP TABLE ");
                writer.Write(Name(dropTableOperation.Name));

                Statement(writer);
            }
        }

        protected override void Generate(DropPrimaryKeyOperation dropPrimaryKeyOperation)
        {
            Contract.Requires(dropPrimaryKeyOperation != null);

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(dropPrimaryKeyOperation.Table));
                writer.Write(" DROP CONSTRAINT ");
                writer.Write(Quote(dropPrimaryKeyOperation.Name));

                Statement(writer);
            }
        }

        protected override void Generate(DropIndexOperation dropIndexOperation)
        {
            Contract.Requires(dropIndexOperation != null);

            using (var writer = Writer())
            {
                writer.Write("DROP INDEX ");
                writer.Write(IndexName(dropIndexOperation, true));

                Statement(writer);
            }
        }

        protected override void Generate(DropForeignKeyOperation dropForeignKeyOperation)
        {
            Contract.Requires(dropForeignKeyOperation != null);

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(dropForeignKeyOperation.DependentTable));
                writer.Write(" DROP CONSTRAINT ");
                writer.Write(Quote(dropForeignKeyOperation.Name));

                Statement(writer);
            }
        }

        protected override void Generate(DropColumnOperation dropColumnOperation)
        {
            Contract.Requires(dropColumnOperation != null);

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(dropColumnOperation.Table));
                writer.Write(" DROP COLUMN ");
                writer.Write(Name(dropColumnOperation.Name));

                Statement(writer);
            }
        }

        protected override void Generate(CreateTableOperation createTableOperation)
        {
            Contract.Requires(createTableOperation != null);

            var databaseName = createTableOperation.Name.ToDatabaseName();

            // create the schema if needed
            if (!string.IsNullOrWhiteSpace(databaseName.Schema))

                if (!_generatedSchemas.Contains(databaseName.Schema))
                {

                    GenerateCreateSchema(databaseName.Schema);

                    _generatedSchemas.Add(databaseName.Schema);

                }

            using (var writer = Writer())
            {
                WriteCreateTable(createTableOperation, writer);

                Statement(writer);
            }
        }

        protected override void Generate(CreateIndexOperation createIndexOperation)
        {
            Contract.Requires(createIndexOperation != null);

            using (var writer = Writer())
            {
                writer.Write("CREATE ");

                if (createIndexOperation.IsUnique)
                {
                    writer.Write("UNIQUE ");
                }

                writer.Write("INDEX ");
                writer.Write(IndexName(createIndexOperation, false));
                writer.Write(" ON ");
                writer.Write(Name(createIndexOperation.Table));
                writer.Write("(");
                writer.Write(createIndexOperation.Columns.Join(Quote));
                writer.Write(")");

                Statement(writer);
            }
        }

        protected override void Generate(AlterColumnOperation alterColumnOperation)
        {
            using (var writer = Writer())
            {

                // create initial SQL
                var sql = string.Format("ALTER TABLE {0} ALTER COLUMN ",
                                        Name(alterColumnOperation.Table));

                writer.Write(sql);

                // generate the column name and type part
                var column = alterColumnOperation.Column;

                Generate(column, writer, true);

                // end the column type
                writer.Write(";");


                if (column.IsNullable != null)
                {

                    // create a new row to set nullable
                    writer.Write(sql);

                    writer.Write(Quote(column.Name));

                    writer.Write(" {0} NOT NULL;", column.IsNullable == true ? "DROP" : "SET" );

                }

                Statement(writer);
            }
        }

        protected override void Generate(AddPrimaryKeyOperation addPrimaryKeyOperation)
        {
            Contract.Requires(addPrimaryKeyOperation != null);

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(addPrimaryKeyOperation.Table));
                writer.Write(" ADD CONSTRAINT ");
                writer.Write(Quote(addPrimaryKeyOperation.Name));
                writer.Write(" PRIMARY KEY (");
                writer.Write(addPrimaryKeyOperation.Columns.Select(Quote).Join());
                writer.Write(")");

                Statement(writer);
            }
        }

        protected override void Generate(AddForeignKeyOperation addForeignKeyOperation)
        {
            Contract.Requires(addForeignKeyOperation != null);

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(addForeignKeyOperation.DependentTable));
                writer.Write(" ADD CONSTRAINT ");
                writer.Write(Quote(addForeignKeyOperation.Name));
                writer.Write(" FOREIGN KEY (");
                writer.Write(addForeignKeyOperation.DependentColumns.Select(Quote).Join());
                writer.Write(") REFERENCES ");
                writer.Write(Name(addForeignKeyOperation.PrincipalTable));
                writer.Write(" (");
                writer.Write(addForeignKeyOperation.PrincipalColumns.Select(Quote).Join());
                writer.Write(")");

                if (addForeignKeyOperation.CascadeDelete)
                {
                    writer.Write(" ON DELETE CASCADE");
                }

                Statement(writer);
            }
        }

        protected override void Generate(AddColumnOperation addColumnOperation)
        {
            Contract.Requires(addColumnOperation != null);

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(addColumnOperation.Table));
                writer.Write(" ADD ");

                var column = addColumnOperation.Column;

                Generate(column, writer);

                if ((column.IsNullable != null)
                    && !column.IsNullable.Value
                    && (column.DefaultValue == null)
                    && (string.IsNullOrWhiteSpace(column.DefaultValueSql))
                    && !column.IsIdentity
                    && !column.IsTimestamp
                    && !column.StoreType.EqualsIgnoreCase("rowversion")
                    && !column.StoreType.EqualsIgnoreCase("timestamp"))
                {
                    writer.Write(" DEFAULT ");

                    if (column.Type
                        == PrimitiveTypeKind.DateTime)
                    {
                        writer.Write(Generate(DateTime.Parse("1900-01-01 00:00:00", CultureInfo.InvariantCulture)));
                    }
                    else
                    {
                        writer.Write(Generate((dynamic)column.ClrDefaultValue));
                    }
                }

                Statement(writer);
            }
        }

        private void Generate(ColumnModel column, IndentedTextWriter writer, bool isAlter = false)
        {
            Contract.Requires(column != null);
            Contract.Requires(writer != null);

            writer.Write(Quote(column.Name));
            writer.Write(" {0}", isAlter ? "TYPE " : "");
            writer.Write(BuildColumnType(column));


            // check if it has a max length
            if (column.MaxLength != null && column.MaxLength > 0)

                writer.Write("({0})", column.MaxLength);


            // check if it's nullable
            if ((column.IsNullable != null)
                && !column.IsNullable.Value
                && isAlter == false)
            
                writer.Write(" NOT NULL");


            // check if it has a default value
            if (column.DefaultValue != null)
            {
                writer.Write(" DEFAULT ");
                writer.Write(Generate((dynamic)column.DefaultValue));
            }
            else if (!string.IsNullOrWhiteSpace(column.DefaultValueSql))
            {
                writer.Write(" DEFAULT ");
                writer.Write(column.DefaultValueSql);
            }
            
        }

        protected override DbConnection CreateConnection()
        {
            return new NpgsqlConnection();
        }

        protected virtual string IndexName(IndexOperation index, bool withSchema)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(index.Table));
            Contract.Requires(!(!index.Columns.Any()));

            var databaseName = index.Table.ToDatabaseName();


            var name = new List<string>();

            // check if I've to add the schema name before the index name
            // needed during drop operation
            if (withSchema)

                name.Add(databaseName.Schema);

            name.Add(string.Format(CultureInfo.InvariantCulture, "IX_{0}_{1}",
                                  index.Table,
                                  index.Columns.Join(separator: "_")).RestrictTo(128));

            return name.Join(Quote, ".");

        }

        private void WriteCreateTable(CreateTableOperation createTableOperation, IndentedTextWriter writer)
        {
            Contract.Requires(createTableOperation != null);
            Contract.Requires(writer != null);

            writer.WriteLine("CREATE TABLE " + Name(createTableOperation.Name) + " (");
            writer.Indent++;

            var columnCount = createTableOperation.Columns.Count();

            createTableOperation.Columns.Each(
                (c, i) =>
                {
                    Generate(c, writer);

                    if (i < columnCount - 1)
                    {
                        writer.WriteLine(",");
                    }
                });

            if (createTableOperation.PrimaryKey != null)
            {
                writer.WriteLine(",");
                writer.Write("CONSTRAINT ");
                writer.Write(Quote(createTableOperation.PrimaryKey.Name));
                writer.Write(" PRIMARY KEY (");
                writer.Write(createTableOperation.PrimaryKey.Columns.Join(Quote));
                writer.WriteLine(")");
            }
            else
            {
                writer.WriteLine();
            }

            writer.Indent--;
            writer.Write(")");

        }

        protected override string BuildColumnType(ColumnModel column)
        {
            Contract.Requires(column != null);

            // if the type is already set, I return it
            if (String.IsNullOrWhiteSpace(column.StoreType) == false)

                return column.StoreType;


            // handle the others cases
            switch (column.Type)
            {
                case PrimitiveTypeKind.Binary:
                    return "bytea";
                case PrimitiveTypeKind.Boolean:
                    return "boolean";
                case PrimitiveTypeKind.Byte:
                    return "smallint";
                case PrimitiveTypeKind.DateTime:
                    return "timestamp";
                case PrimitiveTypeKind.Decimal:
                    return "decimal";
                case PrimitiveTypeKind.Double:
                    return "float8";
                case PrimitiveTypeKind.Single:
                    return "real";
                case PrimitiveTypeKind.Int16:
                    return (column.IsIdentity ? "smallserial" : "smallint");
                case PrimitiveTypeKind.Int32:
                    return (column.IsIdentity ? "serial" : "integer");
                case PrimitiveTypeKind.Int64:
                    return (column.IsIdentity ? "bigserial" : "bigint");
                case PrimitiveTypeKind.String:
                    return (column.MaxLength != null && column.MaxLength > 0 ?
                        "varchar" :
                        "text");
                case PrimitiveTypeKind.Time:
                    return "time";
                case PrimitiveTypeKind.Guid:
                    return "uuid";
                case PrimitiveTypeKind.Geometry:
                    return "point";
                default:
                    return "";
            }
        }

    }
}
