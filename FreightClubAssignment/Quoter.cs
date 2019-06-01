using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml;
using System.IO;

namespace FreightClubAssignment
{
    public class Quoter
    {
        [Dependency]
        public HttpClient client { get; set; }

        public async Task<double> GetQuote(string sourceAddress, string destinationAddress, decimal[] dimensions)
        {
            double quote = -1;
            try
            {
                List<string> urlList = GetURLList();

                //Create a query of taks to be executed
                IEnumerable<Task<double>> apiCallsQuery =
                   from url in urlList select CallApi(url, sourceAddress, destinationAddress, dimensions);

                //Execute the query transforming it into a list making all the tasks fire at the same time
                List<Task<double>> apiCallsList = apiCallsQuery.ToList();

                while (apiCallsList.Count > 0)
                {
                    //When any of the tasks finishes, compare its result with the cheaper quote so far and remove that task from the list of tasks
                    Task<double> finishedTask = await Task.WhenAny(apiCallsList);

                    apiCallsList.Remove(finishedTask);

                    double amount = await finishedTask;

                    if (quote == -1)
                        quote = amount;
                    else if (quote > amount)
                        quote = amount;
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            if (quote == -1)
            {
                throw new Exception("It was not possible to find a valid quote for those parameters");
            }

            return quote;
        }

        protected async Task<double> CallApi(string url, string sourceAddress, string destinationAddress, decimal[] dimensions)
        {
            double amount = 0;
            switch (url)
            {
                case "https://api1.example.com/quote":                   
                    amount = await Api1(url,sourceAddress,destinationAddress,dimensions);
                    break;
                case "https://api2.example.com/quote":              
                    amount = await Api2(url, sourceAddress, destinationAddress, dimensions);
                    break;
                case "https://api3.example.com/quote":
                    amount = await Api3(url, sourceAddress, destinationAddress, dimensions);
                    break;      
            }
            if (amount == 0)
            {
                throw new Exception("It was not possible to find a valid quote for those parameters");
            }
            return amount;
           
        }

        private async Task<double> Api1(string url, string sourceAddress, string destinationAddress, decimal[] dimensions)
        {
            var content = JObject.FromObject(new { contactAddress = destinationAddress, warehouseAddress = sourceAddress, packageDimensions = dimensions });
            var httpContent = new StringContent(content.ToString(), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, httpContent).Result.Content.ReadAsStringAsync();
            var token = JObject.Parse(response);
            var amount = (double)token.SelectToken("total");

            return amount;
        }

        private async Task<double> Api2(string url, string sourceAddress, string destinationAddress, decimal[] dimensions)
        {
            var content = JObject.FromObject(new { consignee = destinationAddress, consignor = sourceAddress, cartons = dimensions });
            var httpContent = new StringContent(content.ToString(), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, httpContent).Result.Content.ReadAsStringAsync();
            var token = JObject.Parse(response);
            var amount = (double)token.SelectToken("amount");

            return amount;
        }

        private async Task<double> Api3(string url, string sourceAddress, string destinationAddress, decimal[] dimensions)
        {
            var sw = new StringWriter();
            var xw = XmlWriter.Create(sw);

            xw.WriteStartDocument();
            xw.WriteStartElement("xml");
            xw.WriteStartElement("source");
            xw.WriteString(sourceAddress);
            xw.WriteEndElement();

            xw.WriteStartElement("destination");
            xw.WriteString(destinationAddress);
            xw.WriteEndElement();
            xw.WriteStartElement("packages");
            for (var i = 0; i < dimensions.Length; i++)
            {
                xw.WriteStartElement("package");
                xw.WriteString(dimensions[i].ToString());
                xw.WriteEndElement();
            }
            xw.WriteEndElement();
            xw.WriteEndElement();
            xw.WriteEndDocument();
            xw.Close();
            var httpContent = new StringContent(sw.ToString(), Encoding.UTF8, "application/xml");
            var response = await client.PostAsync(url, httpContent).Result.Content.ReadAsStringAsync();
            StringReader sr = new StringReader(response);
            XmlReader xr = XmlReader.Create(sr);
            xr.ReadToFollowing("Quote");
            var amount = xr.ReadElementContentAsDouble();
            sr.Dispose();
            xr.Dispose();
            sw.Dispose();

            return amount;
        }

        private List<string> GetURLList()
        {
            List<string> urls = new List<string>
            {
                "https://api1.example.com/quote",
                "https://api2.example.com/quote",
                "https://api3.example.com/quote"
            };
              
            return urls;
        }
    }
}
