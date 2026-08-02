using Microsoft.EntityFrameworkCore;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;
using System.Net.Http.Json;
using System.Text.Json;

namespace OneeProject.Services.Services
{
    public class JobMatchService(AppDbContext context, IHttpClientFactory httpClientFactory)
    {
        private readonly AppDbContext _context = context;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private const double SearchRadiusKm = 7.0;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// AI text path — predicts category name then matches workers.
        /// </summary>
        public async Task<Message<JobMatchResultModel>> FindWorkersByTextAsync(string text, string userId)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new Message<JobMatchResultModel>
                {
                    Status = "E",
                    Text = "Text is required.",
                    Code = "400",
                    Result = null
                };
            }

            var location = await ResolveCustomerLocationAsync(userId);
            if (location.Error != null)
                return location.Error;

            var aiResult = await PredictCategoryAsync(text.Trim());
            if (aiResult.Status != "S" || aiResult.Result == null)
            {
                return new Message<JobMatchResultModel>
                {
                    Status = "E",
                    Text = aiResult.Text,
                    Code = aiResult.Code,
                    Result = null
                };
            }

            var predictedCategory = aiResult.Result.Category?.Trim() ?? string.Empty;
            var confidence = aiResult.Result.Confidence;

            if (string.IsNullOrWhiteSpace(predictedCategory))
                return NoWorkersAvailable(predictedCategory, confidence);

            var category = await _context.Categories
                .Where(c => !c.Isdelete && c.Category_Name.ToLower() == predictedCategory.ToLower())
                .Select(c => new { c.Id, c.Category_Name })
                .FirstOrDefaultAsync();

            if (category == null)
                return NoWorkersAvailable(predictedCategory, confidence);

