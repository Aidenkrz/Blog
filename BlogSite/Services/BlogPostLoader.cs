using BlogSite.Models;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using TagLib;

namespace BlogSite.Services
{
    public class BlogPostLoader
    {
        private readonly string _postsDirectory;
        private readonly MarkdownPipeline _pipeline;
        private List<BlogPost>? _cachedPosts;
        private readonly ILogger<BlogPostLoader> _logger;
        private readonly IWebHostEnvironment _env; 

        public BlogPostLoader(IWebHostEnvironment env, ILogger<BlogPostLoader> logger)
        {
            _logger = logger;
            _env = env; 
            _postsDirectory = Path.Combine(env.ContentRootPath, "Posts");

            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseYamlFrontMatter()
                .Build();
        }

        public async Task<List<BlogPost>> GetPostsAsync(bool forceReload = false)
        {
            if (_cachedPosts != null && !forceReload)
                return _cachedPosts;

            _cachedPosts = new List<BlogPost>();
            if (!System.IO.Directory.Exists(_postsDirectory)) 
            {
                _logger.LogWarning("Posts directory not found at {Directory}", _postsDirectory);
                return _cachedPosts; 
            }

            var markdownFiles = System.IO.Directory.EnumerateFiles(_postsDirectory, "*.md", SearchOption.TopDirectoryOnly); 

            foreach (var filePath in markdownFiles)
            {
                var post = await LoadPostFromFileAsync(filePath);
                if (post != null)
                    _cachedPosts.Add(post);
            }

            _cachedPosts = _cachedPosts.OrderByDescending(p => p.Date).ToList();
            return _cachedPosts;
        }

