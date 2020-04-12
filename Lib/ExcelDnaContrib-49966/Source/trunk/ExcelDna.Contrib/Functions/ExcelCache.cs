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
using System.Linq;
using System.Collections.Generic;

using System.Reflection;

using ExcelDna.Integration;
using ExcelDna.Contrib.Cache;
using ExcelDna.Contrib.Library;

using CU = ExcelDna.Contrib.Cache.CacheUtilities;

namespace ExcelDna.Contrib.Functions
{
    /// <summary>
    /// Excel interface to object cache
    /// </summary>
    public class ExcelCache
    {
        private const string CATEGORY = "ExcelDna.Contrib.ObjectCache";
        private static readonly ICacheManager _cache = new CacheManager();
        private static Dictionary<string, ICacheType> _regObj = new Dictionary<string, ICacheType>();

        #region ctors
        /// <summary>
        /// Initializes a new instance of an ExcelCache object
        /// </summary>
        public ExcelCache()
        {
            Type[] at = CU.ExcelRegisteredObjects();
            addToRegList(at);
        }

        /// <summary>
        /// Initializes a new instance of an ExcelCache object
        /// </summary>
        /// <param name="a">Assembly containing the Excel registered objects to be used</param>
        public ExcelCache(Assembly a)
        {
            Type[] at = CU.ExcelRegisteredObjects(a);
            addToRegList(at);
        }

        private void addToRegList(Type[] at)
        {
            foreach (Type t in at)
            {
                ICacheType ci = new CacheType(t);
                if (!_regObj.ContainsKey(ci.ExcelName))
                    _regObj.Add(ci.ExcelName, ci);
            }
        }
        #endregion

        /// <summary>
        /// Empty object cache.
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }

        #region AllObjects
        /// <summary>
        /// Displays the names of all Excel registered objects.
        /// </summary>
        /// <returns>object containing an array of class names</returns>
        [ExcelFunction(
            Category = CATEGORY,
            Name = "ALLOBJECTS",
            Description = "Display all object type names and descriptions.")]
        public static object AllObjects()
        {
            object[,] res = null;
            if (_regObj.Count > 0)
            {
                res = new object[_regObj.Count, 2];
                int i = 0;
                foreach (ICacheType o in _regObj.Values)
                {
                    res[i, 0] = o.ExcelName;
                    res[i, 1] = o.Description;
                    i++;
                }
            }
            else
                res = new object[,] { { ExcelError.ExcelErrorNull } };

            return res;
        }
        #endregion

        #region ShowCtorParams
        /// <summary>
        /// Displays name and parameters of the Excel registered constructors for the specified type.
        /// </summary>
        /// <param name="Object">Type name of the object to be created</param>
        /// <param name="p">Name of constructor</param>
        /// <returns>object containing an array of (string) parameter data</returns>
        [ExcelFunction(
            Category = CATEGORY,
            Name = "CTORPARAMS",
            Description = "Display constructor names and necessary input parameters.")]
        public static object ShowCtorParams(
            [ExcelArgument(Name = "ObjectType", Description = "Name of an object type.")] string Object,
            [ExcelArgument(Name = "Constructor",
                Description = "Name of the Constructor whose parameter data will be shown.")] string p)
        {
            object res = ExcelError.ExcelErrorNA;

            if (_regObj.ContainsKey(Object))
            {
                ICacheType ctype = _regObj[Object];

                CacheItemCtor c = ctype.Constructor(p);

                if (ctype.Exception == null)
                {
                    res = c.Summary();

                    if (c.Exception != null)
                        res = c.Exception.Message;
                }
                else
                    res = ctype.Exception.Message;
            }

            return res;
        }
        #endregion

