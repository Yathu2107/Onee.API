namespace OneeProject.Database.Model.FEAPI_Model.Worker
{
    public class FEWorkerDeviceTokenRequest
    {
        public string Token { get; set; } = string.Empty;
        public string Platform { get; set; } = "android";
    }

    public class FEWorkerJobConfirmRequest
    {
        public decimal Amount { get; set; }
    }

    public class FEWorkerJobChatSendRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
