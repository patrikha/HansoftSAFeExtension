using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SE.HansoftExtensions
{
    public class ProductOwnerConfig
    {
        private static Dictionary<string, string> productOwners = null;

        public static string GetProductOwner(string team, string default_)
        {
            if (productOwners == null)
                ProductOwnerConfig.ReadConfig();

            if (productOwners.ContainsKey(team.ToLower()))
                return productOwners[team.ToLower()];
            else
                return default_;
        }

        public static void ReadConfig()
        {
            productOwners = new Dictionary<string, string>();
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            FileInfo fInfo = new FileInfo(Path.Combine(currentDirectory, "ProductOwners.xml"));
            if (!fInfo.Exists)
                throw new ArgumentException("Could not find settings file " + fInfo.FullName);

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(fInfo.FullName);
            XmlElement documentElement = xmlDocument.DocumentElement;
            if (documentElement.Name != "ProductOwners")
                throw new FormatException("The root element of the settings file must be of type ProductOwners, got " + documentElement.Name);

            XmlNodeList topNodes = documentElement.ChildNodes;

            foreach (XmlNode node in topNodes)
            {
                if (node.NodeType != XmlNodeType.Element)
                    continue;

                XmlElement el = (XmlElement)node;
                string name = "";
                string po = "";
                foreach (XmlAttribute attr in el.Attributes)
                {
                    if (attr.Name.ToLower() == "name")
                        name = attr.Value.ToLower();
                    else if (attr.Name.ToLower() == "po")
                        po = attr.Value;
                }
                productOwners.Add(name, po);
            }
        }
    }
}
