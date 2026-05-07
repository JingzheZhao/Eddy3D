using System;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EddyLib;
using Newtonsoft.Json.Linq;

namespace Eddy.Analytics
{
    /// <summary>
    /// Privacy-first analytics tracking using Umami.
    /// Sends anonymous usage data to help with funding and development decisions.
    /// </summary>
    public static class Analytics
    {
        // Umami Cloud configuration
        private const string UmamiEndpoint = "https://cloud.umami.is/api/send";
        private const string WebsiteId = "aa96c306-4b0b-4736-832d-381f1c74e07a";

        // Simple HttpClient - .NET Framework 4.8 compatible
        private static readonly HttpClient HttpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        /// <summary>
        /// Configuration for the analytics endpoint.
        /// </summary>
        public static class Config
        {
            /// <summary>Gets or sets the Umami API endpoint URL.</summary>
            public static string Endpoint { get; set; } = UmamiEndpoint;

            /// <summary>Gets or sets the Umami website ID.</summary>
            public static string WebsiteId { get; set; } = Analytics.WebsiteId;

            /// <summary>Gets or sets whether tracking is enabled.</summary>
            public static bool Enabled { get; set; } = true;

            /// <summary>
            /// Emergency quota guard. When enabled, only startup tracking is sent.
            /// Defaults to false because the reduced analytics schema is already low volume.
            /// </summary>
            public static bool LimitTrackingToStartupOnly { get; set; } = false;
        }

