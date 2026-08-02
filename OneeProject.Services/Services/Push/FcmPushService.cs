using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneeProject.Services.FeServices;
using OneeProject.Services.Services.Realtime;

namespace OneeProject.Services.Services.Push
{
    public class FcmPushService : IPushNotificationSender
    {
        private readonly DeviceTokenService _deviceTokens;
        private readonly ILogger<FcmPushService> _logger;
        private readonly bool _ready;

        public FcmPushService(
            IConfiguration config,
            DeviceTokenService deviceTokens,
            ILogger<FcmPushService> logger)
        {
            _deviceTokens = deviceTokens;
            _logger = logger;

            try
            {
                if (FirebaseApp.DefaultInstance == null)
                {
                    var relative = config["Firebase:CredentialPath"] ?? "secrets/firebase-adminsdk.json";
                    var path = Path.IsPathRooted(relative)
                        ? relative
                        : Path.Combine(Directory.GetCurrentDirectory(), relative);

                    if (!File.Exists(path))
                    {
                        _logger.LogWarning("Firebase credential file not found at {Path}. FCM disabled until file is added.", path);
                        _ready = false;
                        return;
                    }

                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(path),
                        ProjectId = config["Firebase:ProjectId"]
                    });
                }

                _ready = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize FirebaseApp.");
                _ready = false;
            }
        }

        public async Task SendToUserAsync(
            string userId,
            string title,
            string body,
            Dictionary<string, string> data)
        {
            if (!_ready || string.IsNullOrWhiteSpace(userId))
                return;

            var tokens = await _deviceTokens.GetTokensForUserAsync(userId);
            if (tokens.Count == 0)
                return;

            // Include title/body in data for foreground handling,
            // plus a Notification payload so Android still shows a tray
            // banner when the app process is fully killed.
            var payload = new Dictionary<string, string>(data)
            {
                ["title"] = title,
                ["body"] = body
            };

            var invalid = new List<string>();

            foreach (var token in tokens)
            {
                try
                {
                    var message = new Message
                    {
                        Token = token,
                        Notification = new FirebaseAdmin.Messaging.Notification
                        {
                            Title = title,
                            Body = body
                        },
                        Data = payload,
                        Android = new AndroidConfig
                        {
                            Priority = Priority.High,
                            Notification = new AndroidNotification
                            {
                                ChannelId = "onee_job_updates",
                                // Drawable resource name (no @drawable/ prefix)
                                Icon = "ic_stat_onee",
                                Color = "#EBB407",
                                DefaultSound = true,
                                Priority = NotificationPriority.HIGH
                            }
                        },
                        Apns = new ApnsConfig
                        {
                            Headers = new Dictionary<string, string>
                            {
                                { "apns-priority", "10" }
                            },
                            Aps = new Aps
                            {
                                Alert = new ApsAlert
                                {
                                    Title = title,
                                    Body = body
                                },
                                Sound = "default",
                                ContentAvailable = true
                            }
                        }
                    };

                    await FirebaseMessaging.DefaultInstance.SendAsync(message);
                }
                catch (FirebaseMessagingException ex) when (
                    ex.MessagingErrorCode == MessagingErrorCode.Unregistered
                    || ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
                {
                    invalid.Add(token);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "FCM send failed for user {UserId}", userId);
                }
            }

            if (invalid.Count > 0)
                await _deviceTokens.RemoveTokensAsync(invalid);
        }
    }
}
