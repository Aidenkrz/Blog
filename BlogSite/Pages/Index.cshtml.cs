using BlogSite.Models;
using BlogSite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlogSite.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly BlogPostLoader _blogPostLoader;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public List<BlogPost> Posts { get; private set; } = new List<BlogPost>();
    public string? CurrentTag { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Tag { get; set; }

    public IndexModel(
        ILogger<IndexModel> logger,
        BlogPostLoader blogPostLoader,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _blogPostLoader = blogPostLoader;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task OnGetAsync()
    {
        _logger.LogInformation("Loading blog posts for index page...");
        var allPosts = await _blogPostLoader.GetPostsAsync();

        if (!string.IsNullOrEmpty(Tag))
        {
            CurrentTag = Tag;
            Posts = allPosts.Where(p => p.Tags.Contains(Tag, StringComparer.OrdinalIgnoreCase)).ToList();
            _logger.LogInformation($"Filtered posts by tag '{Tag}'. Found {Posts.Count} posts.");
            ViewData["Title"] = $"Posts tagged '{Tag}'";
        }
        else
        {
            Posts = allPosts;
            _logger.LogInformation($"Loaded {Posts.Count} posts.");
            ViewData["Title"] = "Home";
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request != null)
        {
            var canonicalUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
            ViewData["CanonicalUrl"] = canonicalUrl;
            ViewData["OgUrl"] = canonicalUrl;
        }

        ViewData["MetaDescription"] = CurrentTag != null
            ? $"Blog posts tagged with '{CurrentTag}'."
            : "Welcome to the GooBlog - I'm goobing so hard rn.";
        ViewData["OgTitle"] = ViewData["Title"];
        ViewData["OgDescription"] = ViewData["MetaDescription"];
        ViewData["OgType"] = "website";
    }
}
