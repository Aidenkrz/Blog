using BlogSite.Models;
using BlogSite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Xml.Linq;

namespace BlogSite.Pages
{
    public class SitemapModel : PageModel
    {
        private readonly ILogger<SitemapModel> _logger;
        private readonly BlogPostLoader _blogPostLoader;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public List<string> SitemapUrls { get; private set; } = new List<string>(); 

        public SitemapModel(
            ILogger<SitemapModel> logger, 
            BlogPostLoader blogPostLoader, 
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _blogPostLoader = blogPostLoader;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> OnGet()
        {
            _logger.LogInformation("Generating sitemap.xml");

            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
            {
                _logger.LogError("HttpContext is null, cannot generate sitemap.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Could not access request context.");
            }

            var baseUrl = $"{request.Scheme}://{request.Host}";
            SitemapUrls.Add(baseUrl + "/");

            var posts = await _blogPostLoader.GetPostsAsync();
            foreach (var post in posts)
                SitemapUrls.Add($"{baseUrl}/BlogPost/{post.Slug}");

            _logger.LogInformation("Generated {UrlCount} URLs for sitemap.", SitemapUrls.Count);

            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var sitemap = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(ns + "urlset",
                    from url in SitemapUrls
                    select new XElement(ns + "url",
                        new XElement(ns + "loc", url)
                    )
                )
            );

            return Content(sitemap.ToString(), "application/xml");
        }
    }
}