        /// <summary>
        /// Tracks an event in Umami analytics.
        /// </summary>
        /// <param name="eventName">Name of the event (e.g., "software_launch", "simulation_run").</param>
        /// <param name="url">Virtual URL path for the event (e.g., "/startup", "/simulation/run").</param>
        /// <param name="additionalData">Optional additional data to include in the event.</param>
        /// <param name="callback">Optional callback with the result message.</param>
        public static void TrackEvent(
            string eventName,
            string url = "/",
            JObject additionalData = null,
            Action<string> callback = null)
        {
            if (!Config.Enabled)
            {
                if (callback != null) callback("Tracking disabled");
                return;
            }

            // High volume guard: skip all non-startup events if the flag is set.
            if (Config.LimitTrackingToStartupOnly && url != "/startup")
            {
                if (callback != null) callback("Event skipped (LimitTrackingToStartupOnly is true)");
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    string result = await TrackEventAsync(eventName, url, additionalData);
                    if (callback != null) callback(result);
                }
                catch (Exception ex)
                {
                    if (callback != null) callback($"Error: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Tracks a page view in Umami analytics asynchronously.
        /// This is required to increment the 'Visitors' count.
        /// </summary>
        public static async Task<string> TrackPageViewAsync(string url, string title)
        {
            if (Config.LimitTrackingToStartupOnly && url != "/startup") return "Page View skipped";
            return await SendPayloadAsync(url, null, title, null);
        }

        /// <summary>
        /// Tracks an event in Umami analytics asynchronously.
        /// </summary>
        /// <param name="eventName">Name of the event (used as Title).</param>
        /// <param name="url">Virtual URL path for the event.</param>
        /// <param name="additionalData">Optional additional data to include.</param>
        /// <returns>Result message indicating success or failure.</returns>
        public static async Task<string> TrackEventAsync(
            string eventName,
            string url = "/",
            JObject additionalData = null)
        {
            if (Config.LimitTrackingToStartupOnly && url != "/startup") return "Event skipped";
            return await SendPayloadAsync(url, eventName, eventName, additionalData);
        }

        // Cache the network org so we don't hit the API for every event
        private static string _cachedNetworkOrg = null;
        private static string _cachedNetworkOrgName = null;
        private static bool _networkOrgFetched = false;
        private static bool _isIdentified = false;

        /// <summary>
        /// Shared logic to send payloads to Umami.
        /// If eventName is null, it is treated as a Page View.
        /// </summary>
        private static async Task<string> SendPayloadAsync(
            string url,
            string eventName,
            string title,
            JObject additionalData)
        {
            if (!Config.Enabled) return "Tracking disabled";

            try
            {
                string visitorId = Identity.GetUniqueId();
                string rhinoVersion = GetRhinoVersion();
                string eddyVersion = GetEddyVersion();
                string deviceType = GetDeviceType();
                string screenSize = GetScreenSize();

                // Fetch network org once per session (cached)
                await EnsureNetworkOrgCachedAsync();

                // Build identify (session profile) data.
                // Keep profile keys distinct from event property keys so Umami
                // can expose event properties cleanly for filtering.
                var identifyData = new JObject
                {
                    { "profile_rhino_version", rhinoVersion },
                    { "profile_eddy_version", eddyVersion },
                    { "profile_device_type", deviceType }
                };

                // Add network_org if available (shows university/company)
                if (!string.IsNullOrWhiteSpace(_cachedNetworkOrg))
                {
                    identifyData["profile_network_org"] = _cachedNetworkOrg;
                }
                if (!string.IsNullOrWhiteSpace(_cachedNetworkOrgName))
                {
                    identifyData["profile_network_org_name"] = _cachedNetworkOrgName;
                }

                // 1. Send IDENTIFY request (Session Setup) - ONLY ONCE
                if (!_isIdentified)
                {
                    var identifyPayloadData = new JObject
                    {
                        { "website", Config.WebsiteId },
                        { "id", visitorId },
                        { "hostname", "eddy3d-plugin" },
                        { "language", "en-US" },
                        { "screen", screenSize },
                        { "url", url },
                        { "referrer", "https://eddy3d.com" },
                        { "title", title },
                        { "data", identifyData }
                    };

                    // For identify requests, only include "name" when a valid value exists.
                    // Sending explicit null can fail Umami schema validation.
                    if (!string.IsNullOrWhiteSpace(eventName))
                    {
                        identifyPayloadData["name"] = eventName;
                    }

                    var identifyPayload = new JObject
                    {
                        { "type", "identify" },
                        { "payload", identifyPayloadData }
                    };

                    var identifyRequest = new HttpRequestMessage(HttpMethod.Post, Config.Endpoint);
                    identifyRequest.Content = new StringContent(identifyPayload.ToString(), Encoding.UTF8, "application/json");
                    identifyRequest.Headers.UserAgent.ParseAdd(GetUserAgent(deviceType, eddyVersion));
                    var identifyResponse = await HttpClient.SendAsync(identifyRequest);

                    // Only mark identify as complete when Umami accepts it.
                    if (identifyResponse.IsSuccessStatusCode)
                    {
                        _isIdentified = true;
                    }
                }

                // 2. Send DATA request (Event or PageView)
                var dataObject = new JObject
                {
                    { "visitor_id", visitorId },
                    { "rhino_version", rhinoVersion },
                    { "eddy_version", eddyVersion },
                    { "device_type", deviceType }
                };

                if (!string.IsNullOrWhiteSpace(_cachedNetworkOrg))
                {
                    dataObject["network_org"] = _cachedNetworkOrg;
                }
                if (!string.IsNullOrWhiteSpace(_cachedNetworkOrgName))
                {
                    dataObject["network_org_name"] = _cachedNetworkOrgName;
                }
                if (additionalData != null)
                {
                    foreach (var prop in additionalData.Properties()) dataObject[prop.Name] = prop.Value;
                }

                // Prepare payload
                var payloadData = new JObject
                {
                    { "website", Config.WebsiteId },
                    { "id", visitorId },
                    { "hostname", "eddy3d-plugin" },
                    { "language", "en-US" },
                    { "screen", screenSize },
                    { "url", url },
                    { "referrer", "https://eddy3d.com" },
                    { "title", title },
                    { "data", dataObject }
                };

                // IF eventName is present -> It's an Event
                // IF eventName is null    -> It's a Page View
                if (!string.IsNullOrEmpty(eventName))
                {
                    payloadData["name"] = eventName;
                }

                var payload = new JObject
                {
                    { "type", "event" },
                    { "payload", payloadData }
                };

                // Send request
                var request = new HttpRequestMessage(HttpMethod.Post, Config.Endpoint);
                request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                request.Headers.UserAgent.ParseAdd(GetUserAgent(deviceType, eddyVersion));

                var response = await HttpClient.SendAsync(request);

                return response.IsSuccessStatusCode
                    ? $"Success: {(eventName ?? "Page View")} tracked"
                    : $"Failed: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Tracks a software launch event. 
        /// Sends both a Page View (/startup) and an identified Event.
        /// Includes Network Organization (ISP) lookup for funding analytics.
        /// </summary>
        public static void TrackLaunch(string organization = null, Action<string> callback = null)
        {
            Task.Run(async () =>
            {
                // 1. Track the Page View (User is visiting the 'Startup' page)
                // This increments the VISITOR count
                await TrackPageViewAsync("/startup", "App Startup");

                // 2. Resolve Network Organization (e.g. "Georgia Tech", "T-Mobile")
                // This helps identify institutional usage for funding.
                await EnsureNetworkOrgCachedAsync();

                // Give Umami a moment to process the pageview before the event
                await Task.Delay(500);

                // 3. Track the Event
                var data = new JObject();
                if (!string.IsNullOrWhiteSpace(organization))
                {
                    data["organization_input"] = CleanOrganizationName(organization);
                }

                string result = await TrackEventAsync("/outdoor/software-launch", "/startup", data);

                if (callback != null) callback(result);
            });
        }

        /// <summary>
        /// Tracks a simulation run for the major product workflows.
        /// This is the primary non-launch analytics event and is intentionally low-volume.
        /// </summary>
        public static void TrackSimulationRun(
            string workflow,
            string engine,
            JObject additionalData = null)
        {
            string normalizedWorkflow = NormalizeWorkflow(workflow);
            var data = new JObject
            {
                { "workflow", normalizedWorkflow },
                { "engine", NormalizeEngine(engine) }
            };

            if (additionalData != null)
            {
                foreach (var prop in additionalData.Properties())
                {
                    data[prop.Name] = prop.Value;
                }
            }

            TrackEvent("simulation_run", "/run/" + normalizedWorkflow, data);
        }

        /// <summary>
        /// Maps the current CFD engine enum to a stable analytics value.
        /// </summary>
        public static string GetAnalyticsEngine(SimEngine engine)
        {
            switch (engine)
            {
                case SimEngine.Docker:
                    return "docker";
                case SimEngine.BlueCFD:
                    return "bluecfd";
                default:
                    return "unknown";
            }
        }

        /// <summary>
        /// Attempts to infer the engine from a path or label.
        /// Useful for workflows that consume external result files rather than a typed engine enum.
        /// </summary>
        public static string InferEngineFromPath(string pathOrLabel, string fallbackEngine = "unknown")
        {
            if (string.IsNullOrWhiteSpace(pathOrLabel))
            {
                return NormalizeEngine(fallbackEngine);
            }

            string value = pathOrLabel.Trim().ToLowerInvariant();
            if (value.Contains("fluidx3d") || value.Contains("fluid-x3d"))
            {
                return "fluidx3d";
            }

            if (Regex.IsMatch(value, @"(^|[^a-z])gan(s)?([^a-z]|$)"))
            {
                return "gan";
            }

            if (value.Contains("bluecfd"))
            {
                return "bluecfd";
            }

            if (value.Contains("docker"))
            {
                return "docker";
            }

            if (Regex.IsMatch(value, @"(^|[^a-z])wsl([^a-z]|$)"))
            {
                return "wsl";
            }

            return NormalizeEngine(fallbackEngine);
        }

        /// <summary>
        /// Cleans an organization name for analytics.
        /// </summary>
        private static string CleanOrganizationName(string org)
        {
            // Remove common noise and standardize
            return org
                .Replace(",", "")
                .Replace(".", "")
                .Trim();
        }

        /// <summary>
        /// Converts component names (including acronyms) to analytics-friendly kebab-case.
        /// Example: "MRTSimulationSettings" -> "mrt-simulation-settings".
        /// </summary>
        private static string ToAnalyticsComponentSlug(string componentName)
        {
            if (string.IsNullOrWhiteSpace(componentName))
            {
                return "unknown-component";
            }

            var input = componentName.Trim();
            var sb = new StringBuilder(input.Length + 8);
            bool justWroteDash = false;

            for (int i = 0; i < input.Length; i++)
            {
                char current = input[i];

                if (!char.IsLetterOrDigit(current))
                {
                    if (sb.Length > 0 && !justWroteDash)
                    {
                        sb.Append('-');
                        justWroteDash = true;
                    }
                    continue;
                }

                bool shouldInsertDash = false;
                if (i > 0 && char.IsUpper(current))
                {
                    char previous = input[i - 1];
                    bool previousIsAlphaNumeric = char.IsLetterOrDigit(previous);
                    bool previousIsLowerOrDigit = char.IsLower(previous) || char.IsDigit(previous);
                    bool previousIsUpper = char.IsUpper(previous);
                    bool currentStartsWordAfterAcronym = false;

                    if (previousIsUpper && i + 1 < input.Length)
                    {
                        char next = input[i + 1];
                        currentStartsWordAfterAcronym = char.IsLower(next);
                    }

                    shouldInsertDash = previousIsAlphaNumeric && (previousIsLowerOrDigit || currentStartsWordAfterAcronym);
                }

                if (shouldInsertDash && sb.Length > 0 && !justWroteDash)
                {
                    sb.Append('-');
                }

                sb.Append(char.ToLowerInvariant(current));
                justWroteDash = false;
            }

            string slug = sb.ToString().Trim('-');
            return string.IsNullOrEmpty(slug) ? "unknown-component" : slug;
        }

        private static string NormalizeWorkflow(string workflow)
        {
            if (string.IsNullOrWhiteSpace(workflow))
            {
                return "unknown";
            }

            switch (workflow.Trim().ToLowerInvariant())
            {
                case "outdoor":
                    return "outdoor";
                case "indoor":
                    return "indoor";
                case "mrt":
                case "legacymrt":
                    return "mrt";
                case "probe":
                case "probing":
                    return "probing";
                default:
                    return "unknown";
            }
        }

        private static string NormalizeEngine(string engine)
        {
            if (string.IsNullOrWhiteSpace(engine))
            {
                return "unknown";
            }

            string value = engine.Trim().ToLowerInvariant()
                .Replace("_", "")
                .Replace("-", "")
                .Replace(" ", "");

            switch (value)
            {
                case "docker":
                case "dockerdesktop":
                    return "docker";
                case "bluecfd":
                case "bluecfdcore":
                    return "bluecfd";
                case "wsl":
                    return "wsl";
                case "fluidx3d":
                    return "fluidx3d";
                case "gan":
                case "gans":
                    return "gan";
                case "native":
                case "radiance":
                case "energyplus":
                case "radianceenergyplus":
                    return "native";
                default:
                    return "unknown";
            }
        }

        /// <summary>
        /// Gets the Rhino version if available.
        /// </summary>
        private static string GetRhinoVersion()
        {
            try
            {
                return Rhino.RhinoApp.Version.ToString();
            }
            catch
            {
                return "unknown";
            }
        }

        /// <summary>
        /// Gets the Eddy3D plugin version.
        /// </summary>
        private static string GetPluginVersion()
        {
            try
            {
                var assembly = typeof(Analytics).Assembly;
                var name = assembly.GetName().Name;
                var version = assembly.GetName().Version != null ? assembly.GetName().Version.ToString() : "unknown";
                return $"{name} v{version}";
            }
            catch
            {
                return "unknown";
            }
        }

        /// <summary>
        /// Gets the Eddy3D version from the codebase constant.
        /// </summary>
        private static string GetEddyVersion()
        {
            try
            {
                return EddyVersion.ProductVersion;
            }
            catch
            {
                return GetPluginVersion();
            }
        }

        /// <summary>
        /// Detects whether this machine is a desktop or laptop.
        /// </summary>
        private static string GetDeviceType()
        {
            // macOS: check hardware model via sysctl
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    string model = RunShellCommand("sysctl", "-n", "hw.model");
                    if (!string.IsNullOrWhiteSpace(model) &&
                        model.StartsWith("MacBook", StringComparison.OrdinalIgnoreCase))
                        return "laptop";
                    return "desktop";
                }
                catch
                {
                    return "desktop";
                }
            }

            // Windows: WMI query
            try
            {
                var managementType = Type.GetType("System.Management.ManagementObjectSearcher, System.Management");
                if (managementType != null)
                {
                    using (var searcher = (IDisposable)Activator.CreateInstance(
                        managementType, "SELECT PCSystemType FROM Win32_ComputerSystem"))
                    {
                        var getMethod = managementType.GetMethod("Get", Type.EmptyTypes);
                        var results = (System.Collections.IEnumerable)getMethod.Invoke(searcher, null);
                        System.Reflection.PropertyInfo indexer = null;
                        foreach (var item in results)
                        {
                            if (indexer == null) indexer = item.GetType().GetProperty("Item", new[] { typeof(string) });
                            var val = indexer?.GetValue(item, new object[] { "PCSystemType" });
                            if (val == null) continue;
                            int type = Convert.ToInt32(val);
                            if (type == 2) return "laptop";
                            if (type == 1 || type == 3) return "desktop";
                        }
                    }
                }
            }
            catch
            {
                // fall through to other heuristics
            }

            // Windows fallback: battery status via WinForms
            try
            {
                var siType = Type.GetType("System.Windows.Forms.SystemInformation, System.Windows.Forms");
                if (siType != null)
                {
                    var powerProp = siType.GetProperty("PowerStatus");
                    var powerStatus = powerProp?.GetValue(null);
                    if (powerStatus != null)
                    {
                        var chargeProp = powerStatus.GetType().GetProperty("BatteryChargeStatus");
                        var chargeStatus = chargeProp?.GetValue(powerStatus);
                        // BatteryChargeStatus.NoSystemBattery == 128
                        if (chargeStatus != null && Convert.ToInt32(chargeStatus) == 128)
                            return "desktop";
                        return "laptop";
                    }
                }
            }
            catch
            {
                // ignore
            }

            return "desktop";
        }

        /// <summary>
        /// Reads the current virtual screen bounds for analytics context.
        /// </summary>
        private static string GetScreenSize()
        {
            // macOS: parse system_profiler for display resolution
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    string output = RunShellCommand("system_profiler", "SPDisplaysDataType");
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        // Matches lines like "Resolution: 3024 x 1964 Retina" or "3456 x 2234"
                        var match = Regex.Match(output, @"Resolution:\s*(\d+)\s*x\s*(\d+)");
                        if (match.Success)
                            return $"{match.Groups[1].Value}x{match.Groups[2].Value}";
                    }
                }
                catch
                {
                    // ignore
                }

                return "unknown";
            }

            // Windows: WinForms virtual screen
            try
            {
                var siType = Type.GetType("System.Windows.Forms.SystemInformation, System.Windows.Forms");
                if (siType != null)
                {
                    var vsProp = siType.GetProperty("VirtualScreen");
                    var bounds = vsProp?.GetValue(null);
                    if (bounds is Rectangle rect && rect.Width > 0 && rect.Height > 0)
                    {
                        return $"{rect.Width}x{rect.Height}";
                    }
                }
            }
            catch
            {
                // ignore and use fallback
            }

            return "unknown";
        }

