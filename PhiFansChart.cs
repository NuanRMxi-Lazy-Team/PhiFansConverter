using Newtonsoft.Json;
using static PhiFansConverter.PhiFansObject;
namespace PhiFansConverter;

public class PhiFansChart
{
    public Info info;
    public int offset = 0;
    public List<BpmItem> bpm = new();
    public List<LineItem> lines = new();
}

public static class PhiFansObject
{
    [JsonObject]
    public class BpmItem()
    {
        public int[] beat = new int[3];
        public float bpm = 120;
    }
    [JsonObject]
    public struct Info
    {
        public string name;        // 曲名
        public string artist;      // 曲师
        public string illustrator;// 插画画师
        public string level;       // 等级
        public string designer;    // 谱师
    }
    [JsonObject]
    public class LineItem()
    {
        public PropsObject props = new();
        public List<Note> notes = new();
    }
    [JsonObject]
    public class EventItem()
    {
        public int[] beat = new int[3];
        public float value = 0;
        public bool continuous = false;
        public int easing = 0;
    }
    [JsonObject]
    public class Note
    {
        public int type = 1;
        public int[] beat = new int[3];
        public float positionX;
        public float speed;
        public bool isAbove = true;
        public int[] holdEndBeat = new int[3];
    }
    [JsonObject]
    public class PropsObject()
    {
        public List<EventItem> speed = new();
        public List<EventItem> positionX = new();
        public List<EventItem> positionY = new();
        public List<EventItem> rotate = new();
        public List<EventItem> alpha = new();
    }
    
}