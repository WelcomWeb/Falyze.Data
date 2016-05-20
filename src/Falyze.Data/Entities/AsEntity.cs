using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace Falyze.Data.Entities
{
    internal static class AsEntity
    {
        private static HashSet<Type> NumericTypes = new HashSet<Type>
            {
                typeof(decimal), typeof(short), typeof(ushort), typeof(int), typeof(float), typeof(double)
            };

        public static T Map<T>(DbDataReader reader, PropertyInfo[] properties)
        {
            var entity = Activator.CreateInstance<T>();
            var fieldNames = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName);

            foreach (var property in properties)
            {
                if (fieldNames.Contains(property.Name))
                {
                    var ordinal = reader.GetOrdinal(property.Name);
                    var value = reader.GetValue(ordinal);

                    if (NumericTypes.Contains(property.PropertyType))
                    {
                        property.SetValue(entity, Convert.ChangeType(value, property.PropertyType));
                    }
                    else
                    {
                        property.SetValue(entity, value == DBNull.Value ? null : value);
                    }
                }
            }

            return entity;
        }
    }
}
