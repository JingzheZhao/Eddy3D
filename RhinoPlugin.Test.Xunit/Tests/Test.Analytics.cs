using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace RhinoPlugin.Test.Xunit.Tests
{
    [Trait("Category", "Analytics")]
    public class Test_Analytics
    {
        private readonly ITestOutputHelper _output;

        // Umami config (same values as Analytics.cs)
        private const string UmamiEndpoint = "https://cloud.umami.is/api/send";
        private const string WebsiteId = "aa96c306-4b0b-4736-832d-381f1c74e07a";
        private const string Salt = "Eddy3D_OutdoorPlus_Analytics_2026!";

        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        private readonly string OsLabel;

        public Test_Analytics(ITestOutputHelper output)
        {
            _output = output;
            OsLabel = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS"
                : RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows"
                : "Linux";
        }

        [RequiresExternalServiceFact]
        public async Task RunAllAnalyticsTests()
        {
            _output.WriteLine($"Eddy3D Analytics – {OsLabel} Integration Test");
            _output.WriteLine("=============================================");

            string deviceType = GetDeviceType();
            string screenSize = GetScreenSize();
            string userAgent = GetUserAgent(deviceType, $"test-{OsLabel}");
            string visitorId = GetUniqueId();

            _output.WriteLine("");
            _output.WriteLine($"Visitor ID: {visitorId}");
            _output.WriteLine($"Machine: {Environment.MachineName}");
            _output.WriteLine($"User: {Environment.UserName}");
            _output.WriteLine($"OS: {Environment.OSVersion}");
            _output.WriteLine($"Device type: {deviceType}");
            _output.WriteLine($"Screen size: {screenSize}");
            _output.WriteLine($"User-Agent: {userAgent}");
            _output.WriteLine("");

            // 1. Device type detection
            Assert.False(string.IsNullOrWhiteSpace(deviceType), "Device type should not be empty");
            _output.WriteLine($"[1/5] Device type: {deviceType}");

            // 2. Screen size detection (macOS usually succeeds, Windows may fallback to unknown in CI)
            Assert.False(string.IsNullOrWhiteSpace(screenSize), "Screen size should not be empty");
            _output.WriteLine($"[2/5] Screen size: {screenSize}");

            // 3. Network org lookup
            string networkOrg = null;
            string networkOrgName = null;
            try
            {
                networkOrg = await GetNetworkOrgAsync();
                networkOrgName = StripAsnPrefix(networkOrg);
                _output.WriteLine($"[3/5] Network org lookup: {networkOrgName ?? "(none)"}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[3/5] Network org lookup skipped or failed ({ex.Message})");
            }

            // 4. Identify request
            var identifyResult = await SendIdentifyAsync(
                visitorId, deviceType, screenSize, userAgent, networkOrg, networkOrgName);

            _output.WriteLine($"[4/5] Identify (session): {identifyResult.msg}");

            // If we hit a rate limit (429), we shouldn't fail the test suite, 
            // as this is an external API restriction beyond our control.
            if (identifyResult.statusCode == 429)
            {
                _output.WriteLine("⚠️ Rate limit (429) hit on Umami API. Skipping further analytics tests for this run.");
                return;
            }

            Assert.True(identifyResult.ok, "Identify request failed: " + identifyResult.msg);

            // 5. Named event
            var eventResult = await SendPayloadAsync(
                visitorId, url: "/startup",
                eventName: "/outdoor/software-launch",
                title: "/outdoor/software-launch",
                deviceType, screenSize, userAgent,
                networkOrg, networkOrgName,
                additionalData: new JObject { { "test_run", true }, { "os", OsLabel }, { "os_description", RuntimeInformation.OSDescription } });

            _output.WriteLine($"[5/5] Event /outdoor/software-launch: {eventResult.msg}");

            if (eventResult.statusCode == 429)
            {
                _output.WriteLine("⚠️ Rate limit (429) hit on Umami API for Event.");
                return;
            }

            Assert.True(eventResult.ok, "Event request failed: " + eventResult.msg);

            _output.WriteLine("");
            _output.WriteLine("→ Check https://cloud.umami.is for your events.");
        }

        // ── Identity (mirrors Identity.cs) ──────────────────────────────────
        private string GetUniqueId()
        {
            string rawId = Environment.MachineName + Environment.UserName;
            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawId + Salt));
            return BitConverter.ToString(hash).Replace("-", "").Substring(0, 12).ToLower();
        }

        // ── Cross-platform helpers (mirrors Analytics.cs) ───────────────────
        private string GetDeviceType()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    string model = RunShellCommand("sysctl", "-n hw.model");
                    if (!string.IsNullOrWhiteSpace(model) &&
                        model.StartsWith("MacBook", StringComparison.OrdinalIgnoreCase))
                        return "laptop";
                    return "desktop";
                }
                catch { return "desktop"; }
            }
            return "desktop"; // fallback
        }

        private string GetScreenSize()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    string output = RunShellCommand("system_profiler", "SPDisplaysDataType");
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        var match = Regex.Match(output, @"Resolution:\s*(\d+)\s*x\s*(\d+)");
                        if (match.Success)
                            return $"{match.Groups[1].Value}x{match.Groups[2].Value}";
                    }
                }
                catch { /* ignore */ }
            }
            return "unknown";
        }

        private string GetUserAgent(string deviceType, string eddyVersion)
        {
            string deviceToken = string.Equals(deviceType, "laptop", StringComparison.OrdinalIgnoreCase)
                ? "Laptop" : "Desktop";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string osVersion = Environment.OSVersion.Version.ToString();
                string arch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "ARM64" : "Intel";
                return $"Mozilla/5.0 (Macintosh; {arch} Mac OS X {osVersion}; {deviceToken}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Eddy3D/{eddyVersion}";
            }

            return $"Mozilla/5.0 (Windows NT 10.0; Win64; x64; {deviceToken}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Eddy3D/{eddyVersion}";
        }

        private string RunShellCommand(string command, string args)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            return output?.Trim();
        }

        // ── Identify request ────────────────────────────────────────────────
        private async Task<(bool ok, int statusCode, string msg)> SendIdentifyAsync(
            string visitorId, string deviceType, string screenSize,
            string userAgent, string networkOrg, string networkOrgName)
        {
            try
            {
                var identifyData = new JObject
                {
                    { "profile_rhino_version", "test" },
                    { "profile_eddy_version", $"test-{OsLabel}" },
                    { "profile_device_type", deviceType }
                };
                if (!string.IsNullOrWhiteSpace(networkOrg))
                    identifyData["profile_network_org"] = networkOrg;
                if (!string.IsNullOrWhiteSpace(networkOrgName))
                    identifyData["profile_network_org_name"] = networkOrgName;

                var payloadData = new JObject
                {
                    { "website", WebsiteId },
                    { "id", visitorId },
                    { "hostname", "eddy3d-plugin" },
                    { "language", "en-US" },
                    { "screen", screenSize },
                    { "url", "/startup" },
                    { "referrer", "https://eddy3d.com" },
                    { "title", "Test Identify" },
                    { "data", identifyData }
                };

                var payload = new JObject
                {
                    { "type", "identify" },
                    { "payload", payloadData }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, UmamiEndpoint);
                request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                request.Headers.UserAgent.ParseAdd(userAgent);

                var response = await Http.SendAsync(request);
                return response.IsSuccessStatusCode
                    ? (true, (int)response.StatusCode, $"HTTP {(int)response.StatusCode}")
                    : (false, (int)response.StatusCode, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            }
            catch (Exception ex) { return (false, 0, ex.Message); }
        }

        // ── Send event or page view ─────────────────────────────────────────
        private async Task<(bool ok, int statusCode, string msg)> SendPayloadAsync(
            string visitorId, string url, string eventName,
            string title, string deviceType, string screenSize,
            string userAgent, string networkOrg, string networkOrgName,
            JObject additionalData)
        {
            try
            {
                var dataObject = new JObject
                {
                    { "visitor_id", visitorId },
                    { "rhino_version", "test" },
                    { "eddy_version", $"test-{OsLabel}" },
                    { "device_type", deviceType }
                };
                if (!string.IsNullOrWhiteSpace(networkOrg))
                    dataObject["network_org"] = networkOrg;
                if (!string.IsNullOrWhiteSpace(networkOrgName))
                    dataObject["network_org_name"] = networkOrgName;
                if (additionalData != null)
                    foreach (var prop in additionalData.Properties())
                        dataObject[prop.Name] = prop.Value;

                var payloadData = new JObject
                {
                    { "website", WebsiteId },
                    { "id", visitorId },
                    { "hostname", "eddy3d-plugin" },
                    { "language", "en-US" },
                    { "screen", screenSize },
                    { "url", url },
                    { "referrer", "https://eddy3d.com" },
                    { "title", title },
                    { "data", dataObject }
                };

                if (!string.IsNullOrEmpty(eventName))
                    payloadData["name"] = eventName;

                var payload = new JObject
                {
                    { "type", "event" },
                    { "payload", payloadData }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, UmamiEndpoint);
                request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                request.Headers.UserAgent.ParseAdd(userAgent);

                var response = await Http.SendAsync(request);
                return response.IsSuccessStatusCode
                    ? (true, (int)response.StatusCode, $"HTTP {(int)response.StatusCode}")
                    : (false, (int)response.StatusCode, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            }
            catch (Exception ex) { return (false, 0, ex.Message); }
        }

        // ── Network org ─────────────────────────────────────────────────────
        private async Task<string> GetNetworkOrgAsync()
        {
            try
            {
                var response = await Http.GetStringAsync("https://ipinfo.io/json");
                var json = JObject.Parse(response);
                string org = json["org"]?.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(org)) return org;
            }
            catch { /* fall through */ }

            try
            {
                var response = await Http.GetStringAsync("http://ip-api.com/json/?fields=org,isp");
                var json = JObject.Parse(response);
                string org = json["org"]?.ToString().Trim() ?? "";
                string isp = json["isp"]?.ToString().Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(org) && !string.Equals(org, isp, StringComparison.OrdinalIgnoreCase))
                    return $"{org} ({isp})";
                return !string.IsNullOrWhiteSpace(org) ? org : isp;
            }
            catch { return null; }
        }

        private string StripAsnPrefix(string org)
        {
            if (string.IsNullOrWhiteSpace(org)) return null;
            if (!org.StartsWith("AS", StringComparison.OrdinalIgnoreCase)) return org;
            int firstSpace = org.IndexOf(' ');
            if (firstSpace < 0 || firstSpace + 1 >= org.Length) return org;
            return org.Substring(firstSpace + 1).Trim();
        }
    }
}
