using System.Data.Common;

namespace Falyze.Data.Exceptions
{
    public class FalyzeConnectionException : DbException
    {
        public FalyzeConnectionException(string message) : base(message) { }
    }
}
