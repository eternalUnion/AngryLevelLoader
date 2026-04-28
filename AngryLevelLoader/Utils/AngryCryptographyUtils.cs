using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AngryLevelLoader.Utils
{
	internal static class AngryCryptographyUtils
	{
		public static byte[] Encrypt(string data, string keyXml)
		{
			RSA rsa = RSA.Create();
			rsa.FromXmlString(keyXml);
			return rsa.Encrypt(Encoding.UTF8.GetBytes(data), RSAEncryptionPadding.Pkcs1);
		}

		public static byte[] Encrypt(byte[] data, string keyXml)
		{
			RSA rsa = RSA.Create();
			rsa.FromXmlString(keyXml);
			return rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
		}

		public static bool VerifyFileCertificate(string filePath, string certificatePath)
		{
			const string angryPublicKey = "<RSAKeyValue><Modulus>+/ueeOpso05dA+5GjKbjQ0VpM+JAHmRRgYRw36G4dXqmpCGfVDNVdjjBBkVWO+6lJoSNaaG4Yprn4uQVslUQ7OYWAw6Y+9E0Ezvr1quWE7i0KGxG6weplRTsu9aO0/9gJgP/gWQxC0Cf83NwyvMPsThtCruAQFT+cW0LGghtFgrBr++aknI06SJI5ydrbZgEtU5i4FfjrV1ms4CRRojhydJglfGQfG8W3pTDge4jVdND+RGB6F01QGi0+Bnq5DfKdjvb3/Zh1ko7WocWgavDaIgLYj88AgbGdC0lidLMIgzdnGxkLyxbTzsgi/mvUpB2foy4uHoV22EaWMj+6H+oXQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
			RSA publicKey = RSA.Create();
			publicKey.FromXmlString(angryPublicKey);
			byte[] cert = File.ReadAllBytes(certificatePath);
			return publicKey.VerifyData(File.ReadAllBytes(filePath), cert, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		}

		// Why don't you exist Convert.ToHexString(byte[])? Why nothing exists in this framework?
		private static string ByteArrayToString(byte[] ba)
		{
			return BitConverter.ToString(ba).Replace("-", "");
		}

		public static string GetSHA256Hash(string filePath)
		{
			SHA256 hash = SHA256.Create();
			hash.Initialize();
			return ByteArrayToString(hash.ComputeHash(File.ReadAllBytes(filePath))).ToLower();
		}

		public static string GetMD5String(string text)
		{
			MD5 md5 = MD5.Create();
			byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(text));
			return ByteArrayToString(hash).ToLower();
		}

		public static string GetMD5String(byte[] data)
		{
			MD5 md5 = MD5.Create();
			byte[] hash = md5.ComputeHash(data);
			return ByteArrayToString(hash).ToLower();
		}
	}
}
