using Microsoft.EntityFrameworkCore;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services.Realtime;

namespace OneeProject.Services.Services
{
    public class NotificationService(
        AppDbContext context,
        IPushNotificationSender pushSender)
    {
        private readonly AppDbContext _context = context;
        private readonly IPushNotificationSender _pushSender = pushSender;

        public async Task CreateInboxAsync(
            string userId,
            string title,
            string body,
            string type,
            int? jobId = null,
            int? adminNotificationId = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            _context.Notifications.Add(new Notification
            {
                FK_user_ID = userId,
                Title = title,
                Body = body,
                Type = type,
                FK_job_ID = jobId,
                FK_admin_notification_ID = adminNotificationId,
                Is_Read = false,
                CreatedOn = CommonResources.LocalDatetime()
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<NotificationModelForView>> GetMineAsync(
            string userId,
            string? type = null)
        {
            var query = _context.Notifications.Where(n => n.FK_user_ID == userId);
            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(n => n.Type == type);

            var list = await query
                .OrderByDescending(n => n.CreatedOn)
                .ToListAsync();
            return list.Select(Map).ToList();
        }

        public async Task<int> GetUnreadCountAsync(string userId, string? type = null)
        {
            var query = _context.Notifications
                .Where(n => n.FK_user_ID == userId && !n.Is_Read);
            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(n => n.Type == type);

            return await query.CountAsync();
        }

        public async Task<Message<string>> MarkReadAsync(int id, string userId, string? type = null)
        {
            var query = _context.Notifications
                .Where(n => n.Id == id && n.FK_user_ID == userId);
            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(n => n.Type == type);

            var entity = await query.FirstOrDefaultAsync();
            if (entity == null)
                return new Message<string> { Status = "E", Text = "Notification not found.", Code = "404" };

            entity.Is_Read = true;
            await _context.SaveChangesAsync();
            return new Message<string>
            {
                Status = "S",
                Text = "Marked as read.",
                Code = "200",
                Result = id.ToString()
            };
        }

        public async Task<Message<string>> MarkAllReadAsync(string userId, string? type = null)
        {
            var query = _context.Notifications
                .Where(n => n.FK_user_ID == userId && !n.Is_Read);
            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(n => n.Type == type);

            var unread = await query.ToListAsync();
            foreach (var n in unread)
                n.Is_Read = true;
            await _context.SaveChangesAsync();
            return new Message<string>
            {
                Status = "S",
                Text = "All notifications marked as read.",
                Code = "200",
                Result = unread.Count.ToString()
            };
        }

        public async Task<NotificationModelWithPagination> GetInboxWithPagination(
            int page,
            int itemsPerPage,
            string? userId,
            string? type)
        {
            var query = _context.Notifications.AsQueryable();
            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(n => n.FK_user_ID == userId);
            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(n => n.Type == type);

            var count = await query.CountAsync();
            var list = await query
                .OrderByDescending(n => n.CreatedOn)
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage)
                .ToListAsync();

            return new NotificationModelWithPagination
            {
                Count = count,
                Notifications = list.Select(Map).ToList()
            };
        }

        public async Task<Message<AdminNotificationModelForView>> CreateAdminBroadcastAsync(
            AdminNotificationModelForInsert model,
            string createdBy)
        {
            var audience = NormalizeAudience(model.Audience);
            if (audience == null)
                return AdminErr("Audience must be Users, Workers, or Both.", "400");

            if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Body))
                return AdminErr("Title and Body are required.", "400");

            var entity = new AdminNotification
            {
                Title = model.Title.Trim(),
                Body = model.Body.Trim(),
                Audience = audience,
                Status = AdminNotificationStatuses.Draft,
                CreatedBy = createdBy,
                CreatedOn = CommonResources.LocalDatetime()
            };

            _context.AdminNotifications.Add(entity);
            await _context.SaveChangesAsync();

            return new Message<AdminNotificationModelForView>
            {
                Status = "S",
                Text = "Notification draft created.",
                Code = "200",
                Result = await MapAdminAsync(entity)
            };
        }

        public async Task<AdminNotificationModelWithPagination> GetAdminBroadcastsWithPagination(
            int page,
            int itemsPerPage,
            string? status)
        {
            var query = _context.AdminNotifications.AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(n => n.Status == status);

            var count = await query.CountAsync();
            var entities = await query
                .OrderByDescending(n => n.CreatedOn)
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage)
                .ToListAsync();

            var views = new List<AdminNotificationModelForView>();
            foreach (var e in entities)
                views.Add(await MapAdminAsync(e));

            return new AdminNotificationModelWithPagination
            {
                Count = count,
                Notifications = views
            };
        }

