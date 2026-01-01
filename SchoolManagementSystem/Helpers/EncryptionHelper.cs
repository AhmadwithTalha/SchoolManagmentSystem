using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SchoolManagementSystem.Helpers
{
    public static class EncryptionHelper
    {
        // Replace with a secure key and store safely
        private static readonly string key = "mysupersecretkey123"; // Must be 16/24/32 bytes for AES

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32)); // ensure 32 bytes
                aes.Mode = CipherMode.CBC;
                aes.GenerateIV();
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using MemoryStream ms = new MemoryStream();
                ms.Write(aes.IV, 0, aes.IV.Length); // prepend IV to the stream
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (StreamWriter sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            byte[] fullCipher = Convert.FromBase64String(cipherText);
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32));
                byte[] iv = new byte[16];
                Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                aes.IV = iv;

                int cipherStart = iv.Length;
                int cipherLength = fullCipher.Length - cipherStart;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using MemoryStream ms = new MemoryStream(fullCipher, cipherStart, cipherLength);
                using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using StreamReader sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
        }
    }
}
