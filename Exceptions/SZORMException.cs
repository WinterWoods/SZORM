using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SZORM.Exceptions
{
    public class SZORMException: Exception
    {
        public SZORMException()
            : this("An exception occurred in the persistence layer.")
        {
        }

        public SZORMException(string message)
            : base(message)
        {
        }

        public SZORMException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }

        public SZORMException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
