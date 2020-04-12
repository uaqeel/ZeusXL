/*
  Copyright (C) 2009 Hayden Smith

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

  Hayden Smith
  hayden.smith@gmail.com
*/

using System;
using System.Collections;
using ExcelDna.Integration;
using ExcelDna.Contrib.Library;
using System.IO;
using System.Collections.Generic;

namespace ExcelDna.Contrib.Functions
{
    /// <summary>
    /// Excel Functions for querying and acting upon the file system
    /// </summary>
    public class FileSystem
    {
        private const string CATEGORY = "ExcelDna.Contrib.FileSystem";

        /// <summary>
        /// Get the time that a file was created
        /// </summary>
        /// <param name="Path">The full path of the file</param>
        /// <param name="UTC">True if UTC is time required</param>
        /// <returns>The time that the file was created</returns>
        [ExcelFunction(Category = CATEGORY, Description = "Returns the Creation Timestamp of the given filename", IsVolatile = false, IsMacroType = false, IsThreadSafe = true)]
        public static object FileCreationTime([ExcelArgument(AllowReference = false, Description = "Full path of file")] string Path, [ExcelArgument(AllowReference = false, Description = "True is UTC time required")] bool UTC)
        {
            //Returns an object as opposed to a DateTime to allow a meaningful error message to be returned if
            //the file provided does not exist

            //Check the file exists. If not throw an error
            if (!File.Exists(Path))
            {
                return Path + " not found";
            }

            if (UTC)
            {
                //Return UTC Creation Time
                return File.GetCreationTimeUtc(Path);
            }
            else
            {
                //Return the Creation Time of the file
                return File.GetCreationTime(Path);

            }
        }

        /// <summary>
        /// Get the Last Modified Timestamp of the given filename
        /// </summary>
        /// <param name="Path">The full path of the file</param>
        /// <param name="UTC">True if UTC is time required</param>
        /// <returns>The Modified Timestamp of the given filename</returns>
        [ExcelFunction(Category = CATEGORY, Description = "Returns the Last Modified Timestamp of the given filename", IsVolatile = true, IsMacroType = false, IsThreadSafe = true)]
        public static object FileLastWriteTime([ExcelArgument(AllowReference = false, Description = "Full path of file")] string Path, [ExcelArgument(AllowReference = false, Description = "True is UTC time required")] bool UTC)
        {
            //Returns an object as opposed to a DateTime to allow a meaningful error message to be returned if
            //the file provided does not exist

            //Check the file exists. If not throw an error
            if (!File.Exists(Path))
            {
                return Path + " not found";
            }

            if (UTC)
            {
                //Return UTC Last Write Time
                return File.GetLastWriteTime(Path);
            }
            else
            {
                //Return the Lst Write Time of the file
                return File.GetLastWriteTimeUtc(Path);

            }
        }
        
        /// <summary>
        /// Get the Last Access Timestamp of the given filename
        /// </summary>
        /// <param name="Path">The full path of the file</param>
        /// <param name="UTC">True if UTC is time required</param>
        /// <returns>The Modified Access of the given filename</returns>
        [ExcelFunction(Category = CATEGORY, Description = "Returns the Last Access Timestamp of the given filename", IsVolatile = true, IsMacroType = false, IsThreadSafe = true)]
        public static object FileLastAccessTime([ExcelArgument(AllowReference = false, Description = "Full path of file")] string Path, [ExcelArgument(AllowReference = false, Description = "True is UTC time required")] bool UTC)
        {
            //Returns an object as opposed to a DateTime to allow a meaningful error message to be returned if
            //the file provided does not exist

            //Check the file exists. If not throw an error
            if (!File.Exists(Path))
            {
                return Path + " not found";
            }

            if (UTC)
            {
                //Return UTC Last Access Time
                return File.GetLastAccessTime(Path);
            }
            else
            {
                //Return the Last Acccess Time of the file
                return File.GetLastAccessTimeUtc(Path);

            }
        }

