using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EddyLib.GAN;
using Xunit;
using Xunit.Abstractions;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "GAN")]
    public class Test_GanApi
    {
        private readonly ITestOutputHelper _output;
        private static readonly string ApiUrl =
            Environment.GetEnvironmentVariable("EDDY3D_GAN_API_URL") ?? GanApiClient.DefaultApiUrl;

        public Test_GanApi(ITestOutputHelper output)
        {
            _output = output;
        }

        [RequiresExternalServiceFact]
        public async Task HealthCheck_ReturnsTrue_WhenServerIsUp()
        {
            // The free Render instance can take up to 60 s to cold-start.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

            bool healthy = await GanApiClient.CheckHealthAsync(
                ApiUrl, cts.Token);

            _output.WriteLine($"Initial health check: {healthy}");

            if (!healthy)
            {
                _output.WriteLine("Server appears to be sleeping — attempting wake-up...");
                healthy = await GanApiClient.WaitForServerAsync(
                    ApiUrl,
                    maxRetries: 12,
                    retryDelayMs: 5000,
                    onStatusChange: msg => _output.WriteLine(msg),
                    cancellationToken: cts.Token);
            }

            Assert.True(healthy, "Server did not become healthy within 90 s.");
        }

        [Fact]
        public async Task HealthCheck_ReturnsFalse_ForBogusUrl()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            bool healthy = await GanApiClient.CheckHealthAsync(
                "https://this-url-does-not-exist-eddy3d.example.com", cts.Token);

            Assert.False(healthy);
        }

        [RequiresExternalServiceFact]
        public async Task PredictArray_ReturnsWindSpeeds_WithArtificialInput()
        {
            // Arrange — 512×512, 3 channels, simple gradient pattern.
            const int imageSize = 512;
            const int channels = 3;
            float[] artificialInput = new float[channels * imageSize * imageSize];

            for (int row = 0; row < imageSize; row++)
            {
                for (int col = 0; col < imageSize; col++)
                {
                    int idx = row * imageSize + col;
                    artificialInput[idx] = (col / (float)(imageSize - 1)) * 2f - 1f;
                    artificialInput[idx + imageSize * imageSize] = (row / (float)(imageSize - 1)) * 2f - 1f;
                    // B channel stays 0
                }
            }

            // Allow generous time: cold-start wake-up + model load + inference.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(180));

            // Ensure server is fully warmed up before sending predict request.
            bool serverReady = await EnsureServerReady(cts.Token);
            if (!serverReady)
            {
                _output.WriteLine("Server did not wake up — skipping prediction test.");
                return;
            }

            // Act — retry up to 3 times to handle transient 502s during model warm-up.
            GanApiClient.GanPredictionResult result = null;
            int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _output.WriteLine($"Sending prediction request (attempt {attempt}/{maxRetries})...");
                    result = await GanApiClient.PredictArrayAsync(
                        artificialInput, ApiUrl, cts.Token);
                    break; // Success
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("404"))
                {
                    _output.WriteLine("Wind-only binary endpoint not yet deployed to remote server — skipping prediction test gracefully.");
                    return;
                }
                catch (HttpRequestException ex) when (
                    ex.Message.Contains("502") || ex.Message.Contains("503"))
                {
                    _output.WriteLine($"Attempt {attempt} got {ex.Message} — model may still be loading.");
                    if (attempt == maxRetries) throw;
                    await Task.Delay(10000, cts.Token);
                }
            }

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.WindSpeeds);
            Assert.True(result.WindSpeeds.Count > 0,
                "Expected at least one wind speed value.");
            Assert.Equal(imageSize * imageSize, result.WindSpeeds.Count);

            _output.WriteLine($"Received {result.WindSpeeds.Count} wind speed values.");
            _output.WriteLine($"Wind speed range: [{result.WindSpeeds.Min():F3}, {result.WindSpeeds.Max():F3}] m/s");
        }

        /// <summary>
        /// Ensures the API server is healthy, waking it up if needed.
        /// </summary>
        private async Task<bool> EnsureServerReady(CancellationToken ct)
        {
            bool healthy = await GanApiClient.CheckHealthAsync(
                ApiUrl, ct);

            if (healthy)
            {
                _output.WriteLine("Server is healthy.");
                return true;
            }

            _output.WriteLine("Server is sleeping — waking up...");
            return await GanApiClient.WaitForServerAsync(
                ApiUrl,
                maxRetries: 12,
                retryDelayMs: 5000,
                onStatusChange: msg => _output.WriteLine(msg),
                cancellationToken: ct);
        }
    }
}