        public async Task<AdminNotificationModelForView?> GetAdminBroadcastByIdAsync(int id)
        {
            var entity = await _context.AdminNotifications.FirstOrDefaultAsync(n => n.Id == id);
            return entity == null ? null : await MapAdminAsync(entity);
        }

        public async Task<Message<AdminNotificationModelForView>> SendAdminBroadcastAsync(
            int id,
            string sentBy)
        {
            var entity = await _context.AdminNotifications.FirstOrDefaultAsync(n => n.Id == id);
            if (entity == null)
                return AdminErr("Notification not found.", "404");

            if (entity.Status == AdminNotificationStatuses.Sent)
                return AdminErr("Notification already sent.", "400");

            var recipients = await ResolveRecipientsAsync(entity.Audience);
            var now = CommonResources.LocalDatetime();

            foreach (var userId in recipients)
            {
                _context.Notifications.Add(new Notification
                {
                    FK_user_ID = userId,
                    Title = entity.Title,
                    Body = entity.Body,
                    Type = NotificationTypes.AdminBroadcast,
                    FK_admin_notification_ID = entity.Id,
                    Is_Read = false,
                    CreatedOn = now
                });
            }

            entity.Status = AdminNotificationStatuses.Sent;
            entity.SentBy = sentBy;
            entity.SentOn = now;
            await _context.SaveChangesAsync();

            var data = new Dictionary<string, string>
            {
                ["type"] = NotificationTypes.AdminBroadcast,
                ["adminNotificationId"] = entity.Id.ToString()
            };

            foreach (var userId in recipients)
            {
                await _pushSender.SendToUserAsync(userId, entity.Title, entity.Body, data);
            }

            return new Message<AdminNotificationModelForView>
            {
                Status = "S",
                Text = $"Notification sent to {recipients.Count} recipient(s).",
                Code = "200",
                Result = await MapAdminAsync(entity)
            };
        }

        private async Task<List<string>> ResolveRecipientsAsync(string audience)
        {
            var query = _context.Users.Where(u => u.IsActive);
            query = audience switch
            {
                AdminNotificationAudiences.Users => query.Where(u => u.UserType == "User"),
                AdminNotificationAudiences.Workers => query.Where(u => u.UserType == "Worker"),
                _ => query.Where(u => u.UserType == "User" || u.UserType == "Worker")
            };
            return await query.Select(u => u.Id).ToListAsync();
        }

        private static string? NormalizeAudience(string? audience)
        {
            if (string.IsNullOrWhiteSpace(audience)) return null;
            var a = audience.Trim();
            if (a.Equals(AdminNotificationAudiences.Users, StringComparison.OrdinalIgnoreCase))
                return AdminNotificationAudiences.Users;
            if (a.Equals(AdminNotificationAudiences.Workers, StringComparison.OrdinalIgnoreCase))
                return AdminNotificationAudiences.Workers;
            if (a.Equals(AdminNotificationAudiences.Both, StringComparison.OrdinalIgnoreCase))
                return AdminNotificationAudiences.Both;
            return null;
        }

        private async Task<AdminNotificationModelForView> MapAdminAsync(AdminNotification e)
        {
            var recipientCount = await _context.Notifications
                .CountAsync(n => n.FK_admin_notification_ID == e.Id);
            return new AdminNotificationModelForView
            {
                Id = e.Id,
                Title = e.Title,
                Body = e.Body,
                Audience = e.Audience,
                Status = e.Status,
                CreatedBy = e.CreatedBy,
                CreatedOn = e.CreatedOn,
                SentBy = e.SentBy,
                SentOn = e.SentOn,
                RecipientCount = recipientCount
            };
        }

        private static NotificationModelForView Map(Notification n) => new()
        {
            Id = n.Id,
            FK_user_ID = n.FK_user_ID,
            Title = n.Title,
            Body = n.Body,
            Type = n.Type,
            FK_job_ID = n.FK_job_ID,
            FK_admin_notification_ID = n.FK_admin_notification_ID,
            Is_Read = n.Is_Read,
            CreatedOn = n.CreatedOn
        };

        private static Message<AdminNotificationModelForView> AdminErr(string text, string code) => new()
        {
            Status = "E",
            Text = text,
            Code = code,
            Result = null
        };
    }
}
