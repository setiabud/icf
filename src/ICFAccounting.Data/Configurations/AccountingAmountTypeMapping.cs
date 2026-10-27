using System.Data.Common;
using System.Data.SqlTypes;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;

namespace ICFAccounting.Data.Configurations;

/// <summary>
/// Preserves the domain decimal range when SQL Server adds two fractional digits.
/// </summary>
internal sealed class AccountingAmountTypeMapping : DecimalTypeMapping
{
    public AccountingAmountTypeMapping() : base("decimal(38,2)", precision: 38, scale: 2)
    {
    }

    private AccountingAmountTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters)
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
    {
        return new AccountingAmountTypeMapping(parameters);
    }

    protected override void ConfigureParameter(DbParameter parameter)
    {
        parameter.Precision = (byte)Precision!.Value;
        parameter.Scale = (byte)Scale!.Value;
        if (parameter.Value is decimal amount)
        {
            parameter.Value = new SqlDecimal(amount);
        }
    }

    public override MethodInfo GetDataReaderMethod()
    {
        return typeof(SqlDataReader).GetMethod(nameof(SqlDataReader.GetSqlDecimal), [typeof(int)])!;
    }

    public override Expression CustomizeDataReaderExpression(Expression expression)
    {
        return Expression.Call(
            typeof(AccountingAmountTypeMapping).GetMethod(
                nameof(ReadAmount), BindingFlags.Static | BindingFlags.NonPublic)!, expression);
    }

    private static decimal ReadAmount(SqlDecimal amount)
    {
        while (amount.Scale > 0 && amount.Data[3] != 0)
        {
            var reduced = SqlDecimal.AdjustScale(amount, -1, false);
            if ((reduced != amount).IsTrue)
            {
                throw new OverflowException("The stored amount exceeds the domain decimal range.");
            }

            amount = reduced;
        }

        return amount.Value;
    }
}