        #region CreateObject with Constructor
        /// <summary>
        /// Instantiate an object and add it to the cache
        /// </summary>
        /// <param name="Object">Type name of the object to be created</param>
        /// <param name="c">Optional: name of constructor to use</param>
        /// <param name="o1">Optional: Constructor parameter</param>
        /// <param name="o2">Optional: Constructor parameter</param>
        /// <param name="o3">Optional: Constructor parameter</param>
        /// <param name="o4">Optional: Constructor parameter</param>
        /// <param name="o5">Optional: Constructor parameter</param>
        /// <param name="o6">Optional: Constructor parameter</param>
        /// <param name="o7">Optional: Constructor parameter</param>
        /// <param name="o8">Optional: Constructor parameter</param>
        /// <param name="o9">Optional: Constructor parameter</param>
        /// <param name="o10">Optional: Constructor parameter</param>
        /// <param name="o11">Optional: Constructor parameter</param>
        /// <param name="o12">Optional: Constructor parameter</param>
        /// <param name="o13">Optional: Constructor parameter</param>
        /// <param name="o14">Optional: Constructor parameter</param>
        /// <param name="o15">Optional: Constructor parameter</param>
        /// <param name="o16">Optional: Constructor parameter</param>
        /// <param name="o17">Optional: Constructor parameter</param>
        /// <param name="o18">Optional: Constructor parameter</param>
        /// <param name="o19">Optional: Constructor parameter</param>
        /// <param name="o20">Optional: Constructor parameter</param>
        /// <param name="o21">Optional: Constructor parameter</param>
        /// <param name="o22">Optional: Constructor parameter</param>
        /// <param name="o23">Optional: Constructor parameter</param>
        /// <param name="o24">Optional: Constructor parameter</param>
        /// <param name="o25">Optional: Constructor parameter</param>
        /// <param name="o26">Optional: Constructor parameter</param>
        /// <param name="o27">Optional: Constructor parameter</param>
        /// <param name="o28">Optional: Constructor parameter</param>
        /// <returns>object containing the handle key name</returns>
        [ExcelFunction(
            Category = CATEGORY,
            Name = "CREATE",
            Description = "Create an object.",
            IsMacroType = true)]
        public static object CreateObject(
            [ExcelArgument(Name = "ObjectType", Description = "Name of an object type.")] string Object,
            [ExcelArgument(Name = "[Constructor]", Description = "Name of the constructor that will be called.")] object c,
            [ExcelArgument(Name = "[Args1]", Description = "Parameter to be passed to the constructor.")] object o1,
            #region optional args 2 - 28
             [ExcelArgument(Name = "[Args2]", Description = "Parameter to be passed to the specified function.")] object o2,
             [ExcelArgument(Name = "[Args3]", Description = "Parameter to be passed to the specified function.")] object o3,
             [ExcelArgument(Name = "[Args4]", Description = "Parameter to be passed to the specified function.")] object o4,
             [ExcelArgument(Name = "[Args5]", Description = "Parameter to be passed to the specified function.")] object o5,
             [ExcelArgument(Name = "[Args6]", Description = "Parameter to be passed to the specified function.")] object o6,
             [ExcelArgument(Name = "[Args7]", Description = "Parameter to be passed to the specified function.")] object o7,
             [ExcelArgument(Name = "[Args8]", Description = "Parameter to be passed to the specified function.")] object o8,
             [ExcelArgument(Name = "[Args9]", Description = "Parameter to be passed to the specified function.")] object o9,
             [ExcelArgument(Name = "[Args10]", Description = "Parameter to be passed to the specified function.")] object o10,
             [ExcelArgument(Name = "[Args11]", Description = "Parameter to be passed to the specified function.")] object o11,
             [ExcelArgument(Name = "[Args12]", Description = "Parameter to be passed to the specified function.")] object o12,
             [ExcelArgument(Name = "[Args13]", Description = "Parameter to be passed to the specified function.")] object o13,
             [ExcelArgument(Name = "[Args14]", Description = "Parameter to be passed to the specified function.")] object o14,
             [ExcelArgument(Name = "[Args15]", Description = "Parameter to be passed to the specified function.")] object o15,
             [ExcelArgument(Name = "[Args16]", Description = "Parameter to be passed to the specified function.")] object o16,
             [ExcelArgument(Name = "[Args17]", Description = "Parameter to be passed to the specified function.")] object o17,
             [ExcelArgument(Name = "[Args18]", Description = "Parameter to be passed to the specified function.")] object o18,
             [ExcelArgument(Name = "[Args19]", Description = "Parameter to be passed to the specified function.")] object o19,
             [ExcelArgument(Name = "[Args20]", Description = "Parameter to be passed to the specified function.")] object o20,
             [ExcelArgument(Name = "[Args21]", Description = "Parameter to be passed to the specified function.")] object o21,
             [ExcelArgument(Name = "[Args22]", Description = "Parameter to be passed to the specified function.")] object o22,
             [ExcelArgument(Name = "[Args23]", Description = "Parameter to be passed to the specified function.")] object o23,
             [ExcelArgument(Name = "[Args24]", Description = "Parameter to be passed to the specified function.")] object o24,
             [ExcelArgument(Name = "[Args25]", Description = "Parameter to be passed to the specified function.")] object o25,
             [ExcelArgument(Name = "[Args26]", Description = "Parameter to be passed to the specified function.")] object o26,
             [ExcelArgument(Name = "[Args27]", Description = "Parameter to be passed to the specified function.")] object o27,
             [ExcelArgument(Name = "[Args28]", Description = "Parameter to be passed to the specified function.")] object o28
            #endregion
        )
        {
            object newHandle = ExcelError.ExcelErrorNA;

            if (_regObj.ContainsKey(Object))
            {
                newHandle = _cache.Register(new CacheItem(_regObj[Object] as CacheType));

                Exception e = null;
                ICacheItem ici = _cache.Lookup(newHandle.ToString()) as CacheItem;

                if (!(c is ExcelMissing))
                {
                    CacheItemCtor ctor = _regObj[Object].Constructor(c.ToString());
                    e = _regObj[Object].Exception;

                    if (e == null)
                    {
                        Type[] parTypes = ctor.Parameters.Select(s => s.ParameterType).ToArray();

                        object[] input = null;
                        if (parTypes.GetLength(0) > 0)
                            input = ParameterCleaner.MultArgs(parTypes, out e, o1, o2, o3, o4, o5, o6, o7, o8, o9, o10,
                                o11, o12, o13, o14, o15, o16, o17, o18, o19, o20, o21, o22, o23, o24, o25, o26, o27, o28);

                        ici.New(c.ToString(), input);
                        e = ici.Exception;
                    }
                }
                else
                {
                    ici.New();
                    e = ici.Exception;
                }

                if (e != null)
                {
                    _cache.Remove(newHandle.ToString());
                    newHandle = e.Message;
                }
            }

            return newHandle;
        }
        #endregion

