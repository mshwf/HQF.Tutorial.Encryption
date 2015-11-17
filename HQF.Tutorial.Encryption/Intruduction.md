#Signed XML Licenses
by Gary H

>Distrust and caution are the parents of security

>Benjamin Franklin

We return after the summer hiatus to continue in our adventures with cryptography. We've looked at some utility classes for symmetric and asymmetric encryption. We will now put those to use in a real world example. In this post we will look at how we can use the tenets of Asymmetric encryption for verification and validation of license credentials.

Asymmetric encryption is already used in the .Net ecosystem - whenever you sign an assembly with a strong name, that is asymmetric encryption. The signing process itself is taking a private key, computing a signature for your binary and then embedding that along with a public key in your assembly. The .Net framework can then load this public key and verify the signature at runtime, giving you confidence that the assembly you are running is the same one that the owner of the private key signed.

To have confidence in a licensing model, you need to have the same ability to trust that the license you are using has not been altered and is the same license that you received from the issuing authority. You also need to have confidence that the key you are using to verify the license is the same one that the parent server expects.

To support these requirements we turn to the XMLDSIG specification and its backing SignedXml classes in the .Net framework.

##Preparing the Ground

We start by defining the boilerplate code that we will need to create and verify XML signatures. To sign an XML document you create an envelope, compute the signature of your original XML and finally add the signature to the end of your unsigned XML. We sign using a private key. The code for this looks like:


```C#
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
```

To verify a signature we load the signed XML, read the signature and then check it using the complimenting public key for the signing private key. To leverage some of the frameworks abilities we will strong name sign our demo program with the same private key as we use for our signing. This will allow us to lift the public key from the assembly itself meaning that it cannot be tampered with short of butchering the assembly - something we could later counter with an online component.

```C#
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
```

##Building the License Server

We'll wrap these up in a SignAndVerify class and it's onto the other components! First we need a license server to actually issue licenses. We'll keep things simple at this stage and implement the server as a class library. The license itself is just a plain lump of XML with a few bits of information that we want to use with our license. Let's put that together using an XmlTextWriter:

```C#
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
var privateKey = Utilities
.GetRSAFromSnkFile("DemoPubPrivPair.snk");
return SignAndVerify
.SignXmlFile(sbXml.ToString(), privateKey);
}
}
```

##Building the Client Wrapper

Next we'll create a wrapper for our Client. This will load a raw License and expose the necessary methods to validate it.

```C#
public class ClientLicense
{
private readonly XmlDocument _license;
private static readonly Lazy<AsymmetricAlgorithm> _publicKey =
new Lazy<AsymmetricAlgorithm>(
() => Utilities.GetPublicKeyFromAssembly(
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
```
We lazily load the Assembly for the license so we dont take the reflection hit should we not actually verify the license. The Verify method itself checks both the validity of the XML signature and also does a quick check against the Expires element that we set in our license.

Putting it all Together

That's all the infrastructure we need to begin issuing and consuming licenses so let's take it for a spin in a console application. We'll start with the simplest scenario - issue a license and verify it.

```C#
const string DemoLicenseKey = "D2287CCA-2A3A-48C2-BCCB-BF12B3E481B0";
var server = new LicenseServer();
var rawLicense = server.IssueLicense(DateTime.Now.AddDays(10),
DemoLicenseKey);
var license = new ClientLicense(rawLicense);
Console.WriteLine("License is valid? - {0}", license.Verify());
Console.WriteLine("Message - {0}", license.Message);
Console.WriteLine();
```

We can then go on to prove that the signing protects against tampering. To do this we issue a license then tamper with the Expires element.

```c# 
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
```

The output for this is a failed validation as expected with a message stating that the license is corrupt or has been tampered with. Finally, lets check that we catch an unmolested but expired license.

``C#
rawLicense = server.IssueLicense(DateTime.Now.AddDays(-10),
DemoLicenseKey);
license = new ClientLicense(rawLicense);
Console.WriteLine("License is valid? - {0}", license.Verify());
Console.WriteLine("Message - {0}", license.Message);
```

##Conclusions

We've looked at how we can leverage asymmetric cryptography to make a start on a secure, tamper proof licensing model. However it still has a number of vulnerabilities. A malicious thrid party could strip the strong name from the assemblies, sign them with their own private key and sign the license XML with that same key giving them a workaround. In the next post we will look at how we can counter this by adding an online component - a handshake with the server which is outside of a local attackers control (short of them writing a proxy server themselves).