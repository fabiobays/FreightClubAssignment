using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using FreightClubAssignment;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unity;
using Unity.Injection;

namespace FreightClubAssignmentTests
{
    [TestClass]
    public class UnitTest1
    {

        public UnityContainer Container { get; set; }
        public List<double> Quotes  { get; set;}

        [TestInitialize]
        public void Setup()
        {
            //Generating 3 random quotes between 450$ and 500$
            Random r = new Random();
            Quotes =  new List<double>();
            Quotes.Add((r.NextDouble() * (500 - 450)) + 450);
            Quotes.Add((r.NextDouble() * (500 - 450)) + 450);
            Quotes.Add((r.NextDouble() * (500 - 450)) + 450);
            //Random quotes generated
            Container = new UnityContainer();
            var mockResponseHandler = new MockResponseHandler();
            mockResponseHandler.AddFakeResponse(
                new Uri("https://api1.example.com/quote"),
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ObjectContent(typeof(object),
                    new
                    {
                        total = Quotes[0] //Assign first quote as a result of the first api call
                    }, new JsonMediaTypeFormatter())
                });

            mockResponseHandler.AddFakeResponse(
               new Uri("https://api2.example.com/quote"),
               new HttpResponseMessage(HttpStatusCode.OK)
               {
                   Content = new ObjectContent(typeof(object),
                   new
                   {
                       amount = Quotes[1] //Assign second quote as a result of the second api call
                   }, new JsonMediaTypeFormatter())
               });
            Xml contentResponse = new Xml(Quotes[2]); //Assign third quote as a result of the third api call;
            mockResponseHandler.AddFakeResponse(
               new Uri("https://api3.example.com/quote"),
               new HttpResponseMessage(HttpStatusCode.OK)
               {
                   Content = new ObjectContent(typeof(Xml),
                             contentResponse, 
                             new XmlMediaTypeFormatter{ UseXmlSerializer = true })
               });


            Container.RegisterType<Quoter, Quoter>();

            Container.RegisterType<HttpClient>(
                new InjectionFactory(x =>
                new HttpClient(mockResponseHandler)
                {
                    BaseAddress = new Uri("http://example.com/")
                }));
        }

        [TestMethod]
        public void TestGetQuote1()
        {
            var quoter = Container.Resolve<Quoter>();
            decimal[] dimensions = { 2340.0m, 4523.69m, 3421.0m };

            var response = quoter.GetQuote("2025 Hamilton Avenue San Jose, California 95125", "7880 Mayfield St, Burnaby, BC, V3M3K2",dimensions).Result;

            Assert.IsNotNull(response);
            var minimumQuote = Quotes.Min();
            Assert.AreEqual(minimumQuote, response); //Comparing if the method is returning the cheapest quote
        }
    }
}
