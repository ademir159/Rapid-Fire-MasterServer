using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace RapidFire_MasterServer
{
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MasterParams
    {
        [XmlElement(ElementName = "createMatchBotNum")]
        public int createMatchBotNum = 0;
        [XmlElement(ElementName = "createMathUserQueue_DM")]
        public int createMathUserQueue_DM = 10;
        [XmlElement(ElementName = "createMathUserQueue_5v5")]
        public int createMathUserQueue_5v5 = 10;
        [XmlElement(ElementName = "createMathUserQueue_Defuse")]
        public int createMathUserQueue_Defuse = 10;
        [XmlElement(ElementName = "port_Start")]
        public int port_Start = 7777;
        [XmlElement(ElementName = "port_End")]
        public int port_End = 9000;
    }
}
