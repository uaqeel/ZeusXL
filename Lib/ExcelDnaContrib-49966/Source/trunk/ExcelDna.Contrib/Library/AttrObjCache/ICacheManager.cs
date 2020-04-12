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


namespace ExcelDna.Contrib.Cache
{
    /// <summary>
    /// Interface for CacheManager class
    /// </summary>
    public interface ICacheManager
    {
        /// <summary>
        /// Read only; gives total number of objects in the cache
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Read only; allows for enumeration over active object handle keys
        /// </summary>
        System.Collections.Generic.IEnumerable<string> Keys { get; }

        /// <summary>
        /// Adds an object to the cache
        /// </summary>
        /// <param name="o">Object to be added</param>
        /// <returns>Object handle key</returns>
        string Register(object o);

        /// <summary>
        /// Assigns an object to the specified key. If key doesn't exist, then it is added to cache.
        /// </summary>
        /// <param name="o">Object to be assigned</param>
        /// <param name="key">Object handle key being referenced</param>
        /// <returns>Object handle key that was originally passed</returns>
        string Register(object o, string key);

        /// <summary>
        /// Returns object associated with the specified objent handle key
        /// </summary>
        /// <param name="key">Object handle key to be searched</param>
        /// <returns>object containing cached item</returns>
        object Lookup(string key);

        /// <summary>
        /// Empties the CacheManager
        /// </summary>
        void Clear();

        /// <summary>
        /// Remove object with the specified object handle key from the cache
        /// </summary>
        /// <param name="key">Object handle key</param>
        void Remove(string key);

        /// <summary>
        /// Determines if the CacheManager contains the specified key
        /// </summary>
        /// <param name="key">Object handle key</param>
        /// <returns>Boolean denoting existence of key</returns>
        bool ContainsKey(string key);
    }
}
