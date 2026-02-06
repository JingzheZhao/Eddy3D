using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
            return await SendPayloadAsync(url, eventName, eventName, additionalData);
        }

        // Cache the network org so we don't hit the API for every event
        private static string _cachedNetworkOrg = null;
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
                
                // Fetch network org once per session (cached)
                if (!_networkOrgFetched)
                {
                    _cachedNetworkOrg = await GetNetworkOrgAsync();
                    _networkOrgFetched = true;
                }

                // Build identify data with all properties
                var identifyData = new JObject
                {
                    { "id", visitorId },
                    { "rhino_version", rhinoVersion },
                    { "plugin_version", GetPluginVersion() }
                };
                
                // Add network_org if available (shows university/company)
                if (!string.IsNullOrWhiteSpace(_cachedNetworkOrg))
                {
                    identifyData["network_org"] = _cachedNetworkOrg;
                }

                // 1. Send IDENTIFY request (Session Setup) - ONLY ONCE
                if (!_isIdentified)
                {
                    var identifyPayload = new JObject
                    {
                        { "type", "identify" },
                        { "payload", new JObject
                            {
                                { "website", Config.WebsiteId },
                                { "hostname", "eddy3d-plugin" },
                                { "language", "en-US" },
                                { "screen", "1920x1080" },
                                { "url", url },
                                { "referrer", "https://eddy3d.com" },
                                { "title", title },
                                { "name", eventName },
                                { "data", identifyData }
                            }
                        }
                    };
                    
                    var identifyRequest = new HttpRequestMessage(HttpMethod.Post, Config.Endpoint);
                    identifyRequest.Content = new StringContent(identifyPayload.ToString(), Encoding.UTF8, "application/json");
                    identifyRequest.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Eddy3D/1.0");
                    await HttpClient.SendAsync(identifyRequest);
                    _isIdentified = true;
                }

                // 2. Send DATA request (Event or PageView)
                var dataObject = new JObject();
                if (additionalData != null)
                {
                    foreach (var prop in additionalData.Properties()) dataObject[prop.Name] = prop.Value;
                }
                
                // Add visitor_id to data just in case
                dataObject["visitor_id"] = visitorId;

                // Prepare payload
                var payloadData = new JObject
                {
                    { "website", Config.WebsiteId },
                    { "hostname", "eddy3d-plugin" },
                    { "language", "en-US" },
                    { "screen", "1920x1080" },
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
                request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Eddy3D/1.0");

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
                string networkOrg = await GetNetworkOrgAsync();

                // Give Umami a moment to process the pageview before the event
                await Task.Delay(500);

                // 3. Track the Event
                var data = new JObject();
                if (!string.IsNullOrWhiteSpace(organization))
                {
                    data["organization_input"] = CleanOrganizationName(organization);
                }
                
                if (!string.IsNullOrWhiteSpace(networkOrg))
                {
                    data["network_org"] = networkOrg;
                }

                string result = await TrackEventAsync("/outdoor/software-launch", "/startup", data);
                
                if (callback != null) callback(result);
            });
        }

        /// <summary>
        /// Tracks which component was added to the canvas.
        /// Sent as a Page View ONLY to avoid cluttering the custom events timeline.
        /// </summary>
        public static void TrackComponentView(string componentName)
        {
            Task.Run(async () =>
            {
                await TrackPageViewAsync($"/gh/{componentName.ToLower()}", $"Component: {componentName}");
            });
        }

        /// <summary>
        /// Tracks a "Write Case" action.
        /// </summary>
        public static void TrackWriteCase()
        {
            TrackEvent("/outdoor/write-case", "/workflow/write");
        }

        /// <summary>
        /// Tracks a "Mesh Case" action (simulation preparation).
        /// </summary>
        /// <param name="mode">Execution mode (e.g., "BlueCFD", "Docker", "WSL").</param>
        public static void TrackMeshCase(string mode = "unknown")
        {
            TrackEvent("/outdoor/mesh-case", "/workflow/mesh", new JObject { { "mode", mode } });
        }

        /// <summary>
        /// Tracks a "Simulate Case" action.
        /// </summary>
        /// <param name="mode">Execution mode (e.g., "BlueCFD", "Docker", "WSL").</param>
        public static void TrackSimulateCase(string mode = "unknown")
        {
            TrackEvent("/outdoor/simulate-case", "/workflow/simulate", new JObject { { "mode", mode } });
        }

        /// <summary>
        /// Tracks a "Probe Case" action.
        /// </summary>
        /// <param name="probeCount">Number of probe points.</param>
        public static void TrackProbeCase(int probeCount = 0)
        {
            TrackEvent("/outdoor/probe-case", "/workflow/probe", new JObject { { "probe_count", probeCount } });
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
        /// Safely retrieves the Network Organization (ISP) from external API.
        /// Returns null on any failure to ensure no blocking/crashing.
        /// </summary>
        private static async Task<string> GetNetworkOrgAsync()
        {
            try
            {
                // Use ip-api.com (Free, non-commercial use allowed, very reliable)
                // fetch 'org' and 'isp' fields
                var response = await HttpClient.GetStringAsync("http://ip-api.com/json/?fields=org,isp");
                var json = JObject.Parse(response);
                
                string org = json["org"] != null ? json["org"].ToString() : "";
                string isp = json["isp"] != null ? json["isp"].ToString() : "";

                // Return the most descriptive one, or combine
                if (!string.IsNullOrWhiteSpace(org) && org != isp)
                    return $"{org} ({isp})";
                
                return !string.IsNullOrWhiteSpace(org) ? org : isp;
            }
            catch
            {
                return null; // Fail silently, analytics shouldn't break app
            }
        }
    }
}
