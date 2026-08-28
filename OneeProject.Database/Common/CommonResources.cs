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
        /// Absolute path to the shared wwwroot folder (OneeProjectAPI).
        /// Both Admin API and FEAPI write uploads here.
        /// </summary>
        private static string? _systemFilePath;

        public static string System_File_Path
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_systemFilePath))
                    ConfigureFileStorage(null);

                return _systemFilePath!;
            }
            set => ConfigureFileStorage(value);
        }

        /// <summary>
        /// Points file storage at <paramref name="storagePath"/> when set (FEAPI uses
        /// OneeProjectAPI wwwroot). Empty/null falls back to this process wwwroot.
        /// </summary>
        public static void ConfigureFileStorage(string? storagePath)
        {
            string wwwrootPath;
            if (!string.IsNullOrWhiteSpace(storagePath))
            {
                wwwrootPath = Path.GetFullPath(storagePath);
            }
            else
            {
                wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            Directory.CreateDirectory(wwwrootPath);
            Directory.CreateDirectory(Path.Combine(wwwrootPath, "Uploads", "UploadImages", "User"));
            Directory.CreateDirectory(Path.Combine(wwwrootPath, "Uploads", "UploadImages", "Worker"));
            _systemFilePath = wwwrootPath;
        }

        public static string UploadFolderForUserType(string? userType)
            => string.Equals(userType, "Worker", StringComparison.OrdinalIgnoreCase)
                ? "Worker"
                : "User";

        /// <summary>
        /// Builds a public URL under OneeProjectAPI static files.
        /// DB still stores the filename only.
        /// </summary>
        public static string BuildUploadUrl(string? uploadBase, string folder, string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            var name = fileName.Trim();
            if (name.Equals("Default.png", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            if (name.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return name;

            var baseUrl = (uploadBase ?? string.Empty).Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
                return name;

            var folderName = string.IsNullOrWhiteSpace(folder)
                ? "User"
                : folder.Trim().Trim('/');

            return $"{baseUrl}/{folderName}/{name}";
        }
    }
}
