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

using System.Reflection;
using CU = ExcelDna.Contrib.Cache.CacheUtilities;

namespace ExcelDna.Contrib.Cache
{
    internal class CacheItemProperty
    {
        PropertyInfo _pi;
        ExcelObjectPropertyAttribute _attr;

        public CacheItemProperty(PropertyInfo pi)
        {
            _pi = pi;
            _attr = CU.GetAttribute(_pi);
        }

        public string Name { get { return _pi.Name; } }
        public string ExcelName { get { return (_attr.Name == string.Empty) ? _pi.Name : _attr.Name; } }
        public string Description { get { return _attr.Description; } }
        public Type Type { get { return _pi.PropertyType; } }
        public Exception Exception { get; protected set; }

        public bool Set(object handle, object input, object[] index)
        {
            bool res = false;

            if (_pi.CanWrite)
            {
                try
                {
                    _pi.SetValue(handle, input, index);
                    Exception = null;
                    res = true;
                }
                catch (Exception e)
                {
                    Exception = e;
                }
            }
            else
                Exception = new Exception("Invoke error: Read Only", new InvalidOperationException());

            return res;
        }

        public object Get(object handle, object[] index)
        {
            object res = null;

            if (_pi.CanRead)
            {
                try
                {
                    res = _pi.GetValue(handle, index);
                    Exception = null;
                }
                catch (Exception e)
                {
                    Exception = e;
                }
            }
            else
                Exception = new Exception("Invoke error: Write Only", new InvalidOperationException());

            return res;
        }

        public object[,] Summary()
        {
            return new object[,] { { "Property", ExcelName, Description, Type.ToString() } };
        }

        public override bool Equals(object obj)
        {
            CacheItemProperty m = obj as CacheItemProperty;
            return this.ToString().Equals(m.ToString());
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("Property: \n\tName: {1}\n\tDescription: {2}\n\tType: {3}",
                ExcelName, Description, Type.ToString());
        }
    }
}
