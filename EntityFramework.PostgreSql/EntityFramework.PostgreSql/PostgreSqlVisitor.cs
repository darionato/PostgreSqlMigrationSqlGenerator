using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Migrations.Sql;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityFramework.PostgreSql
{
    internal class PostgreSqlVisitor : DbExpressionVisitor
    {

        private readonly StringBuilder _commandText;

        internal PostgreSqlVisitor(StringBuilder commandText)
        {
            _commandText = commandText;
        }

        public override void Visit(DbVariableReferenceExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbUnionAllExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbTreatExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbSortExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbSkipExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbScanExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbRelationshipNavigationExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbRefExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbQuantifierExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbPropertyExpression expression)
        {
            Check.NotNull(expression, "expression");


            _commandText.Append(GenerateMemberTSql(expression.Property));
        }

        internal static string GenerateMemberTSql(EdmMember member)
        {
            return "\"" + member.Name + "\"";
        }

        public override void Visit(DbProjectExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbParameterReferenceExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbOrExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbOfTypeExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbNullExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbNotExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbNewInstanceExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbLimitExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbLikeExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbJoinExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbIsOfExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbIsNullExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbIsEmptyExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbIntersectExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbGroupByExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbRefKeyExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbEntityRefExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbFunctionExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbFilterExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbExceptExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbElementExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbDistinctExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbDerefExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbCrossJoinExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbConstantExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbComparisonExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbCastExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbCaseExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbArithmeticExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbApplyExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbAndExpression expression)
        {
            throw new NotImplementedException();
        }

        public override void Visit(DbExpression expression)
        {
            throw new NotImplementedException();
        }
    }
}
