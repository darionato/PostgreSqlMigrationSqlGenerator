using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Migrations.History;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.Migrations.Utilities;
using System.Data.Entity.Spatial;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EntityFramework.PostgreSql.Utilities;

// ReSharper disable CheckNamespace
namespace System.Data.Entity.Migrations.Sql
// ReSharper restore CheckNamespace
{
    public class PostgreSqlMigrationSqlGeneratorNew : MigrationSqlGenerator
    {

        private const string BatchTerminator = "GO";

        internal const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffK";

        private IList<MigrationStatement> _statements;
        private HashSet<string> _generatedSchemas;
        private string _providerManifestToken;

        public override IEnumerable<MigrationStatement> Generate(IEnumerable<MigrationOperation> migrationOperations, string providerManifestToken)
        {

            Check.NotNull(migrationOperations, "migrationOperations");
            Check.NotNull(providerManifestToken, "providerManifestToken");

            _statements = new List<MigrationStatement>();
            _generatedSchemas = new HashSet<string>();

            GenerateStatements(migrationOperations);

            return _statements;

        }

        private void GenerateStatements(IEnumerable<MigrationOperation> migrationOperations)
        {
            Check.NotNull(migrationOperations, "migrationOperations");

            DetectHistoryRebuild(migrationOperations).Each<dynamic>(o => Generate(o));
        }

        private static IEnumerable<MigrationOperation> DetectHistoryRebuild(
            IEnumerable<MigrationOperation> operations)
        {
            DebugCheck.NotNull(operations);

            var enumerator = operations.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var sequence = HistoryRebuildOperationSequence.Detect(enumerator);

                yield return sequence ?? enumerator.Current;
            }
        }

        private void Generate(AlterColumnOperation migration)
        {


            using (var writer = Writer())
            {

                // create initial SQL
                var sql = string.Format("ALTER TABLE {0} ALTER COLUMN ",
                                        Name(migration.Table));

                writer.Write(sql);

                // generate the column name and type part
                var column = migration.Column;

                Generate(column, writer, true);

                // end the column type
                writer.Write(";");


                if (column.IsNullable != null)
                {

                    // create a new row to set nullable
                    writer.Write(sql);

                    writer.Write(Quote(column.Name));

                    writer.Write(" {0} NOT NULL;", column.IsNullable == true ? "DROP" : "SET");

                }

                Statement(writer);
            }
        }


        private void Generate(DropColumnOperation dropColumnOperation)
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


        private void Generate(ColumnModel column, TextWriter writer, bool isAlter = false)
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


        private void Generate(AddColumnOperation addColumnOperation)
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

                    if (column.Type == PrimitiveTypeKind.DateTime)
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


        private void Generate(CreateIndexOperation createIndexOperation)
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


        protected virtual void Generate(SqlOperation sqlOperation)
        {
            Check.NotNull(sqlOperation, "sqlOperation");

            StatementBatch(sqlOperation.Sql, sqlOperation.SuppressTransaction);
        }


        protected void StatementBatch(string sqlBatch, bool suppressTransaction = false)
        {
            Check.NotNull(sqlBatch, "sqlBatch");

            // Handle backslash utility statement (see http://technet.microsoft.com/en-us/library/dd207007.aspx)
            sqlBatch = Regex.Replace(sqlBatch, @"\\(\r\n|\r|\n)", "");

            // Handle batch splitting utility statement (see http://technet.microsoft.com/en-us/library/ms188037.aspx)
            var batches = Regex.Split(sqlBatch,
                String.Format(CultureInfo.InvariantCulture, @"\s+({0}[ \t]+[0-9]+|{0})(?:\s+|$)", BatchTerminator),
                RegexOptions.IgnoreCase);

            for (int i = 0; i < batches.Length; ++i)
            {
                // Skip batches that merely contain the batch terminator
                if (batches[i].StartsWith(BatchTerminator, StringComparison.OrdinalIgnoreCase) ||
                    (i == batches.Length - 1 && string.IsNullOrWhiteSpace(batches[i])))
                {
                    continue;
                }

                // Include batch terminator if the next element is a batch terminator
                if (batches.Length > i + 1 &&
                    batches[i + 1].StartsWith(BatchTerminator, StringComparison.OrdinalIgnoreCase))
                {
                    int repeatCount = 1;

                    // Handle count parameter on the batch splitting utility statement
                    if (!batches[i + 1].EqualsIgnoreCase(BatchTerminator))
                    {
                        repeatCount = int.Parse(Regex.Match(batches[i + 1], @"([0-9]+)").Value, CultureInfo.InvariantCulture);
                    }

                    for (int j = 0; j < repeatCount; ++j)
                        Statement(batches[i], suppressTransaction, BatchTerminator);
                }
                else
                {
                    Statement(batches[i], suppressTransaction);
                }
            }
        }


        private void Generate(AddForeignKeyOperation addForeignKeyOperation)
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


        private void Generate(PrimaryKeyOperation addPrimaryKeyOperation)
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


        private void Generate(CreateTableOperation createTableOperation)
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


        private void Generate(DropIndexOperation dropIndexOperation)
        {
            Contract.Requires(dropIndexOperation != null);

            using (var writer = Writer())
            {
                writer.Write("DROP INDEX ");
                writer.Write(IndexName(dropIndexOperation, true));

                Statement(writer);
            }
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


        private void GenerateCreateSchema(string schema)
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



        protected virtual string Generate(DbGeometry defaultValue)
        {
            return "'" + defaultValue + "'";
        }


        protected virtual string Generate(string defaultValue)
        {
            Check.NotNull(defaultValue, "defaultValue");

            return "'" + defaultValue + "'";
        }


        protected virtual string Generate(object defaultValue)
        {
            Check.NotNull(defaultValue, "defaultValue");
            //Debug.Assert(defaultValue.GetType().IsValueType());

            return string.Format(CultureInfo.InvariantCulture, "{0}", defaultValue);
        }


        private string Generate(bool defaultValue)
        {
            return (defaultValue ? "TRUE" : "FALSE");
        }


        private string Generate(byte[] defaultValue)
        {
            Contract.Requires(defaultValue != null);

            return "decode('" + defaultValue.ToHexString() + "', 'hex')";

        }


        protected virtual string Generate(DateTime defaultValue)
        {
            return "'" + defaultValue.ToString(DateTimeFormat, CultureInfo.InvariantCulture) + "'";
        }


        private void Generate(DropTableOperation dropTableOperation)
        {
            Contract.Requires(dropTableOperation != null);

            using (var writer = Writer())
            {
                writer.Write("DROP TABLE ");
                writer.Write(Name(dropTableOperation.Name));

                Statement(writer);
            }
        }


        private void Generate(DropPrimaryKeyOperation dropPrimaryKeyOperation)
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


        private static string BuildColumnType(ColumnModel column)
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


        /// <summary>
        /// Quotes an identifier for SQL Server.
        /// </summary>
        /// <param name="identifier"> The identifier to be quoted. </param>
        /// <returns> The quoted identifier. </returns>
        protected virtual string Quote(string identifier)
        {
            Check.NotEmpty(identifier, "identifier");

            return "\"" + identifier + "\"";
        }

        private static string Escape(string s)
        {
            DebugCheck.NotNull(s);

            return s.Replace("'", "''");
        }

        /// <summary>
        /// Generates a quoted name. The supplied name may or may not contain the schema.
        /// </summary>
        /// <param name="name"> The name to be quoted. </param>
        /// <returns> The quoted name. </returns>
        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "0#")]
        protected virtual string Name(string name)
        {
            Check.NotEmpty(name, "name");

            var databaseName = DatabaseName.Parse(name);

            return new[] { databaseName.Schema, databaseName.Name }.Join(Quote, ".");
        }


        /// <summary>
        /// Gets a new <see cref="IndentedTextWriter" /> that can be used to build SQL.
        /// This is just a helper method to create a writer. Writing to the writer will
        /// not cause SQL to be registered for execution. You must pass the generated
        /// SQL to the Statement method.
        /// </summary>
        /// <returns> An empty text writer to use for SQL generation. </returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected static IndentedTextWriter Writer()
        {
            return new IndentedTextWriter(new StringWriter(CultureInfo.InvariantCulture));
        }


        /// <summary>
        /// Adds a new Statement to be executed against the database.
        /// </summary>
        /// <param name="writer"> The writer containing the SQL to be executed. </param>
        /// <param name="batchTerminator">The batch terminator for the database provider.</param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        protected void Statement(IndentedTextWriter writer, string batchTerminator = null)
        {
            Check.NotNull(writer, "writer");

            Statement(writer.InnerWriter.ToString(), batchTerminator: batchTerminator);
        }

        /// <summary>
        /// Adds a new Statement to be executed against the database.
        /// </summary>
        /// <param name="sql"> The statement to be executed. </param>
        /// <param name="suppressTransaction"> Gets or sets a value indicating whether this statement should be performed outside of the transaction scope that is used to make the migration process transactional. If set to true, this operation will not be rolled back if the migration process fails. </param>
        /// <param name="batchTerminator">The batch terminator for the database provider.</param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        protected void Statement(string sql, bool suppressTransaction = false, string batchTerminator = null)
        {
            Check.NotEmpty(sql, "sql");

            _statements.Add(
                new MigrationStatement
                {
                    Sql = sql,
                    SuppressTransaction = suppressTransaction,
                    BatchTerminator = batchTerminator
                });
        }


        private class HistoryRebuildOperationSequence : MigrationOperation
        {
            public readonly AddColumnOperation AddColumnOperation;
            public readonly DropPrimaryKeyOperation DropPrimaryKeyOperation;

            private HistoryRebuildOperationSequence(
                AddColumnOperation addColumnOperation,
                DropPrimaryKeyOperation dropPrimaryKeyOperation)
                : base(null)
            {
                AddColumnOperation = addColumnOperation;
                DropPrimaryKeyOperation = dropPrimaryKeyOperation;
            }

            public override bool IsDestructiveChange
            {
                get { return false; }
            }

            public static HistoryRebuildOperationSequence Detect(IEnumerator<MigrationOperation> enumerator)
            {
                const string HistoryTableName = "dbo." + HistoryContext.DefaultTableName;

                var addColumnOperation = enumerator.Current as AddColumnOperation;
                if (addColumnOperation == null
                    || addColumnOperation.Table != HistoryTableName
                    || addColumnOperation.Column.Name != "ContextKey")
                {
                    return null;
                }

                Debug.Assert(addColumnOperation.Column.DefaultValue is string);

                enumerator.MoveNext();
                var dropPrimaryKeyOperation = (DropPrimaryKeyOperation)enumerator.Current;
                Debug.Assert(dropPrimaryKeyOperation.Table == HistoryTableName);
                DebugCheck.NotNull(dropPrimaryKeyOperation.CreateTableOperation);

                enumerator.MoveNext();
                var alterColumnOperation = (AlterColumnOperation)enumerator.Current;
                Debug.Assert(alterColumnOperation.Table == HistoryTableName);

                enumerator.MoveNext();
                var addPrimaryKeyOperation = (AddPrimaryKeyOperation)enumerator.Current;
                Debug.Assert(addPrimaryKeyOperation.Table == HistoryTableName);

                return new HistoryRebuildOperationSequence(
                    addColumnOperation, dropPrimaryKeyOperation);
            }
        }


    }


    internal class DebugCheck
    {
        [Conditional("DEBUG")]
        public static void NotNull<T>(T value) where T : class
        {
            Debug.Assert(value != null);
        }

        [Conditional("DEBUG")]
        public static void NotNull<T>(T? value) where T : struct
        {
            Debug.Assert(value != null);
        }

        [Conditional("DEBUG")]
        public static void NotEmpty(string value)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(value));
        }
    }


    internal class Check
    {
        public static T NotNull<T>(T value, string parameterName) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return value;
        }

        public static T? NotNull<T>(T? value, string parameterName) where T : struct
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return value;
        }

        public static string NotEmpty(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException();
            }

            return value;
        }
    }

}
