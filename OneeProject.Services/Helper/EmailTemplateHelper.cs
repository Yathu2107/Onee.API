using System;
using System.Collections.Generic;
using System.Text;

namespace OneeProject.Services.Helper
{
    public class EmailTemplateHelper
    {
        private static readonly string TemplateDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Helper", "Templates");

        /// <summary>
        /// Loads the specified HTML template file from the Templates folder.
        /// </summary>
        /// <param name="templateFileName">Filename like "OtpTemplate.html"</param>
        /// <returns>Raw HTML content as string</returns>
        public static string LoadTemplate(string templateFileName)
        {
            var fullPath = Path.Combine(TemplateDirectory, templateFileName);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Email template not found: {fullPath}");

            return File.ReadAllText(fullPath);
        }

        /// <summary>
        /// Replaces placeholders in the template with actual values.
        /// </summary>
        /// <param name="template">Raw HTML template string</param>
        /// <param name="placeholders">Dictionary of placeholder keys and values</param>
        /// <returns>Formatted HTML string</returns>
        public static string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
        {
            foreach (var pair in placeholders)
            {
                template = template.Replace($"{{{{{pair.Key}}}}}", pair.Value);
            }

            return template;
        }

        /// <summary>
        /// Loads and formats a template in one step.
        /// </summary>
        /// <param name="templateFileName">Template file name</param>
        /// <param name="placeholders">Placeholder values</param>
        /// <returns>Final HTML content</returns>
        public static string LoadAndFormat(string templateFileName, Dictionary<string, string> placeholders)
        {
            var rawTemplate = LoadTemplate(templateFileName);
            return ReplacePlaceholders(rawTemplate, placeholders);
        }
    }
}
