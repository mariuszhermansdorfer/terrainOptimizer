using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Grasshopper.Kernel;

using Newtonsoft.Json;



namespace terrainOptimizer.Components
{
    public class EPWScraper : GH_Component
    {

        public EPWScraper()
          : base("EPWScraper", "EPWScraper",
              "Description",
              "PHD", "EPW")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("inputKMLFile", "inputKMLFile", "", GH_ParamAccess.item);
            pManager.AddTextParameter("outputPath", "outputPath", "", GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {

        }

        public class EPWDataSources
        {
            public string Name { get; set; }
            public string Coordinates { get; set; }
            public string Url { get; set; }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string kmlFilePath = "";
            string outputPath = "";
            DA.GetData(0, ref kmlFilePath);
            DA.GetData(1, ref outputPath);

            string jsonFilePath = outputPath + @"\output.json";

            string kmlContent = File.ReadAllText(kmlFilePath);
            XDocument doc = XDocument.Parse(kmlContent);

            var placemarksData = doc.Descendants()
                                    .Where(d => d.Name.LocalName == "Placemark")
                                    .Select(placemark => new EPWDataSources
                                    {
                                        Name = placemark.Descendants().FirstOrDefault(d => d.Name.LocalName == "name")?.Value,
                                        Coordinates = FormatCoordinates(placemark.Descendants().FirstOrDefault(d => d.Name.LocalName == "coordinates")?.Value),
                                        Url = ExtractUrlFromDescription(placemark.Descendants().FirstOrDefault(d => d.Name.LocalName == "description")?.Value)
                                    }).ToList();

            string json = JsonConvert.SerializeObject(placemarksData, Formatting.Indented);
            File.WriteAllText(jsonFilePath, json);

        }

        private static string FormatCoordinates(string coordinates)
        {
            if (string.IsNullOrEmpty(coordinates))
                return null;

            // Split the coordinates string and remove the third (altitude) part
            var parts = coordinates.Split(',');
            if (parts.Length >= 2)
                return $"{parts[0]},{parts[1]}"; // Only return longitude and latitude
            return coordinates;
        }
        private static string ExtractUrlFromDescription(string description)
        {
            if (description == null)
                return null;

            var urlMatch = Regex.Match(description, @"URL\s+(https?://\S+)");
            if (urlMatch.Success)
            {
                // Remove the trailing "</td></tr></table>"
                var url = urlMatch.Groups[1].Value;
                return Regex.Replace(url, @"<\/td><\/tr><\/table>$", string.Empty);
            }
            return null;
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }


        public override Guid ComponentGuid
        {
            get { return new Guid("2FB5808C-160A-4E18-935E-E18C2E2F7596"); }
        }
    }
}