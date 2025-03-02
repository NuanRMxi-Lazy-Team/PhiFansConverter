using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PhiFansConverter;

public struct RpeChart
{
    // 构造
    public RpeChart(bool init = true)
    {
        if (!init) throw new Exception("init must be true");
        BpmList = new List<RePhiEditObject.RpeBpm>();
        Meta = new RePhiEditObject.Meta();
        JudgeLineList = new RePhiEditObject.JudgeLineList();
    }

    [JsonProperty("BPMList")] public static List<RePhiEditObject.RpeBpm> BpmList;

    [JsonIgnore]
    public List<RePhiEditObject.RpeBpm> bpmlist
    {
        set => BpmList = value;
        get => BpmList;
    }

    [JsonProperty("META")] public RePhiEditObject.Meta Meta;
    [JsonProperty("judgeLineList")] public RePhiEditObject.JudgeLineList JudgeLineList;
}

public static class RePhiEditObject
{
    private const float RpeSpeedToOfficial = 4.5f; // RPE速度转换为官谱速度的比例

    [JsonConverter(typeof(BeatJsonConverter))]
    public struct Beat
    {
        private int[] _beat;
        private float? _time;

        public Beat(int[]? timeArray = null)
        {
            _beat = timeArray ?? new[] { 0, 0, 1 };
            _time = null;
        }

        // 存储单个拍的时间，格式为 [0]:[1]/[2]
        public int this[int index]
        {
            get
            {
                if (index > 2)
                {
                    throw new IndexOutOfRangeException();
                }

                return _beat[index];
            }
            set
            {
                if (index > 2)
                {
                    throw new IndexOutOfRangeException();
                }

                _beat[index] = value;
            }
        }

        public float CurBeat => (float)this[1] / this[2] + this[0];

        public int[] Array => _beat;
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
        public string Texture = "line.png"; // 判定线材质路径
        [JsonProperty("anchor")] public float[] Anchor = { 0.5f, 0.5f }; // 判定线材质锚点
        [JsonProperty("eventLayers")] public EventLayers EventLayers = new(); // 事件层
        [JsonProperty("father")] public int Father = -1; // 父级
        [JsonProperty("isCover")] public int IsCover = 1; // 是否遮罩（1为遮罩，0为不遮罩）
        [JsonProperty("notes")] public List<Note> Notes = new(); // note列表
        [JsonProperty("zOrder")] public int ZOrder; // Z轴顺序
        [JsonProperty("attachUI")] public string? AttachUi; // 绑定UI名，当不绑定时为null
        [JsonProperty("isGif")] public bool IsGif; // 材质是否为GIF
        [JsonProperty("bpmfactor")] public float BpmFactor = 1.0f; // BPM因子
    }

    public class JudgeLineList : List<JudgeLine>
    {
        public (float, float) GetLinePosition(int index, float beat)
        {
            // 在没有父线的情况下直接返回
            if (this[index].Father == -1)
                return (this[index].EventLayers.GetXAtBeat(beat), this[index].EventLayers.GetYAtBeat(beat));

            int fatherIndex = this[index].Father;
            // 获取父线位置
            var (fatherX, fatherY) = GetLinePosition(fatherIndex, beat);

            // 获取当前线相对于父线的偏移量
            float offsetX = this[index].EventLayers.GetXAtBeat(beat);
            float offsetY = this[index].EventLayers.GetYAtBeat(beat);

            // 获取父线的角度并转换为弧度
            float angleDegrees = -this[fatherIndex].EventLayers.GetAngleAtBeat(beat);
            var angleRadians = (angleDegrees % 360 + 360) % 360 * Math.PI / 180f;

            // 对偏移量进行旋转
            float rotatedOffsetX = (float)(offsetX * Math.Cos(angleRadians) - offsetY * Math.Sin(angleRadians));
            float rotatedOffsetY = (float)(offsetX * Math.Sin(angleRadians) + offsetY * Math.Cos(angleRadians));
            float x = fatherX + rotatedOffsetX;
            float y = fatherY + rotatedOffsetY;

            // 最后加上父线的位置得到最终位置
            return (fatherX + rotatedOffsetX, fatherY + rotatedOffsetY);
        }

        public bool FatherAndTheLineHasXyEvent(int index, float beat)
        {
            // 递归计算，先判断是否有父线，如果有，递归
            if (this[index].Father != -1)
            {
                return FatherAndTheLineHasXyEvent(this[index].Father, beat) ||
                       this[index].EventLayers.HasXEventAtBeat(beat) ||
                       this[index].EventLayers.HasYEventAtBeat(beat);
            }
            else
            {
                return this[index].EventLayers.HasXEventAtBeat(beat) || this[index].EventLayers.HasYEventAtBeat(beat);
            }
        }
    }


