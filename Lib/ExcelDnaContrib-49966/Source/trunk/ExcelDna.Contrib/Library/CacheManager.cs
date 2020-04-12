using System;
using System.Collections.Generic;
using System.Text;

namespace ExcelDna.Contrib.Cache
{
    //public class CacheManager : ICacheManager
    //{
    //    readonly Dictionary<string, object> _objectCache = new Dictionary<string, object>();

    //    public int Count { get { return _objectCache.Count; } }

    //    public string Register(object o)
    //    {
    //        return Register(o, "N" + o.GetHashCode());
    //    }

    //    public string Register(object o, string key)
    //    {
    //        if (_objectCache.ContainsKey(key))
    //        {
    //            _objectCache[key] = o;
    //        }
    //        else
    //        {
    //            _objectCache.Add(key, o);
    //        }
    //        return key;
    //    }

    //    public object Lookup(string key)
    //    {
    //        try
    //        {
    //            return _objectCache[key];
    //        }
    //        catch (Exception)
    //        {
    //            throw new ApplicationException(string.Format("{0} does not exist", key));
    //        }
    //    }

    //    public void Clear()
    //    {
    //        _objectCache.Clear();
    //    }

    //    public void Remove(string key)
    //    {
    //        if (_objectCache.ContainsKey(key))
    //            _objectCache.Remove(key);
    //    }

    //    public bool ContainsKey(string key)
    //    {
    //        return _objectCache.ContainsKey(key);
    //    }
    //}
}
