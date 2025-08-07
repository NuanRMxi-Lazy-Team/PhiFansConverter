using static PhiFansConverter.RePhiEditObject;

namespace PhiFansConverter;

/// <summary>
/// Simple functional tests to ensure optimizations don't break existing functionality
/// </summary>
public static class FunctionalTests
{
    public static void RunTests()
    {
        Console.WriteLine("=== PhiFansConverter Functional Tests ===");
        
        bool allTestsPassed = true;
        
        allTestsPassed &= TestEventListFunctionality();
        allTestsPassed &= TestJudgeLineListFunctionality();
        allTestsPassed &= TestEasingFunctionality();
        allTestsPassed &= TestEventLayersCaching();
        
        if (allTestsPassed)
        {
            Console.WriteLine("✅ All functional tests passed!");
        }
        else
        {
            Console.WriteLine("❌ Some tests failed!");
        }
        
        Console.WriteLine("=== Functional Tests Complete ===");
    }
    
    private static bool TestEventListFunctionality()
    {
        Console.WriteLine("\n--- Testing EventList Functionality ---");
        
        try
        {
            var eventList = new EventList();
            
            // Test empty list
            if (eventList.GetValueAtBeat(0.5f) != 0)
            {
                Console.WriteLine("❌ Empty EventList should return 0");
                return false;
            }
            
            // Add test events
            eventList.Add(new Event
            {
                StartTime = new Beat([0, 0, 1]),     // Beat 0
                EndTime = new Beat([1, 0, 1]),       // Beat 1
                Start = 10,
                End = 20,
                EasingType = 1
            });
            
            eventList.Add(new Event
            {
                StartTime = new Beat([2, 0, 1]),     // Beat 2
                EndTime = new Beat([3, 0, 1]),       // Beat 3
                Start = 30,
                End = 40,
                EasingType = 1
            });
            
            // Test value within first event
            float value = eventList.GetValueAtBeat(0.5f);
            if (Math.Abs(value - 15) > 0.1f) // Should be halfway between 10 and 20
            {
                Console.WriteLine($"❌ Expected ~15 at beat 0.5, got {value}");
                return false;
            }
            
            // Test value between events (should return end of previous event)
            value = eventList.GetValueAtBeat(1.5f);
            if (value != 20)
            {
                Console.WriteLine($"❌ Expected 20 at beat 1.5, got {value}");
                return false;
            }
            
            // Test HasEventAtBeat
            if (!eventList.HasEventAtBeat(0.5f))
            {
                Console.WriteLine("❌ Should have event at beat 0.5");
                return false;
            }
            
            if (eventList.HasEventAtBeat(1.5f))
            {
                Console.WriteLine("❌ Should not have event at beat 1.5");
                return false;
            }
            
            // Test caching - should work the same (test with same beat as before)
            float cachedValue = eventList.GetValueAtBeat(0.5f);
            if (Math.Abs(cachedValue - 15) > 0.1f) // Should still be ~15
            {
                Console.WriteLine($"❌ Cached value differs: {cachedValue} vs 15");
                return false;
            }
            
            Console.WriteLine("✅ EventList functionality tests passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ EventList test failed: {ex.Message}");
            return false;
        }
    }
    