        /// <summary>
        /// Builds the analytics user agent with explicit desktop/laptop context.
        /// </summary>
        private static string GetUserAgent(string deviceType, string eddyVersion)
        {
            string deviceToken = string.Equals(deviceType, "laptop", StringComparison.OrdinalIgnoreCase)
                ? "Laptop"
                : "Desktop";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string osVersion = Environment.OSVersion.Version.ToString();
                string arch = RuntimeInformation.OSArchitecture == Architecture.Arm64
                    ? "ARM64" : "Intel";
                return $"Mozilla/5.0 (Macintosh; {arch} Mac OS X {osVersion}; {deviceToken}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Eddy3D/{eddyVersion}";
            }

            return $"Mozilla/5.0 (Windows NT 10.0; Win64; x64; {deviceToken}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Eddy3D/{eddyVersion}";
        }

        /// <summary>
        /// Runs a shell command and returns trimmed stdout. Timeout: 3 seconds.
        /// </summary>
        private static string RunShellCommand(string command, params string[] arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in arguments)
            {
                psi.ArgumentList.Add(arg);
            }

            using var process = new Process
            {
                StartInfo = psi
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            return output?.Trim();
        }

        /// <summary>
        /// Ensures network organization data is fetched once and cached for this session.
        /// </summary>
        private static async Task EnsureNetworkOrgCachedAsync()
        {
            if (_networkOrgFetched) return;

            _cachedNetworkOrg = await GetNetworkOrgAsync();
            _cachedNetworkOrgName = StripAsnPrefix(_cachedNetworkOrg);
            _networkOrgFetched = true;
        }

