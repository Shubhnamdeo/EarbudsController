using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace EarbudsController
{
    public class DeviceProfile
    {
        public string Brand { get; set; }
        public string Model { get; set; }
        public bool SupportsAnc { get; set; }
        public List<string> SupportedModes { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }

        public DeviceProfile()
        {
            SupportedModes = new List<string>();
        }
    }

    public class GeminiService
    {
        private readonly string _configFile;
        private readonly string _cacheFile;
        private readonly JavaScriptSerializer _serializer;
        private string _apiKey;
        private bool _aiEnabled = true;

        public GeminiService()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            _configFile = Path.Combine(appDir, "gemini_config.json");
            _cacheFile = Path.Combine(appDir, "device_cache.json");
            _serializer = new JavaScriptSerializer();
            LoadConfig();
        }

        public bool AiEnabled
        {
            get { return _aiEnabled; }
            set
            {
                _aiEnabled = value;
                SaveConfig();
            }
        }

        public string ApiKey
        {
            get { return _apiKey; }
            set
            {
                _apiKey = value;
                SaveConfig();
            }
        }

        public bool HasApiKey
        {
            get { return !string.IsNullOrEmpty(_apiKey); }
        }

        public void ClearCache()
        {
            try
            {
                if (File.Exists(_cacheFile))
                {
                    File.Delete(_cacheFile);
                }
            }
            catch { }
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configFile))
                {
                    string json = File.ReadAllText(_configFile);
                    var dict = _serializer.Deserialize<Dictionary<string, object>>(json);
                    if (dict != null)
                    {
                        if (dict.ContainsKey("apiKey")) _apiKey = dict["apiKey"] as string;
                        if (dict.ContainsKey("aiEnabled")) _aiEnabled = Convert.ToBoolean(dict["aiEnabled"]);
                    }
                }
            }
            catch { }
        }

        private void SaveConfig()
        {
            try
            {
                var dict = new Dictionary<string, object>
                {
                    { "apiKey", _apiKey ?? "" },
                    { "aiEnabled", _aiEnabled }
                };
                File.WriteAllText(_configFile, _serializer.Serialize(dict));
            }
            catch { }
        }

        private Dictionary<string, DeviceProfile> LoadCache()
        {
            try
            {
                if (File.Exists(_cacheFile))
                {
                    string json = File.ReadAllText(_cacheFile);
                    var raw = _serializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json);
                    if (raw != null)
                    {
                        var res = new Dictionary<string, DeviceProfile>();
                        foreach (var kvp in raw)
                        {
                            var p = new DeviceProfile();
                            if (kvp.Value.ContainsKey("brand")) p.Brand = kvp.Value["brand"] as string;
                            if (kvp.Value.ContainsKey("model")) p.Model = kvp.Value["model"] as string;
                            if (kvp.Value.ContainsKey("supportsAnc")) p.SupportsAnc = Convert.ToBoolean(kvp.Value["supportsAnc"]);
                            if (kvp.Value.ContainsKey("description")) p.Description = kvp.Value["description"] as string;
                            if (kvp.Value.ContainsKey("source")) p.Source = kvp.Value["source"] as string;
                            if (kvp.Value.ContainsKey("supportedModes"))
                            {
                                var modes = kvp.Value["supportedModes"] as System.Collections.ArrayList;
                                if (modes != null)
                                {
                                    foreach (var m in modes) p.SupportedModes.Add(m.ToString());
                                }
                            }
                            res[kvp.Key] = p;
                        }
                        return res;
                    }
                }
            }
            catch { }
            return new Dictionary<string, DeviceProfile>();
        }

        private void SaveCache(string key, DeviceProfile profile)
        {
            try
            {
                var cache = LoadCache();
                cache[key] = profile;
                File.WriteAllText(_cacheFile, _serializer.Serialize(cache));
            }
            catch { }
        }

        public DeviceProfile IdentifyDevice(string deviceName, bool isBluetooth)
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                return new DeviceProfile
                {
                    Brand = "Unknown",
                    Model = "Audio Output",
                    SupportsAnc = false,
                    Description = "Standard Windows audio output",
                    Source = "System Default"
                };
            }

            // If user turned AI off, run instant local detection without touching the web
            if (!_aiEnabled)
            {
                var local = LocalHeuristicIdentify(deviceName, isBluetooth);
                local.Source = "Local Mode (AI Disabled)";
                return local;
            }

            // 1. Check local cache
            var cache = LoadCache();
            if (cache.ContainsKey(deviceName))
            {
                var cached = cache[deviceName];
                cached.Source = "Local Cache";
                return cached;
            }

            // 2. Try Gemini API if key is available
            if (HasApiKey)
            {
                try
                {
                    var geminiResult = QueryGemini(deviceName, isBluetooth);
                    if (geminiResult != null)
                    {
                        geminiResult.Source = "Gemini AI";
                        SaveCache(deviceName, geminiResult);
                        return geminiResult;
                    }
                }
                catch { }
            }

            // 3. Built-in Local Heuristic Fallback
            var heuristic = LocalHeuristicIdentify(deviceName, isBluetooth);
            heuristic.Source = HasApiKey ? "Heuristic (Gemini Unavailable)" : "Local Detection (Add Gemini Key for AI)";
            SaveCache(deviceName, heuristic);
            return heuristic;
        }

        private DeviceProfile QueryGemini(string deviceName, bool isBluetooth)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(8);
                client.DefaultRequestHeaders.TryAddWithoutValidation("x-goog-api-key", _apiKey);

                string prompt = string.Format(
                    "You are a hardware specialist. Analyze this audio device name reported by Windows: \"{0}\" (Bluetooth: {1}). " +
                    "Determine if this is a known earbud/headphone model, its brand, whether it has hardware Active Noise Cancellation (ANC), " +
                    "low-latency game mode, and sound modes. Respond ONLY in valid raw JSON with this exact schema (no markdown, no backticks):\n" +
                    "{{\"brand\":\"...\",\"model\":\"...\",\"supportsAnc\":true/false,\"supportedModes\":[\"anc\",\"ambient\",\"off\",\"game\"],\"description\":\"one short sentence\"}}",
                    deviceName, isBluetooth);

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                string jsonPayload = _serializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                string[] models = new[] { "gemini-1.5-flash", "gemini-2.0-flash", "gemini-2.5-flash" };
                HttpResponseMessage response = null;

                foreach (string model in models)
                {
                    try
                    {
                        string url = string.Format(
                            "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}",
                            model, _apiKey);
                        response = client.PostAsync(url, content).Result;
                        if (response.IsSuccessStatusCode) break;
                    }
                    catch { }
                }

                // If query param didn't work, try with Bearer token authorization header
                if (response == null || !response.IsSuccessStatusCode)
                {
                    try
                    {
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
                        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";
                        response = client.PostAsync(url, content).Result;
                    }
                    catch { }
                }

                if (response != null && response.IsSuccessStatusCode)
                {
                    string rawResponse = response.Content.ReadAsStringAsync().Result;
                    return ParseGeminiResponse(rawResponse);
                }
            }
            return null;
        }

        private DeviceProfile ParseGeminiResponse(string raw)
        {
            try
            {
                var root = _serializer.Deserialize<Dictionary<string, object>>(raw);
                if (root == null || !root.ContainsKey("candidates")) return null;

                var candidates = root["candidates"] as System.Collections.ArrayList;
                if (candidates == null || candidates.Count == 0) return null;

                var cand0 = candidates[0] as Dictionary<string, object>;
                if (cand0 == null || !cand0.ContainsKey("content")) return null;

                var contentObj = cand0["content"] as Dictionary<string, object>;
                if (contentObj == null || !contentObj.ContainsKey("parts")) return null;

                var parts = contentObj["parts"] as System.Collections.ArrayList;
                if (parts == null || parts.Count == 0) return null;

                var part0 = parts[0] as Dictionary<string, object>;
                if (part0 == null || !part0.ContainsKey("text")) return null;

                string text = part0["text"] as string;
                if (string.IsNullOrEmpty(text)) return null;

                // Strip markdown code blocks if Gemini returned ```json ... ```
                text = Regex.Replace(text, @"^```(json)?", "", RegexOptions.Multiline | RegexOptions.IgnoreCase).Trim();
                text = Regex.Replace(text, @"```$", "", RegexOptions.Multiline).Trim();

                var parsed = _serializer.Deserialize<Dictionary<string, object>>(text);
                if (parsed != null)
                {
                    var p = new DeviceProfile();
                    if (parsed.ContainsKey("brand")) p.Brand = parsed["brand"] as string;
                    if (parsed.ContainsKey("model")) p.Model = parsed["model"] as string;
                    if (parsed.ContainsKey("supportsAnc")) p.SupportsAnc = Convert.ToBoolean(parsed["supportsAnc"]);
                    if (parsed.ContainsKey("description")) p.Description = parsed["description"] as string;
                    if (parsed.ContainsKey("supportedModes"))
                    {
                        var modes = parsed["supportedModes"] as System.Collections.ArrayList;
                        if (modes != null)
                        {
                            foreach (var m in modes) p.SupportedModes.Add(m.ToString());
                        }
                    }
                    if (p.SupportedModes.Count == 0)
                    {
                        p.SupportedModes.Add("off");
                    }
                    return p;
                }
            }
            catch { }
            return null;
        }

        private DeviceProfile LocalHeuristicIdentify(string deviceName, bool isBluetooth)
        {
            string lower = deviceName.ToLowerInvariant();
            var profile = new DeviceProfile();

            if (lower.Contains("beats") || lower.Contains("studio pro") || lower.Contains("solo") || lower.Contains("powerbeats"))
            {
                profile.Brand = "Beats / Apple";
                profile.Model = deviceName;
                profile.SupportsAnc = true;
                profile.SupportedModes = new List<string> { "anc", "ambient", "off", "game" };
                profile.Description = "Beats Studio Pro Headset with Active Noise Cancellation & Transparency Mode";
            }
            else if (lower.Contains("sony") || lower.Contains("wh-1000") || lower.Contains("wf-1000"))
            {
                profile.Brand = "Sony";
                profile.Model = deviceName;
                profile.SupportsAnc = true;
                profile.SupportedModes = new List<string> { "anc", "ambient", "off" };
                profile.Description = "Sony Wireless Headset with Active Noise Cancellation";
            }
            else if (lower.Contains("airpods pro") || lower.Contains("airpods max"))
            {
                profile.Brand = "Apple";
                profile.Model = deviceName;
                profile.SupportsAnc = true;
                profile.SupportedModes = new List<string> { "anc", "ambient", "off" };
                profile.Description = "Apple AirPods with Active Noise Cancellation";
            }
            else if (lower.Contains("airpods"))
            {
                profile.Brand = "Apple";
                profile.Model = deviceName;
                profile.SupportsAnc = false;
                profile.SupportedModes = new List<string> { "off" };
                profile.Description = "Standard Apple AirPods (No Active Noise Cancellation)";
            }
            else if (lower.Contains("galaxy buds") || lower.Contains("buds pro") || lower.Contains("buds2"))
            {
                profile.Brand = "Samsung";
                profile.Model = deviceName;
                bool hasAnc = !lower.Contains("buds live") && !lower.Contains("buds+");
                profile.SupportsAnc = hasAnc;
                profile.SupportedModes = hasAnc ? new List<string> { "anc", "ambient", "off" } : new List<string> { "off" };
                profile.Description = "Samsung Galaxy Buds Series";
            }
            else if (lower.Contains("bose") || lower.Contains("quietcomfort") || lower.Contains("nc700"))
            {
                profile.Brand = "Bose";
                profile.Model = deviceName;
                profile.SupportsAnc = true;
                profile.SupportedModes = new List<string> { "anc", "ambient", "off" };
                profile.Description = "Bose Noise Cancelling Headset";
            }
            else if (lower.Contains("nord buds 3r"))
            {
                profile.Brand = "OnePlus";
                profile.Model = "Nord Buds 3r";
                profile.SupportsAnc = false;
                profile.SupportedModes = new List<string> { "off", "game" };
                profile.Description = "OnePlus Nord Buds 3r (Dual Mic AI Clear Call, Low-Latency Game Mode)";
            }
            else if (lower.Contains("oneplus buds") || lower.Contains("nord buds"))
            {
                profile.Brand = "OnePlus";
                profile.Model = deviceName;
                bool hasAnc = lower.Contains("pro") || lower.Contains("nord buds 2") || lower.Contains("nord buds 3");
                profile.SupportsAnc = hasAnc;
                profile.SupportedModes = hasAnc ? new List<string> { "anc", "ambient", "off", "game" } : new List<string> { "off", "game" };
                profile.Description = "OnePlus Wireless Earbuds (Low-Latency Game Mode)";
            }
            else
            {
                profile.Brand = isBluetooth ? "Bluetooth Audio" : "Built-in / Speaker";
                profile.Model = deviceName;
                profile.SupportsAnc = false;
                profile.SupportedModes = new List<string> { "off" };
                profile.Description = isBluetooth ? "Standard Bluetooth Audio Device" : "Windows System Audio Device";
            }

            return profile;
        }
    }
}
