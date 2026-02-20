using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EddyLib.GAN
{
    /// <summary>
    /// HTTP client for the Eddy3D GAN wind prediction API.
    /// </summary>
    public static class GanApiClient
    {
        private static readonly HttpClient HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(120)
        };

        public const string DefaultApiUrl = "https://eddy3d-gan-api.onrender.com";

        /// <summary>
        /// Result returned from the GAN prediction API.
        /// </summary>
        public class GanPredictionResult
        {
            public List<double> WindSpeeds { get; set; }
            public byte[] ImageBytes { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        /// <summary>
        /// Sends a pre-normalised float array to the GAN API and returns wind speeds + output image.
        /// This bypasses image encoding, sending the raw array directly.
        /// </summary>
        /// <param name="inputArray">Flat float array of length 3*512*512, channel-first (R,G,B), values in [-1,1].</param>
        /// <param name="apiUrl">API base URL. Uses default if null.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task<GanPredictionResult> PredictArrayAsync(
            float[] inputArray,
            string apiUrl = null,
            CancellationToken cancellationToken = default)
        {
            string url = (apiUrl ?? DefaultApiUrl).TrimEnd('/') + "/predict_array";

            var payload = new ArrayPredictRequest { data = inputArray };
            string json = JsonConvert.SerializeObject(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await HttpClient.PostAsync(url, content, cancellationToken);

            if ((int)response.StatusCode == 429)
            {
                throw new HttpRequestException("Rate limit exceeded. Please wait and try again.");
            }

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse>(responseJson);

            return new GanPredictionResult
            {
                WindSpeeds = result.wind_speeds,
                ImageBytes = Convert.FromBase64String(result.image_base64),
                Width = result.width,
                Height = result.height,
            };
        }

        /// <summary>
        /// Sends a PNG image to the GAN API and returns wind speeds + output image.
        /// Kept for backward compatibility / testing.
        /// </summary>
        public static async Task<GanPredictionResult> PredictImageAsync(
            byte[] pngBytes,
            string apiUrl = null,
            CancellationToken cancellationToken = default)
        {
            string url = (apiUrl ?? DefaultApiUrl).TrimEnd('/') + "/predict";

            using var formContent = new MultipartFormDataContent();
            using var imageContent = new ByteArrayContent(pngBytes);
            imageContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            formContent.Add(imageContent, "file", "input.png");

            using var response = await HttpClient.PostAsync(url, formContent, cancellationToken);

            if ((int)response.StatusCode == 429)
            {
                throw new HttpRequestException("Rate limit exceeded. Please wait and try again.");
            }

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse>(responseJson);

            return new GanPredictionResult
            {
                WindSpeeds = result.wind_speeds,
                ImageBytes = Convert.FromBase64String(result.image_base64),
                Width = result.width,
                Height = result.height,
            };
        }

        /// <summary>
        /// Checks if the API is reachable.
        /// </summary>
        public static async Task<bool> CheckHealthAsync(
            string apiUrl = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                string url = (apiUrl ?? DefaultApiUrl).TrimEnd('/') + "/health";
                var response = await HttpClient.GetAsync(url, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Waits for the API to become healthy, polling at intervals.
        /// Returns true once healthy, false if cancelled or max retries exceeded.
        /// Invokes <paramref name="onStatusChange"/> on each poll so callers can update UI.
        /// </summary>
        public static async Task<bool> WaitForServerAsync(
            string apiUrl = null,
            int maxRetries = 10,
            int retryDelayMs = 5000,
            Action<string> onStatusChange = null,
            CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                if (cancellationToken.IsCancellationRequested) return false;

                onStatusChange?.Invoke($"Waking up server... ({i + 1}/{maxRetries})");

                if (await CheckHealthAsync(apiUrl, cancellationToken))
                    return true;

                await Task.Delay(retryDelayMs, cancellationToken);
            }

            return false;
        }

        // Internal DTOs
        private class ArrayPredictRequest
        {
            public float[] data { get; set; }
        }

        private class ApiResponse
        {
            public List<double> wind_speeds { get; set; }
            public string image_base64 { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }
    }
}
