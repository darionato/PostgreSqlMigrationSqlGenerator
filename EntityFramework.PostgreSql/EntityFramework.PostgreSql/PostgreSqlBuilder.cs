using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Sql;
using System.Text;
using EntityFramework.PostgreSql.Interfaces;

namespace EntityFramework.PostgreSql
{
    internal class PostgreSqlBuilder: IPostgreSqlFragment
    {

        private readonly List<object> _sqlFragments = new List<object>();

        public void Append(object s)
        {
            DebugCheck.NotNull(s);
            _sqlFragments.Add(s);
        }

        public void AppendLine()
        {
            _sqlFragments.Add("\r\n");
        }

        public virtual bool IsEmpty
        {
            get { return ((null == _sqlFragments) || (0 == _sqlFragments.Count)); }
        }

        public void WriteSql(StringBuilder writer, PostgreSqlGenerator sqlGenerator)
        {
            if (null != _sqlFragments)
            {
                foreach (var o in _sqlFragments)
                {
                    var str = (o as String);
                    if (null != str)
                    {
                        writer.Append(str);
                    }
                    else
                    {
                        var sqlFragment = (o as IPostgreSqlFragment);
                        if (null != sqlFragment)
                        {
                            sqlFragment.WriteSql(writer, sqlGenerator);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }
            }
        }
    }
}
