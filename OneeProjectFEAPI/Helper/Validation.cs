using OneeProject.Database.Common;

namespace OneeProjectFEAPI.Helper
{
    public class Validation
    {
        public static Message<string> ValidateMobile(string mobile, string countryCode)
        {
            // Check if mobile is empty
            if (string.IsNullOrWhiteSpace(mobile))
            {
                return new Message<string>
                {
                    Text = "Mobile number is required."
                };
            }

            // Check country code
            if (string.IsNullOrWhiteSpace(countryCode) || countryCode != "+94")
            {
                return new Message<string>
                {
                    Text = "The provided country code is not supported."
                };
            }

            // Check mobile number format: must start with 7 and be 9 digits
            if (!(mobile.StartsWith("7") && mobile.Length == 9))
            {
                return new Message<string>
                {
                    Text = "Invalid number. Must start with 7 and be exactly 9 digits."
                };
            }

            // Valid
            return null;
        }
    }
}
