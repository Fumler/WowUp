﻿using System.Collections.Generic;

namespace WowUp.WPF.Models.Curse
{
    public class CurseGetFeaturedResponse
    {
        public IEnumerable<CurseSearchResult> Featured { get; set; }
        public IEnumerable<CurseSearchResult> Popular { get; set; }
        public IEnumerable<CurseSearchResult> RecentlyUpdated { get; set; }
    }
}
