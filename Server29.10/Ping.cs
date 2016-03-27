using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Server29._10
{
    [XmlRootAttribute("ping")]
    public class Ping : BaseCommand
    {
        public Ping()
        {
            id = 13;
        }
    }
}
