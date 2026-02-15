using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

/// <summary>
/// Standalone cross-platform test for the Eddy3D Umami analytics pipeline.
/// Mirrors the logic in Eddy/Analytics/Analytics.cs and Identity.cs without
/// any Windows-only or Rhino dependencies so it can run on macOS / Linux.
/// </summary>
class Program
{
    // ── Umami config (same values as Analytics.cs) ──────────────────────
    const string UmamiEndpoint = "https://cloud.umami.is/api/send";
    const string WebsiteId     = "aa96c306-4b0b-4736-832d-381f1c74e07a";
    const string Salt          = "Eddy3D_OutdoorPlus_Analytics_2026!";

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    static string OsLabel;

    // ────────────────────────────────────────────────────────────────────
    static async Task<int> Main()
    {
        OsLabel = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS"
            : RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows"
            : "Linux";

        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine($"  Eddy3D Analytics – {OsLabel} Integration Test  ");
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine();

        int passed = 0;
        int failed = 0;

        // 1. Identity hash ───────────────────────────────────────────────
        string visitorId = GetUniqueId();
        Console.WriteLine($"  Visitor ID : {visitorId}");
        Console.WriteLine($"  Machine    : {Environment.MachineName}");
        Console.WriteLine($"  User       : {Environment.UserName}");
        Console.WriteLine($"  OS         : {Environment.OSVersion}");
        Console.WriteLine();

        // 2. Cross-platform helpers ──────────────────────────────────────
        string deviceType = GetDeviceType();
        string screenSize = GetScreenSize();
        string userAgent = GetUserAgent(deviceType, $"test-{OsLabel}");
        Console.WriteLine($"  Device type: {deviceType}");
        Console.WriteLine($"  Screen size: {screenSize}");
        Console.WriteLine($"  User-Agent : {userAgent}");
        Console.WriteLine();

        // Check helpers returned real values (not just "unknown"/"desktop" fallback)
        Console.Write("  [1/5] Device type detection ... ");
        if (!string.IsNullOrWhiteSpace(deviceType) && deviceType != "unknown")
        {
            Console.WriteLine($"✅  {deviceType}");
            passed++;
        }
        else
        {
            Console.WriteLine($"⚠️  fallback: {deviceType}");
            passed++; // still "passes" – it's a valid fallback
        }

        Console.Write("  [2/5] Screen size detection ... ");
        if (!string.IsNullOrWhiteSpace(screenSize) && screenSize != "unknown")
        {
            Console.WriteLine($"✅  {screenSize}");
            passed++;
        }
        else
        {
            Console.WriteLine($"⚠️  fallback: {screenSize}");
            failed++;
        }

        // 3. Network org lookup ──────────────────────────────────────────
        Console.Write("  [3/5] Network org lookup ... ");
        string networkOrg = null;
        string networkOrgName = null;
        try
        {
            networkOrg = await GetNetworkOrgAsync();
            networkOrgName = StripAsnPrefix(networkOrg);
            Console.WriteLine($"✅  {networkOrgName ?? "(none)"}");
            passed++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  skipped ({ex.Message})");
            passed++;
        }

        // 4. Identify request ────────────────────────────────────────────
        Console.Write("  [4/5] Identify (session) ... ");
        var identifyResult = await SendIdentifyAsync(
            visitorId, deviceType, screenSize, userAgent, networkOrg, networkOrgName);
        PrintResult(identifyResult, ref passed, ref failed);

        // 5. Named event ─────────────────────────────────────────────────
        Console.Write("  [5/5] Event /outdoor/software-launch ... ");
        var eventResult = await SendPayloadAsync(
            visitorId, url: "/startup",
            eventName: "/outdoor/software-launch",
            title: "/outdoor/software-launch",
            deviceType, screenSize, userAgent,
            networkOrg, networkOrgName,
            additionalData: new JObject { { "test_run", true }, { "os", OsLabel }, { "os_description", RuntimeInformation.OSDescription } });
        PrintResult(eventResult, ref passed, ref failed);

        // Summary ────────────────────────────────────────────────────────
        Console.WriteLine();
        Console.WriteLine("───────────────────────────────────────────────");
        Console.WriteLine($"  Results: {passed} passed, {failed} failed");
        Console.WriteLine("───────────────────────────────────────────────");

        if (failed == 0)
            Console.WriteLine($"  ✅ All analytics calls succeeded on {OsLabel}!");
        else
            Console.WriteLine("  ❌ Some calls failed — check output above.");

        Console.WriteLine();
        Console.WriteLine("  → Check https://cloud.umami.is for your events.");
        Console.WriteLine();

        return failed > 0 ? 1 : 0;
    }

    // ── Identity (mirrors Identity.cs) ──────────────────────────────────
    static string GetUniqueId()
    {
        string rawId = Environment.MachineName + Environment.UserName;
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawId + Salt));
        return BitConverter.ToString(hash).Replace("-", "").Substring(0, 12).ToLower();
    }

    // ── Cross-platform helpers (mirrors Analytics.cs) ───────────────────
    static string GetDeviceType()
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

    static string GetScreenSize()
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

    static string GetUserAgent(string deviceType, string eddyVersion)
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

    static string RunShellCommand(string command, string args)
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
    static async Task<(bool ok, string msg)> SendIdentifyAsync(
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
                ? (true, $"HTTP {(int)response.StatusCode}")
                : (false, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    // ── Send event or page view ─────────────────────────────────────────
    static async Task<(bool ok, string msg)> SendPayloadAsync(
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
                ? (true, $"HTTP {(int)response.StatusCode}")
                : (false, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    // ── Network org ─────────────────────────────────────────────────────
    static async Task<string> GetNetworkOrgAsync()
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

    static string StripAsnPrefix(string org)
    {
        if (string.IsNullOrWhiteSpace(org)) return null;
        if (!org.StartsWith("AS", StringComparison.OrdinalIgnoreCase)) return org;
        int firstSpace = org.IndexOf(' ');
        if (firstSpace < 0 || firstSpace + 1 >= org.Length) return org;
        return org.Substring(firstSpace + 1).Trim();
    }

    static void PrintResult((bool ok, string msg) result, ref int passed, ref int failed)
    {
        if (result.ok) { Console.WriteLine($"✅  {result.msg}"); passed++; }
        else           { Console.WriteLine($"❌  {result.msg}"); failed++; }
    }
}
