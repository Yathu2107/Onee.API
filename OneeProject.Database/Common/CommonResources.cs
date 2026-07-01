using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace OneeProject.Database.Common
{
    public class CommonResources
    {
        // <summary>
        /// Returns the current date and time in Sri Lanka Standard Time (UTC+5:30).
        /// </summary>
        public static DateTime LocalDatetime()
        {
            var sriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, sriLankaTimeZone);
        }

        /// <summary>
        /// Code Encrypt and Decrypt
        /// </summary>
        public static string SecretKey;

        public static string EncodeId(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(SecretKey));
            aes.IV = new byte[16];

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);

            return Convert.ToBase64String(encrypted)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public static string DecodeId(string cipherText)
        {
            cipherText = cipherText
                .Replace("-", "+")
                .Replace("_", "/");

            switch (cipherText.Length % 4)
            {
                case 2: cipherText += "=="; break;
                case 3: cipherText += "="; break;
            }

            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(SecretKey));
            aes.IV = new byte[16];

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var bytes = Convert.FromBase64String(cipherText);
            var decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);

            return Encoding.UTF8.GetString(decrypted);
        }

        /// <summary>
        /// Returns the absolute file path to the wwwroot folder.
        /// Ensures consistency for saving uploaded files.
        /// </summary>
        private static string? _systemFilePath;

        public static string System_File_Path
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_systemFilePath))
                {
                    string basePath = Directory.GetCurrentDirectory();

                    // Usually folder name is "wwwroot"
                    string wwwrootPath = Path.Combine(basePath, "wwwroot");

                    if (!Directory.Exists(wwwrootPath))
                        Directory.CreateDirectory(wwwrootPath);

                    _systemFilePath = wwwrootPath;
                }

                return _systemFilePath;
            }
            set
            {
                _systemFilePath = value;
            }
        }
    }
}
