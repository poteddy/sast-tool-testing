using System.Xml;
using System.Xml.Serialization;
using ToolTester.Infrastructure;

namespace ToolTester.Infrastructure.Extensions
{
    public class XMLExtensions
    {
        public static Weakness_Catalog ReadXML(string path)
        {

            string xml = File.ReadAllText(path);
            var x = xml.ParseXML<Weakness_Catalog>();
            return x;
        }
        public static async Task<Weakness_Catalog> ReadXMLAsync(string path)
        {

            string xml = File.ReadAllText(path);
            var x = xml.ParseXML<Weakness_Catalog>();
            return x;
        }


    }
    public static class ParseHelpers
    {

        public static Stream ToStream(this string @this)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(@this);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }


        public static T ParseXML<T>(this string @this) where T : class
        {
            var reader = XmlReader.Create(@this.Trim().ToStream(), new XmlReaderSettings() { ConformanceLevel = ConformanceLevel.Document });
            return new XmlSerializer(typeof(T)).Deserialize(reader) as T;
        }

    }
}