        #region Cache
        /// <summary>
        /// Displays all active objects in the cache.
        /// </summary>
        /// <returns>object containing an array of all active object handles.</returns>
        [ExcelFunction(
            Category = CATEGORY,
            Name = "CACHE",
            Description = "Display all active objects.",
            IsVolatile = true)]
        public static object Cache()
        {
            object[,] res = null;
            if (_cache.Count > 0)
            {
                int i = 0;
                res = new object[_cache.Count, 1];
                foreach (string o in _cache.Keys)
                {
                    res[i, 0] = o;
                    i++;
                }
            }
            else
                res = new object[,] { { ExcelError.ExcelErrorNull } };

            return res;
        }
        #endregion

        #region Remove
        /// <summary>
        /// Remove an object from the cache.
        /// </summary>
        /// <param name="o">Object handle.</param>
        /// <returns>object containing a string confirming removal of specified handle.</returns>
        [ExcelFunction(
            Category = CATEGORY,
            Name = "REMOVE",
            Description = "Remove the given handle. To remove all, pass ALL")]
        public static object Remove(
            [ExcelArgument(Name = "ObjectHandle", Description = "Handle to an object.")] string o)
        {
            object res = string.Format("REMOVED {0}", o);
            if (o.ToString() == "ALL")
                _cache.Clear();
            else
                _cache.Remove(o);
            
            return res;
        }
        #endregion

        #region ObjectInfo
        /// <summary>
        /// Displays the name, return type and description of all Excel registered 
        /// constructors, properties and methods.
        /// </summary>
        /// <param name="o">Object type name</param>
        /// <returns>object containing array of all info</returns>
        [ExcelFunction(
            Category = CATEGORY,
            Name = "TYPEINFO",
            Description = "Display the object type's property and method names and descriptions.")]
        public static object AllTypeInfo(
            [ExcelArgument(Name = "ObjectType", Description = "Name of an object type.")] string o)
        {
            object res = ExcelError.ExcelErrorNA;

            if (_regObj.ContainsKey(o))
                res = _regObj[o].AllTypeInfo();

            return res;
        }
        #endregion

        #region SetProperties
        /// <summary>
        /// Assign a value to the specified property of the object associated with the given object handle key.
        /// </summary>
        /// <param name="h">Object handle key</param>
        /// <param name="p">Name of property</param>
        /// <param name="o">Data to be assigned</param>
        /// <returns>object containing confirmation of data assginment</returns>
        [ExcelFunction(
            Category = CATEGORY,
            Name = "SET",
            Description = "Set the object's property to the given value.")]
        public static object SetProperty(
            [ExcelArgument(Name = "ObjectHandle", Description = "Handle to an object.")] string h,
            [ExcelArgument(Name = "Property", Description = "Name of property to which data will be assigned.")] string p,
            [ExcelArgument(Name = "PropertyData", Description = "Data to be assiged.")] object o)
        {
            object res = ExcelError.ExcelErrorNA;
            if (_cache.ContainsKey(h))
            {
                ICacheItem handle = _cache.Lookup(h) as CacheItem;

                CacheItemProperty cip = handle.CacheType.Property(p);
                if (handle.CacheType.Exception == null)
                {
                    Exception e = null;
                    o = ParameterCleaner.Arg(o, cip.Type, out e);

                    if (e == null)
                    {
                        handle.Set(p, o);
                        if (handle.Exception != null)
                            res = handle.Exception.Message;
                        else
                            res = string.Format("SET {0}", p);
                    }
                    else
                        res = e.Message;
                }
                else
                    res = handle.CacheType.Exception.Message;
            }

            return res;
        }
        #endregion

