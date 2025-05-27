using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PhiFansConverter;

public struct RpeChart
{
    // 构造
    public RpeChart()
    {
        BpmList = [];
        Meta = new RePhiEditObject.Meta();
        JudgeLineList = [];
    }

    [JsonProperty("BPMList")] public List<RePhiEditObject.RpeBpm> BpmList = [];

/*
    [JsonProperty(nameof(BpmList))]
    public List<RePhiEditObject.RpeBpm> bpmlist
    {
        set => BpmList = value;
        get => BpmList;
    }
*/
    [JsonProperty("META")] public RePhiEditObject.Meta Meta;
    [JsonProperty("judgeLineList")] public RePhiEditObject.JudgeLineList JudgeLineList;
}

public static partial class RePhiEditObject
{
    // private const float RpeSpeedToOfficial = 4.5f; // RPE速度转换为官谱速度的比例

    [JsonConverter(typeof(BeatJsonConverter))]
    public struct Beat
    {
        private readonly int[] _beat;

        public Beat(int[]? beatArray = null)
        {
            _beat = beatArray ?? [0, 0, 1];
        }

        // 存储单个拍的时间，格式为 [0]:[1]/[2]
        public int this[int index]
        {
            get
            {
                if (index > 2)
                    throw new IndexOutOfRangeException();
                return _beat[index];
            }
            set
            {
                if (index > 2)
                    throw new IndexOutOfRangeException();
                _beat[index] = value;
            }
        }

        [Obsolete("请直接赋值给float或double类型")] public float CurBeat => (float)this[1] / this[2] + this[0];

        [Obsolete("请直接赋值给int[]类型")]
        public int[] Array
        {
            get => _beat;
        }

        // 隐式转换为 float，返回 CurBeat
        public static implicit operator float(Beat beat) => (float)beat[1] / beat[2] + beat[0];

        // 隐式转换为 double，返回 CurBeat
        public static implicit operator double(Beat beat) => (double)beat[1] / beat[2] + beat[0];

        // 隐式转换为 int[]，返回 _beat
        public static implicit operator int[](Beat beat) => beat._beat;
    }

    /// <summary>
    /// BPM
    /// </summary>
    public struct RpeBpm
    {
        [JsonProperty("bpm")] public float Bpm;
        [JsonProperty("startTime")] public Beat StartTime;
    }

    /// <summary>
    /// 元数据
    /// </summary>
    public struct Meta
    {
        [JsonProperty("RPEVersion")] public int RpeVersion; // RPE版本
        [JsonProperty("background")] public string Background; // 曲绘
        [JsonProperty("charter")] public string Charter; // 谱师
        [JsonProperty("composer")] public string Composer; // 曲师
        [JsonProperty("illustration")] public string Illustration; // 曲绘画师
        [JsonProperty("level")] public string Level; // 难度
        [JsonProperty("name")] public string Name; // 曲名
        [JsonProperty("offset")] public int Offset; // 音乐偏移
        [JsonProperty("song")] public string Song; // 音乐
    }

    public class JudgeLine
    {
        public string Name = "PhiFansLine";
        public string Texture = "line.png"; // 判定线纹理路径
        [JsonProperty("anchor")] public float[] Anchor = { 0.5f, 0.5f }; // 判定线纹理锚点
        [JsonProperty("eventLayers")] public EventLayers EventLayers = new(); // 事件层
        [JsonProperty("father")] public int Father = -1; // 父级
        [JsonProperty("isCover")] public int IsCover = 1; // 是否遮罩（1为遮罩，0为不遮罩）
        [JsonProperty("notes")] public List<Note> Notes = new(); // note列表
        [JsonProperty("zOrder")] public int ZOrder; // Z轴顺序
        [JsonProperty("attachUI")] public string? AttachUi; // 绑定UI名，当不绑定时为null
        [JsonProperty("isGif")] public bool IsGif; // 纹理是否为GIF
        [JsonProperty("bpmfactor")] public float BpmFactor = 1.0f; // BPM因子

        [JsonProperty("alphaControl")] public readonly object[] AlphaControl =
        [
            new
            {
                alpha = 1.0,
                easing = 1,
                x = 0.0
            },
            new
            {
                alpha = 1.0,
                easing = 1,
                x = 9999999.0
            }
        ]; // 透明度控制

        [JsonProperty("extended")] public readonly object Extended = new(); // 扩展数据

