using BlogSite.Models;

namespace BlogSite.Services
{
    public class DiscordNotificationService
    {
        private readonly ILogger<DiscordNotificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string? _webhookUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DiscordNotificationService(
            ILogger<DiscordNotificationService> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _webhookUrl = _configuration["Discord:WebhookUrl"];
        }

        public async Task SendNewPostNotificationAsync(BlogPost post)
        {
            if (string.IsNullOrWhiteSpace(_webhookUrl))
            {
                _logger.LogWarning("Discord webhook URL is not configured. Skipping notification.");
                return;
            }

            if (!Uri.TryCreate(_webhookUrl, UriKind.Absolute, out _))
            {
                 _logger.LogError("Invalid Discord webhook URL configured: {WebhookUrl}", _webhookUrl);
                 return;
            }

            string? baseUrl = null;
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
                baseUrl = $"{request.Scheme}://{request.Host}";

            if (baseUrl == null)
            {
                 _logger.LogError("Could not determine base URL from HttpContext. Cannot send absolute URLs to Discord.");
                 return;
            }

            _logger.LogInformation("Sending Discord notification for new post: {PostTitle}", post.Title);

            var client = _httpClientFactory.CreateClient();

            var absolutePostUrl = $"{baseUrl}/BlogPost/{post.Slug}";
            string? absoluteThumbnailUrl = null;
            if (!string.IsNullOrEmpty(post.Thumbnail))
            {
                absoluteThumbnailUrl = post.Thumbnail.StartsWith("/") ? $"{baseUrl}{post.Thumbnail}" : post.Thumbnail;
                if (!absoluteThumbnailUrl.StartsWith("http://") && !absoluteThumbnailUrl.StartsWith("https://"))
                     _logger.LogWarning("Thumbnail URL '{ThumbnailUrl}' might not be absolute. Discord might not display it.", absoluteThumbnailUrl);
            }

             var embed = new {
                title = $"New Blog Post: {post.Title}",
                description = post.Excerpt,
                url = absolutePostUrl,
                color = 12255484,
                author = new {
                    name = post.Author,
                },
                thumbnail = !string.IsNullOrEmpty(absoluteThumbnailUrl) ? new { url = absoluteThumbnailUrl } : null,
                fields = post.Tags.Any()
                    ? new[] { new { name = "Tags", value = string.Join(", ", post.Tags), inline = false } }
                    : null
            };

            var payload = new { embeds = new[] { embed } };


            try
            {
                var response = await client.PostAsJsonAsync(_webhookUrl, payload);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send Discord notification. Status: {StatusCode}, Response: {ErrorContent}", response.StatusCode, errorContent);
                }
                else
                {
                    _logger.LogInformation("Discord notification sent successfully for post: {PostTitle}", post.Title);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending Discord notification for post: {PostTitle}", post.Title);
            }
        }
    }
}