        /// <summary>
        /// Get the attributes of the given filename
        /// </summary>
        /// <param name="Path">The full path of the file</param>
        /// <returns>The attributes of the given filename</returns>
        [ExcelFunction(Category = CATEGORY, Description = "Returns the attributes of the given filename", IsVolatile = true, IsMacroType = false, IsThreadSafe = true)]
        public static object GetFileAttributes([ExcelArgument(AllowReference = false, Description = "Full path of file")] string Path)
        {
            if (!File.Exists(Path))
            {
                //Check the file exists. If not throw an error
                return Path + " not found";
            }

            object[,] ret = new object[13, 2];

            FileAttributes attr = File.GetAttributes(Path);

            ret[0, 0] = "Archive";
            ret[0, 1] = (attr & FileAttributes.Archive) == FileAttributes.Archive;

            ret[1, 0] = "Compressed";
            ret[1, 1] = (attr & FileAttributes.Compressed) == FileAttributes.Compressed;

            ret[2, 0] = "Directory";
            ret[2, 1] = (attr & FileAttributes.Directory) == FileAttributes.Directory;

            ret[3, 0] = "Encrypted";
            ret[3, 1] = (attr & FileAttributes.Encrypted) == FileAttributes.Encrypted;

            ret[4, 0] = "Hidden";
            ret[4, 1] = (attr & FileAttributes.Hidden) == FileAttributes.Hidden;

            ret[5, 0] = "Normal";
            ret[5, 1] = (attr & FileAttributes.Normal) == FileAttributes.Normal;

            ret[6, 0] = "NotContentIndexed";
            ret[6, 1] = (attr & FileAttributes.NotContentIndexed) == FileAttributes.NotContentIndexed;

            ret[7, 0] = "Offline";
            ret[7, 1] = (attr & FileAttributes.Offline) == FileAttributes.Offline;

