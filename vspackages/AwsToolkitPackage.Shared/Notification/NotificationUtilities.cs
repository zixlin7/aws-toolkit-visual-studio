using System.Net.Http;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.VisualStudio.Notification
{
    internal static class NotificationUtilities
    {
        public static async Task<string> FetchHttpContentAsStringAsync(string url)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
