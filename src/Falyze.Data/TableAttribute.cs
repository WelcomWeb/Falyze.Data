using System;

namespace Falyze.Data
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TableAttribute : Attribute
    {
        public string TableName { get; set; }
    }
}
