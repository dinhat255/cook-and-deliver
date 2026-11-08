using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace CookAndDeliver.SaveLoad.Core
{
    public static class EncryptUtil
    {
        // 32-byte secret key for AES-256
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("C00k&D3l1v3rS3cur3K3y!2026AES256");

        // 16-byte initialization vector (IV)
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("CookDeliverIV16B");

        public static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            try
            {
                byte[] cipherBytes = Encrypt(plainText);
                return Convert.ToBase64String(cipherBytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EncryptUtil] Encryption failed: {ex.Message}");
                return plainText;
            }
        }

        public static string DecryptString(string cipherBase64)
        {
            if (string.IsNullOrEmpty(cipherBase64)) return string.Empty;

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherBase64);
                return Decrypt(cipherBytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EncryptUtil] Decryption failed: {ex.Message}");
                return cipherBase64;
            }
        }

        public static byte[] Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cs, Encoding.UTF8))
                    {
                        writer.Write(plainText);
                    }
                    return ms.ToArray();
                }
            }
        }

        public static string Decrypt(byte[] cipherBytes)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                using (var ms = new MemoryStream(cipherBytes))
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
