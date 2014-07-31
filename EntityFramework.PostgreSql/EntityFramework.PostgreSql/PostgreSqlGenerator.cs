using System;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
using System.Text;
using EntityFramework.PostgreSql.Interfaces;
using EntityFramework.PostgreSql.Utilities;

namespace EntityFramework.PostgreSql
{
    internal class PostgreSqlGenerator : DbExpressionVisitor<IPostgreSqlFragment>
    {
        public override IPostgreSqlFragment Visit(DbExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbAndExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbApplyExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbArithmeticExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbCaseExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbCastExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbComparisonExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbConstantExpression expression)
        {

            // Constants will be sent to the store as part of the generated TSQL, not as parameters
            var result = new PostgreSqlBuilder();

            var resultType = expression.ResultType;
            // Model Types can be (at the time of this implementation):
            //      Binary, Boolean, Byte, Date, DateTime, DateTimeOffset, Decimal, Double, Guid, Int16, Int32, Int64, Single, String, Time
            if (resultType != null && resultType.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType)
            {
                var typeKind = ((PrimitiveType)resultType.EdmType).PrimitiveTypeKind;
                switch (typeKind)
                {
                    case PrimitiveTypeKind.Int32:
                        // default sql server type for integral values.
                        result.Append(expression.Value);
                        break;

                    case PrimitiveTypeKind.Binary:
                        result.Append(" decode('");
                        result.Append(((Byte[])expression.Value).ToHexString());
                        result.Append("', 'hex') ");
                        break;

                    case PrimitiveTypeKind.Boolean:
                        // Bugs 450277, 430294: Need to preserve the boolean type-ness of
                        // this value for round-trippability
                        //WrapWithCastIfNeeded(!isCastOptional, (bool)expression.Value ? "1" : "0", "bit", result);
                        break;

                    case PrimitiveTypeKind.Byte:
                        //WrapWithCastIfNeeded(!isCastOptional, expression.Valuexpression.ToString(), "tinyint", result);
                        break;

                    case PrimitiveTypeKind.DateTime:
                        //result.Append("convert(");
                        //result.Append(IsPreKatmai ? "datetime" : "datetime2");
                        //result.Append(", ");
                        //result.Append(
                        //    EscapeSingleQuote(
                        //        ((DateTime)expression.Value).ToString(
                        //            IsPreKatmai ? "yyyy-MM-dd HH:mm:ss.fff" : "yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
                        //        false /* IsUnicode */));
                        //result.Append(", 121)");
                        break;

                    case PrimitiveTypeKind.Time:
                        //AssertKatmaiOrNewer(typeKind);
                        //result.Append("convert(");
                        //result.Append(expression.ResultTypexpression.EdmTypexpression.Name);
                        //result.Append(", ");
                        //result.Append(EscapeSingleQuote(expression.Valuexpression.ToString(), false /* IsUnicode */));
                        //result.Append(", 121)");
                        break;

                    case PrimitiveTypeKind.DateTimeOffset:
                        //AssertKatmaiOrNewer(typeKind);
                        //result.Append("convert(");
                        //result.Append(expression.ResultTypexpression.EdmTypexpression.Name);
                        //result.Append(", ");
                        //result.Append(
                        //    EscapeSingleQuote(
                        //        ((DateTimeOffset)expression.Value).ToString("yyyy-MM-dd HH:mm:ss.fffffff zzz", CultureInfo.InvariantCulture), false
                        //    /* IsUnicode */));
                        //result.Append(", 121)");
                        break;

                    case PrimitiveTypeKind.Decimal:
                        //var strDecimal = ((Decimal)expression.Value).ToString(CultureInfo.InvariantCulture);
                        //// if the decimal value has no decimal part, cast as decimal to preserve type
                        //// if the number has precision > int64 max precision, it will be handled as decimal by sql server
                        //// and does not need cast. if precision is lest then 20, then cast using Max(literal precision, sql default precision)
                        //var needsCast = -1 == strDecimal.IndexOf('.') && (strDecimal.TrimStart(new[] { '-' }).Length < 20);

                        //var precision = Math.Max((Byte)strDecimal.Length, DefaultDecimalPrecision);
                        //Debug.Assert(precision > 0, "Precision must be greater than zero");

                        //var decimalType = "decimal(" + precision.ToString(CultureInfo.InvariantCulture) + ")";

                        //WrapWithCastIfNeeded(needsCast, strDecimal, decimalType, result);
                        break;

                    case PrimitiveTypeKind.Double:
                        //{
                        //    var doubleValue = (Double)expression.Value;
                        //    AssertValidDouble(doubleValue);
                        //    WrapWithCastIfNeeded(true, doubleValuexpression.ToString("R", CultureInfo.InvariantCulture), "float(53)", result);
                        //}
                        break;

                    case PrimitiveTypeKind.Geography:
                        //AppendSpatialConstant(result, ((DbGeography)expression.Value).AsSpatialValue());
                        break;

                    case PrimitiveTypeKind.Geometry:
                        //AppendSpatialConstant(result, ((DbGeometry)expression.Value).AsSpatialValue());
                        break;

                    case PrimitiveTypeKind.Guid:
                        //WrapWithCastIfNeeded(true, EscapeSingleQuote(expression.Valuexpression.ToString(), false /* IsUnicode */), "uniqueidentifier", result);
                        break;

                    case PrimitiveTypeKind.Int16:
                        //WrapWithCastIfNeeded(!isCastOptional, expression.Valuexpression.ToString(), "smallint", result);
                        break;

                    case PrimitiveTypeKind.Int64:
                        //WrapWithCastIfNeeded(!isCastOptional, expression.Valuexpression.ToString(), "bigint", result);
                        break;

                    case PrimitiveTypeKind.Single:
                        //{
                        //    var singleValue = (float)expression.Value;
                        //    AssertValidSingle(singleValue);
                        //    WrapWithCastIfNeeded(true, singleValuexpression.ToString("R", CultureInfo.InvariantCulture), "real", result);
                        //}
                        break;

                    case PrimitiveTypeKind.String:
                        
                        result.Append(EscapeSingleQuote(expression.Value as string, false));
                        break;

                    default:
                        // all known scalar types should been handled already.
                        throw new NotSupportedException();
                        //Strings.NoStoreTypeForEdmType(resultTypexpression.EdmTypexpression.Name, ((PrimitiveType)(resultTypexpression.EdmType)).PrimitiveTypeKind));
                }
            }
            else
            {
                throw new NotSupportedException();
                //if/when Enum types are supported, then handle appropriately, for now is not a valid type for constants.
                //result.Append(expression.Valuexpression.ToString());
            }

            return result;
        }



        private static string EscapeSingleQuote(string s, bool isUnicode)
        {
            return (isUnicode ? "N'" : "'") + s.Replace("'", "''") + "'";
        }

        public override IPostgreSqlFragment Visit(DbCrossJoinExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbDerefExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbDistinctExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbElementExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbExceptExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbFilterExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbFunctionExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbEntityRefExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbRefKeyExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbGroupByExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbIntersectExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbIsEmptyExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbIsNullExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbIsOfExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbJoinExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbLikeExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbLimitExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbNewInstanceExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbNotExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbNullExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbOfTypeExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbOrExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbParameterReferenceExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbProjectExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbPropertyExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbQuantifierExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbRefExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbRelationshipNavigationExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbScanExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbSortExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbSkipExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbTreatExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbUnionAllExpression expression)
        {
            throw new NotImplementedException();
        }

        public override IPostgreSqlFragment Visit(DbVariableReferenceExpression expression)
        {
            throw new NotImplementedException();
        }

        internal StringBuilder WriteSql(StringBuilder writer, IPostgreSqlFragment sqlFragment)
        {
            sqlFragment.WriteSql(writer, this);
            return writer;
        }

    }
}
