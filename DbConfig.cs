using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SZORM
{
    /// <summary>
    /// DbContext 配置
    /// </summary>
    public class DbConfig
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionStr { get; set; }
        /// <summary>
        /// Provider名字
        /// </summary>
        public string ProviderName { get; set; }
    }
}
