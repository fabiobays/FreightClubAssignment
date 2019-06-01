using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FreightClubAssignment
{
    [XmlInclude(typeof(Xml))]
    [Serializable]
    public class Xml
    {
        public double Quote { get; set; }

        public Xml(double quote)
        {
            Quote = quote;
        }

        public Xml()
        {
        }
    }
}