    /// <summary>
    /// 单个事件层
    /// </summary>
    public class EventLayer
    {
        [JsonProperty("moveXEvents")] public EventList MoveXEvents = new(); // 移动事件
        [JsonProperty("moveYEvents")] public EventList MoveYEvents = new(); // 移动事件
        [JsonProperty("rotateEvents")] public EventList RotateEvents = new(); // 旋转事件
        [JsonProperty("alphaEvents")] public EventList AlphaEvents = new(); // 透明度事件
        [JsonProperty("speedEvents")] public SpeedEventList SpeedEvents = new(); // 速度事件
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
    public class Event
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

        public float GetValueAtBeat(float beat)
        {
            float startTime = StartTime.CurBeat;
            float endTime = EndTime.CurBeat;
            //获得这个拍在这个事件的时间轴上的位置
            float t = (beat - startTime) / (endTime - startTime);
            //获得当前拍的值
            var easedTime = Easing.Evaluate(EasingType, EasingLeft, EasingRight, t);
            //插值
            return Mathf.LerpUnclamped(Start, End, (float)easedTime);
        }
    }

    public class EventList : List<Event>
    {
        private int _lastIndex;

        public float GetValueAtBeat(float beat)
        {
            for (int i = _lastIndex; i < Count; i++)
            {
                var e = this[i];
                if (beat >= e.StartTime.CurBeat && beat <= e.EndTime.CurBeat)
                {
                    _lastIndex = i;
                    return e.GetValueAtBeat(beat);
                }

                if (beat < e.StartTime.CurBeat)
                {
                    break;
                }
            }

            var previousEvent = FindLast(e => beat > e.EndTime.CurBeat);
            return previousEvent?.End ?? 0;
        }
        public bool HasEventAtBeat(float beat)
        {
            // 如果有事件在这个拍上，返回true
            return this.Any(e => beat >= e.StartTime.CurBeat && beat <= e.EndTime.CurBeat);
        }
        // 最后一个事件的结束拍
        public float LastEventEndBeat()
        {
            if (Count == 0) return 0;
            
            return this.Last().EndTime.CurBeat;
        }
    }

    public class SpeedEventList : List<SpeedEvent>
    {
    }

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
            StartTime = new();
            EndTime = new();
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
        [JsonProperty("speed")] public float SpeedMultiplier; // 速度倍率
        [JsonProperty("type")] public int Type; // 类型（1 为 Tap、2 为 Hold、3 为 Flick、4 为 Drag）
        [JsonProperty("visibleTime")] public float VisibleTime; // 可见时间（单位为秒）
        [JsonProperty("yOffset")] public float YOffset; // Y偏移
        [JsonProperty("hitsound")] public string? HitSound; // 音效
    }
}

public class BeatJsonConverter : JsonConverter<RePhiEditObject.Beat>
{
    public override RePhiEditObject.Beat ReadJson(JsonReader reader, Type objectType, RePhiEditObject.Beat existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        JArray array = JArray.Load(reader);
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
    public static int[] BeatToBeatArray(float beat)
    {
        int RPEBeat0 = (int)Math.Floor(beat);
        double fractionalPart = beat - RPEBeat0;
        int maxDenominator = 10000;
        int RPEBeat1, RPEBeat2;

        FractionalApproximation(fractionalPart, maxDenominator, out RPEBeat1, out RPEBeat2);

        return new[] { RPEBeat0, RPEBeat1, RPEBeat2 };
    }

    private static void FractionalApproximation(double x, int maxDenominator, out int numerator, out int denominator)
    {
        if (x == 0)
        {
            numerator = 0;
            denominator = 1;
            return;
        }

        int sign = x < 0 ? -1 : 1;
        x = Math.Abs(x);

        int n = 0, d = 1;
        int n1 = 1, d1 = 0;
        int n2 = 0, d2 = 1;

        double fraction = x;
        while (d <= maxDenominator)
        {
            int a = (int)Math.Floor(fraction);
            double newFraction = fraction - a;

            n = a * n1 + n2;
            d = a * d1 + d2;

            if (d > maxDenominator)
                break;

            n2 = n1;
            d2 = d1;
            n1 = n;
            d1 = d;

            if (newFraction < 1e-10)
                break;

            fraction = 1.0 / newFraction;
        }

        numerator = n * sign;
        denominator = d;
    }
}