using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using EddyLib;
using EddyLib.GlobalSettings;

namespace EddyLib.Web
{
    public static class UpdateChecker
    {
        private const string RepoOwner = "Eddy3D-Dev";
        private const string RepoName = "Eddy3D";
        private const string ApiUrl = "https://api.github.com/repos/" + RepoOwner + "/" + RepoName + "/releases/latest";

        public static bool IsUpdateAvailable { get; private set; } = false;
        public static string LatestVersion { get; private set; }
        public static string CurrentVersion => EddyVersion.ProductVersion;

        private static bool _hasChecked = false;

        /// <summary>
        /// Checks for the latest release on GitHub asynchronously.
        /// This method is safe to call multiple times; it will only perform the network request once per session.
        /// </summary>
        public static async void CheckForUpdateAsync()
        {
            if (_hasChecked) return;
            _hasChecked = true;

            try
            {
                using (var client = new HttpClient())
                {
                    // GitHub API requires a User-Agent header
                    client.DefaultRequestHeaders.Add("User-Agent", "Eddy3D-UpdateChecker");

                    var response = await client.GetAsync(ApiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var release = JObject.Parse(json);
                        var tagName = release["tag_name"]?.ToString();

                        if (!string.IsNullOrEmpty(tagName))
                        {
                            // Remove 'v' prefix if present
                            var cleanTag = tagName.TrimStart('v', 'V');
                            
                            if (Version.TryParse(cleanTag, out Version latest) && 
                                Version.TryParse(CurrentVersion, out Version current))
                            {
                                if (latest > current)
                                {
                                    IsUpdateAvailable = true;
                                    LatestVersion = tagName;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Silently fail to avoid disrupting the user experience
            }
        }
    }
}
