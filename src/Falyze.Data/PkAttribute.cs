using System;

namespace Falyze.Data
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PkAttribute : Attribute
    {
        public string Field { get; set; }
    }
}
