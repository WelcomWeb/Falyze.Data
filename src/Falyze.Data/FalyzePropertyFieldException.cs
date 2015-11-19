using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Falyze.Data
{
    public class FalyzePropertyFieldException : DbException
    {
        public FalyzePropertyFieldException(string message) : base(message) { }
    }
}
