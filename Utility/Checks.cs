using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SZORM.Utility
{
    class Checks
    {
        public static void NotNull(object obj, string paramName)
        {
            if (obj == null)
                throw new ArgumentNullException(paramName);
        }
        public static void FileNotFind(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("没有找到文件:" + filePath);
        }
    }
}
