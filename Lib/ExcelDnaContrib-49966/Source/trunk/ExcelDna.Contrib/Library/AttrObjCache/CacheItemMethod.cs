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
    internal class CacheItemMethod
    {
        MethodInfo _mi;
        ExcelObjectMethodAttribute _attr;

        public CacheItemMethod(MethodInfo mi)
        {
            _mi = mi;
            _attr = CU.GetAttribute(_mi);
        }

        public string Name { get { return _mi.Name; } }
        public string ExcelName { get { return (_attr.Name == string.Empty) ? _mi.Name : _attr.Name; } }
        public string Description { get { return _attr.Description; } }

        public Type ReturnType { get { return _mi.ReturnType; } }
        public ParameterInfo[] Parameters { get { return _mi.GetParameters(); } }

        public Exception Exception { get; protected set; }

        public object Call(object handle, object[] input)
        {
            object or = null;
            try
            {
                or = _mi.Invoke(handle, input);
                Exception = null;
            }
            catch (Exception e)
            {
                Exception = e;
            }

            return or;
        }

        public object[,] Summary()
        {
            ParameterInfo[] pi = _mi.GetParameters();
            int c = pi.GetLength(0);
            object[,] r = new object[c + 1 + Math.Min(c, 1), 4];

            r[0, 0] = string.Format("Count: {0}", c);
            r[0, 1] = ExcelName;
            r[0, 2] = ReturnType.Name;
            r[0, 3] = Description;

            if (c > 0)
            {
                r[1, 0] = "Position";
                r[1, 1] = "Name";
                r[1, 2] = "Type";
                r[1, 3] = "Description";

                for (int i = 0; i < c; i++)
                {
                    r[i + 2, 0] = pi[i].Position;
                    r[i + 2, 1] = pi[i].Name;
                    r[i + 2, 2] = pi[i].ParameterType.Name;
                    r[i + 2, 3] = string.Empty;
                }
            }

            return r;
        }

        public override bool Equals(object obj)
        {
            CacheItemMethod m = obj as CacheItemMethod;
            return this.ToString().Equals(m.ToString());
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("Method: \n\tName: {1}\n\tDescription: {2}\n\tType: {3}",
                ExcelName, Description, ReturnType.ToString());
        }
    }
}
