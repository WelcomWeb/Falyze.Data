using System.Data.Common;

namespace Falyze.Data
{
    public class FalyzeConnectionException : DbException
    {
        public FalyzeConnectionException(string message) : base(message) { }
    }
}
