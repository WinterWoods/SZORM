using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using SZORM.Factory;

namespace SZORM
{
    public interface ISZORM
    {
        
    }
    public interface ISZORM<out T> : ISZORM
    {
    }
}
