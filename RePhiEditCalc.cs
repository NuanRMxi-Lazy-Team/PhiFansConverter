namespace PhiFansConverter;

public partial class RePhiEditObject
{
    public class JudgeLineList : List<JudgeLine>
    {
        private readonly Dictionary<(int index, float beat), (float x, float y)> _positionCache = new();
        private readonly Dictionary<(int index, float beat), bool> _eventCache = new();

        public (float, float) GetLinePosition(int index, float beat)
        {
            var key = (index, beat);
            if (_positionCache.TryGetValue(key, out var cachedPosition))
            {
                return cachedPosition;
            }

            (float x, float y) result;

            // 在没有父线的情况下直接返回
            if (this[index].Father == -1)
            {
                result = (this[index].EventLayers.GetXAtBeat(beat), this[index].EventLayers.GetYAtBeat(beat));
            }
            else
            {
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
                result = (fatherX + rotatedOffsetX, fatherY + rotatedOffsetY);
            }

            _positionCache[key] = result;
            return result;
        }

        public bool FatherAndTheLineHasXyEvent(int index, float beat)
        {
            var key = (index, beat);
            if (_eventCache.TryGetValue(key, out var cachedResult))
            {
                return cachedResult;
            }

            bool result;
            // 递归计算，先判断是否有父线，如果有，递归
            if (this[index].Father != -1)
            {
                result = FatherAndTheLineHasXyEvent(this[index].Father, beat) ||
                       this[index].EventLayers.HasXEventAtBeat(beat) ||
                       this[index].EventLayers.HasYEventAtBeat(beat);
            }
            else
            {
                result = this[index].EventLayers.HasXEventAtBeat(beat) || this[index].EventLayers.HasYEventAtBeat(beat);
            }

            _eventCache[key] = result;
            return result;
        }

        public void ClearCache()
        {
            _positionCache.Clear();
            _eventCache.Clear();
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
        private bool _sorted = false;
        private float _lastEventEndBeat = -1;

        public new void Add(Event item)
        {
            base.Add(item);
            _sorted = false;
            _lastEventEndBeat = -1;
        }

        public new void AddRange(IEnumerable<Event> collection)
        {
            base.AddRange(collection);
            _sorted = false;
            _lastEventEndBeat = -1;
        }

        private void EnsureSorted()
        {
            if (!_sorted && Count > 0)
            {
                Sort((a, b) => ((float)a.StartTime).CompareTo((float)b.StartTime));
                _sorted = true;
            }
        }

        public float GetValueAtBeat(float beat)
        {
            if (Count == 0) return 0;
            
            EnsureSorted();

            // 二分搜索优化
            int left = 0, right = Count - 1;
            Event? targetEvent = null;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                var e = this[mid];

                if (beat >= e.StartTime && beat <= e.EndTime)
                {
                    return e.GetValueAtBeat(beat);
                }

                if (beat < e.StartTime)
                {
                    right = mid - 1;
                }
                else
                {
                    targetEvent = e;
                    left = mid + 1;
                }
            }

            return targetEvent?.End ?? 0;
        }

        public bool HasEventAtBeat(float beat)
        {
            if (Count == 0) return false;
            
            EnsureSorted();

            // 二分搜索优化
            int left = 0, right = Count - 1;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                var e = this[mid];

                if (beat >= e.StartTime && beat <= e.EndTime)
                {
                    return true;
                }

                if (beat < e.StartTime)
                {
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }

            return false;
        }

        // 缓存最后一个事件的结束拍
        public float LastEventEndBeat()
        {
            if (Count == 0) return 0;

            if (_lastEventEndBeat < 0)
            {
                EnsureSorted();
                _lastEventEndBeat = this[Count - 1].EndTime;
            }

            return _lastEventEndBeat;
        }
    }
}