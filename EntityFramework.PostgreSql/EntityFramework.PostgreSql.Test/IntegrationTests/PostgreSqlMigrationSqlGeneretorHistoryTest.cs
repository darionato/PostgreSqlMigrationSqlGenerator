using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity;
using System.Data.Entity.Migrations.Sql;
using Npgsql;
using NUnit.Framework;

namespace EntityFramework.PostgreSql.Test.IntegrationTests
{

    [TestFixture]
    public class PostgreSqlMigrationSqlGeneretorHistoryTest
    {

        private const string ConnectionString = "Server=127.0.0.1;Port=5432;Database=testEF6;User Id=postgres;Password=p0o9i8u7y6;CommandTimeout=20;Preload Reader = true;";
        private const string ProviderName = "Npgsql";


        [Test]
        public void CreateNewDatabase()
        {


            const string cs = "Server=127.0.0.1;Port=5432;Database=testEFxx;User Id=postgres;Password=p0o9i8u7y6;CommandTimeout=20;Preload Reader = true;";

            var db = new LocalPgContext(cs);


            if (!db.Database.Exists())
                db.Database.Create();

            var exists = db.Database.Exists();


            db.Database.Delete();

            Assert.IsTrue(exists);

        }


        //[Test]
        public void GenerateInsertHistoryOperation()
        {


            //var f = DbProviderFactories.GetFactory("Npgsql");

            var migrator = new DbMigrator(new LocalMigrationConfiguration());
            var migs = migrator.GetLocalMigrations();
            migrator.Update();


        }


        public class LocalMigrationConfiguration : DbMigrationsConfiguration<LocalPgContext>
        {
            public LocalMigrationConfiguration()
            {
                AutomaticMigrationDataLossAllowed = true;
                AutomaticMigrationsEnabled = false;
                SetSqlGenerator("Npgsql", new PostgreSqlMigrationSqlGenerator());
                MigrationsDirectory = "Migrations";
                MigrationsNamespace = "EntityFramework.PostgreSql.Test.IntegrationTests.Migrations";
                MigrationsAssembly = typeof(PostgreSqlMigrationSqlGeneretorHistoryTest).Assembly;
                TargetDatabase = new DbConnectionInfo(ConnectionString, ProviderName);
            }
        }


        public class LocalPgContext : DbContext, IDbProviderFactoryResolver, IDbConnectionFactory
        {

            public LocalPgContext(string nameOrConnectionString) : base(nameOrConnectionString)
            {
                Database.SetInitializer(new CreateDatabaseIfNotExists<LocalPgContext>());
            }


            public DbProviderFactory ResolveProviderFactory(DbConnection connection)
            {
                return DbProviderFactories.GetFactory("Npgsql");
            }

            public DbConnection CreateConnection(string nameOrConnectionString)
            {
                return new NpgsqlConnection(nameOrConnectionString);
            }

            DbConnection IDbConnectionFactory.CreateConnection(string nameOrConnectionString)
            {
                return CreateConnection(nameOrConnectionString);
            }

            DbProviderFactory IDbProviderFactoryResolver.ResolveProviderFactory(DbConnection connection)
            {
                return new LocalPgProviderFactory();
            }

        }

        public class LocalPgProviderFactory : DbProviderFactory
        {

            public override DbConnectionStringBuilder CreateConnectionStringBuilder()
            {
                return new NpgsqlConnectionStringBuilder(ConnectionString);
            }

            public override DbConnection CreateConnection()
            {
                return new NpgsqlConnection(ConnectionString);
            }
        }


        /*
         * 
        public class LocalConfiguration : DbConfiguration
        {
            public LocalConfiguration()
            {

                // can't set this cos NpgsqlServices is internal
                SetProviderServices(
                    "Npgsql", provider: NpgsqlServices.Instance
                    );
            }

        }
        */
    }
}