        [JsonProperty("posControl")] public readonly object[] PosControl =
        [
            new
            {
                easing = 1,
                pos = 1.0,
                x = 0.0
            },
            new
            {
                easing = 1,
                pos = 1.0,
                x = 9999999.0
            }
        ];

        [JsonProperty("sizeControl")] public readonly object[] SizeControl =
        [
            new
            {
                easing = 1,
                size = 1.0,
                x = 0.0
            },
            new
            {
                easing = 1,
                size = 1.0,
                x = 9999999.0
            }
        ];

        [JsonProperty("skewControl")] public readonly object[] SkewControl =
        [
            new
            {
                easing = 1,
                skew = 0.0,
                x = 0.0
            },
            new
            {
                easing = 1,
                skew = 0.0,
                x = 9999999.0
            }
        ];

        [JsonProperty("yControl")] public readonly object[] YControl =
        [
            new
            {
                easing = 1,
                x = 0.0,
                y = 1.0
            },
            new
            {
                easing = 1,
                x = 9999999.0,
                y = 1.0
            }
        ];
    }

    /// <summary>
    /// 单个事件层
    /// </summary>
    public class EventLayer
    {
        [JsonProperty("moveXEvents")] public EventList MoveXEvents = []; // 移动事件
        [JsonProperty("moveYEvents")] public EventList MoveYEvents = []; // 移动事件
        [JsonProperty("rotateEvents")] public EventList RotateEvents = []; // 旋转事件
        [JsonProperty("alphaEvents")] public EventList AlphaEvents = []; // 透明度事件
        [JsonProperty("speedEvents")] public EventList SpeedEvents = []; // 速度事件
    }

    /// <summary>
    /// 事件层列表
    /// </summary>
    public class EventLayers : List<EventLayer>
    {
        public float LastEventEndBeat()
        {
            // 求下面所有事件种类的最后一个事件的结束拍，返回最大值
            float x = LastXEventEndBeat;
            float y = LastYEventEndBeat;
            float angle = LastAngleEventEndBeat;
            float alpha = LastAlphaEventEndBeat;
            return Math.Max(x, Math.Max(y, Math.Max(angle, alpha)));
        }

        public float GetXAtBeat(float t) =>
            this.Sum(eventLayer => eventLayer.MoveXEvents.GetValueAtBeat(t));

        public bool HasXEventAtBeat(float beat) =>
            this.Any(eventLayer => eventLayer.MoveXEvents.HasEventAtBeat(beat));

        public float LastXEventEndBeat =>
            this.Max(eventLayer => eventLayer.MoveXEvents.LastEventEndBeat());

        public float GetYAtBeat(float t) =>
            this.Sum(eventLayer => eventLayer.MoveYEvents.GetValueAtBeat(t));

        public bool HasYEventAtBeat(float beat) =>
            this.Any(eventLayer => eventLayer.MoveYEvents.HasEventAtBeat(beat));

        public float LastYEventEndBeat =>
            this.Max(eventLayer => eventLayer.MoveYEvents.LastEventEndBeat());

        public float GetAngleAtBeat(float t) =>
            this.Sum(eventLayer => eventLayer.RotateEvents.GetValueAtBeat(t));

        public bool HasAngleEventAtBeat(float beat) =>
            this.Any(eventLayer => eventLayer.RotateEvents.HasEventAtBeat(beat));

        public float LastAngleEventEndBeat =>
            this.Max(eventLayer => eventLayer.RotateEvents.LastEventEndBeat());

        public float GetAlphaAtBeat(float t) =>
            this.Sum(eventLayer => eventLayer.AlphaEvents.GetValueAtBeat(t));

        public bool HasAlphaEventAtBeat(float beat) =>
            this.Any(eventLayer => eventLayer.AlphaEvents.HasEventAtBeat(beat));

        public float LastAlphaEventEndBeat =>
            this.Max(eventLayer => eventLayer.AlphaEvents.LastEventEndBeat());
    }


    /// <summary>
    /// 普通事件
    /// </summary>
    public partial class Event
    {
        [JsonProperty("bezier")] public int Bezier; // 是否为贝塞尔曲线
        [JsonProperty("bezierPoints")] public float[] BezierPoints = new float[4]; // 贝塞尔曲线点
        [JsonProperty("easingLeft")] public float EasingLeft; // 缓动开始
        [JsonProperty("easingRight")] public float EasingRight = 1.0f; // 缓动结束
        [JsonProperty("easingType")] public int EasingType = 1; // 缓动类型
        [JsonProperty("start")] public float Start; // 开始值
        [JsonProperty("end")] public float End; // 结束值
        [JsonProperty("startTime")] public Beat StartTime; // 开始时间
        [JsonProperty("endTime")] public Beat EndTime; // 结束时间
    }

