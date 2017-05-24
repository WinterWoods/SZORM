using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SZORM.Factory.Models
{
    public class TableModel
    {
        public TableModel()
        {
            Columns = new List<ColumnModel>();
        }
        public string Name { get; set; }
        public List<ColumnModel> Columns { get; set; }
    }
}
