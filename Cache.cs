using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SZORM
{
    public class Cache
    {
        static Hashtable cache = Hashtable.Synchronized(new Hashtable());
        public static void Add(string name,object obj)
        {
            lock (cache.SyncRoot)
            {
                if (!cache.ContainsKey(name))
                    cache.Add(name, obj);
                
            }
        }
        public static object Get(string name)
        {
            if (cache.ContainsKey(name))
                return cache[name];
            return null;
        }
    }
}
