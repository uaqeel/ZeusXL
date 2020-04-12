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
using System.Text;

using ExcelDna.Integration;

namespace ExcelDna.Contrib.Library
{
    internal static class ObjectConv
    {
        public enum GenericFuncs
        {
            IsType,
            Convert
        }

        public static bool Logical(object i)
        {
            bool res = false;
            double dres = 0.0;

            if (bool.TryParse(i.ToString(), out res))
                return res;
            else
            {
                if (double.TryParse(i.ToString(), out dres))
                    return (dres != 0.0);
                else
                    return res;
            }
        }

        #region IsType<T>
        public static bool IsType<T>(object i)
        {
            if (typeof(T) == typeof(bool))
                return i != null && (i is int || i is bool || i is double || i is long);
            else
            {
                if (typeof(T) == typeof(DateTime))
                {
                    object o;
                    return XlCall.TryExcel(XlCall.xlCoerce, out o, i) == XlCall.XlReturn.XlReturnSuccess;
                }
                else
                {
                    if (typeof(T).IsArray)
                    {
                        bool res = true;
                        if (typeof(T).GetArrayRank() == 1)
                        {
                            object[] ia = (object[])i;
                            for (int j = 0; j < ia.GetLength(0) && res; j++)
                                res = ia[j].GetType() == typeof(T);
                        }
                        else
                        {
                            object[,] ia = (object[,])i;
                            for (int j = 0; j < ia.GetLength(0) && res; j++)
                            {
                                for (int k = 0; k < ia.GetLength(1) && res; k++)
                                    res = ia[j, k].GetType() == typeof(T);
                            }
                        }

                        return res;
                    }
                    else
                        return ((i != null && i is T)) ? true : false;
                }
            }
        }
        #endregion

        public static T Convert<T>(object i, out bool typeErr)
        {
            Type type = typeof(T);
            typeErr = false;
            T res;

            if (type == typeof(bool) && IsType<bool>(i))
                res = (T)(object)ObjectConv.Logical(i);
            else
            {
                if (type == typeof(DateTime) && IsType<DateTime>(i))
                {
                    object o;
                    double d;

                    XlCall.XlReturn r = XlCall.TryExcel(XlCall.xlCoerce, out o, i);
                    if (r == XlCall.XlReturn.XlReturnSuccess && double.TryParse(o.ToString(), out d))
                        res = (T)(object)DateTime.FromOADate(d);
                    else
                    {
                        typeErr = true;
                        res = (T)(object)DateTime.MinValue;
                    }
                }
                else
                {
                    if (IsType<double>(i) && type == typeof(int))
                    {
                        double t = (double)i;
                        res = (T)(object)System.Convert.ToInt32(t);
                    }
                    else
                    {
                        if (type == typeof(string) && !(i is ExcelEmpty))
                            res = (T)(object)i.ToString();
                        else
                        {
                            if (IsType<T>(i))
                                res = (T)i;
                            else
                            {
                                typeErr = (i is ExcelEmpty) ? false : true;
                                if (type == typeof(string))
                                    res = (T)(object)string.Empty;
                                else
                                    res = default(T);
                            }
                        }
                    }
                }
            }

            return res;
        }

        public static T Convert<T>(object i)
        {
            bool typeErr;
            return ObjectConv.Convert<T>(i, out typeErr);
        }

        public static T[] Convert<T>(object[] o, out bool typeErr)
        {
            typeErr = false;
            T[] res = new T[o.GetLength(0)];
            for (int i = 0; i < o.GetLength(0) && !typeErr; i++)
                res[i] = Convert<T>(o[i], out typeErr);

            if (typeErr)
                res = new T[] { default(T) };

            return res;
        }

        public static T[,] Convert<T>(object[,] o, out bool typeErr)
        {
            typeErr = false;
            T[,] res = new T[o.GetLength(0), o.GetLength(1)];
            for (int i = 0; i < o.GetLength(0) && !typeErr; i++)
            {
                for (int j = 0; j < o.GetLength(1) && !typeErr; j++)
                    res[i, j] = Convert<T>(o[i, j], out typeErr);
            }

            if (typeErr)
                res = new T[,] { { default(T) } };

            return res;
        }
    }
}
