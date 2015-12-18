using System.Data.Common;

namespace Falyze.Data
{
    public class FalyzePropertyFieldException : DbException
    {
        public FalyzePropertyFieldException(string message) : base(message) { }
    }
}
