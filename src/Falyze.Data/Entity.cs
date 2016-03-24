using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace Falyze.Data
{
    public abstract class Entity
    {
        protected internal class Helper
        {
            private static ConcurrentDictionary<string, PropertyInfo[]> _properties = new ConcurrentDictionary<string, PropertyInfo[]>();

            private static HashSet<Type> NumericTypes = new HashSet<Type>
            {
                typeof(decimal), typeof(short), typeof(ushort), typeof(int), typeof(float), typeof(double)
            };

            public string GetTableName(Type type)
            {
                var attribute = type.GetTypeInfo().GetCustomAttribute(typeof(TableAttribute));
                return attribute != null ? (attribute as TableAttribute).TableName : type.Name;
            }

            public string GetPrimaryKey(Type type)
            {
                var attribute = type.GetTypeInfo().GetCustomAttribute(typeof(PkAttribute));
                return attribute != null ? (attribute as PkAttribute).Field : null;
            }

            public PropertyInfo[] CheckTypeAccess<T>()
            {
                var type = typeof(T);
                if (!_properties.ContainsKey(type.FullName))
                {
                    _properties.GetOrAdd(type.FullName, type.GetProperties());
                }

                return _properties[type.FullName];
            }

            public T MapEntity<T>(DbDataReader reader, PropertyInfo[] properties)
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
}
