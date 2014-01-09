using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;

namespace EntityFramework.PostgreSql.Test.IntegrationTests.Migrations
{
    public class TestMigration : DbMigration, IMigrationMetadata
    {
        public override void Up()
        {
            CreateTable("dbo.TestMigration", t => new
            {
                Id = t.Int(identity: true, nullable: false),
                Name = t.String(false, 20)
            });
        }

        public override void Down()
        {
            DropTable("dbo.TestMigration");
        }

        public string Id { get { return "1"; } }
        public string Source { get; private set; }
        public string Target { get { return "TestMigration"; } }
    }
}
