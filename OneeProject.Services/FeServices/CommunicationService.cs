using System;
using System.Collections.Generic;
using System.Text;

namespace OneeProject.Services.FeServices
{
    public class CommunicationService
    {
        private static readonly HttpClient httpClient = new();

        public async Task SendMessageAsync(string mobile, string message)
        {
            // Hard-coded items
            string esmsqk = ""; // Replace with your client key
            string sourceAddress = "MP Mart"; // Replace with your source address mask


            // Construct the request URL
            string requestUrl = $"https://e-sms.dialog.lk/api/v1/message-via-url/create/url-campaign" +
                                $"?esmsqk={esmsqk}&list={mobile}&source_address={sourceAddress}&message={message}";

            try
            {
                // Send the GET request
                var response = await httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode(); // Throw if not a success code.

                // Read the response
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response: " + responseBody);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Request error: " + e.Message);
            }
        }
    }
}
