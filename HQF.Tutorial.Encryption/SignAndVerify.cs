using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HQF.Tutorial.Encryption
{
    public class SignAndVerify
    {
        public static string SignXmlFile(string sourceXml,
                                    AsymmetricAlgorithm key)
        {
            var unsignedXml = new XmlDocument { PreserveWhitespace = false };
            unsignedXml.LoadXml(sourceXml);

            return SignXml(unsignedXml, key);
        }

        private static string SignXml(XmlDocument unsignedXml,
                                        AsymmetricAlgorithm key)
        {
            if (unsignedXml.DocumentElement == null)
            {
                throw new ArgumentNullException("unsignedXml");
            }

            // Create a reference to be signed. Blank == Everything
                var emptyReference = new Reference { Uri = "" };

            // Add an enveloped transformation to the reference.
            var envelope = new XmlDsigEnvelopedSignatureTransform();
            emptyReference.AddTransform(envelope);

            var signedXml = new SignedXml(unsignedXml) { SigningKey = key };
            signedXml.AddReference(emptyReference);
            signedXml.ComputeSignature();

            var digitalSignature = signedXml.GetXml();
       
                unsignedXml.DocumentElement.AppendChild(
                    unsignedXml.ImportNode(digitalSignature, true));

            var signedXmlOut = new StringBuilder();
            using (var swOut = new StringWriter(signedXmlOut))
            {
                unsignedXml.Save(swOut);
            }

            return signedXmlOut.ToString();
        }


        public static bool XmlIsValid(XmlDocument signedXml,
                                AsymmetricAlgorithm key)
        {
            var nsm = new XmlNamespaceManager(new NameTable());
            nsm.AddNamespace("dsig", SignedXml.XmlDsigNamespaceUrl);

            var signatureGenerator = new SignedXml(signedXml);
            var signatureNode = signedXml
                    .SelectSingleNode("//dsig:Signature", nsm);
            signatureGenerator.LoadXml((XmlElement)signatureNode);

            return signatureGenerator.CheckSignature(key);
        }

        public static bool XmlFileIsValid(string signedXmlPath,
                                            AsymmetricAlgorithm key)
        {
            var signedXml = new XmlDocument { PreserveWhitespace = false };
            signedXml.Load(signedXmlPath);

            return XmlIsValid(signedXml, key);
        }

    }
}
