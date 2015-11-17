using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HQF.Tutorial.Encryption
{
    public class LicenseServer
    {
        public string IssueLicense(DateTime expiry, string uniqueKey)
        {
            var sbXml = new StringBuilder();
            using (var swOut = new StringWriter(sbXml))
            using (var xmlOut = new XmlTextWriter(swOut))
            {
                xmlOut.WriteStartDocument();
                xmlOut.WriteStartElement("License");
                xmlOut.WriteStartElement("Key");
                xmlOut.WriteString(uniqueKey);
                xmlOut.WriteEndElement();
                xmlOut.WriteStartElement("IssueDate");
                xmlOut.WriteString(DateTime.Now
                    .ToString("dd/MM/yyyy HH:mm:ss"));
                xmlOut.WriteEndElement();
                xmlOut.WriteStartElement("Expires");
                xmlOut.WriteString(expiry
                    .ToString("dd/MM/yyyy HH:mm:ss"));
                xmlOut.WriteEndElement();
                xmlOut.WriteStartElement("IssuedBy");
                xmlOut.WriteString("Demo Licensing Server");
                xmlOut.WriteEndElement();
                xmlOut.WriteEndElement();
                xmlOut.WriteEndDocument();
                xmlOut.Close();
            }

            var privateKey = EncryptionUtils.GetRSAFromSnkFile("DemoPubPrivPair.snk");
            return SignAndVerify
            .SignXmlFile(sbXml.ToString(), privateKey);
        }
    }
}