        #region GetPropertyData
        /// <summary>
        /// Display the property data for object associated with the specified object handle key
        /// </summary>
        /// <param name="h">Object handle key</param>
        /// <param name="p">Optional: name of property to be retrieved. If ommitted, all public
        /// stat data will be returned</param>
        /// <returns>object containing property data</returns>
        [ExcelFunction(
            Category = CATEGORY,
            Name = "GET",
            Description = "Display property names and current values.")]
        public static object ShowPropertyData(
            [ExcelArgument(Name = "ObjectHandle", Description = "Handle to an object.")] string h,
            [ExcelArgument(Name = "[Property]", Description = "Name of the property being retrieved.")] object p)
        {
            object res = ExcelError.ExcelErrorNA;

            if (_cache.ContainsKey(h))
            {
                ICacheItem handle = _cache.Lookup(h) as CacheItem;

                if (!(p is ExcelMissing))
                    res = handle.Get(p.ToString());
                else
                    res = handle.Get();

                if (handle.Exception != null)
                    res = handle.Exception.Message;
            }

            return res;
        }
        #endregion

        #region ShowMethodParams
        /// <summary>
        /// Displays the parameter information for the speficied method
        /// </summary>
        /// <param name="h">Name of object type</param>
        /// <param name="p">Name of method</param>
        /// <returns>object containing array of method</returns>
        [ExcelFunction(
            Category = CATEGORY,
            Name = "METHODPARAMS",
            Description = "Display method names and necessary input parameters.")]
        public static object ShowMethodParams(
            [ExcelArgument(Name = "ObjectType", Description = "Type name of an object.")] string h,
            [ExcelArgument(Name = "Method", Description = "Name of the method whose parameter data will be shown.")] string p)
        {
            object res = ExcelError.ExcelErrorNA;

            if (_regObj.ContainsKey(h))
            {
                CacheItemMethod cim = _regObj[h].Method(p);

                res = cim.Summary();

                if (cim.Exception != null)
                    res = cim.Exception.Message;
            }

            return res;
        }
        #endregion

