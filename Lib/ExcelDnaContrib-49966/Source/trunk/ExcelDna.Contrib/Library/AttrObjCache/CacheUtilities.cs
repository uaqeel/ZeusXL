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

namespace ExcelDna.Contrib.Cache
{
    internal static class CacheUtilities
    {
        #region Excel Registration Filter
        public static bool IsExcelObj(Type t)
        {
            object[] xlObjAtt = t.GetCustomAttributes(typeof(ExcelObjectAttribute), false);
            return xlObjAtt.GetLength(0) > 0;
        }

        public static Type[] ExcelRegisteredObjects(Assembly asm)
        {
            return asm.GetTypes().Where<Type>(s => IsExcelObj(s)).ToArray<Type>();
        }

        public static Type[] ExcelRegisteredObjects()
        {
            Assembly[] a = AppDomain.CurrentDomain.GetAssemblies();
            List<Type> t = new List<Type>();

            foreach (Assembly ay in a)
                t.AddRange(ExcelRegisteredObjects(ay));

            return t.ToArray<Type>();
        }

        public static MethodInfo[] ExcelRegisteredMethods(Type t)
        {
            List<MethodInfo> m = new List<MethodInfo>();

            if (IsExcelObj(t))
            {
                MethodInfo[] am = t.GetMethods();
                for (int i = 0; i < am.GetLength(0); i++)
                {
                    object[] ama = am[i].GetCustomAttributes(typeof(ExcelObjectMethodAttribute), false);
                    if (ama.GetLength(0) > 0)
                        m.Add(am[i]);
                }
            }

            return m.ToArray<MethodInfo>();
        }

        public static ConstructorInfo[] ExcelRegisteredCtors(Type t)
        {
            List<ConstructorInfo> m = new List<ConstructorInfo>();

            if (IsExcelObj(t))
            {
                ConstructorInfo[] am = t.GetConstructors();
                for (int i = 0; i < am.GetLength(0); i++)
                {
                    object[] ama = am[i].GetCustomAttributes(typeof(ExcelObjectConstructorAttribute), false);
                    if (ama.GetLength(0) > 0)
                        m.Add(am[i]);
                }
            }

            return m.ToArray<ConstructorInfo>();
        }

        public static PropertyInfo[] ExcelRegisteredProperties(Type t)
        {
            List<PropertyInfo> p = new List<PropertyInfo>();

            if (IsExcelObj(t))
            {
                PropertyInfo[] ap = t.GetProperties();
                for (int i = 0; i < ap.GetLength(0); i++)
                {
                    object[] apa = ap[i].GetCustomAttributes(typeof(ExcelObjectPropertyAttribute), false);
                    if (apa.GetLength(0) > 0)
                        p.Add(ap[i]);
                }
            }

            return p.ToArray<PropertyInfo>();
        }
        #endregion

        #region Invoke/Set

        #region method reflection
        public static bool ExtractMethod(object obj, string method, out MethodInfo mi)
        {
            bool res = true;
            try
            {
                mi = obj.GetType().GetMethod(method);
            }
            catch
            {
                res = false;
                mi = null;
            }

            return res;
        }

        public static bool CallObjectMethod(object obj, string method, object[] param, out object result)
        {
            bool ret = false;
            result = null;
            try
            {
                MethodInfo mi = obj.GetType().GetMethod(method);
                result = mi.Invoke(obj, param);
                ret = true;
            }
            catch (Exception e)
            {
                result = new Exception(string.Format("Invoke error on {0}.{1}", obj.ToString(), method), e);
            }

            return ret;
        }
        #endregion

        #region property reflection
        public static bool ExtractProperty(object obj, string property, out PropertyInfo pi)
        {
            bool res = true;
            try
            {
                pi = obj.GetType().GetProperty(property);
            }
            catch
            {
                res = false;
                pi = null;
            }

            return res;
        }

        public static bool SetObjectProp(object objectHandle, string property, object param,
            object[] index, out object result)
        {
            result = null;   
            PropertyInfo pi;

            bool ret = ExtractProperty(objectHandle, property, out pi);
            if (ret && pi.CanWrite)
            {
                try
                {
                    pi.SetValue(objectHandle, param, index);
                }
                catch (Exception e)
                {
                    result = new Exception(string.Format("Invoke error on {0}.{1}", objectHandle.ToString(), property), e);
                }
            }
            else
                result = new Exception("Invoke error: Read Only", new InvalidOperationException());
            

            return ret;
        }

        public static bool GetObjectProp(object objectHandle, string property,
            object[] index, out object result)
        {
            result = null;
            PropertyInfo pi;

            bool ret = ExtractProperty(objectHandle, property, out pi);
            if (ret && pi.CanRead)
            {
                try
                {
                    pi.GetValue(objectHandle, index);
                }
                catch (Exception e)
                {
                    result = new Exception(string.Format("Invoke error on {0}.{1}", objectHandle.ToString(), property), e);
                }
            }
            else
                result = new Exception("Invoke error: Write Only", new InvalidOperationException());


            return ret;
        }
        #endregion

        #endregion

        #region get attribute
        public static ExcelObjectAttribute GetAttribute(Type t)
        {
            object[] o = t.GetCustomAttributes(typeof(ExcelObjectAttribute), false);
            ExcelObjectAttribute r = (ExcelObjectAttribute)o[0];

            return r;
        }

        public static ExcelObjectConstructorAttribute GetAttribute(ConstructorInfo ci)
        {
            object[] o = ci.GetCustomAttributes(typeof(ExcelObjectConstructorAttribute), false);
            ExcelObjectConstructorAttribute r = (ExcelObjectConstructorAttribute)o[0];

            return r;
        }

        public static ExcelObjectMethodAttribute GetAttribute(MethodInfo mi)
        {
            object[] o = mi.GetCustomAttributes(typeof(ExcelObjectMethodAttribute), false);
            ExcelObjectMethodAttribute r = (ExcelObjectMethodAttribute)o[0];

            return r;
        }

        public static ExcelObjectPropertyAttribute GetAttribute(PropertyInfo pi)
        {
            object[] o = pi.GetCustomAttributes(typeof(ExcelObjectPropertyAttribute), false);
            ExcelObjectPropertyAttribute r = (ExcelObjectPropertyAttribute)o[0];

            return r;
        }
        #endregion
    }
}
