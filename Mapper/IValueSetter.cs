using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SZORM.Mapper
{
    public interface IValueSetter
    {
        void SetValue(object obj, IDataReader reader);
    }
}
