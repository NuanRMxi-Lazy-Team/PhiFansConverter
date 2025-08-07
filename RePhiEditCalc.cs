namespace PhiFansConverter;

public partial class RePhiEditObject
{
    public class JudgeLineList : List<JudgeLine>
    {
        // Cache for line positions to avoid recalculation
        private readonly Dictionary<(int index, float beat), (float x, float y)> _positionCache = new();
        private readonly Dictionary<(int index, float beat), bool> _hasEventCache = new();

        public (float, float) GetLinePosition(int index, float beat)
        {
            // Check cache first
            var cacheKey = (index, beat);
            if (_positionCache.TryGetValue(cacheKey, out var cachedPosition))
            {
                return cachedPosition;
            }

            var result = GetLinePositionOptimized(index, beat);
            
            // Cache the result
            _positionCache[cacheKey] = result;
            return result;
        }

        private (float, float) GetLinePositionOptimized(int index, float beat)
        {
            // Use iterative approach instead of recursion to prevent stack overflow
            var positionStack = new Stack<(float x, float y, float angle)>();
            var currentIndex = index;

            // Build the hierarchy chain from child to root
            var hierarchyChain = new List<int>();
            while (currentIndex != -1 && currentIndex < Count)
            {
                hierarchyChain.Add(currentIndex);
                currentIndex = this[currentIndex].Father;
            }

            // Process from root to child (reverse order)
            float finalX = 0, finalY = 0;
            
            for (int i = hierarchyChain.Count - 1; i >= 0; i--)
            {
                int lineIndex = hierarchyChain[i];
                var line = this[lineIndex];
                
                // Get this line's offset
                float offsetX = line.EventLayers.GetXAtBeat(beat);
                float offsetY = line.EventLayers.GetYAtBeat(beat);

                if (i == hierarchyChain.Count - 1)
                {
                    // Root line - just use its position
                    finalX = offsetX;
                    finalY = offsetY;
                }
                else
                {
                    // Child line - apply parent's rotation and add to parent's position
                    int parentIndex = hierarchyChain[i + 1];
                    
                    // Get parent line's angle
                    float angleDegrees = -this[parentIndex].EventLayers.GetAngleAtBeat(beat);
                    var angleRadians = (angleDegrees % 360 + 360) % 360 * Math.PI / 180f;

                    // Rotate the offset
                    float rotatedOffsetX = (float)(offsetX * Math.Cos(angleRadians) - offsetY * Math.Sin(angleRadians));
                    float rotatedOffsetY = (float)(offsetX * Math.Sin(angleRadians) + offsetY * Math.Cos(angleRadians));

                    // Add to parent's position
                    finalX += rotatedOffsetX;
                    finalY += rotatedOffsetY;
                }
            }

            return (finalX, finalY);
        }

        public bool FatherAndTheLineHasXyEvent(int index, float beat)
        {
            // Check cache first
            var cacheKey = (index, beat);
            if (_hasEventCache.TryGetValue(cacheKey, out bool cachedResult))
            {
                return cachedResult;
            }

            bool result = FatherAndTheLineHasXyEventOptimized(index, beat);
            
            // Cache the result
            _hasEventCache[cacheKey] = result;
            return result;
        }

        private bool FatherAndTheLineHasXyEventOptimized(int index, float beat)
        {
            // Use iterative approach to check the entire hierarchy
            var currentIndex = index;
            
            while (currentIndex != -1 && currentIndex < Count)
            {
                var line = this[currentIndex];
                
                // Check if current line has X or Y events at this beat
                if (line.EventLayers.HasXEventAtBeat(beat) || line.EventLayers.HasYEventAtBeat(beat))
                {
                    return true;
                }
                
                // Move to parent
                currentIndex = line.Father;
            }
            
            return false;
        }

        // Method to clear cache when lines are modified
        public void InvalidateCache()
        {
            _positionCache.Clear();
            _hasEventCache.Clear();
        }

