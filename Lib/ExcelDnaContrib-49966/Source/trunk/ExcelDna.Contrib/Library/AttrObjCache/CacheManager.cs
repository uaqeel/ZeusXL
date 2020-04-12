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


namespace ExcelDna.Contrib.Cache
{
    /// <summary>
    /// Stores and maintains the cache of active objects.
    /// </summary>
    public class CacheManager : ICacheManager
    {
        readonly Dictionary<string, object> _objectCache = new Dictionary<string, object>();

        /// <summary>
        /// Read only; gives total number of objects in the cache
        /// </summary>
        public int Count { get { return _objectCache.Count; } }

        /// <summary>
        /// Read only; allows for enumeration over active object handle keys
        /// </summary>
        public IEnumerable<string> Keys { get { return _objectCache.Keys; } }

        /// <summary>
        /// Adds an object to the cache
        /// </summary>
        /// <param name="o">Object to be added</param>
        /// <returns>Object handle key</returns>
        public string Register(object o)
        {
            return Register(o, "N" + o.GetHashCode());
        }

        /// <summary>
        /// Assigns an object to the specified key. If key doesn't exist, then it is added to cache.
        /// </summary>
        /// <param name="o">Object to be assigned</param>
        /// <param name="key">Object handle key being referenced</param>
        /// <returns>Object handle key that was originally passed</returns>
        public string Register(object o, string key)
        {
            if (_objectCache.ContainsKey(key))
            {
                _objectCache[key] = o;
            }
            else
            {
                _objectCache.Add(key, o);
            }
            return key;
        }

        /// <summary>
        /// Returns object associated with the specified objent handle key
        /// </summary>
        /// <param name="key">Object handle key to be searched</param>
        /// <returns>object containing cached item</returns>
        public object Lookup(string key)
        {
            try
            {
                return _objectCache[key];
            }
            catch (Exception)
            {
                throw new ApplicationException(string.Format("{0} does not exist", key));
            }
        }

        /// <summary>
        /// Empties the CacheManager
        /// </summary>
        public void Clear()
        {
            _objectCache.Clear();
        }

        /// <summary>
        /// Remove object with the specified object handle key from the cache
        /// </summary>
        /// <param name="key">Object handle key</param>
        public void Remove(string key)
        {
            if (_objectCache.ContainsKey(key))
            {
                if (_objectCache[key] is IDisposable)
                    (_objectCache[key] as IDisposable).Dispose();

                _objectCache.Remove(key);
            }
        }

        /// <summary>
        /// Determines if the CacheManager contains the specified key
        /// </summary>
        /// <param name="key">Object handle key</param>
        /// <returns>Boolean denoting existence of key</returns>
        public bool ContainsKey(string key)
        {
            return _objectCache.ContainsKey(key);
        }
    }
}
