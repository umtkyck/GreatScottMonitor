using Newtonsoft.Json;

namespace CXA.Shared.Models;

public class IpcMessage
{
    [JsonProperty("command")]
    public string Command { get; set; } = string.Empty;

    [JsonProperty("frame_data")]
    public string? FrameData { get; set; }

    [JsonProperty("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
}

public class IpcResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("data")]
    public object? Data { get; set; }

    [JsonProperty("error")]
    public string? Error { get; set; }
}

public static class IpcCommands
{
    public const string Detect = "DETECT";
    public const string ExtractEmbedding = "EXTRACT_EMBEDDING";
    public const string Compare = "COMPARE";
    public const string EnrollCapture = "ENROLL_CAPTURE";
}






