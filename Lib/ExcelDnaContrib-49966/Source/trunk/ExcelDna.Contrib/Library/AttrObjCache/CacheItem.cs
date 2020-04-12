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

using ExcelDna.Contrib.Library;
using CU = ExcelDna.Contrib.Cache.CacheUtilities;
using System.Reflection;

namespace ExcelDna.Contrib.Cache
{
    internal class CacheItem : ICacheItem, IDisposable
    {
        object _handle = null;
        private CacheType _ct;

        private bool _disposed = false;

        public CacheItem(CacheType ct)
        {
            _ct = ct;
        }

        public CacheType CacheType { get { return _ct; } }
        public Type ItemType { get { return _ct.ItemType; } }
        public string ExcelName { get { return _ct.ExcelName; } }
        public string Description { get { return _ct.Description; } }
        public Exception Exception { get; protected set; }

        public object Handle
        {
            get
            {
                if (_handle == null)
                {
                    _handle = _ct.CreateInstance();
                    Exception = _ct.Exception;
                }

                return _handle;
            }
        }

        #region create
        public void New()
        {
            Exception = null;
            _handle = _ct.CreateInstance();
            Exception = _ct.Exception;
        }

        public void New(string Constructor, object[] o)
        {
            Exception = null;
            _handle = _ct.CreateInstance(Constructor, o);
            Exception = _ct.Exception;
        }
        #endregion

        #region property access
        public void Set(string Property, object o)
        {
            Exception = null;

            CacheItemProperty pi = _ct.Property(Property);

            if (_ct.Exception == null)
            {
                pi.Set(Handle, o, null);
                Exception = pi.Exception;
            }
            else
                Exception = _ct.Exception;
        }

        public object Get(string Property)
        {
            Exception = null;

            CacheItemProperty pi = _ct.Property(Property);

            object res = null;
            if (_ct.Exception == null)
            {
                res = pi.Get(Handle, null);
                if (pi.Exception == null)
                {
                    if (res.GetType().IsArray)
                    {
                        object[,] p = null;
                        ParameterCleaner.Build2DOutput(string.Empty, res, out p);
                        res = p;
                    }
                }
                else
                    Exception = pi.Exception;
            }
            else
                Exception = _ct.Exception;

            return res;
        }

        public object Get()
        {
            Exception = null;

            List<object[,]> r = new List<object[,]>();

            int x = 0, y = 0;
            foreach (CacheItemProperty k in _ct.Property())
            {
                object t = k.Get(Handle, null);
                if (k.Exception != null)
                {
                    Exception = k.Exception;
                    break;
                }
                else
                {
                    object[,] p = null;
                    ParameterCleaner.Build2DOutput(k.ExcelName, t, out p);
                    r.Add(p);

                    x += p.GetLength(0);
                    y = Math.Max(y, p.GetLength(1));
                }
            }

            object[,] res = new object[x, y];
            for (int i = 0, z = 0; i < r.Count; i++)
            {
                for (int j = 0; j < r[i].GetLength(0); j++)
                {
                    for (int k = 0; k < r[i].GetLength(1); k++)
                        res[z + j, k] = r[i][j, k];

                    for (int k = r[i].GetLength(1); k < y; k++)
                        res[z + j, k] = string.Empty;
                }

                z += r[i].GetLength(0);
            }

            return res;
        }
        #endregion

        #region method access
        public object Call(string Method, object[] o)
        {
            Exception = null;

            CacheItemMethod mi = _ct.Method(Method);

            object res = null;
            if (_ct.Exception == null)
            {
                res = mi.Call(Handle, o);
                Exception = mi.Exception;
            }
            else
                Exception = _ct.Exception;

            return res;
        }
        #endregion

        #region IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_handle != null && _handle is IDisposable)
                        (_handle as IDisposable).Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
