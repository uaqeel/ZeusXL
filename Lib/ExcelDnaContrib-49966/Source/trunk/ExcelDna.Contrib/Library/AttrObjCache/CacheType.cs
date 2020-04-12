/*
  Copyright (C) 2010 Robert Howley

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Robert Howley
  howley.robert@gmail.com
*/

using System;
using System.Collections.Generic;
using System.Linq;

using CU = ExcelDna.Contrib.Cache.CacheUtilities;
using System.Reflection;

namespace ExcelDna.Contrib.Cache
{
    internal class CacheType : ICacheType
    {
        private ExcelObjectAttribute _attr;

        private bool _allProp = false;
        private Dictionary<string, CacheItemProperty> _prop = new Dictionary<string, CacheItemProperty>();

        private bool _allMeth = false;
        private Dictionary<string, CacheItemMethod> _meth = new Dictionary<string, CacheItemMethod>();

        private bool _allCtor = false;
        private Dictionary<string, CacheItemCtor> _ctor = new Dictionary<string, CacheItemCtor>();

        public CacheType(Type t)
        {
            ItemType = t;
            _attr = CU.GetAttribute(ItemType);
        }

        public Type ItemType { get; protected set; }
        public string ExcelName { get { return (_attr.Name == string.Empty) ? ItemType.Name : _attr.Name; } }
        public string Description { get { return _attr.Description; } }
        public Exception Exception { get; protected set; }

        #region type info
        public object[,] AllTypeInfo()
        {
            int x = 0;
            List<object[]> s = new List<object[]>();

            loadAllCtors();
            foreach (KeyValuePair<string, CacheItemCtor> k in _ctor)
            {
                object[] o = new object[] { "Constructor", k.Value.ExcelName, "N/A", k.Value.Description };
                x++;

                s.Add(o);
            }

            loadAllProps();
            foreach (KeyValuePair<string, CacheItemProperty> k in _prop)
            {
                object[] o = new object[] { "Property", k.Value.ExcelName, k.Value.Type.Name, k.Value.Description };
                x++;

                s.Add(o);
            }

            loadAllMethods();
            foreach (KeyValuePair<string, CacheItemMethod> k in _meth)
            {
                object[] o = new object[] { "Method", k.Value.ExcelName, k.Value.ReturnType.Name, k.Value.Description };
                x++;

                s.Add(o);
            }

            object[,] all = new object[x, 4];
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < 4; j++)
                    all[i, j] = s[i][j];
            }

            return all;
        }
        #endregion

        #region constructor
        public CacheItemCtor Constructor(string ConstructorName)
        {
            Exception = null;

            CacheItemCtor ci = extractCtor(ConstructorName);

            if (ci == null)
                Exception = getE("Constructor", ExcelName, ConstructorName);

            return ci;
        }

        public CacheItemCtor[] Constructor()
        {
            loadAllCtors();
            return _ctor.Values.ToArray();
        }

        public object CreateInstance(string Constructor, object[] o)
        {
            Exception = null;

            CacheItemCtor ci = extractCtor(Constructor);

            object handle = null;
            if (ci != null)
            {
                handle = ci.Construct(o);
                Exception = ci.Exception;
            }
            else
                Exception = getE("Constructor", ExcelName, Constructor);

            return handle;
        }

        public object CreateInstance()
        {
            Exception = null;

            object handle = CacheItemCtor.DefaultConstruct(ItemType);
            if (handle == null)
                Exception = getE("Constructor", ExcelName, "Default");

            return handle;
        }
        #endregion

        #region method
        public CacheItemMethod Method(string MethodName)
        {
            Exception = null;

            CacheItemMethod mi = extractCIM(MethodName);

            if (mi == null)
                Exception = getE("Method", ExcelName, MethodName);

            return mi;
        }

        public CacheItemMethod[] Method()
        {
            loadAllMethods();
            return _meth.Values.ToArray();
        }
        #endregion

        #region property
        public CacheItemProperty Property(string PropertyName)
        {
            Exception = null;

            CacheItemProperty pi = extractCIP(PropertyName);

            if (pi == null)
                Exception = getE("Property", ExcelName, PropertyName);

            return pi;
        }

        public CacheItemProperty[] Property()
        {
            loadAllProps();
            return _prop.Values.ToArray();
        }
        #endregion

        // private
        #region extract property/method info
        private CacheItemProperty extractCIP(string name)
        {
            CacheItemProperty p = null;
            if (!_prop.ContainsKey(name))
            {
                IEnumerable<PropertyInfo> pi = CU.ExcelRegisteredProperties(ItemType)
                    .Where(w => new CacheItemProperty(w).ExcelName == name);

                if (pi.Count() > 0)
                {
                    p = new CacheItemProperty(pi.First());
                    _prop.Add(name, p);
                }
            }
            else
                p = _prop[name];

            return p;
        }

        private CacheItemMethod extractCIM(string name)
        {
            CacheItemMethod m = null;
            if (!_meth.ContainsKey(name))
            {
                IEnumerable<MethodInfo> mi = CU.ExcelRegisteredMethods(ItemType)
                    .Where(w => new CacheItemMethod(w).ExcelName == name);

                if (mi.Count() > 0)
                {
                    m = new CacheItemMethod(mi.First());
                    _meth.Add(name, m);
                }
            }
            else
                m = _meth[name];

            return m;
        }

        private CacheItemCtor extractCtor(string name)
        {
            CacheItemCtor c = null;
            if (!_ctor.ContainsKey(name))
            {
                IEnumerable<ConstructorInfo> ci = CU.ExcelRegisteredCtors(ItemType)
                    .Where(w => new CacheItemCtor(w).ExcelName == name);

                if (ci.Count() > 0)
                {
                    c = new CacheItemCtor(ci.First());
                    _ctor.Add(name, c);
                }
            }
            else
                c = _ctor[name];

            return c;
        }
        #endregion

        #region load
        private void loadAllMethods()
        {
            if (!_allMeth)
            {
                MethodInfo[] mi = CU.ExcelRegisteredMethods(ItemType);
                if (mi.GetLength(0) != _meth.Count)
                {
                    foreach (MethodInfo m in mi)
                    {
                        CacheItemMethod c = new CacheItemMethod(m);
                        if (!_meth.ContainsKey(c.ExcelName))
                            _meth.Add(c.ExcelName, c);
                    }
                }

                _allMeth = true;
            }
        }

        private void loadAllProps()
        {
            if (!_allProp)
            {
                PropertyInfo[] pi = CU.ExcelRegisteredProperties(ItemType);
                if (pi.GetLength(0) != _prop.Count)
                {
                    foreach (PropertyInfo m in pi)
                    {
                        CacheItemProperty c = new CacheItemProperty(m);
                        if (!_prop.ContainsKey(c.ExcelName))
                            _prop.Add(c.ExcelName, c);
                    }
                }

                _allProp = true;
            }
        }

        private void loadAllCtors()
        {
            if (!_allCtor)
            {
                ConstructorInfo[] pi = CU.ExcelRegisteredCtors(ItemType);
                if (pi.GetLength(0) != _ctor.Count)
                {
                    foreach (ConstructorInfo m in pi)
                    {
                        CacheItemCtor c = new CacheItemCtor(m);
                        if (!_ctor.ContainsKey(c.ExcelName))
                            _ctor.Add(c.ExcelName, c);
                    }
                }

                _allCtor = true;
            }
        }
        #endregion

        private Exception getE(string type, string name, string typename)
        {
            return new Exception(string.Format("Invalid {0}: {1}.{2}", type, name, typename));
        }
    }
}
