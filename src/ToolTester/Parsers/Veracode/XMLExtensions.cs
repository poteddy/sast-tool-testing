namespace ToolTester.Parsers.Veracode
{
    public class XMLExtensions
    {
      
        public static async Task<detailedreport> ReadXMLAsync(Stream fs)
        {

            using (StreamReader r = new StreamReader(fs))
            {
                string xml = await r.ReadToEndAsync();
                var x = xml.ParseXML<detailedreport>();
                return x;
            }
        }


    }
}