    private static bool TestJudgeLineListFunctionality()
    {
        Console.WriteLine("\n--- Testing JudgeLineList Functionality ---");
        
        try
        {
            var judgeLineList = new JudgeLineList();
            
            // Create parent line
            var parentLine = new JudgeLine
            {
                Father = -1,
                EventLayers = new EventLayers()
            };
            
            var parentEventLayer = new EventLayer();
            parentEventLayer.MoveXEvents.Add(new Event
            {
                StartTime = new Beat([0, 0, 1]),
                EndTime = new Beat([1, 0, 1]),
                Start = 100,
                End = 200,
                EasingType = 1
            });
            
            parentEventLayer.MoveYEvents.Add(new Event
            {
                StartTime = new Beat([0, 0, 1]),
                EndTime = new Beat([1, 0, 1]),
                Start = 50,
                End = 100,
                EasingType = 1
            });
            
            parentLine.EventLayers.Add(parentEventLayer);
            judgeLineList.Add(parentLine);
            
            // Create child line
            var childLine = new JudgeLine
            {
                Father = 0, // Parent is index 0
                EventLayers = new EventLayers()
            };
            
            var childEventLayer = new EventLayer();
            childEventLayer.MoveXEvents.Add(new Event
            {
                StartTime = new Beat([0, 0, 1]),
                EndTime = new Beat([1, 0, 1]),
                Start = 10,
                End = 20,
                EasingType = 1
            });
            
            childEventLayer.MoveYEvents.Add(new Event
            {
                StartTime = new Beat([0, 0, 1]),
                EndTime = new Beat([1, 0, 1]),
                Start = 5,
                End = 10,
                EasingType = 1
            });
            
            childLine.EventLayers.Add(childEventLayer);
            judgeLineList.Add(childLine);
            
            // Test parent position
            var parentPos = judgeLineList.GetLinePosition(0, 0.5f);
            if (Math.Abs(parentPos.Item1 - 150) > 1 || Math.Abs(parentPos.Item2 - 75) > 1)
            {
                Console.WriteLine($"❌ Parent position wrong: {parentPos.Item1}, {parentPos.Item2}");
                return false;
            }
            
            // Test child position (should include parent position)
            var childPos = judgeLineList.GetLinePosition(1, 0.5f);
            // Child should be at parent position + child offset
            float expectedX = 150 + 15; // Parent X + Child X offset
            float expectedY = 75 + 7.5f;  // Parent Y + Child Y offset
            
            if (Math.Abs(childPos.Item1 - expectedX) > 2 || Math.Abs(childPos.Item2 - expectedY) > 2)
            {
                Console.WriteLine($"❌ Child position wrong: {childPos.Item1}, {childPos.Item2}, expected: {expectedX}, {expectedY}");
                return false;
            }
            
            // Test FatherAndTheLineHasXyEvent
            if (!judgeLineList.FatherAndTheLineHasXyEvent(1, 0.5f))
            {
                Console.WriteLine("❌ Should detect XY event in child or parent");
                return false;
            }
            
            // Test caching - positions should be same on second call
            var cachedPos = judgeLineList.GetLinePosition(1, 0.5f);
            if (Math.Abs(cachedPos.Item1 - childPos.Item1) > 0.01f || 
                Math.Abs(cachedPos.Item2 - childPos.Item2) > 0.01f)
            {
                Console.WriteLine($"❌ Cached position differs");
                return false;
            }
            
            Console.WriteLine("✅ JudgeLineList functionality tests passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ JudgeLineList test failed: {ex.Message}");
            return false;
        }
    }
    
    private static bool TestEasingFunctionality()
    {
        Console.WriteLine("\n--- Testing Easing Functionality ---");
        
        try
        {
            // Test linear easing
            double result = Easing.Evaluate(1, 0, 100, 0.5);
            if (Math.Abs(result - 0.5) > 0.01)
            {
                Console.WriteLine($"❌ Linear easing failed: {result}");
                return false;
            }
            
            // Test caching - same parameters should return same result
            double cachedResult = Easing.Evaluate(1, 0, 100, 0.5);
            if (Math.Abs(result - cachedResult) > 0.01)
            {
                Console.WriteLine($"❌ Easing cache inconsistent: {result} vs {cachedResult}");
                return false;
            }
            
            // Test different easing types work
            result = Easing.Evaluate(2, 0, 100, 0.5); // EaseOutSine
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                Console.WriteLine($"❌ EaseOutSine returned invalid result: {result}");
                return false;
            }
            
            Console.WriteLine("✅ Easing functionality tests passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Easing test failed: {ex.Message}");
            return false;
        }
    }
    
    private static bool TestEventLayersCaching()
    {
        Console.WriteLine("\n--- Testing EventLayers Caching ---");
        
        try
        {
            var eventLayers = new EventLayers();
            var eventLayer = new EventLayer();
            
            // Add test event
            eventLayer.MoveXEvents.Add(new Event
            {
                StartTime = new Beat([0, 0, 1]),
                EndTime = new Beat([1, 0, 1]),
                Start = 10,
                End = 20,
                EasingType = 1
            });
            
            eventLayers.Add(eventLayer);
            
            // Test GetXAtBeat
            float x1 = eventLayers.GetXAtBeat(0.5f);
            float x2 = eventLayers.GetXAtBeat(0.5f); // Should use cache
            
            if (Math.Abs(x1 - x2) > 0.01f)
            {
                Console.WriteLine($"❌ EventLayers cache inconsistent: {x1} vs {x2}");
                return false;
            }
            
            // Test HasXEventAtBeat
            bool hasEvent1 = eventLayers.HasXEventAtBeat(0.5f);
            bool hasEvent2 = eventLayers.HasXEventAtBeat(0.5f); // Should use cache
            
            if (hasEvent1 != hasEvent2)
            {
                Console.WriteLine($"❌ EventLayers HasEvent cache inconsistent");
                return false;
            }
            
            Console.WriteLine("✅ EventLayers caching tests passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ EventLayers test failed: {ex.Message}");
            return false;
        }
    }
}