using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace EddyLib.Web
{
    public static class FileDownloader
    {
        /// <summary>
        /// Asynchronously downloads a file from the specified URL and saves it to the given file path.
        /// </summary>
        /// <param name="url">The URL of the file to download.</param>
        /// <param name="filePath">The local file path to save the downloaded file.</param>
        public static async Task DownloadFileAsync(string url, string filePath)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty.", nameof(url));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            try
            {
                // Download file using HttpClient
                using (var httpClient = new HttpClient())
                {
                    using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        // Save the file
                        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Re-throw or log as needed; for now, we let the component handle it or crash
                // Console.WriteLine($"Error downloading file: {ex.Message}");
                throw new Exception($"Failed to download file from {url}. Error: {ex.Message}", ex);
            }
        }
    }
}
