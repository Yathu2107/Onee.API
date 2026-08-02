namespace OneeProject.Database.Model.FEAPI_Model.User
{
    public class FEDeviceTokenRequest
    {
        public string Token { get; set; } = string.Empty;
        public string Platform { get; set; } = "android";
    }

    public class FEJobFindWorkersRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class FEJobCreateRequest
    {
        public string Text { get; set; } = string.Empty;
        public List<string> WorkerIds { get; set; } = new();
        public int? AddressId { get; set; }
    }

    public class FEJobCancelRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class FEJobChatSendRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class FEJobRatingRequest
    {
        public int Rating { get; set; }
        public string Feedback { get; set; } = string.Empty;
    }

    public class FEComplaintCreateRequest
    {
        public int JobId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