        // Override collection modification methods to invalidate cache
        public new void Add(JudgeLine item)
        {
            base.Add(item);
            InvalidateCache();
        }

        public new void Insert(int index, JudgeLine item)
        {
            base.Insert(index, item);
            InvalidateCache();
        }

        public new bool Remove(JudgeLine item)
        {
            var result = base.Remove(item);
            if (result) InvalidateCache();
            return result;
        }

        public new void RemoveAt(int index)
        {
            base.RemoveAt(index);
            InvalidateCache();
        }

        public new void Clear()
        {
            base.Clear();
            InvalidateCache();
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
        // Cache for frequently accessed beat values to improve performance
        private readonly Dictionary<float, float> _beatValueCache = new();
        private readonly Dictionary<float, bool> _hasEventCache = new();
        private float _lastEventEndBeatCache = -1;
        private bool _isLastEventEndBeatCacheValid = false;

        // Override Add/Insert/Remove methods to invalidate cache
        public new void Add(Event item)
        {
            base.Add(item);
            InvalidateCache();
        }

        public new void Insert(int index, Event item)
        {
            base.Insert(index, item);
            InvalidateCache();
        }

        public new bool Remove(Event item)
        {
            var result = base.Remove(item);
            if (result) InvalidateCache();
            return result;
        }

        public new void RemoveAt(int index)
        {
            base.RemoveAt(index);
            InvalidateCache();
        }

        public new void Clear()
        {
            base.Clear();
            InvalidateCache();
        }

        private void InvalidateCache()
        {
            _beatValueCache.Clear();
            _hasEventCache.Clear();
            _isLastEventEndBeatCacheValid = false;
        }

        public float GetValueAtBeat(float beat)
        {
            // Check cache first
            if (_beatValueCache.TryGetValue(beat, out float cachedValue))
            {
                return cachedValue;
            }

            float result = GetValueAtBeatOptimized(beat);
            
            // Cache the result
            _beatValueCache[beat] = result;
            return result;
        }

        private float GetValueAtBeatOptimized(float beat)
        {
            if (Count == 0) return 0;

            // Binary search for the event containing this beat
            int left = 0, right = Count - 1;
            Event? containingEvent = null;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                var e = this[mid];

                if (beat >= e.StartTime && beat <= e.EndTime)
                {
                    containingEvent = e;
                    break;
                }
                else if (beat < e.StartTime)
                {
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }

            if (containingEvent != null)
            {
                return containingEvent.GetValueAtBeat(beat);
            }

            // If no containing event found, find the last event before this beat
            Event? previousEvent = null;
            for (int i = Count - 1; i >= 0; i--)
            {
                if (beat > this[i].EndTime)
                {
                    previousEvent = this[i];
                    break;
                }
            }

            return previousEvent?.End ?? 0;
        }

        public bool HasEventAtBeat(float beat)
        {
            // Check cache first
            if (_hasEventCache.TryGetValue(beat, out bool cachedResult))
            {
                return cachedResult;
            }

            bool result = HasEventAtBeatOptimized(beat);
            
            // Cache the result
            _hasEventCache[beat] = result;
            return result;
        }

        private bool HasEventAtBeatOptimized(float beat)
        {
            if (Count == 0) return false;

            // Binary search optimization for checking if event exists at beat
            int left = 0, right = Count - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                var e = this[mid];

                if (beat >= e.StartTime && beat <= e.EndTime)
                {
                    return true;
                }
                else if (beat < e.StartTime)
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

        // 最后一个事件的结束拍
        public float LastEventEndBeat()
        {
            if (Count == 0) return 0;

            if (_isLastEventEndBeatCacheValid)
            {
                return _lastEventEndBeatCache;
            }

            _lastEventEndBeatCache = this.Last().EndTime;
            _isLastEventEndBeatCacheValid = true;
            return _lastEventEndBeatCache;
        }
    }
}