            ret[8, 0] = "ReadOnly";
            ret[8, 1] = (attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;

            ret[9, 0] = "ReparsePoint";
            ret[9, 1] = (attr & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

            ret[10, 0] = "SparseFile";
            ret[10, 1] = (attr & FileAttributes.SparseFile) == FileAttributes.SparseFile;

            ret[11, 0] = "System";
            ret[11, 1] = (attr & FileAttributes.System) == FileAttributes.System;

            ret[12, 0] = "Temporary";
            ret[12, 1] = (attr & FileAttributes.Temporary) == FileAttributes.Temporary;

            return ret;

        }

        /// <summary>
        /// Get a specific attributes of the given filename
        /// </summary>
        /// <param name="Path">The full path of the file</param>
        /// <param name="Attribute">The attribute to get. Possible values: ARCHIVE, COMPRESSED, DIRECTORY, ENCRYPTED, HIDDEN, NORMAL, NOTCONTENTINDEXED, OFFLINE, READONLY, REPARSEPOINT, SPARSEFILE, SYSTEM, TEMPORARY</param>
        /// <returns>The attributes of the given filename</returns>
        [ExcelFunction(Category = CATEGORY, Description = "Returns the requested attribute of the given filename", IsVolatile = true, IsMacroType = false, IsThreadSafe = true)]
        public static object GetFileAttribute([ExcelArgument(AllowReference = false, Description = "Full path of file")] string Path, [ExcelArgument(AllowReference = false, Description = "Attribute to Query")] string Attribute)
        {
            if (!File.Exists(Path))
            {
                //Check the file exists. If not throw an error
                return Path + " not found";
            }

            FileAttributes attr = File.GetAttributes(Path);

            switch (Attribute.ToUpper())
            {
                case "ARCHIVE":
                    return (attr & FileAttributes.Archive) == FileAttributes.Archive;
                case "COMPRESSED":
                    return (attr & FileAttributes.Compressed) == FileAttributes.Compressed;
                case "DIRECTORY":
                    return (attr & FileAttributes.Directory) == FileAttributes.Directory;
                case "ENCRYPTED":
                    return (attr & FileAttributes.Encrypted) == FileAttributes.Encrypted;
                case "HIDDEN":
                    return (attr & FileAttributes.Hidden) == FileAttributes.Hidden;
                case "NORMAL":
                    return (attr & FileAttributes.Normal) == FileAttributes.Normal;
                case "NOTCONTENTINDEXED":
                    return (attr & FileAttributes.NotContentIndexed) == FileAttributes.NotContentIndexed;
                case "OFFLINE":
                    return (attr & FileAttributes.Offline) == FileAttributes.Offline;
                case "READONLY":
                    return (attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
                case "REPARSEPOINT":
                    return (attr & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
                case "SPARSEFILE":
                    return (attr & FileAttributes.SparseFile) == FileAttributes.SparseFile;
                case "SYSTEM":
                    return (attr & FileAttributes.System) == FileAttributes.System;
                case "TEMPORARY":
                    return (attr & FileAttributes.Temporary) == FileAttributes.Temporary;
                default:
                    return "Unknown Attribute : " + Attribute;
            }
        }

        /// <summary>
        /// Check if the file exists
        /// </summary>
        /// <param name="Path">The full path of the file</param>
        /// <returns>True if the file exists</returns>
        [ExcelFunction(Category = CATEGORY, Description = "Returns TRUE if File Exists FALSE if file does not exist", IsVolatile = false, IsMacroType = false, IsThreadSafe = true)]
        public static bool FileExists([ExcelArgument(AllowReference = false, Description = "Full path of file")]string Path)
        {
            return File.Exists(Path);

        }

        /// <summary>
        /// Check if the Directory exists
        /// </summary>
        /// <param name="Path">The full path of the drectory</param>
        /// <returns>True if the directory exists</returns>
        [ExcelFunction(Category = CATEGORY, Description = "Returns TRUE if File Exists FALSE if file does not exist", IsVolatile = false, IsMacroType = false, IsThreadSafe = true)]
        public static bool DirectoryExists([ExcelArgument(AllowReference = false, Description = "Full path of directory")]string Path)
        {
            return Directory.Exists(Path);

        }

        /// <summary>
        /// Displays the contents of a directory
        /// </summary>
        /// <param name="Path">The full path of the file</param>
        /// <param name="Filter">Wildcard filter to apply to results</param>
        /// <returns>Array of files in directory meeting wildcard confitions</returns>
        [ExcelFunction(Category = CATEGORY, Description = "Returns list of files in given directory using given wildcard (if any)", IsVolatile = false, IsMacroType = false, IsThreadSafe = true)]
        public static object[,] GetDirectoryContents([ExcelArgument(AllowReference = false, Description = "Full path of file")]string Path,[ExcelArgument(AllowReference=false,Description="Wildcard Filter")] string Filter)
        {
            if (!DirectoryExists(Path))
            {
                throw new Exception("Directory " + Path + " cannot be found");
            }

            if (Filter == string.Empty)
            {
                Filter = "*";
            }

            string[] files = Directory.GetFiles(Path, Filter);
            string[,] ret = new string[files.GetUpperBound(0)+1, 2];

            for (int i = 0; i <= files.GetUpperBound(0); i++)
            {
                ret[i, 0] = files[i];
                ret[i, 1] = ExcelEmpty.Value.ToString();
            }

            return ret;

        }

        /// <summary>
        /// Reads and returns the contents of the given file
        /// </summary>
        /// <param name="Path">Full path to file</param>
        /// <returns>Contents of file as an array</returns>
        public static object ReadFile(string Path)
        {
            if(!File.Exists(Path))
            {
                return Path + " does not exist";
            }

            using (StreamReader r = new StreamReader(Path))
            {

                ExcelList lines = new ExcelList();
                string line = string.Empty;
                while ((line = r.ReadLine()) != null)
                {
                    lines.Add(line);
                }

                r.Close();

                return lines.ToArray();
            }

        }

    }
}