using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SZORM.Mapper;

namespace SZORM.Query.Mapping
{
    public interface IObjectActivatorCreator
    {
        IObjectActivator CreateObjectActivator();
        IObjectActivator CreateObjectActivator(IDbContext dbContext);
    }
}
