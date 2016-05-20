using System;

namespace Falyze.Data.Entities
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TableAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
