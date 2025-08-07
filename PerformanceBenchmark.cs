using System.Diagnostics;
using static PhiFansConverter.RePhiEditObject;

namespace PhiFansConverter;

/// <summary>
/// Performance benchmark to test optimization improvements
/// </summary>
public static class PerformanceBenchmark
{
    public static void RunBenchmarks()
    {
        Console.WriteLine("=== PhiFansConverter Performance Benchmark ===");
        
        // Create test data
        var testEventList = CreateTestEventList(10000);
        var testJudgeLineList = CreateTestJudgeLineList(100);
        
        // Benchmark EventList.GetValueAtBeat
        BenchmarkEventListPerformance(testEventList);
        
        // Benchmark JudgeLineList.GetLinePosition
        BenchmarkJudgeLineListPerformance(testJudgeLineList);
        
        // Benchmark easing calculations
        BenchmarkEasingPerformance();
        
        Console.WriteLine("=== Benchmark Complete ===");
    }
    
    private static EventList CreateTestEventList(int eventCount)
    {
        var eventList = new EventList();
        var random = new Random(42); // Fixed seed for reproducible results
        
        for (int i = 0; i < eventCount; i++)
        {
            var startTime = i * 0.5f;
            var endTime = startTime + 0.4f;
            
            eventList.Add(new Event
            {
                StartTime = new Beat([0, (int)(startTime * 8), 8]),
                EndTime = new Beat([0, (int)(endTime * 8), 8]),
                Start = random.NextSingle() * 100,
                End = random.NextSingle() * 100,
                EasingType = random.Next(1, 29)
            });
        }
        
        return eventList;
    }
    
    private static JudgeLineList CreateTestJudgeLineList(int lineCount)
    {
        var judgeLineList = new JudgeLineList();
        var random = new Random(42);
        
        for (int i = 0; i < lineCount; i++)
        {
            var judgeLine = new JudgeLine
            {
                Father = i > 0 && random.NextDouble() < 0.3 ? random.Next(0, i) : -1,
                EventLayers = new EventLayers()
            };
            
            // Add some test events
            var eventLayer = new EventLayer();
            for (int j = 0; j < 50; j++)
            {
                var beat = j * 0.25f;
                eventLayer.MoveXEvents.Add(new Event
                {
                    StartTime = new Beat([0, (int)(beat * 8), 8]),
                    EndTime = new Beat([0, (int)((beat + 0.2f) * 8), 8]),
                    Start = random.NextSingle() * 200 - 100,
                    End = random.NextSingle() * 200 - 100,
                    EasingType = 1
                });
                
                eventLayer.MoveYEvents.Add(new Event
                {
                    StartTime = new Beat([0, (int)(beat * 8), 8]),
                    EndTime = new Beat([0, (int)((beat + 0.2f) * 8), 8]),
                    Start = random.NextSingle() * 200 - 100,
                    End = random.NextSingle() * 200 - 100,
                    EasingType = 1
                });
                
                eventLayer.RotateEvents.Add(new Event
                {
                    StartTime = new Beat([0, (int)(beat * 8), 8]),
                    EndTime = new Beat([0, (int)((beat + 0.2f) * 8), 8]),
                    Start = random.NextSingle() * 360,
                    End = random.NextSingle() * 360,
                    EasingType = 1
                });
            }
            
            judgeLine.EventLayers.Add(eventLayer);
            judgeLineList.Add(judgeLine);
        }
        
        return judgeLineList;
    }
    
    private static void BenchmarkEventListPerformance(EventList eventList)
    {
        Console.WriteLine("\n--- EventList Performance Test ---");
        
        const int iterations = 100000;
        var testBeats = new float[1000];
        var random = new Random(42);
        
        // Generate test beats
        for (int i = 0; i < testBeats.Length; i++)
        {
            testBeats[i] = random.NextSingle() * 5000; // 0 to 5000 beats
        }
        
        // Warm up
        for (int i = 0; i < 1000; i++)
        {
            eventList.GetValueAtBeat(testBeats[i % testBeats.Length]);
        }
        
        // Benchmark GetValueAtBeat
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            eventList.GetValueAtBeat(testBeats[i % testBeats.Length]);
        }
        stopwatch.Stop();
        
