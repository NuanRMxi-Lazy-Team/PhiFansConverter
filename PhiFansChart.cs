using Newtonsoft.Json;
using static PhiFansConverter.PhiFansObject;

namespace PhiFansConverter;

public class PhiFansChart
{
    [JsonProperty("info")]
    public Info Info = new();
    [JsonProperty("offset")]
    public int Offset;
    [JsonProperty("bpm")]
    public List<BpmItem> Bpm = [];
    [JsonProperty("lines")]
    public List<LineItem> Lines = [];
}

public static class PhiFansObject
{
    [JsonObject]
    public class BpmItem
    {
        [JsonProperty("beat")]
        public int[] Beat = new int[3];
        [JsonProperty("bpm")]
        public float Bpm = 120;
    }

    [JsonObject]
    public class Info
    {
        [JsonProperty("name")] public string Name = ""; // 曲名
        [JsonProperty("artist")] public string Artist = ""; // 曲师
        [JsonProperty("illustration")] public string Illustration = ""; // 插画
        [JsonProperty("level")] public string Level = ""; // 等级
        [JsonProperty("designer")] public string Designer = ""; // 谱师
    }

    [JsonObject]
    public class LineItem
    {
        [JsonProperty("props")] public PropsObject Props = new();
        [JsonProperty("notes")] public List<Note> Notes = [];
    }

    [JsonObject]
    public class EventItem
    {
        [JsonProperty("beat")] public int[] Beat = new int[3];
        [JsonProperty("value")] public float Value;
        [JsonProperty("continuous")] public bool Continuous;
        [JsonProperty("easing")] public int Easing;
    }

    [JsonObject]
    public class Note
    {
        [JsonProperty("type")] public int Type = 1;
        [JsonProperty("beat")] public int[] Beat = new int[3];
        [JsonProperty("positionX")] public float PositionX;
        [JsonProperty("speed")] public float Speed;
        [JsonProperty("isAbove")] public bool IsAbove = true;
        [JsonProperty("holdEndBeat")] public int[] HoldEndBeat = new int[3];
    }

    [JsonObject]
    public class PropsObject
    {
        [JsonProperty("speed")] public List<EventItem> Speed = [];
        [JsonProperty("positionX")] public List<EventItem> PositionX = [];
        [JsonProperty("positionY")] public List<EventItem> PositionY = [];
        [JsonProperty("rotate")] public List<EventItem> Rotate = [];
        [JsonProperty("alpha")] public List<EventItem> Alpha = [];
    }
}