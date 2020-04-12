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

using ExcelDna.Integration;
using System.Reflection;

namespace ExcelDna.Contrib.Library
{
    internal static class ParameterCleaner
    {
        public static object Arg(object o, Type target, out Exception ex)
        {
            ex = null;
            bool typeErr = false;
            object res = null;

            if (target.IsArray)
            {
                if (target.GetArrayRank() == 1)
                {
                    if (o is object[,])
                    {
                        object[,] p = (object[,])o;

                        if (p.GetLength(0) != 1 && p.GetLength(1) != 1)
                            typeErr = true;
                        else
                        {
                            object[] g = null;
                            TwoDtoOne(p, out g);

                            object[] inv = new object[] { g, typeErr };
                            MethodInfo convert = getConv(target.GetElementType(), typeof(object[]));
                            res = convert.Invoke(null, inv);
                            typeErr = (bool)inv[1];
                        }
                    }
                    else
                    {
                        Type und = target.GetElementType();
                        Array padd = Array.CreateInstance(und, 1);
                        typeErr = convSingle(o, und, out o);
                        padd.SetValue(o, 0);
                        res = padd;
                    }
                }

                if (target.GetArrayRank() == 2)
                {
                    if (o is object[,])
                    {
                        object[,] p = (object[,])o;

                        object[] inv = new object[] { p, typeErr };
                        MethodInfo convert = getConv(target.GetElementType(), typeof(object[,]));
                        res = convert.Invoke(null, inv);
                        typeErr = (bool)inv[1];
                    }
                    else
                    {
                        Type und = target.GetElementType();
                        Array padd = Array.CreateInstance(und, 1, 1);
                        typeErr = convSingle(o, und, out o);
                        padd.SetValue(o, 0, 0);
                        res = padd;
                    }
                }
            }
            else
            {
                if (!(o is ExcelEmpty))
                    typeErr = convSingle(o, target, out res);
                else
                    res = (target.IsValueType) ? Activator.CreateInstance(target) : null;
            }

            if (typeErr)
                ex = new ArgumentException(string.Format("Cannot convert {0} to {1}", o.GetType().ToString(), target.Name));

            return res;
        }

        public static object[] MultArgs(Type[] target, out Exception ex, params object[] o)
        {
            o = NotMissing(o);

            ex = null;
            List<object> r = new List<object>();
            if (o.GetLength(0) == target.GetLength(0))
            {
                for (int i = 0; i < o.GetLength(0) && (ex == null); i++)
                {
                    object oi = Arg(o[i], target[i], out ex);
                    if (ex == null)
                        r.Add(oi);
                }
            }
            else
                ex = new Exception("Parameter Count Mismatch in ParameterCleaner");
            
            return r.ToArray<object>();
        }

        public static object[] NotMissing(params object[] o)
        {
            List<object> nem = new List<object>();
            for (int i = 0; i < o.GetLength(0); i++)
                if (!(o[i] is ExcelMissing)) nem.Add(o[i]);
            return nem.ToArray();
        }

        #region Build2DOutput: Get object Return Val in object[,] form
        public static void Build2DOutput(string name, object o, out object[,] r)
        {
            int sh = (name == string.Empty) ? 0 : 1;
            if (o.GetType().IsArray)
            {
                if (o.GetType().GetArrayRank() == 1)
                {
                    Array t = (Array)o;
                    r = new object[t.GetLength(0), 1 + sh];

                    for (int i = 0; i < t.GetLength(0); i++)
                    {
                        if (sh > 0)
                            r[i, 0] = (i == 0) ? name : string.Empty;

                        r[i, sh] = t.GetValue(i);
                    }
                }
                else
                {
                    Array t = (Array)o;
                    r = new object[t.GetLength(0), t.GetLength(1) + sh];

                    for (int i = 0; i < t.GetLength(0); i++)
                    {
                        if (sh > 0)
                            r[i, 0] = (i == 0) ? name : string.Empty;

                        for (int j = 0; j < t.GetLength(1); j++)
                            r[i, j + sh] = t.GetValue(i, j);
                    }
                }
            }
            else
                r = (sh > 0) ? new object[,] { { name, o } } : new object[,] { { o } };
        }
        #endregion

        #region private
        private static void TwoDtoOne(object[,] o, out object[] r)
        {
            int x = o.GetLength(0);
            int y = o.GetLength(1);
            bool dir = x > y;

            r = new object[Math.Max(x, y)];
            for (int i = 0; i < Math.Max(x, y); i++)
                r[i] = (dir) ? o[i, 0] : o[0, i];
        }

        private static void OneDtoTwo(object[] o, out object[,] r)
        {
            int x = o.GetLength(0);
            r = new object[x, 1];
            for (int i = 0; i < x; i++)
                r[i, 0] = o[i];
        }

        private static bool convSingle(object o, Type target, out object f)
        {
            bool res = false;

            object[] inv = new object[] { o, res };
            MethodInfo convert = getConv(target, typeof(object));
            f = convert.Invoke(null, inv);
            res = (bool)inv[1];

            return res;
        }

        private static MethodInfo getConv(Type und, Type signature)
        {
            ParameterModifier[] arrPmods = new ParameterModifier[1];
            arrPmods[0] = new ParameterModifier(2);
            arrPmods[0][0] = false; // not out
            arrPmods[0][1] = true;  // out

            Type t = typeof(ObjectConv);
            MethodInfo convert = t.GetMethod(ObjectConv.GenericFuncs.Convert.ToString(), 
                new Type[] { signature, Type.GetType("System.Boolean&") }, arrPmods);

            convert = convert.MakeGenericMethod(und);

            return convert;
        }
        #endregion
    }
}
