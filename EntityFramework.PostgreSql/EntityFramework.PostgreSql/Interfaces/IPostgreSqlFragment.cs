using System.Text;

namespace EntityFramework.PostgreSql.Interfaces
{
    internal interface IPostgreSqlFragment
    {

        void WriteSql(StringBuilder writer, PostgreSqlGenerator sqlGenerator);

    }
}
