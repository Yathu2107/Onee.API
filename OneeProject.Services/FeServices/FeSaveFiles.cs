using Microsoft.AspNetCore.Http;
using OneeProject.Database.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace OneeProject.Services.FeServices
{
    public class FeSaveFiles
    {
        public static T SetImageUrl<T>(
            T model,
            IEnumerable<IFormFile> images,
            string[] imageProperties,
            string subFolderName,
            string defaultImage = "Default.png")
        {
            string baseFolderPath = CommonResources.System_File_Path;
            string folderPath = Path.Combine(baseFolderPath, "Uploads", "UploadImages", subFolderName);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var imageList = images?.ToList() ?? new List<IFormFile>();

            for (int i = 0; i < imageProperties.Length; i++)
            {
                string propertyName = imageProperties[i];
                PropertyInfo property = typeof(T).GetProperty(propertyName);

                if (property == null || !property.CanWrite)
                    continue;

                string currentImageUrl = property.GetValue(model)?.ToString();

                // ✅ Only proceed if a NEW image is uploaded
                if (i < imageList.Count && imageList[i] != null && imageList[i].Length > 0)
                {
                    // 🔥 Delete old image (if not default)
                    if (!string.IsNullOrWhiteSpace(currentImageUrl) &&
                        currentImageUrl != defaultImage)
                    {
                        string currentImagePath = Path.Combine(folderPath, currentImageUrl);
                        if (File.Exists(currentImagePath))
                            File.Delete(currentImagePath);
                    }

                    // ✅ Save new image
                    var imageFile = imageList[i];
                    string extension = Path.GetExtension(imageFile.FileName);
                    string newFileName = $"{Guid.NewGuid()}{extension}";
                    string newFilePath = Path.Combine(folderPath, newFileName);

                    using var stream = new FileStream(newFilePath, FileMode.Create);
                    imageFile.CopyTo(stream);

                    property.SetValue(model, newFileName);
                }
            }

            return model;
        }
    }
}