        public async Task<BlogPost?> GetPostBySlugAsync(string slug)
        {
             var posts = await GetPostsAsync(); 
             return posts.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<BlogPost?> LoadPostFromFileAsync(string filePath)
        {
            try
            {
                var markdownContent = await System.IO.File.ReadAllTextAsync(filePath, Encoding.UTF8); 
                var document = Markdown.Parse(markdownContent, _pipeline);

                var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
                BlogPost? post = null;
                Dictionary<string, object>? metadata = null;

                if (yamlBlock != null)
                {
                    var yaml = yamlBlock.Lines.ToString();
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .IgnoreUnmatchedProperties()
                        .Build();

                    metadata = deserializer.Deserialize<Dictionary<string, object>>(yaml);

                    post = new BlogPost
                    {
                        Title = metadata.GetValueOrDefault("title", "Untitled")?.ToString() ?? "Untitled",
                        Author = metadata.GetValueOrDefault("author", "Unknown")?.ToString() ?? "Unknown",
                        Thumbnail = metadata.GetValueOrDefault("thumbnail", "")?.ToString() ?? "",
                        ThumbnailCredits = metadata.GetValueOrDefault("thumbnailCredits", "")?.ToString() ?? "",
                        Tags = metadata.TryGetValue("tags", out var tagsObj) && tagsObj is List<object> tagsList
                               ? tagsList.Select(t => t?.ToString() ?? string.Empty).ToList()
                               : new List<string>(),
                        SoundtrackUrl = metadata.GetValueOrDefault("soundtrackUrl", null)?.ToString() 
                    };

                    if (metadata.TryGetValue("date", out var dateObj) && DateOnly.TryParse(dateObj?.ToString(), out var date))
                    {
                        post.Date = date;
                    }
                    else
                    {
                        post.Date = DateOnly.FromDateTime(System.IO.File.GetCreationTime(filePath)); 
                    }
                }
                else
                {
                    post = new BlogPost 
                    {
                        Title = Path.GetFileNameWithoutExtension(filePath).Replace('-', ' '),
                        Date = DateOnly.FromDateTime(System.IO.File.GetCreationTime(filePath)) 
                    };
                }

                post.Slug = Path.GetFileNameWithoutExtension(filePath)
                                .ToLowerInvariant()
                                .Replace(" ", "-");

                using (var writer = new StringWriter())
                {
                    var renderer = new Markdig.Renderers.HtmlRenderer(writer);
                    _pipeline.Setup(renderer);
                    var contentBlocks = document.Where(block => !(block is YamlFrontMatterBlock));
                    foreach (var block in contentBlocks) { renderer.Render(block); }
                    post.HtmlContent = writer.ToString();
                }

                string? descriptionFromMeta = metadata?.GetValueOrDefault("description", "")?.ToString();
                var plainTextContent = Markdig.Markdown.ToPlainText(markdownContent.Substring(yamlBlock?.Span.End ?? 0));

                if (!string.IsNullOrWhiteSpace(descriptionFromMeta))
                {
                    post.Excerpt = descriptionFromMeta;
                }
                else
                {
                    const int excerptLength = 200;
                    post.Excerpt = new string(plainTextContent.Take(excerptLength).ToArray()) + (plainTextContent.Length > excerptLength ? "..." : "");
                }

                const double wordsPerMinute = 200.0;
                var wordCount = plainTextContent.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
                post.ReadingTimeMinutes = (int)Math.Ceiling(wordCount / wordsPerMinute);

                if (post != null && !string.IsNullOrEmpty(post.SoundtrackUrl))
                {
                    ReadSoundtrackMetadata(post); 
                }

                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading post from {FilePath}", filePath); 
                return null;
            }
        }

        private void ReadSoundtrackMetadata(BlogPost post)
        {
            if (string.IsNullOrEmpty(post.SoundtrackUrl)) return;

            var relativePath = post.SoundtrackUrl.TrimStart('/');
            var physicalPath = Path.Combine(_env.WebRootPath, relativePath); 

            if (!System.IO.File.Exists(physicalPath)) 
            {
                _logger.LogWarning("Soundtrack file not found for post '{Slug}' at path: {FilePath}", post.Slug, physicalPath);
                post.SoundtrackUrl = null;
                return;
            }

            try
            {
                using (var tagFile = TagLib.File.Create(physicalPath)) 
                {
                    post.SoundtrackTitle = tagFile.Tag?.Title;
                    post.SoundtrackArtist = tagFile.Tag?.FirstPerformer ?? tagFile.Tag?.FirstAlbumArtist;

                    if (tagFile.Tag?.Pictures?.Length > 0)
                    {
                        var picture = tagFile.Tag.Pictures[0];
                        _logger.LogDebug("Found picture data for post '{Slug}'. MimeType: {MimeType}, Size: {Size} bytes.", post.Slug, picture.MimeType, picture.Data.Count);

                        if (picture.Data.Count > 0 && !string.IsNullOrEmpty(picture.MimeType) && picture.MimeType.StartsWith("image/"))
                        {
                            try
                            {
                                var base64 = Convert.ToBase64String(picture.Data.Data);
                                if (!string.IsNullOrEmpty(base64))
                                {
                                    post.SoundtrackAlbumArtBase64 = $"data:{picture.MimeType};base64,{base64}";
                                    _logger.LogDebug("Successfully generated Base64 data URI for album art (Length: {Length}).", post.SoundtrackAlbumArtBase64.Length);
                                }
                                else
                                {
                                     _logger.LogWarning("Base64 conversion resulted in empty string for post '{Slug}'.", post.Slug);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error converting album art to Base64 for post '{Slug}'.", post.Slug);
                            }
                        }
                        else
                        {
                             _logger.LogWarning("Picture data found but seems invalid (zero size or non-image MIME type: {MimeType}) for post '{Slug}'.", picture.MimeType, post.Slug);
                        }
                    } else {
                         _logger.LogDebug("No embedded pictures found in soundtrack metadata for post '{Slug}'.", post.Slug);
                    }
                }
                 _logger.LogInformation("Finished attempting to read soundtrack metadata for post '{Slug}'", post.Slug);
            }
            catch (CorruptFileException ex) 
            {
                 _logger.LogWarning(ex, "Could not read metadata from potentially corrupt soundtrack file for post '{Slug}': {FilePath}", post.Slug, physicalPath);
                 post.SoundtrackTitle = null; post.SoundtrackArtist = null; post.SoundtrackAlbumArtBase64 = null;
            }
            catch (UnsupportedFormatException ex)
            {
                 _logger.LogWarning(ex, "Format of soundtrack file not supported by TagLib# for post '{Slug}': {FilePath}", post.Slug, physicalPath);
                 post.SoundtrackUrl = null; 
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error reading soundtrack metadata for post '{Slug}': {FilePath}", post.Slug, physicalPath);
                 post.SoundtrackTitle = null; post.SoundtrackArtist = null; post.SoundtrackAlbumArtBase64 = null;
            }
        }
    }

    public static class DictionaryExtensions
    {
        public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, object> dictionary, TKey key, TValue defaultValue)
        {
            if (dictionary.TryGetValue(key, out object? value) && value is TValue typedValue)
            {
                return typedValue;
            }
            if (value != null && typeof(TValue) == typeof(string))
            {
                return (TValue)(object)value.ToString()!;
            }
             if (value != null && typeof(TValue) == typeof(List<string>) && value is List<object> list)
            {
                 return (TValue)(object)list.Select(i => i.ToString() ?? "").ToList();
            }
            return defaultValue;
        }
    }
}