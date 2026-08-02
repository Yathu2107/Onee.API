using OneeProject.Database.Common;
using OneeProject.Database.Model.API_Model;
using OneeProject.Database.Model.FEAPI_Model.User;
using OneeProject.Services.Services;

namespace OneeProject.Services.FeServices.User
{
    public class FEComplaintService(ComplaintService complaintService)
    {
        private readonly ComplaintService _complaintService = complaintService;

        public Task<Message<ComplaintModelForView>> CreateAsync(
            FEComplaintCreateRequest model,
            string customerId)
            => _complaintService.CreateAsync(new ComplaintModelForInsert
            {
                JobId = model.JobId,
                Subject = model.Subject,
                Description = model.Description
            }, customerId);
    }
}
