using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace EddyLib.Web
{
    public static class FileDownloader
    {
        static readonly string DefaultEpwCacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Eddy3D", "Weather");

        /// <summary>
        /// Resolves an EPW input that may be a local path or an http(s) URL.
        /// If a URL is given, the file is downloaded to <paramref name="cacheDir"/>
        /// (default: %AppData%\Eddy3D\Weather) and the local path is returned.
        /// Skips the download when the filename already exists in the cache.
        /// Returns null when <paramref name="epwInput"/> is not a URL (caller keeps original path).
        /// </summary>
        public static (string LocalPath, bool WasDownloaded) ResolveEpwPath(string epwInput, string cacheDir = null)
        {
            cacheDir ??= DefaultEpwCacheDir;

            string fileName = Path.GetFileName(new Uri(epwInput).AbsolutePath);
            string localPath = Path.Combine(cacheDir, fileName);

            if (File.Exists(localPath))
                return (localPath, false);

            Task.Run(async () => await DownloadFileAsync(epwInput, localPath)).Wait();
            return (localPath, true);
        }

        /// <summary>
        /// Asynchronously downloads a file from the specified URL and saves it to the given file path.
        /// </summary>
        public static async Task DownloadFileAsync(string url, string filePath)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty.", nameof(url));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            try
            {
                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fileStream);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download file from {url}. Error: {ex.Message}", ex);
            }
        }
    }
}
