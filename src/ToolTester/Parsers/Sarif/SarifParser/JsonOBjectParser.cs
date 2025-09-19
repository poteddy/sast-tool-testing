using Newtonsoft.Json.Linq;

namespace ToolTester.Parsers.Sarif
{

    public class JsonOBjectParser
    {

        public async Task Parse(Stream fs)
        {
            using (StreamReader r = new StreamReader(fs))
            {

                string json = r.ReadToEnd();
                var tree = JObject.Parse(json);
                var items = new List<object>();
                foreach (var run in (List<JToken>)tree["runs"].ToList())
                {
                    items.Add(Get_Items_From_Run(run));

                }

            }
        }
        private object Get_Items_From_Run(JToken run)
        {
            var items = new List<JToken>();

            var rules = get_rules(run);
            var artifacts = get_artifacts(run);
            foreach (var result in run.SelectTokens("results"))
            {
                object run_date = null;
                var item = get_item(result, rules, artifacts, run_date);

                if (item != null)
                {
                    items.Add(item);
                }

            }

            return items;
        }
        private class LocationField()
        {
            string file_path = null;
            int? line = null;
        }
        private LocationField get_location(JToken result)
        {
            LocationField locationfield = new LocationField();

            if (result["locations"].HasValues)
            {
                foreach (var location in result["locations"])
                {
                    string filePath = null;
                    int? line = null;

                    if (location["physicalLocation"].HasValues)
                    {
                        filePath = location["physicalLocation"]["artifactLocation"]["uri"].ToString();

                        // 'region' attribute is optional
                        if (location["physicalLocation"]["region"].HasValues)
                        {
                            // need to check whether it is byteOffset
                            if (!location["physicalLocation"]["region"]["byteOffset"].HasValues)
                            {
                                line = (int?)location["physicalLocation"]["region"]["startLine"];
                            }
                        }
                    }

                    //  files.Add((filePath, line, location));
                }
            }

            return locationfield;
        }
        private JToken get_item(JToken result, Dictionary<string, object> rules, object? artifacts, object? run_date)
        {
            var kind = result.Value<string>("kind") ?? "fail";
            if (kind != "fail") return null;
            LocationField locationField = get_location(result);
            throw new NotImplementedException();


        }

        private Dictionary<string, object> get_rules(JToken run)
        {
            var rules = new Dictionary<string, object>();
            foreach (JToken item in run["tool"]["driver"].SelectToken("rules"))
            {
                rules.Add(item["id"].ToString(), item);
            }

            return rules;
        }
        private Dictionary<string, object> get_artifacts(JToken run)
        {
            var artifacts = new Dictionary<string, object>();
            foreach (JToken item in run.SelectTokens("artifacts"))
            {
                artifacts.Add(item["id"].ToString(), item);
            }

            return artifacts;
        }
    }
}
