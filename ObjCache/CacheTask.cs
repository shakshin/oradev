using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace oradev.ObjCache
{
    public class CacheTask
    {
        public delegate void CachePackageCallback(CachePackage pkg);
        public String DataBase;
        public String PackageName;
        public Cache.CachePackageCallback Callback;
    }
}
