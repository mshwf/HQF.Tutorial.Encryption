using System;
using System.IO;
using System.Text;
using System.Xml;

namespace HQF.Tutorial.Encryption.Conso
{
    class Program
    {
        static void Main(string[] args)
        {
            //-----------
            const string DemoLicenseKey = "D2287CCA-2A3A-48C2-BCCB-BF12B3E481B0";
            var server = new LicenseServer();

            var rawLicense = server.IssueLicense(DateTime.Now.AddDays(10),
                                                  DemoLicenseKey);
            var license = new ClientLicense(rawLicense);
            Console.WriteLine("License is valid? - {0}", license.Verify());
            Console.WriteLine("Message - {0}", license.Message);
            Console.WriteLine();

            Console.ReadKey();

            //-----------Expires
            rawLicense = server.IssueLicense(DateTime.Now.AddDays(10),
                                  DemoLicenseKey);
            var tamper = new XmlDocument();
            tamper.LoadXml(rawLicense);
            tamper.SelectSingleNode("//Expires").InnerText =
                DateTime.Now.AddYears(5).ToString("dd/MM/yyyy HH:mm:ss");

            var tamperedXml = new StringBuilder();
            using (var swOut = new StringWriter(tamperedXml))
            {
                tamper.Save(swOut);
            }
            license = new ClientLicense(tamperedXml.ToString());

            Console.WriteLine("License is valid? - {0}", license.Verify());
            Console.WriteLine("Message - {0}", license.Message);
            Console.WriteLine();

            Console.ReadKey();


            //-----------
            rawLicense = server.IssueLicense(DateTime.Now.AddDays(-10),
                                  DemoLicenseKey);
            license = new ClientLicense(rawLicense);
            Console.WriteLine("License is valid? - {0}", license.Verify());
            Console.WriteLine("Message - {0}", license.Message);


            Console.ReadKey();


        }
    }
}
