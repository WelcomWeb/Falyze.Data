using System.Data.Common;

namespace Falyze.Data.Exceptions
{
    public class FalyzePropertyFieldException : DbException
    {
        public FalyzePropertyFieldException(string message) : base(message) { }
    }
}