        Console.WriteLine($"GetValueAtBeat: {iterations:N0} calls in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average: {stopwatch.ElapsedTicks / (double)iterations:F2} ticks per call");
        
        // Clear cache and test HasEventAtBeat
        var hasEventStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            eventList.HasEventAtBeat(testBeats[i % testBeats.Length]);
        }
        hasEventStopwatch.Stop();
        
        Console.WriteLine($"HasEventAtBeat: {iterations:N0} calls in {hasEventStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average: {hasEventStopwatch.ElapsedTicks / (double)iterations:F2} ticks per call");
    }
    
    private static void BenchmarkJudgeLineListPerformance(JudgeLineList judgeLineList)
    {
        Console.WriteLine("\n--- JudgeLineList Performance Test ---");
        
        const int iterations = 50000;
        var random = new Random(42);
        
        // Warm up
        for (int i = 0; i < 1000; i++)
        {
            int lineIndex = i % judgeLineList.Count;
            float beat = random.NextSingle() * 100;
            judgeLineList.GetLinePosition(lineIndex, beat);
        }
        
        // Benchmark GetLinePosition
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            int lineIndex = i % judgeLineList.Count;
            float beat = random.NextSingle() * 100;
            judgeLineList.GetLinePosition(lineIndex, beat);
        }
        stopwatch.Stop();
        
        Console.WriteLine($"GetLinePosition: {iterations:N0} calls in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average: {stopwatch.ElapsedTicks / (double)iterations:F2} ticks per call");
        
        // Test FatherAndTheLineHasXyEvent
        var hasEventStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            int lineIndex = i % judgeLineList.Count;
            float beat = random.NextSingle() * 100;
            judgeLineList.FatherAndTheLineHasXyEvent(lineIndex, beat);
        }
        hasEventStopwatch.Stop();
        
        Console.WriteLine($"FatherAndTheLineHasXyEvent: {iterations:N0} calls in {hasEventStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average: {hasEventStopwatch.ElapsedTicks / (double)iterations:F2} ticks per call");
    }
    
    private static void BenchmarkEasingPerformance()
    {
        Console.WriteLine("\n--- Easing Performance Test ---");
        
        const int iterations = 100000;
        var random = new Random(42);
        
        // Generate test parameters
        var testParams = new (int easingType, double start, double end, double t)[1000];
        for (int i = 0; i < testParams.Length; i++)
        {
            testParams[i] = (
                random.Next(1, 29),
                random.NextDouble() * 200 - 100,
                random.NextDouble() * 200 - 100,
                random.NextDouble()
            );
        }
        
        // Warm up
        for (int i = 0; i < 1000; i++)
        {
            var param = testParams[i % testParams.Length];
            Easing.Evaluate(param.easingType, param.start, param.end, param.t);
        }
        
        // Clear cache to test performance without cache
        Easing.ClearCache();
        
        // Benchmark first run (no cache)
        var noCacheStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var param = testParams[i % testParams.Length];
            Easing.Evaluate(param.easingType, param.start, param.end, param.t);
        }
        noCacheStopwatch.Stop();
        
        Console.WriteLine($"Easing (no cache): {iterations:N0} calls in {noCacheStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average: {noCacheStopwatch.ElapsedTicks / (double)iterations:F2} ticks per call");
        
        // Benchmark second run (with cache)
        var cachedStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var param = testParams[i % testParams.Length];
            Easing.Evaluate(param.easingType, param.start, param.end, param.t);
        }
        cachedStopwatch.Stop();
        
        Console.WriteLine($"Easing (cached): {iterations:N0} calls in {cachedStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average: {cachedStopwatch.ElapsedTicks / (double)iterations:F2} ticks per call");
        Console.WriteLine($"Cache speedup: {(double)noCacheStopwatch.ElapsedTicks / cachedStopwatch.ElapsedTicks:F2}x");
    }
}