        /// <summary>
        /// Safely retrieves the Network Organization (ISP) from external API.
        /// Returns null on any failure to ensure no blocking/crashing.
        /// </summary>
        private static async Task<string> GetNetworkOrgAsync()
        {
            try
            {
                // Primary source: ipinfo.io (Windows-friendly endpoint first)
                string[] ipInfoEndpoints =
                {
                    "http://ipinfo.io/json",
                    "https://ipinfo.io/json"
                };

                foreach (string endpoint in ipInfoEndpoints)
                {
                    try
                    {
                        var response = await HttpClient.GetStringAsync(endpoint);
                        var json = JObject.Parse(response);
                        string org = json["org"] != null ? json["org"].ToString().Trim() : null;
                        if (!string.IsNullOrWhiteSpace(org))
                        {
                            return org;
                        }
                    }
                    catch
                    {
                        // try next endpoint
                    }
                }
            }
            catch
            {
                // fall back to secondary provider
            }

            try
            {
                // Secondary fallback (org + isp) if ipinfo is unavailable.
                var response = await HttpClient.GetStringAsync("http://ip-api.com/json/?fields=org,isp");
                var json = JObject.Parse(response);

                string org = json["org"] != null ? json["org"].ToString().Trim() : "";
                string isp = json["isp"] != null ? json["isp"].ToString().Trim() : "";

                if (!string.IsNullOrWhiteSpace(org) && !string.Equals(org, isp, StringComparison.OrdinalIgnoreCase))
                {
                    return $"{org} ({isp})";
                }

                return !string.IsNullOrWhiteSpace(org) ? org : isp;
            }
            catch
            {
                return null; // Fail silently, analytics shouldn't break app
            }
        }

        /// <summary>
        /// Converts "AS21928 T-Mobile USA, Inc." to "T-Mobile USA, Inc." for cleaner grouping.
        /// </summary>
        private static string StripAsnPrefix(string org)
        {
            if (string.IsNullOrWhiteSpace(org)) return null;
            if (!org.StartsWith("AS", StringComparison.OrdinalIgnoreCase)) return org;

            int firstSpace = org.IndexOf(' ');
            if (firstSpace < 0 || firstSpace + 1 >= org.Length) return org;
            return org.Substring(firstSpace + 1).Trim();
        }
    }
}
