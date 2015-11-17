#HQF.Tutorial.Encryption

To Refrence [Here](http://stackoverflow.com/a/359923/1616023)  

Using Public/Private cryptography to sign a license token (an XML Fragment or file for example) so you can detect tampering. The simplest way to handle it is to do the following steps:

1) Generate a keypair for your company. You can do this in the Visual Studio command line using the SN tool. Syntax is:

```
C:\Program Files (x86)\Microsoft Visual Studio 14.0>sn -k d:\DemoPubPrivPair.snk

Microsoft (R) .NET Framework Strong Name Utility  Version 4.0.30319.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Key pair written to d:\DemoPubPrivPair.snk

C:\Program Files (x86)\Microsoft Visual Studio 14.0>
```  

2) Use the keypair to strongly name (i.e. sign) your client application. You can set this using the signing tab in the properties page on your application

3) Create a license for your client, this should be an XML document and sign it using your Private key. This involves simply computing a digital signature and steps to accomplish it can be found at:

http://msdn.microsoft.com/en-us/library/ms229745.aspx

4) On the client, when checking the license you load the XmlDocument and use your Public key to verify the signature to prove the license has not been tampered with. Details on how to do this can be found at:

http://msdn.microsoft.com/en-us/library/ms229950.aspx

To get around key distribution (i.e. ensuring your client is using the correct public key) you can actually pull the public key from the signed assembly itself. Thus ensuring you dont have another key to distribute and even if someone tampers with the assembly the .net framework will die with a security exception because the strong name will no longer match the assembly itself.

To pull the public key from the client assembly you want to use code similar to:

```c#
    /// <summary>
    /// Retrieves an RSA public key from a signed assembly
    /// </summary>
    /// <param name="assembly">Signed assembly to retrieve the key from</param>
    /// <returns>RSA Crypto Service Provider initialised with the key from the assembly</returns>
    public static RSACryptoServiceProvider GetPublicKeyFromAssembly(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException("assembly", "Assembly may not be null");

        byte[] pubkey = assembly.GetName().GetPublicKey();
        if (pubkey.Length == 0)
            throw new ArgumentException("No public key in assembly.");

        RSAParameters rsaParams = EncryptionUtils.GetRSAParameters(pubkey);
        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        rsa.ImportParameters(rsaParams);

        return rsa;
    }

```

I've uploaded a sample class with a lot of helpful Encryption Utilities on Snipt at: http://snipt.net/Wolfwyrd/encryption-utilities/ to help get you on your way.

I have also included a sample program at: https://snipt.net/Wolfwyrd/sign-and-verify-example/. The sample requires that you add it to a solution with the encryption utils library and provide a test XML file and a SNK file for signing. The project should be set to be signed with the SNK you generate. It demonstrates how to sign the test XML file using a private key from the SNK and then verify from the public key on the assembly.

##Update

Added an [up to date blog post](http://www.leapinggorilla.com/Blog/Read/1019/signed-xml-licenses) with a nice detailed run through on license files


