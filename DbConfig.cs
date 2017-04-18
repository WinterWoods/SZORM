using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SZORM
{
    /// <summary>
    /// DbContext 配置
    /// </summary>
    public class DbConfig
    {
        /// <summary>
        /// 配置文件中 连接字符串的名字,或者自定义名字
        /// </summary>
        public string SettingName { get; set; }
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
