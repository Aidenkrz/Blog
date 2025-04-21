using System;
using System.Collections.Generic;

namespace BlogSite.Models
{
    public class BlogPost
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string Thumbnail { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public string Slug { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public int ReadingTimeMinutes { get; set; }
        public string? SoundtrackUrl { get; set; }
        public string? SoundtrackTitle { get; set; }
        public string? SoundtrackArtist { get; set; }
        public string? SoundtrackAlbumArtBase64 { get; set; }

    }
}