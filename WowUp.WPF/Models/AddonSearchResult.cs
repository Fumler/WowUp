﻿using System.Collections.Generic;

namespace WowUp.WPF.Models
{
    public class AddonSearchResult
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string GameVersion { get; set; }
        public string ThumbnailUrl { get; set; }
        public IEnumerable<string> Folders { get; set; }
        public string ExternalId { get; set; }
        public string DownloadUrl { get; set; }
        public string ExternalUrl { get; set; }
        public string ProviderName { get; set; }
    }
}
