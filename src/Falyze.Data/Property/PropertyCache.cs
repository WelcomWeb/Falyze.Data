using System.Collections.Concurrent;
using System.Reflection;

namespace Falyze.Data.Property
{
    internal static class PropertyCache
    {
        private static ConcurrentDictionary<string, PropertyInfo[]> _properties = new ConcurrentDictionary<string, PropertyInfo[]>();

        public static PropertyInfo[] GetProperties<T>()
        {
            var type = typeof(T);
            if (!_properties.ContainsKey(type.FullName))
            {
                return _properties.GetOrAdd(type.FullName, type.GetProperties());
            }

            return _properties[type.FullName];
        }
    }
}
