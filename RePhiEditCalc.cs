namespace PhiFansConverter;

public partial class RePhiEditObject
{
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

    public partial class Event
    {
        public float GetValueAtBeat(float beat)
        {
            float startTime = StartTime;
            float endTime = EndTime;
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
        public float GetValueAtBeat(float beat)
        {
            for (int i = 0; i < Count; i++)
            {
                var e = this[i];
                if (beat >= e.StartTime && beat <= e.EndTime)
                {
                    return e.GetValueAtBeat(beat);
                }

                if (beat < e.StartTime)
                {
                    break;
                }
            }

            var previousEvent = FindLast(e => beat > e.EndTime);
            return previousEvent?.End ?? 0;
        }

        public bool HasEventAtBeat(float beat)
        {
            // 如果有事件在这个拍上，返回true
            return this.Any(e => beat >= e.StartTime && beat <= e.EndTime);
        }

        // 最后一个事件的结束拍
        public float LastEventEndBeat()
        {
            if (Count == 0) return 0;

            return this.Last().EndTime;
        }
    }
}