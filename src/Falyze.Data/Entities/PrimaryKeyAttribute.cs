using System;

namespace Falyze.Data.Entities
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PrimaryKeyAttribute : Attribute
    {
        public string Field { get; set; }
    }
}