    [Obsolete("不应再使用SpeedEventList类，请使用EventList类")]
    public class SpeedEventList : List<SpeedEvent>
    {
    }

    [Obsolete("不应再使用SpeedEvent类，请使用Event类")]
    public class SpeedEvent : Event
    {
        //public float FloorPosition;
    }

    public struct Note
    {
        // 结构体初始化，避免空引用，以下原本是class的属性
        public Note(bool init = true)
        {
            if (!init) throw new Exception("init must be true");
            StartTime = new Beat();
            EndTime = new Beat();
            Alpha = 255;
            Above = 1;
            IsFake = 0;
            PositionX = 0.0f;
            Size = 1.0f;
            SpeedMultiplier = 1.0f;
            Type = 1;
            VisibleTime = 999999.0000f;
            YOffset = 0.0f;
            HitSound = null;
        }

        [JsonProperty("above")] public int Above; // 是否在判定线上方（1为上方，2为下方）
        [JsonProperty("alpha")] public int Alpha; // 透明度，255为不透明，0为透明
        [JsonProperty("startTime")] public Beat StartTime; // 开始时间
        [JsonProperty("endTime")] public Beat EndTime; // 结束时间
        [JsonProperty("isFake")] public int IsFake; // 是否为假note（1为假note，0为真note）
        [JsonProperty("positionX")] public float PositionX; // X坐标
        [JsonProperty("size")] public float Size; // 宽度倍率
        [JsonProperty("Speed")] public float SpeedMultiplier; // 速度倍率
        [JsonProperty("type")] public int Type; // 类型（1 为 Tap、2 为 Hold、3 为 Flick、4 为 Drag）
        [JsonProperty("visibleTime")] public float VisibleTime; // 可见时间（单位为秒）
        [JsonProperty("yOffset")] public float YOffset; // Y偏移

        [JsonProperty("hitsound", NullValueHandling = NullValueHandling.Ignore)]
        public string? HitSound; // 音效
    }
}

public class BeatJsonConverter : JsonConverter<RePhiEditObject.Beat>
{
    public override RePhiEditObject.Beat ReadJson(JsonReader reader, Type objectType,
        RePhiEditObject.Beat existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        var array = JArray.Load(reader);
        return new RePhiEditObject.Beat(array.ToObject<int[]>());
    }

    public override void WriteJson(JsonWriter writer, RePhiEditObject.Beat value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        for (int i = 0; i < 3; i++)
        {
            writer.WriteValue(value[i]);
        }

        writer.WriteEndArray();
    }
}

public static class Mathf
{
    public static float LerpUnclamped(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}

public static class BeatConverter
{
    [Obsolete("请使用RestoreArray方法")]
    public static int[] BeatToBeatArray(double beat)
    {
        return RestoreArray(beat);
    }

    public static int[] RestoreArray(double result, int maxA2 = 10000, double epsilon = 1e-6)
    {
        int[] best = null;
        int minSum = int.MaxValue;

        // 枚举 array[2]，即分母，从小到大（更可能找到“最简”的组合）
        for (int a2 = 1; a2 <= maxA2; a2++)
        {
            // a1 = (result - a0) * a2 -> 推导成：a1 = result * a2 - a0 * a2
            double a1Raw = result * a2;
            int a1 = (int)Math.Round(a1Raw);

            // 推回 a0
            double a0Raw = (a1Raw - a1) == 0 ? result - ((double)a1 / a2) : -1;
            if (a0Raw < 0 || a0Raw % 1 != 0) continue;

            int a0 = (int)Math.Round(a0Raw);
            if (a0 < 0 || a0 > result) continue;

            if (Math.Abs((a1 / (double)a2) + a0 - result) < epsilon)
            {
                int sum = a0 + a1 + a2;
                if (sum < minSum)
                {
                    minSum = sum;
                    best = new int[] { a0, a1, a2 };
                    if (sum == result + 1) break; // 已经最简（例如 result=351 → [351,0,1]）
                }
            }
        }

        return best;
    }
}