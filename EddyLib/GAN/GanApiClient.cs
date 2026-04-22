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
        }

        /// <summary>
        /// Sends a pre-normalised float array to the GAN API and returns wind speeds.
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
            string url = (apiUrl ?? DefaultApiUrl).TrimEnd('/') + "/predict.bin";

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

            // 3. Decode wind speeds: gzip(float32[])
            byte[] compWindBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            List<double> windSpeeds = DecompressFloatsFromGzip(compWindBytes);

            return new GanPredictionResult
            {
                WindSpeeds = windSpeeds
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

        private static List<double> DecompressFloatsFromGzip(byte[] compressed)
        {
            // Detect format from magic bytes before attempting decompression.
            // GZip: 0x1F 0x8B 0x08 (method must be DEFLATE=8) | zlib: 0x78 xx | ZIP: 0x50 0x4B
            if (compressed.Length < 10)
                throw new InvalidDataException(
                    $"Compressed wind speed payload is too short ({compressed.Length} bytes) to be a valid GZip stream. " +
                    "The response may have been truncated in transit.");

            bool isGzip = compressed[0] == 0x1F && compressed[1] == 0x8B;
            if (!isGzip)
            {
                byte b0 = compressed[0], b1 = compressed[1];
                string hint = (b0 == 0x78) ? "zlib/deflate" : (b0 == 0x50 && b1 == 0x4B) ? "ZIP" : $"unknown (0x{b0:X2} 0x{b1:X2})";
                throw new InvalidDataException(
                    $"GAN API returned wind speeds in {hint} format instead of GZip. " +
                    "The server and client compression formats are mismatched.");
            }

            if (compressed[2] != 0x08)
            {
                throw new InvalidDataException(
                    $"GAN API returned GZip-framed data using compression method 0x{compressed[2]:X2} (not DEFLATE). " +
                    $"Payload size: {compressed.Length} bytes. This typically indicates transport-level corruption " +
                    "(e.g. base64 truncation or a middle-box mangling the response).");
            }

            using (var ms = new MemoryStream(compressed))
            using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
            using (var msOut = new MemoryStream())
            {
                try
                {
                    gzip.CopyTo(msOut);
                }
                catch (InvalidDataException ex)
                {
                    throw new InvalidDataException(
                        $"GZip decompression failed on a {compressed.Length}-byte payload: {ex.Message}. " +
                        "The response body may have been truncated.", ex);
                }
                byte[] decompressed = msOut.ToArray();
                float[] floatArr = new float[decompressed.Length / sizeof(float)];
                Buffer.BlockCopy(decompressed, 0, floatArr, 0, decompressed.Length);
                var result = new List<double>(floatArr.Length);
                for (int i = 0; i < floatArr.Length; i++)
                    result.Add(floatArr[i]);
                return result;
            }
        }

        private class PredictRequest
        {
            public string data_b64 { get; set; }
        }

    }
}
