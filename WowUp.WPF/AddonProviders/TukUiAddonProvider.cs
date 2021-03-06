﻿using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WowUp.WPF.AddonProviders.Contracts;
using WowUp.WPF.Extensions;
using WowUp.WPF.Models;
using WowUp.WPF.Models.TukUi;

namespace WowUp.WPF.AddonProviders
{
    public class TukUiAddonProvider : IAddonProvider
    {
        private const string ApiUrl = "https://www.tukui.org/api.php";
        private const string RetailAddonKey = "addon";
        private const string ClassicAddonKey = "classic-addon";
        private const string ElvUiRetailTocUrl = "https://git.tukui.org/elvui/elvui/-/raw/master/ElvUI/ElvUI.toc";
        private const string TukUiRetailTocUrl = "https://git.tukui.org/Tukz/Tukui/-/raw/master/Tukui/Tukui.toc";

        private readonly IMemoryCache _cache;

        public string Name => "TukUI";

        public TukUiAddonProvider(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        public bool IsValidAddonUri(Uri addonUri)
        {
            return false;
        }

        public async Task<AddonSearchResult> GetById(
            string addonId,
            WowClientType clientType)
        {
            return null;
        }

        public async Task<IList<AddonSearchResult>> GetAll(WowClientType clientType, IEnumerable<string> addonIds)
        {
            var results = new List<AddonSearchResult>();

            try
            {
                var addons = await GetAllAddons(clientType);
                results = addons
                    .Where(a => addonIds.Any(aid => aid == a.Id))
                    .Select(a => ToSearchResult(a, string.Empty))
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to search TukUi");
            }

            return results;
        }

        public async Task<IEnumerable<AddonSearchResult>> Search(string addonName, string folderName, WowClientType clientType, string nameOverride = null)
        {
            var results = new List<AddonSearchResult>();
            try
            {
                var addons = await GetAllAddons(clientType);
                var addon = addons
                    .Where(a => a.Name.Equals(addonName, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if(addon != null)
                {
                    results.Add(ToSearchResult(addon, folderName));
                }
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Failed to search TukUi");
            }

            return results;
        }

        private AddonSearchResult ToSearchResult(TukUiAddon addon, string folderName)
        {
            return new AddonSearchResult
            {
                Author = addon.Author,
                ExternalId = addon.Id,
                Folders = new[] { folderName },
                GameVersion = addon.Patch,
                Name = addon.Name,
                ThumbnailUrl = addon.ScreenshotUrl,
                Version = addon.Version,
                DownloadUrl = addon.Url,
                ExternalUrl = addon.WebUrl
            };
        }

        private async Task<IEnumerable<TukUiAddon>> GetAllAddons(WowClientType clientType)
        {
            var cacheKey = GetCacheKey(clientType);
            if(_cache.TryGetValue(cacheKey, out var cachedAddons))
            {
                return cachedAddons as IEnumerable<TukUiAddon>;
            }

            var query = GetAddonsSuffix(clientType);
            var result = await ApiUrl
                .SetQueryParam(query, "all")
                .GetJsonAsync<List<TukUiAddon>>();

            if (clientType.IsRetail())
            {
                result.Add(await GetTukUiRetailAddon());
                result.Add(await GetElvUiRetailAddon());
            }

            _cache.CacheForAbsolute(cacheKey, result, TimeSpan.FromMinutes(10));

            return result;
        }

        private async Task<TukUiAddon> GetElvUiRetailAddon()
        {
            var tocText = await ElvUiRetailTocUrl.GetStringAsync();
            var toc = new Utilities.TocParser(tocText).Toc;

            return new TukUiAddon
            {
                Author = toc.Author,
                Category = "Interfaces",
                Patch = toc.Interface,
                Changelog = string.Empty,
                DonateUrl = "https://www.tukui.org/support.php",
                Downloads = "1",
                Id = "123321",
                LastDownload = string.Empty,
                LastUpdate = string.Empty,
                Name = toc.Title,
                ScreenshotUrl = "https://www.tukui.org/images/apple-touch-icon-120x120.png",
                SmallDesc = "A USER INTERFACE DESIGNED AROUND USER-FRIENDLINESS WITH EXTRA FEATURES THAT ARE NOT INCLUDED IN THE STANDARD UI.",
                Url = $"https://www.tukui.org/downloads/elvui-{toc.Version}.zip",
                Version = toc.Version,
                WebUrl = "https://www.tukui.org/download.php?ui=elvui"
            };
        }

        private async Task<TukUiAddon> GetTukUiRetailAddon()
        {
            var tocText = await TukUiRetailTocUrl.GetStringAsync();
            var toc = new Utilities.TocParser(tocText).Toc;

            return new TukUiAddon
            {
                Author = toc.Author,
                Category = "Interfaces",
                Patch = toc.Interface,
                Changelog = string.Empty,
                DonateUrl = "https://www.tukui.org/support.php",
                Downloads = "1",
                Id = "43252",
                LastDownload = string.Empty,
                LastUpdate = string.Empty,
                Name = toc.Title,
                ScreenshotUrl = "https://www.tukui.org/images/apple-touch-icon-120x120.png",
                SmallDesc = "A clean, lightweight, minimalist and popular user interface among the warcraft community since 2007.",
                Url = $"https://www.tukui.org/downloads/tukui-{toc.Version}.zip",
                Version = toc.Version,
                WebUrl = "https://www.tukui.org/download.php?ui=tukui"
            };
        }

        private string GetCacheKey(WowClientType clientType)
        {
            switch (clientType)
            {
                case WowClientType.Classic:
                case WowClientType.ClassicPtr:
                    return "tukui_classic_addons";
                case WowClientType.Retail:
                case WowClientType.RetailPtr:
                default:
                    return "tukui_addons";
            }
        }

        private string GetAddonsSuffix(WowClientType clientType)
        {
            switch (clientType)
            {
                case WowClientType.Classic:
                case WowClientType.ClassicPtr:
                    return "classic-addons";
                case WowClientType.Retail:
                case WowClientType.RetailPtr:
                default:
                    return "addons";
            }
        }

        private string GetAddonSuffix(WowClientType clientType)
        {
            switch (clientType)
            {
                case WowClientType.Classic:
                case WowClientType.ClassicPtr:
                    return "classic-addon";
                case WowClientType.Retail:
                case WowClientType.RetailPtr:
                default:
                    return "addon";
            }
        }

        public Task<AddonSearchResult> Search(Uri addonUri, WowClientType clientType)
        {
            throw new NotImplementedException();
        }

        public async Task<IList<PotentialAddon>> GetFeaturedAddons(WowClientType clientType)
        {
            return new List<PotentialAddon>();
        }
    }
}