            return await MatchWorkersForCategoryAsync(
                category.Id,
                category.Category_Name,
                predictedCategory,
                confidence,
                location.Latitude,
                location.Longitude);
        }

        /// <summary>
        /// Manual category path — user picks categoryId from Admin list (no AI).
        /// </summary>
        public async Task<Message<JobMatchResultModel>> FindWorkersByCategoryIdAsync(
            int categoryId,
            string userId)
        {
            if (categoryId <= 0)
            {
                return new Message<JobMatchResultModel>
                {
                    Status = "E",
                    Text = "A valid CategoryId is required.",
                    Code = "400",
                    Result = null
                };
            }

            var location = await ResolveCustomerLocationAsync(userId);
            if (location.Error != null)
                return location.Error;

            var category = await _context.Categories
                .Where(c => c.Id == categoryId && !c.Isdelete)
                .Select(c => new { c.Id, c.Category_Name })
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return new Message<JobMatchResultModel>
                {
                    Status = "E",
                    Text = "Category not found.",
                    Code = "404",
                    Result = null
                };
            }

            return await MatchWorkersForCategoryAsync(
                category.Id,
                category.Category_Name,
                category.Category_Name,
                confidence: 1.0,
                location.Latitude,
                location.Longitude);
        }

        private async Task<(double Latitude, double Longitude, Message<JobMatchResultModel>? Error)>
            ResolveCustomerLocationAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return (0, 0, new Message<JobMatchResultModel>
                {
                    Status = "E",
                    Text = "UserId is required.",
                    Code = "400",
                    Result = null
                });
            }

            var selectedUser = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id })
                .FirstOrDefaultAsync();

            if (selectedUser == null)
            {
                return (0, 0, new Message<JobMatchResultModel>
                {
                    Status = "E",
                    Text = "Selected user not found.",
                    Code = "404",
                    Result = null
                });
            }

            var customerAddress = await _context.SavedAddresses
                .FirstOrDefaultAsync(a => a.FK_user_ID == userId && a.Is_Default);

            if (customerAddress == null)
            {
                return (0, 0, new Message<JobMatchResultModel>
                {
                    Status = "E",
                    Text = "Selected user does not have a default saved address.",
                    Code = "400",
                    Result = null
                });
            }

            return (customerAddress.Latitude, customerAddress.Longitude, null);
        }

        private async Task<Message<JobMatchResultModel>> MatchWorkersForCategoryAsync(
            int categoryId,
            string categoryName,
            string predictedLabel,
            double confidence,
            double userLat,
            double userLng)
        {
            var workers = await (
                from wc in _context.WorkerCategories
                join u in _context.Users on wc.FK_user_ID equals u.Id
                join addr in _context.SavedAddresses on u.Id equals addr.FK_user_ID
                where wc.Category_id == categoryId
                      && u.IsActive
                      && u.IsOnline
                      && u.UserType == "Worker"
                      && addr.Is_Default
                select new JobMatchWorkerProfileModel
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    ProfileImageUrl = u.ProfileImageUrl,
                    UserType = u.UserType,
                    IsActive = u.IsActive ? "Active" : "Blocked",
                    IsOnline = u.IsOnline ? "Online" : "Offline",
                    Latitude = addr.Latitude,
                    Longitude = addr.Longitude
                })
                .ToListAsync();

            var nearbyWorkers = workers
                .GroupBy(w => w.Id)
                .Select(g => g.First())
                .Select(w =>
                {
                    w.Distance_Km = Math.Round(
                        GetDistanceKm(userLat, userLng, w.Latitude!.Value, w.Longitude!.Value),
                        2);
                    return w;
                })
                .Where(w => w.Distance_Km <= SearchRadiusKm)
                .OrderBy(w => w.Distance_Km)
                .ThenBy(w => w.Name)
                .ToList();

            if (nearbyWorkers.Count == 0)
                return NoWorkersAvailable(predictedLabel, confidence, categoryId, categoryName);

            var workerIds = nearbyWorkers.Select(w => w.Id).ToList();
            var ratingLookup = await _context.JobRatings
                .Where(r => workerIds.Contains(r.FK_worker_ID))
                .GroupBy(r => r.FK_worker_ID)
                .Select(g => new
                {
                    WorkerId = g.Key,
                    Count = g.Count(),
                    Average = g.Average(r => (double)r.Rating)
                })
                .ToDictionaryAsync(x => x.WorkerId);

            foreach (var worker in nearbyWorkers)
            {
                if (ratingLookup.TryGetValue(worker.Id, out var stats))
                {
                    worker.AverageRating = Math.Round(stats.Average, 1);
                    worker.RatingCount = stats.Count;
                }
            }

            return new Message<JobMatchResultModel>
            {
                Status = "S",
                Text = "Workers found successfully.",
                Code = "200",
                Result = new JobMatchResultModel
                {
                    Predicted_Category = predictedLabel,
                    Confidence = confidence,
                    Category_id = categoryId,
                    Category_Name = categoryName,
                    Workers = nearbyWorkers
                }
            };
        }

        private static double GetDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusKm = 6371.0;
            double dLat = DegreesToRadians(lat2 - lat1);
            double dLon = DegreesToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                       + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
                       * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

        private async Task<Message<OneeAiPredictData>> PredictCategoryAsync(string text)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("OneeeAi");
                var request = new OneeAiPredictRequest { Text = text };

                using var response = await client.PostAsJsonAsync("/api/v1/predict", request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new Message<OneeAiPredictData>
                    {
                        Status = "E",
                        Text = "AI prediction service failed.",
                        Code = ((int)response.StatusCode).ToString(),
                        Result = null
                    };
                }

                var parsed = JsonSerializer.Deserialize<OneeAiPredictResponse>(body, JsonOptions);
                if (parsed == null || !parsed.Success || parsed.Data == null)
                {
                    return new Message<OneeAiPredictData>
                    {
                        Status = "E",
                        Text = "AI prediction returned an invalid response.",
                        Code = "502",
                        Result = null
                    };
                }

                return new Message<OneeAiPredictData>
                {
                    Status = "S",
                    Text = "Prediction successful.",
                    Code = "200",
                    Result = parsed.Data
                };
            }
            catch (Exception ex)
            {
                return new Message<OneeAiPredictData>
                {
                    Status = "E",
                    Text = $"Unable to reach AI prediction service. {ex.Message}",
                    Code = "503",
                    Result = null
                };
            }
        }

        private static Message<JobMatchResultModel> NoWorkersAvailable(
            string predictedCategory,
            double confidence,
            int? categoryId = null,
            string? categoryName = null)
        {
            return new Message<JobMatchResultModel>
            {
                Status = "E",
                Text = "No workers available for this work.",
                Code = "404",
                Result = new JobMatchResultModel
                {
                    Predicted_Category = predictedCategory,
                    Confidence = confidence,
                    Category_id = categoryId,
                    Category_Name = categoryName,
                    Workers = new List<JobMatchWorkerProfileModel>()
                }
            };
        }
    }
}
