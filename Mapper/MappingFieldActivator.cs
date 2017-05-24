using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using SZORM.Exceptions;

namespace SZORM.Mapper
{
    public class MappingFieldActivator : IObjectActivator
    {
        Func<IDataReader, int, object> _fn = null;
        int _readerOrdinal;
        public MappingFieldActivator(Func<IDataReader, int, object> fn, int readerOrdinal)
        {
            this._fn = fn;
            this._readerOrdinal = readerOrdinal;
        }
        public object CreateInstance(IDataReader reader)
        {
            try
            {
                return _fn(reader, _readerOrdinal);
            }
            catch (Exception ex)
            {
                throw new SZORMException(ObjectActivator.AppendErrorMsg(reader, this._readerOrdinal, ex), ex);
            }
        }
    }
}
