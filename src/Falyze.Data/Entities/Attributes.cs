using System;
using System.Reflection;

namespace Falyze.Data.Entities
{
    internal static class Attributes
    {
        public static string GetTableName(Type type)
        {
            var attribute = type.GetTypeInfo().GetCustomAttribute(typeof(TableAttribute));
            return attribute != null ? (attribute as TableAttribute).Name : type.Name;
        }

        public static string GetPrimaryKey(Type type)
        {
            var attribute = type.GetTypeInfo().GetCustomAttribute(typeof(PrimaryKeyAttribute));
            return attribute != null ? (attribute as PrimaryKeyAttribute).Field : null;
        }
    }
}
