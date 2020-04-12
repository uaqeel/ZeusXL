using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using ExcelDna.Integration;
using CommonTypes;


namespace XL
{
    partial class XLOM
    {
        private static Dictionary<OMKey, object> OM = new Dictionary<OMKey, object>();


        private static string Add(string name, object o, int c)
        {
            OMKey key = new OMKey(name, o.GetType().Name, c);

            if (OM.ContainsKey(key))
                return Add(name, o, ++c);
            else
            {
                OM.Add(key, o);

                return key.ToString();
            }
        }


        public static string Add(string name, object o)
        {
            string sanitisedName = name.Replace("::", "__").Replace("@@", "__");
            return Add(sanitisedName, o, -1);
        }


        // Unsanitised names will cause problems with this function.
        public static bool Contains(string name, bool strict)
        {
            string[] tokens = name.Split(new string[] { "::", "@@" }, StringSplitOptions.RemoveEmptyEntries);

            if (strict)
            {
                if (tokens.Length == 1 || tokens.Length > 3)
                    return false;

                OMKey key = new OMKey(tokens[0], tokens[1], (tokens.Length == 2 ? -1 : int.Parse(tokens[2])));
                return OM.ContainsKey(key);
            }
            else
            {
                return (OM.Keys.Where(x => x.Name == tokens[0]).Count() > 0);
            }
        }


        public static bool Contains(string name)
        {
            return Contains(name, false);
        }


        // Unsanitised names will cause problems with this function.
        public static object Get(string name)
        {
            string[] tokens = name.Split(new string[] { "::",  "@@" }, StringSplitOptions.RemoveEmptyEntries);
            string realname = tokens[0];

            int version = -1;
            if (tokens.Length == 3)
                version = int.Parse(tokens[2]);

            var objects = OM.Where(x => x.Key.Name == realname);
            if (version != -1 && objects.Count() > 1)
            {
                return objects.Where(x => x.Key.Version == version).Select(x => x.Value).SingleOrDefault();
            }
            else if (objects.Count() >= 1)
            {
                return objects.OrderByDescending(x => x.Key.Version).Select(x => x.Value).First();
            }

            return null;
        }


        public static T Get<T>(string name)
        {
            object o = Get(name);
            if (o != null && o is T)
            {
                return (T)o;
            }
            else
                return default(T);
        }


        public static T GetById<T>(int id) where T : IIdentifiable
        {
            return OM.Where(x => x.Value is T && (x.Value as IIdentifiable).Id == id).Select(x => (T)x.Value).SingleOrDefault();
        }


        public static Dictionary<string, T> GetAll<T>()
        {
            return OM.Where(x => x.Value is T).ToDictionary(x => x.Key.Name, x => (T)x.Value);
        }


        public static string Key(object value)
        {
            return OM.Where(x => x.Value == value)
                     .Select(x => x.Key.ToString())
                     .SingleOrDefault();
        }


        public static IEnumerable<string> Keys()
        {
            return OM.Select(x => x.Key.ToString());
        }


        public static IEnumerable<OMKey> GetKeys()
        {
            return OM.Select(x => x.Key);
        }


        public static bool Remove(string name)
        {
            object o = Get(name);
            if (o != null)
            {
                OMKey key = OM.Where(x => x.Value == o).Select(x => x.Key).SingleOrDefault();
                OM.Remove(key);

                return true;
            }

            return false;
        }


        public static void Reset()
        {
            OM.Clear();
        }
    }


    [DataContract]
    public class OMKey
    {
        public string _Name;
        public string _Type;
        public int _Version;

        public OMKey(string n, string t, int v)
        {
            _Name = n;
            _Type = t;
            _Version = v;
        }


        public string Name { get { return _Name; } set { _Name = value; } }
        public string Type { get { return _Type; } set { _Type = value; } }
        public int Version { get { return _Version; } set { _Version = value; } }


        public override string ToString()
        {
            return Name + "::" + Type + ((Version > 0) ? "@@" + Version : "");
        }


        // To make value comparisons work.
        public override int GetHashCode()
        {
            if (Name == null) return 0;
            return ToString().GetHashCode();
        }


        // To make value comparisons work.
        public override bool Equals(object obj)
        {
            OMKey other = obj as OMKey;
            return other != null && other.ToString() == this.ToString();
        }
    }
}
