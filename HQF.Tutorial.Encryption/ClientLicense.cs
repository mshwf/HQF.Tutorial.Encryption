using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HQF.Tutorial.Encryption
{
    public class ClientLicense
    {
        private readonly XmlDocument _license;

        private static readonly Lazy<AsymmetricAlgorithm> _publicKey =
                new Lazy<AsymmetricAlgorithm>(
                    () => EncryptionUtils.GetPublicKeyFromAssembly(
                        Assembly.GetExecutingAssembly()));

        public string Message { get; set; }

        public ClientLicense(string rawLicense)
        {
            _license = new XmlDocument();
            _license.LoadXml(rawLicense);
            Message = "Not yet verified";
        }

        public bool Verify()
        {
            if (_license == null)
            {
                throw new ArgumentNullException("license");
            }

            var xmlOk = SignAndVerify.XmlIsValid(_license, _publicKey.Value);
            if (!xmlOk)
            {
                Message = "License XML is corrupt or has been tampered" +
                            " with - signature could not be verified";
                return false;
            }

            var expiry = DateTime.ParseExact(
                _license.SelectSingleNode("//Expires").InnerText,
                "dd/MM/yyyy HH:mm:ss",
                CultureInfo.InvariantCulture);

            if (expiry < DateTime.Now)
            {
                Message = "This License has Expired";
                return false;
            }

            Message = "Ok";
            return true;
        }
    }
}
