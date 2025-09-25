using Newtonsoft.Json;

namespace VoiceReader
{
    /// <summary>
    /// Represents a Vosk model from the API response
    /// </summary>
    public class VoskModel
    {
        [JsonProperty("lang")]
        public string Language { get; set; } = string.Empty;

        [JsonProperty("lang_text")]
        public string LanguageText { get; set; } = string.Empty;

        [JsonProperty("md5")]
        public string Md5 { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("obsolete")]
        public string ObsoleteString { get; set; } = "false";

        public bool IsObsolete => ObsoleteString?.ToLower() == "true";

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("size_text")]
        public string SizeText { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("url")]
        public string Url { get; set; } = string.Empty;

        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        // Additional properties for local management
        [JsonIgnore]
        public ModelStatus Status { get; set; } = ModelStatus.Available;

        [JsonIgnore]
        public string LocalPath { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{LanguageText} ({Name}) - {SizeText}";
        }
    }

    /// <summary>
    /// Status of the model in the local system
    /// </summary>
    public enum ModelStatus
    {
        Available,      // Can be downloaded
        Downloading,    // Currently downloading
        Downloaded      // Downloaded and ready to use
    }
}