using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using System.Xml;

using ExcelDna.Integration;


namespace XL
{
    partial class XLOM
    {
        public static string SerialiseLocation { get; set; }


        public static bool Serialise()
        {
            var serializer = new DataContractSerializer(OM.GetType(), null, Int32.MaxValue, false, false, null, new SharedTypeResolver());
            string xmlString;
            using (var sw = new StringWriter())
            {
                using (var writer = new XmlTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;
                    serializer.WriteObject(writer, OM);
                    writer.Flush();
                    xmlString = sw.ToString();
                }
            }


            var xd = new XmlDocument();
            xd.LoadXml(xmlString);

            string filename = SerialiseLocation + "\\XLOM_DataStore.xml";
            xd.Save(filename);

            return true;
        }


        public static bool Deserialise()
        {
            string filename = SerialiseLocation + "\\XLOM_DataStore.xml";

            var deserializer = new DataContractSerializer(OM.GetType(), null, Int32.MaxValue, false, false, null, new SharedTypeResolver());
            FileStream fs = new FileStream(filename, FileMode.Open);
            XmlDictionaryReader reader =XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());

            OM = (Dictionary<OMKey, object>)deserializer.ReadObject(reader);
            reader.Close();
            fs.Close();

            return true;
        }
    }


    public class SharedTypeResolver : DataContractResolver
    {
        public override bool TryResolveType(Type dataContractType, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            if (!knownTypeResolver.TryResolveType(dataContractType, declaredType, null, out typeName, out typeNamespace))
            {
                XmlDictionary dictionary = new XmlDictionary();
                typeName = dictionary.Add(dataContractType.BaseType.Name);
                typeNamespace = dictionary.Add(dataContractType.Assembly.ToString());
            }
            return true;
        }

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            if (typeName == "ArrayOfKeyValueOfdateTimedouble")
            {
                return typeof(KeyValuePair<DateTime,double>[]);
            }
            else
            {
                return knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, null) ?? Type.GetType(typeName + ", " + typeNamespace);
            }
        }
    }
}
