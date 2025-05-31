using System;
using System.Reflection;
using System.Text;
using Dapper;
using Dapper.Contrib.Extensions;

namespace Play.Infrastructure.Common.Databases;

public class SqlCommandHelper
{
    public static string GetTableName<T>() where T : class
    {
        return typeof(T).GetCustomAttribute<TableAttribute>()?.Name ?? typeof(T).Name;
    }

    public static string GetSelectSql<T>() where T : class
    {
        return $"SELECT * from {GetTableName<T>()}";
    }

    public static string GetSelectSqlWithCondition<T>(object param) where T : class
    {
        var condition = new StringBuilder();
        var start = true;
        foreach (var property in param.GetType().GetProperties())
        {
            var andOperator = start == true ? "" : " AND ";
            condition.Append($"\"{andOperator}{property.Name}\" = @{property.Name}");
            start = false;
        }

        return $"{GetSelectSql<T>()} WHERE {condition}";
    }
}
