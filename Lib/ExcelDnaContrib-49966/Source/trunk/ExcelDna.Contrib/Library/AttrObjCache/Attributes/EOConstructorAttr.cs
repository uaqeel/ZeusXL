﻿/*
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
    /// Provides attribute data for Excel registered object constuctors
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public sealed class ExcelObjectConstructorAttribute : Attribute
    {
        private string _name = string.Empty;
        private string _des = string.Empty;

        /// <summary>
        /// Read only; name to be used within Excel
        /// </summary>
        public string Name { get { return _name; } private set { _name = value; } }

        /// <summary>
        /// Read-Write; description of object
        /// </summary>
        public string Description { get { return _des; } set { _des = value; } }

        /// <summary>
        /// Instantiate new ExcelObjectConstructorAttribute
        /// </summary>
        /// <param name="Name">Name to be used within Excel</param>
        /// <param name="Description">Description of object</param>
        public ExcelObjectConstructorAttribute(string Name, string Description)
        {
            this.Name = Name;
            this.Description = Description;
        }
    }
}