        #region CallObjectMethod
        /// <summary>
        /// Calls the specified method for the object associated with the given object handle key.
        /// </summary>
        /// <param name="h">Object handle key</param>
        /// <param name="m">Name of method to be called</param>
        /// <param name="o1">Optional: Constructor parameter</param>
        /// <param name="o2">Optional: Constructor parameter</param>
        /// <param name="o3">Optional: Constructor parameter</param>
        /// <param name="o4">Optional: Constructor parameter</param>
        /// <param name="o5">Optional: Constructor parameter</param>
        /// <param name="o6">Optional: Constructor parameter</param>
        /// <param name="o7">Optional: Constructor parameter</param>
        /// <param name="o8">Optional: Constructor parameter</param>
        /// <param name="o9">Optional: Constructor parameter</param>
        /// <param name="o10">Optional: Constructor parameter</param>
        /// <param name="o11">Optional: Constructor parameter</param>
        /// <param name="o12">Optional: Constructor parameter</param>
        /// <param name="o13">Optional: Constructor parameter</param>
        /// <param name="o14">Optional: Constructor parameter</param>
        /// <param name="o15">Optional: Constructor parameter</param>
        /// <param name="o16">Optional: Constructor parameter</param>
        /// <param name="o17">Optional: Constructor parameter</param>
        /// <param name="o18">Optional: Constructor parameter</param>
        /// <param name="o19">Optional: Constructor parameter</param>
        /// <param name="o20">Optional: Constructor parameter</param>
        /// <param name="o21">Optional: Constructor parameter</param>
        /// <param name="o22">Optional: Constructor parameter</param>
        /// <param name="o23">Optional: Constructor parameter</param>
        /// <param name="o24">Optional: Constructor parameter</param>
        /// <param name="o25">Optional: Constructor parameter</param>
        /// <param name="o26">Optional: Constructor parameter</param>
        /// <param name="o27">Optional: Constructor parameter</param>
        /// <param name="o28">Optional: Constructor parameter</param>
        /// <returns></returns>
        [ExcelFunction(
            Category = CATEGORY,
            Name = "CALLMETHOD",
            Description = "Call the specified method.")]
        public static object CallMethod(
            [ExcelArgument(Name = "ObjectHandle", Description = "Handle to an object.")] string h,
            [ExcelArgument(Name = "Method", Description = "Name of the method that will be called.")] string m,
            [ExcelArgument(Name = "[Args1]", Description = "Parameter to be passed to the specified function.")] object o1,
            #region optional args 2 - 28
             [ExcelArgument(Name = "[Args2]", Description = "Parameter to be passed to the specified function.")] object o2,
             [ExcelArgument(Name = "[Args3]", Description = "Parameter to be passed to the specified function.")] object o3,
             [ExcelArgument(Name = "[Args4]", Description = "Parameter to be passed to the specified function.")] object o4,
             [ExcelArgument(Name = "[Args5]", Description = "Parameter to be passed to the specified function.")] object o5,
             [ExcelArgument(Name = "[Args6]", Description = "Parameter to be passed to the specified function.")] object o6,
             [ExcelArgument(Name = "[Args7]", Description = "Parameter to be passed to the specified function.")] object o7,
             [ExcelArgument(Name = "[Args8]", Description = "Parameter to be passed to the specified function.")] object o8,
             [ExcelArgument(Name = "[Args9]", Description = "Parameter to be passed to the specified function.")] object o9,
             [ExcelArgument(Name = "[Args10]", Description = "Parameter to be passed to the specified function.")] object o10,
             [ExcelArgument(Name = "[Args11]", Description = "Parameter to be passed to the specified function.")] object o11,
             [ExcelArgument(Name = "[Args12]", Description = "Parameter to be passed to the specified function.")] object o12,
             [ExcelArgument(Name = "[Args13]", Description = "Parameter to be passed to the specified function.")] object o13,
             [ExcelArgument(Name = "[Args14]", Description = "Parameter to be passed to the specified function.")] object o14,
             [ExcelArgument(Name = "[Args15]", Description = "Parameter to be passed to the specified function.")] object o15,
             [ExcelArgument(Name = "[Args16]", Description = "Parameter to be passed to the specified function.")] object o16,
             [ExcelArgument(Name = "[Args17]", Description = "Parameter to be passed to the specified function.")] object o17,
             [ExcelArgument(Name = "[Args18]", Description = "Parameter to be passed to the specified function.")] object o18,
             [ExcelArgument(Name = "[Args19]", Description = "Parameter to be passed to the specified function.")] object o19,
             [ExcelArgument(Name = "[Args20]", Description = "Parameter to be passed to the specified function.")] object o20,
             [ExcelArgument(Name = "[Args21]", Description = "Parameter to be passed to the specified function.")] object o21,
             [ExcelArgument(Name = "[Args22]", Description = "Parameter to be passed to the specified function.")] object o22,
             [ExcelArgument(Name = "[Args23]", Description = "Parameter to be passed to the specified function.")] object o23,
             [ExcelArgument(Name = "[Args24]", Description = "Parameter to be passed to the specified function.")] object o24,
             [ExcelArgument(Name = "[Args25]", Description = "Parameter to be passed to the specified function.")] object o25,
             [ExcelArgument(Name = "[Args26]", Description = "Parameter to be passed to the specified function.")] object o26,
             [ExcelArgument(Name = "[Args27]", Description = "Parameter to be passed to the specified function.")] object o27,
             [ExcelArgument(Name = "[Args28]", Description = "Parameter to be passed to the specified function.")] object o28
            #endregion
)
        {
            object res = ExcelError.ExcelErrorNA;
            if (_cache.ContainsKey(h))
            {
                object or = null;
                CacheItem handle = _cache.Lookup(h) as CacheItem;

                CacheItemMethod cim = handle.CacheType.Method(m);
                Exception e = handle.CacheType.Exception;

                object[] input = null;
                if (e == null)
                {
                    Type[] parTypes = cim.Parameters.Select(s => s.ParameterType).ToArray();

                    if (parTypes.GetLength(0) > 0)
                        input = ParameterCleaner.MultArgs(parTypes, out e, o1, o2, o3, o4, o5, o6, o7, o8, o9, o10,
                            o11, o12, o13, o14, o15, o16, o17, o18, o19, o20, o21, o22, o23, o24, o25, o26, o27, o28);

                    or = handle.Call(m, input);
                    e = handle.Exception;

                    if (e != null)
                    {
                        string inner = (e.InnerException == null) ? string.Empty : e.InnerException.Message;
                        res = string.Format("{0} {1}", e.Message, inner);
                    }
                    else
                        res = or;
                }
                else
                    res = e.Message;
            }

            return res;
        }
        #endregion
    }
}
