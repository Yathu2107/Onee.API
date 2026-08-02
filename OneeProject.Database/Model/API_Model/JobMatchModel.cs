using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OneeProject.Database.Model.API_Model
{
    public class JobMatchRequestModel
    {
        [Required]
        public string Text { get; set; } = string.Empty;
        [Required]
        public string UserId { get; set; } = string.Empty;
    }

    public class OneeAiPredictRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class OneeAiPredictResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public OneeAiPredictData? Data { get; set; }
    }

    public class OneeAiPredictData
    {
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }

    public class JobMatchWorkerProfileModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string IsActive { get; set; } = "Active";
        public string IsOnline { get; set; } = "Online";
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double Distance_Km { get; set; }
        /// <summary>Average rating out of 5 (0 if no ratings).</summary>
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
    }

    public class JobMatchResultModel
    {
        public string Predicted_Category { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public int? Category_id { get; set; }
        public string? Category_Name { get; set; }
        public List<JobMatchWorkerProfileModel> Workers { get; set; } = new();
    }
}
