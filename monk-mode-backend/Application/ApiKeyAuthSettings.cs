using System;

namespace monk_mode_backend.Application {
    public class ApiKeyAuthSettings {
        public bool Enabled { get; set; } = false;
        public string Header { get; set; } = "X-Api-Key";
        public string Key { get; set; } = string.Empty;
        public string[] ExcludedPaths { get; set; } = Array.Empty<string>();
    }
}
