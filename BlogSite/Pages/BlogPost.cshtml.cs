using BlogSite.Models;
using BlogSite.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlogSite.Pages
{
    public class BlogPostModel : PageModel
    {
        private readonly ILogger<BlogPostModel> _logger;
        private readonly BlogPostLoader _blogPostLoader;
        private readonly IConfiguration _configuration;
        private readonly DiscordNotificationService _discordNotifier;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly HashSet<string> _notifiedSlugs = new HashSet<string>();
        private static readonly object _lock = new object();
        private readonly string _notifiedPostsFilePath;

        public BlogPost? Post { get; private set; }

        public bool GiscusEnabled { get; private set; }
        public string GiscusRepo { get; private set; } = "";
        public string GiscusRepoId { get; private set; } = "";
        public string GiscusCategory { get; private set; } = "";
        public string GiscusCategoryId { get; private set; } = "";
        public string GiscusMapping { get; private set; } = "pathname";
        public string GiscusReactionsEnabled { get; private set; } = "1";
        public string GiscusEmitMetadata { get; private set; } = "0";
        public string GiscusInputPosition { get; private set; } = "top";
        public string GiscusTheme { get; private set; } = "preferred_color_scheme";
        public string GiscusLang { get; private set; } = "en";


        [BindProperty(SupportsGet = true)]
        public string Slug { get; set; } = string.Empty;

        public BlogPostModel(
            ILogger<BlogPostModel> logger,
            BlogPostLoader blogPostLoader,
            IConfiguration configuration,
            DiscordNotificationService discordNotifier,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _blogPostLoader = blogPostLoader;
            _configuration = configuration;
            _discordNotifier = discordNotifier;
            _env = env;
            _httpContextAccessor = httpContextAccessor;

            _notifiedPostsFilePath = Path.Combine(_env.ContentRootPath, "notified_posts.log");
            LoadNotifiedSlugs();

            GiscusEnabled = _configuration.GetValue<bool>("Giscus:Enabled", false);
            if (GiscusEnabled)
            {
                GiscusRepo = _configuration.GetValue<string>("Giscus:Repo") ?? "";
                GiscusRepoId = _configuration.GetValue<string>("Giscus:RepoId") ?? "";
                GiscusCategory = _configuration.GetValue<string>("Giscus:Category") ?? "";
                GiscusCategoryId = _configuration.GetValue<string>("Giscus:CategoryId") ?? "";
                GiscusMapping = _configuration.GetValue<string>("Giscus:Mapping") ?? "pathname";
                GiscusReactionsEnabled = _configuration.GetValue<string>("Giscus:ReactionsEnabled") ?? "1";
                GiscusEmitMetadata = _configuration.GetValue<string>("Giscus:EmitMetadata") ?? "0";
                GiscusInputPosition = _configuration.GetValue<string>("Giscus:InputPosition") ?? "top";
                GiscusTheme = _configuration.GetValue<string>("Giscus:Theme") ?? "preferred_color_scheme";
                GiscusLang = _configuration.GetValue<string>("Giscus:Lang") ?? "en";
            }
        }

        private void LoadNotifiedSlugs()
        {
            lock (_lock)
            {
                if (_notifiedSlugs.Count == 0 && System.IO.File.Exists(_notifiedPostsFilePath))
                {
                    try
                    {
                        var slugs = System.IO.File.ReadAllLines(_notifiedPostsFilePath);
                        foreach (var slug in slugs)
                        {
                            if (!string.IsNullOrWhiteSpace(slug))
                            {
                                _notifiedSlugs.Add(slug.Trim());
                            }
                        }
                         _logger.LogInformation("Loaded {Count} notified slugs from file.", _notifiedSlugs.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading notified posts file: {FilePath}", _notifiedPostsFilePath);
                    }
                }
            }
        }

        private void AddNotifiedSlug(string slug)
        {
             lock (_lock)
             {
                 if (_notifiedSlugs.Add(slug))
                 {
                     try
                     {
                         System.IO.File.AppendAllLines(_notifiedPostsFilePath, new[] { slug });
                         _logger.LogInformation("Added slug '{Slug}' to notified posts file.", slug);
                     }
                     catch (Exception ex)
                     {
                         _logger.LogError(ex, "Error appending slug '{Slug}' to notified posts file: {FilePath}", slug, _notifiedPostsFilePath);
                     }
                 }
             }
        }


        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Slug))
            {
                _logger.LogWarning("Attempted to access blog post page without a slug.");
                return NotFound("Blog post slug not provided.");
            }

            _logger.LogInformation($"Loading blog post with slug: {Slug}");
            Post = await _blogPostLoader.GetPostBySlugAsync(Slug);

            if (Post == null)
            {
                _logger.LogWarning($"Blog post with slug '{Slug}' not found.");
                return NotFound($"Blog post '{Slug}' not found.");
            }

            ViewData["Title"] = Post.Title;
            _logger.LogInformation($"Successfully loaded blog post: {Post.Title}");

            bool alreadyNotified;
            lock(_lock)
            {
                alreadyNotified = _notifiedSlugs.Contains(Slug);
            }

            if (!alreadyNotified)
            {
                _logger.LogInformation("Post '{Slug}' has not been notified yet. Attempting to send notification.", Slug);
                var postUrl = Url.PageLink("/BlogPost", null, new { slug = Slug }, Request.Scheme) ?? $"/{Slug}";

                _ = _discordNotifier.SendNewPostNotificationAsync(Post);

                AddNotifiedSlug(Slug);
            }
            else
            {
                 _logger.LogDebug("Post '{Slug}' already notified. Skipping Discord notification.", Slug);
            }

            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null && Post != null)
            {
                var canonicalUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
                ViewData["CanonicalUrl"] = canonicalUrl;
                ViewData["OgUrl"] = canonicalUrl;

                if (!string.IsNullOrEmpty(Post.Thumbnail))
                {
                    var baseUrl = $"{request.Scheme}://{request.Host}";
                    var absoluteThumbnailUrl = Post.Thumbnail.StartsWith("/") ? $"{baseUrl}{Post.Thumbnail}" : Post.Thumbnail;
                     if (Uri.TryCreate(absoluteThumbnailUrl, UriKind.Absolute, out _)) {
                         ViewData["OgImage"] = absoluteThumbnailUrl;
                     }
                }
                ViewData["MetaDescription"] = Post.Excerpt;
                ViewData["OgTitle"] = ViewData["Title"];
                ViewData["OgDescription"] = ViewData["MetaDescription"];
                ViewData["OgType"] = "article";
                ViewData["ArticleAuthor"] = Post.Author;
                ViewData["ArticlePublishedTime"] = Post.Date.ToDateTime(TimeOnly.MinValue).ToString("o");
            }


            return Page();
        }
    }
}