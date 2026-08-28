namespace OneeProject.Services.Services.Realtime
{
    public static class JobNotificationHelper
    {
        /// <summary>Returns the other party on the job; never the sender.</summary>
        public static string? ResolveChatRecipient(
            string customerId,
            string? workerId,
            string? senderId)
        {
            var sender = (senderId ?? string.Empty).Trim();
            var customer = (customerId ?? string.Empty).Trim();
            var worker = (workerId ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(sender))
                return null;

            string? recipient;
            if (string.Equals(sender, customer, StringComparison.OrdinalIgnoreCase))
                recipient = string.IsNullOrEmpty(worker) ? null : worker;
            else if (string.Equals(sender, worker, StringComparison.OrdinalIgnoreCase))
                recipient = string.IsNullOrEmpty(customer) ? null : customer;
            else if (!string.IsNullOrEmpty(customer) &&
                     !string.Equals(sender, customer, StringComparison.OrdinalIgnoreCase))
                recipient = customer;
            else if (!string.IsNullOrEmpty(worker) &&
                     !string.Equals(sender, worker, StringComparison.OrdinalIgnoreCase))
                recipient = worker;
            else
                recipient = null;

            if (!string.IsNullOrEmpty(recipient) &&
                string.Equals(recipient, sender, StringComparison.OrdinalIgnoreCase))
                return null;

            return recipient;
        }
    }
}
