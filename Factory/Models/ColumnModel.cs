using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SZORM.Factory.Models
{
    public class ColumnModel
    {
        public string Name { get; set; }
        public bool Required { get; set; }
        public bool IsKey { get; set; }
        public bool IsText { get; set; }
        /// <summary>
        /// 只能在查询的时候使用
        /// </summary>
        public string ColumnFullType { get; set; }
        /// <summary>
        /// 最大值
        /// </summary>
        public int MaxLength { get; set; }
        /// <summary>
        /// 数字的最大位数
        /// </summary>
        public int NumberSize { get; set; }
        /// <summary>
        /// 数字的最大精度 可以从负数到整数 如-1 存储111 结果如110
        /// </summary>
        public int NumberPrecision { get; set; }
        public Type type { get; set; }
    }
}
