using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace OneeProject.Services.FeServices
{
    public class CommunicationService(IConfiguration configuration)
    {
        private static readonly HttpClient HttpClient = new();
        private readonly IConfiguration _configuration = configuration;

        /// <summary>
        /// Sends an SMS via Text.lk OAuth 2.0 API (POST /api/v3/sms/send).
        /// <paramref name="mobile"/> may be local (07xxxxxxxx) or international (947xxxxxxxx).
        /// </summary>
        public async Task<(bool Success, string Message)> SendMessageAsync(string mobile, string message)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return (false, "Mobile number is required.");

            if (string.IsNullOrWhiteSpace(message))
                return (false, "Message is required.");

            var apiToken = _configuration["TextLkSettings:ApiToken"];
            if (string.IsNullOrWhiteSpace(apiToken))
                return (false, "Text.lk API token is not configured.");

            var senderId = _configuration["TextLkSettings:SenderId"] ?? "TextLKDemo";
            var baseUrl = (_configuration["TextLkSettings:BaseUrl"] ?? "https://app.text.lk/api/v3/")
                .TrimEnd('/') + "/";
            var recipient = NormalizeToTextLkRecipient(mobile);

            if (string.IsNullOrWhiteSpace(recipient) || recipient.Length < 10)
                return (false, "Invalid mobile number.");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl + "sms/send");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = JsonContent.Create(new Dictionary<string, string>
                {
                    ["recipient"] = recipient,
                    ["sender_id"] = senderId,
                    ["type"] = "plain",
                    ["message"] = message
                });

                var response = await HttpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                var parsed = TryParseResponse(body);

                if (!response.IsSuccessStatusCode || !IsSuccessStatus(parsed?.Status))
                {
                    var errMsg = parsed?.Message
                        ?? TryReadRawMessage(body)
                        ?? $"SMS provider returned {(int)response.StatusCode}.";
                    return (false, errMsg);
                }

                return (true, parsed?.Message ?? "SMS queued successfully.");
            }
            catch (HttpRequestException ex)
            {
                return (false, $"SMS network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"SMS unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Normalizes Sri Lankan numbers to Text.lk format (e.g. 94771234567).
        /// </summary>
        public static string NormalizeToTextLkRecipient(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return string.Empty;

            var digits = new string(mobile.Where(char.IsDigit).ToArray());

            if (digits.StartsWith("94") && digits.Length >= 11)
                return digits;

            if (digits.StartsWith('0') && digits.Length == 10)
                return "94" + digits[1..];

            if (digits.Length == 9 && digits.StartsWith('7'))
                return "94" + digits;

            return digits;
        }

        /// <summary>
        /// Normalizes Sri Lankan numbers for DB storage (e.g. 0771234567).
        /// </summary>
        public static string NormalizePhoneForDb(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return string.Empty;

            var digits = new string(mobile.Where(char.IsDigit).ToArray());

            if (digits.StartsWith("94") && digits.Length >= 11)
                return "0" + digits[2..];

            if (digits.StartsWith('0') && digits.Length == 10)
                return digits;

            if (digits.Length == 9 && digits.StartsWith('7'))
                return "0" + digits;

            return digits;
        }

        private static bool IsSuccessStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return status.Equals("success", StringComparison.OrdinalIgnoreCase)
                || status.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private static TextLkSmsResponse? TryParseResponse(string body)
        {
            try
            {
                return JsonSerializer.Deserialize<TextLkSmsResponse>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        private static string? TryReadRawMessage(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var message))
                    return message.GetString();
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private sealed class TextLkSmsResponse
        {
            // Text.lk returns "success" / "error" (string), not a boolean.
            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("message")]
            public string? Message { get; set; }
        }
    }
}
