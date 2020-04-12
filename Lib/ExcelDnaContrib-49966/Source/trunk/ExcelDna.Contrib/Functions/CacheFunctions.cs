using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExcelDna.Contrib.Library;
using ExcelDna.Integration;
using ExcelDna.Contrib.Cache;

namespace ExcelDna.Contrib.Functions
{
    /// <summary>
    /// Contains ExcelDna functions relating to cache management
    /// </summary>
    //public class CacheFunctions
    //{
    //    private const string CATEGORY = "ExcelDna.Contrib.ObjectCache";
    //    private static readonly ICacheManager _cache = new CacheManager();

    //    /// <summary>
    //    /// Registers the given object in the cache
    //    /// </summary>
    //    /// <param name="o">object to add to cache</param>
    //    /// <returns>The key that was used to cache the object with</returns>
    //    [ExcelFunction(Category = CATEGORY, Description = "Display cached object")]
    //    public static string RegisterObjectInCache([ExcelArgument(Name="Object",Description="Object to Cache")] object o)
    //    {
    //        return _cache.Register(o);
    //    }

    //    /// <summary>
    //    /// Registers the given object in the cache
    //    /// </summary>
    //    /// <param name="o">object to add to cache</param>
    //    /// <returns>The key that was used to cache the object with
    //    /// or an empty string if object cannot be found</returns>
    //    [ExcelFunction(Category = CATEGORY, Description = "Display cached object")]
    //    public static object LookupObjectInCache([ExcelArgument(Name="Key",Description="Key to lookup")] string key)
    //    {
    //        try
    //        {
    //            return _cache.Lookup(key);
    //        }
    //        catch
    //        {
    //            return string.Empty;
    //        }
    //    }

    //    /// <summary>
    //    /// Display the contents of an object that is currently in the cache
    //    /// </summary>
    //    /// <param name="key">The key of the object in cache</param>
    //    /// <returns>An two column array of properties/fields and their respective values</returns>
    //    [ExcelFunction(Category = CATEGORY, Description = "Display cached object")]
    //    public static object[,] DisplayObjectInCache([ExcelArgument(Description = "The key of the object to display")] string key)
    //    {
    //        var o = _cache.Lookup(key);
    //        var items = new List<KeyValuePair<string, object>>();

    //        // get all fields
    //        foreach (var field in o.GetType().GetFields())
    //        {
    //            items.Add(new KeyValuePair<string, object>(field.Name, field.GetValue(o)));
    //        }

    //        // get all 'getter' properties
    //        foreach (var method in o.GetType().GetMethods())
    //        {
    //            if (method.Name.StartsWith("get_"))
    //                try
    //                {                        
    //                    items.Add(new KeyValuePair<string, object>(method.Name.Substring(4), method.Invoke(o, null)));
    //                }
    //                catch (Exception ex)
    //                {
    //                    Console.WriteLine(ex.Message);
    //                }
    //        }

    //        // return as obj array
    //        var ret = new object[items.Count, 2];
    //        for (int i = 0; i < items.Count; i++)
    //        {
    //            ret[i, 0] = items[i].Key;
    //            ret[i, 1] = items[i].Value;
    //        }
    //        return ret;
    //    }

    //    /// <summary>
    //    /// Removes all objects that are currently cached
    //    /// </summary>
    //    [ExcelFunction(Category = CATEGORY, Description = "Remove all cached objects")]
    //    public static void ClearCache()
    //    {
    //        _cache.Clear();
    //    }
 
    //    /// <summary>
    //    /// Removes a specific object from the cache
    //    /// </summary>
    //    /// <param name="key">The key of the object to remove</param>
    //    [ExcelFunction(Category = CATEGORY, Description = "Remove a specific cached object")]
    //    public static void Remove([ExcelArgument(Description = "The key of the object to remove")] string key)
    //    {
    //        _cache.Remove(key);
    //    }

    //    /// <summary>
    //    /// Cache a specific object
    //    /// </summary>
    //    /// <param name="o">The object to cache</param>
    //    /// <param name="key">The key to use when caching</param>
    //    /// <returns>The key that was used to cache the object with</returns>
    //    internal static string Register(object o, string key)
    //    {
    //        return _cache.Register(o, key);
    //    }

    //    /// <summary>
    //    /// Cache a specific object
    //    /// </summary>
    //    /// <param name="o">The object to cache</param>
    //    /// <returns>The key that was used to cache the object with</returns>
    //    internal static string Register(object o)
    //    {
    //        return _cache.Register(o);
    //    }

    //    /// <summary>
    //    /// Retrieve an object from the cache
    //    /// </summary>
    //    /// <param name="key">The key that was used to cache the object</param>
    //    /// <returns>The cached object</returns>
    //    internal static object Lookup(string key)
    //    {
    //        return _cache.Lookup(key);
    //    }

    //    #region example usage of object caching

    //    [ExcelFunction]
    //    public static string CreateTestObjectInCache()
    //    {
    //        MyTestClass o = new MyTestClass();
    //        return Register(o);
    //    }

    //    [ExcelFunction]
    //    public static string UseTestObjectFromCache(object option)
    //    {
    //        if (Utilities.IsMissing(option)) return "Missing parameter: option";

    //        MyTestClass o = _cache.Lookup(option.ToString()) as MyTestClass;
    //        if (o == null) return "Not an Option";
    //        return o.prop1;
    //    }

    //    public class MyTestClass
    //    {
    //        public int i = 2;
    //        public string member1 = "123String";
    //        public string prop1 { get { return "456string"; } }
    //    }

    //    #endregion

    //}
}