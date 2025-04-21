using BlogSite.Models;
using BlogSite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlogSite.Pages
{
    public class SearchModel : PageModel
    {
        private readonly ILogger<SearchModel> _logger;
        private readonly BlogPostLoader _blogPostLoader;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public List<BlogPost> SearchResults { get; private set; } = new List<BlogPost>();
        public string? Query { get; private set; }

        [BindProperty(SupportsGet = true)]
        public string? q { get; set; } 

        public SearchModel(
            ILogger<SearchModel> logger, 
            BlogPostLoader blogPostLoader,
            IHttpContextAccessor httpContextAccessor) 
        {
            _logger = logger;
            _blogPostLoader = blogPostLoader;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task OnGetAsync()
        {
            Query = q; 

            ViewData["Title"] = $"Search Results for \"{Query}\"";
            _logger.LogInformation("Searching for posts containing: {Query}", Query);

            if (string.IsNullOrWhiteSpace(Query))
            {
                _logger.LogInformation("Search query was empty.");
                SearchResults = new List<BlogPost>(); 
                var emptyRequest = _httpContextAccessor.HttpContext?.Request;
                if (emptyRequest != null)
                {
                    var canonicalUrl = $"{emptyRequest.Scheme}://{emptyRequest.Host}{emptyRequest.Path}";
                    ViewData["CanonicalUrl"] = canonicalUrl;
                    ViewData["OgUrl"] = canonicalUrl; 
                }
                ViewData["MetaDescription"] = "Search the GooBlog for articles."; 
                ViewData["OgTitle"] = ViewData["Title"]; 
                ViewData["OgDescription"] = ViewData["MetaDescription"];
                ViewData["OgType"] = "website";
                return;
            }

            var allPosts = await _blogPostLoader.GetPostsAsync();

            SearchResults = allPosts
                .Where(post =>
                    (post.Title?.Contains(Query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (post.Excerpt?.Contains(Query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (post.Tags?.Any(tag => tag.Contains(Query, StringComparison.OrdinalIgnoreCase)) ?? false)
                 )
                .ToList();

            _logger.LogInformation("Found {Count} search results for query: {Query}", SearchResults.Count, Query);

            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                var canonicalUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
                ViewData["CanonicalUrl"] = canonicalUrl;
                ViewData["OgUrl"] = canonicalUrl; 
            }

            ViewData["MetaDescription"] = $"Search results for '{Query}'."; 
            ViewData["OgTitle"] = ViewData["Title"]; 
            ViewData["OgDescription"] = ViewData["MetaDescription"];
            ViewData["OgType"] = "website"; 
        }
    }
}