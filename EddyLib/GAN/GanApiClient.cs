using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.IO;
using System.IO.Compression;
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

        public const string DefaultApiUrl = "https://sustainableurbansystemslab-eddy3d-gan.hf.space";

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
            string url = (apiUrl ?? DefaultApiUrl).TrimEnd('/') + "/predict";

            // 1. Convert float[] to byte[]
            byte[] rawBytes = new byte[inputArray.Length * sizeof(float)];
            Buffer.BlockCopy(inputArray, 0, rawBytes, 0, rawBytes.Length);

            // 2. Compress byte[] with GZip
            string b64Data;
            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionLevel.Optimal, true))
                {
                    gzip.Write(rawBytes, 0, rawBytes.Length);
                }
                b64Data = Convert.ToBase64String(ms.ToArray());
            }

            var payload = new PredictRequest { data_b64 = b64Data };
            string json = JsonConvert.SerializeObject(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await HttpClient.PostAsync(url, content, cancellationToken);

            if ((int)response.StatusCode == 429)
            {
                throw new HttpRequestException("Rate limit exceeded. Please wait and try again.");
            }

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<PredictResponse>(responseJson);

            // 3. Decompress the returned wind speeds
            byte[] compWindBytes = Convert.FromBase64String(result.wind_speeds_b64);
            List<double> windSpeeds = new List<double>();
            using (var ms = new MemoryStream(compWindBytes))
            using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
            using (var msOut = new MemoryStream())
            {
                gzip.CopyTo(msOut);
                byte[] decompressed = msOut.ToArray();
                float[] floatArr = new float[decompressed.Length / sizeof(float)];
                Buffer.BlockCopy(decompressed, 0, floatArr, 0, decompressed.Length);
                
                windSpeeds.Capacity = floatArr.Length;
                for (int i = 0; i < floatArr.Length; i++)
                {
                    windSpeeds.Add(floatArr[i]);
                }
            }

            return new GanPredictionResult
            {
                WindSpeeds = windSpeeds,
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

        private class PredictRequest
        {
            public string data_b64 { get; set; }
        }

        private class PredictResponse
        {
            public string wind_speeds_b64 { get; set; }
            public string image_base64 { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }
    }
}
