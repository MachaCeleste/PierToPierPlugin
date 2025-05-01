using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PierToPierPlugin
{
    public class WebhookHandler
    {
        private readonly string _webhookUrl;

        public WebhookHandler(string url)
        {
            _webhookUrl = url;
            if (string.IsNullOrEmpty(_webhookUrl))
                throw new ArgumentNullException(nameof(_webhookUrl));
        }

        /// <summary>
        /// Sends a simple message to the Discord webhook.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task SendMessageAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Message content cannot be empty!", nameof(content));
            var payload = new { content };
            await SendPayloadAsync(payload);
        }

        /// <summary>
        /// Sends an embed to the Discord webhook.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public async Task SendEmbedAsync(string title, string description,  int color = 0x7289DA)
        {
            var payload = new
            {
                embeds = new[]
                {
                    new
                    {
                        title,
                        description,
                        color
                    }
                }
            };
            await SendPayloadAsync(payload);
        }

        private async Task SendPayloadAsync(object payload)
        {
            using var client = new HttpClient();
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(_webhookUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error sending webhook: {error}");
            }
        }
    }
}