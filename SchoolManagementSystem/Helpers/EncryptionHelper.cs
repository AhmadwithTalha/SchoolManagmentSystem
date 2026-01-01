using System.Security.Cryptography;
using System.Text;

namespace SchoolManagementSystem.Helpers
{
    public static class EncryptionHelper
    {
        private static readonly string Key = "1234567890123456";

        public static string Encrypt(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(Key);
            aes.IV = new byte[16];

            var encryptor = aes.CreateEncryptor();
            var bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(
                encryptor.TransformFinalBlock(bytes, 0, bytes.Length));
        }

        public static string Decrypt(string cipher)
        {
            if (string.IsNullOrEmpty(cipher)) return cipher;

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(Key);
            aes.IV = new byte[16];

            var decryptor = aes.CreateDecryptor();
            var bytes = Convert.FromBase64String(cipher);
            return Encoding.UTF8.GetString(
                decryptor.TransformFinalBlock(bytes, 0, bytes.Length));
        }
    }
}
