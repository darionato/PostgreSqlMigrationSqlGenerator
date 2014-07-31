using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Migrations.Sql;
using System.Globalization;
using System.Linq;
using System.Text;
using Npgsql;

namespace EntityFramework.PostgreSql
{
    internal class PostgreSqlVisitor : DbExpressionVisitor
    {

        private readonly StringBuilder _commandText;
        private readonly bool _createParameters;
        private readonly List<NpgsqlParameter> _parameters;
        private readonly PostgreSqlGenerator _sqlGenerator;

        internal PostgreSqlVisitor(
            StringBuilder commandText,
            PostgreSqlGenerator sqlGenerator,
            bool createParameters = false)
        {
            _commandText = commandText;
            _createParameters = createParameters;
            _sqlGenerator = sqlGenerator;

            _parameters = new List<NpgsqlParameter>();
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

            Check.NotNull(expression, "expression");

            _commandText.Append(GetTargetTSql(expression.Target));

        }


        // <summary>
        // Gets escaped TSql identifier describing this entity set.
        // </summary>
        internal static string GetTargetTSql(EntitySetBase entitySetBase)
        {
            var definingQuery = entitySetBase.GetMetadataPropertyValue<string>("DefiningQuery");
            if (definingQuery != null)
            {
                return "(" + definingQuery + ")";
            }
            // construct escaped T-SQL referencing entity set
            var builder = new StringBuilder(50);

            var schema = entitySetBase.GetMetadataPropertyValue<string>("Schema");
            if (!string.IsNullOrEmpty(schema))
            {
                builder.Append(QuoteIdentifier(schema));
                builder.Append(".");
            }
            else
            {
                builder.Append(QuoteIdentifier(entitySetBase.EntityContainer.Name));
                builder.Append(".");
            }

            var table = entitySetBase.GetMetadataPropertyValue<string>("Table");
            builder.Append(
                string.IsNullOrEmpty(table)
                    ? QuoteIdentifier(entitySetBase.Name)
                    : QuoteIdentifier(table));

            return builder.ToString();
        }

        internal static string QuoteIdentifier(string name)
        {
            DebugCheck.NotEmpty(name);
            // We assume that the names are not quoted to begin with.
            return "\"" + name + "\"";
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
            Check.NotNull(expression, "expression");

            if (_createParameters)
            {
                var parameter = CreateParameter(expression.Value, expression.ResultType);
                _commandText.Append(parameter.ParameterName);
            }
            else
            {

                _sqlGenerator.WriteSql(_commandText, expression.Accept(_sqlGenerator));

            }
        }

        internal NpgsqlParameter CreateParameter(object value, TypeUsage type, string name = null)
        {

            var parameter = new NpgsqlParameter(
                name ?? GetParameterName(_parameters.Count), GetDbType(type));

            _parameters.Add(parameter);

            return parameter;

        }

        private static DbType GetDbType(
            TypeUsage type)
        {
            // only supported for primitive type
            var primitiveTypeKind = ((PrimitiveType)type.EdmType).PrimitiveTypeKind;


            // CONSIDER(CMeek):: add logic for Xml here
            switch (primitiveTypeKind)
            {
                case PrimitiveTypeKind.Binary:
                    return DbType.Binary;

                case PrimitiveTypeKind.Boolean:
                    return DbType.Boolean;

                case PrimitiveTypeKind.Byte:
                    return DbType.Byte;

                case PrimitiveTypeKind.Time:
                    return DbType.Time;

                case PrimitiveTypeKind.DateTimeOffset:
                    return DbType.DateTimeOffset;

                case PrimitiveTypeKind.DateTime:
                    return DbType.DateTime;

                case PrimitiveTypeKind.Decimal:
                    return DbType.Decimal;

                case PrimitiveTypeKind.Double:
                    return DbType.Double;

                case PrimitiveTypeKind.Geography:
                    return DbType.Int32;

                case PrimitiveTypeKind.Geometry:
                    return DbType.Int32;

                case PrimitiveTypeKind.Guid:
                    return DbType.Guid;

                case PrimitiveTypeKind.Int16:
                    return DbType.Int16;

                case PrimitiveTypeKind.Int32:
                    return DbType.Int32;

                case PrimitiveTypeKind.Int64:
                    return DbType.Int64;

                case PrimitiveTypeKind.SByte:
                    return DbType.SByte;

                case PrimitiveTypeKind.Single:
                    return DbType.Single;

                case PrimitiveTypeKind.String:
                    return DbType.String;

                default:
                    return DbType.String;

            }

        }

        internal static string GetParameterName(int index)
        {
            return string.Concat("@", index.ToString(CultureInfo.InvariantCulture));
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

    internal static class MetdataItemExtensions
    {
        public static T GetMetadataPropertyValue<T>(this MetadataItem item, string propertyName)
        {
            DebugCheck.NotNull(item);

            var property = item.MetadataProperties.FirstOrDefault(p => p.Name == propertyName);
            return property == null ? default(T) : (T)property.Value;
        }
    }

}
