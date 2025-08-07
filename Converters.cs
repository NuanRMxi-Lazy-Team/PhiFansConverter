using System.Diagnostics.Contracts;
using static PhiFansConverter.RePhiEditObject;

namespace PhiFansConverter;

public static class Converters
{
    private const float Precision = 1f / 8f;
    private const float SpeedRatio = 6f;

    public static RpeChart PhiFansConverter(PhiFansChart chart)
    {
        RpeChart rpeChart = new();
        rpeChart.Meta.RpeVersion = 150;
        rpeChart.Meta.Illustration = chart.Info.Illustration;
        rpeChart.Meta.Composer = chart.Info.Artist;
        rpeChart.Meta.Charter = chart.Info.Designer;
        rpeChart.Meta.Name = chart.Info.Name;
        rpeChart.Meta.Level = chart.Info.Level;
        rpeChart.Meta.Offset = chart.Offset;
        
        // Pre-allocate collections with expected capacity
        rpeChart.JudgeLineList = new JudgeLineList();
        rpeChart.BpmList = new List<RpeBpm>(chart.Bpm.Count);
        
        foreach (var bpmItem in chart.Bpm)
        {
            rpeChart.BpmList.Add(new RpeBpm
            {
                StartTime = new Beat(bpmItem.Beat),
                Bpm = bpmItem.Bpm
            });
        }

        // Pre-allocate judge line list
        rpeChart.JudgeLineList.Capacity = chart.Lines.Count;

        foreach (var lineItem in chart.Lines)
        {
            JudgeLine judgeLine = new()
            {
                Notes = new List<Note>(lineItem.Notes.Count) // Pre-allocate notes
            };
            
            foreach (var note in lineItem.Notes)
            {
                Note rpeNote = new(true)
                {
                    Type = note.Type switch
                    {
                        2 => 4, // Drag
                        4 => 3, // Flick
                        3 => 2, // Hold
                        _ => 1 // Default to Tap
                    },
                    StartTime = new Beat(note.Beat),
                    PositionX = PhiFans.TransformX(note.PositionX),
                    SpeedMultiplier = note.Speed,
                    Above = note.IsAbove ? 1 : 0,
                    Size = 1f,
                    EndTime = new Beat(note.HoldEndBeat)
                };
                judgeLine.Notes.Add(rpeNote);
            }

            judgeLine.EventLayers = [new EventLayer()];
            
            // Pre-allocate event lists
            var eventLayer = judgeLine.EventLayers[0];
            eventLayer.SpeedEvents = new EventList();
            eventLayer.MoveXEvents = new EventList();
            eventLayer.MoveYEvents = new EventList();
            eventLayer.AlphaEvents = new EventList();
            eventLayer.RotateEvents = new EventList();
            
            // Speed events processing with pre-allocation
            eventLayer.SpeedEvents.Capacity = lineItem.Props.Speed.Count;
            for (var i = 0; i < lineItem.Props.Speed.Count; i++)
            {
                var item = lineItem.Props.Speed[i];
                var value = PhiFans.TransformX(item.Value);
                var start = item.Continuous ? lineItem.Props.Speed[i - 1].Value : value;
                Event eventItem = new()
                {
                    StartTime = item.Continuous
                        ? new Beat(lineItem.Props.Speed[i - 1].Beat)
                        : new Beat(item.Beat),
                    EndTime = new Beat(item.Beat),
                    Start = start * SpeedRatio,
                    End = item.Value * SpeedRatio,
                    EasingType = 1
                };
                eventLayer.SpeedEvents.Add(eventItem);
            }

            // X events processing with pre-allocation
            eventLayer.MoveXEvents.Capacity = lineItem.Props.PositionX.Count;
            for (var i = 0; i < lineItem.Props.PositionX.Count; i++)
            {
                var item = lineItem.Props.PositionX[i];
                var value = PhiFans.TransformX(item.Value);

                Event eventItem = new()
                {
                    StartTime = item.Continuous
                        ? new Beat(lineItem.Props.PositionX[i - 1].Beat)
                        : new Beat(item.Beat),
                    EndTime = new Beat(item.Beat),
                    Start = item.Continuous ? PhiFans.TransformX(lineItem.Props.PositionX[i - 1].Value) : value,
                    End = value,
                    EasingType = RePhiEdit.EasingNumber(item.Easing)
                };

                eventLayer.MoveXEvents.Add(eventItem);
            }

            // Y events processing with pre-allocation
            eventLayer.MoveYEvents.Capacity = lineItem.Props.PositionY.Count;
            for (var i = 0; i < lineItem.Props.PositionY.Count; i++)
            {
                var item = lineItem.Props.PositionY[i];
                var value = PhiFans.TransformY(item.Value);
                Event eventItem = new()
                {
                    StartTime = item.Continuous
                        ? new Beat(lineItem.Props.PositionY[i - 1].Beat)
                        : new Beat(item.Beat),
                    EndTime = new Beat(item.Beat),
                    Start = item.Continuous ? PhiFans.TransformY(lineItem.Props.PositionY[i - 1].Value) : value,
                    End = value,
                    EasingType = RePhiEdit.EasingNumber(item.Easing)
                };
                eventLayer.MoveYEvents.Add(eventItem);
            }

            // alpha
            eventLayer.AlphaEvents.Capacity = lineItem.Props.Alpha.Count;
            for (var i = 0; i < lineItem.Props.Alpha.Count; i++)
            {
                var item = lineItem.Props.Alpha[i];
                Event eventItem = new()
                {
                    StartTime = lineItem.Props.Alpha[i].Continuous
                        ? new Beat(lineItem.Props.Alpha[i - 1].Beat)
                        : new Beat(item.Beat),
                    EndTime = new Beat(item.Beat),
                    Start = item.Continuous ? lineItem.Props.Alpha[i - 1].Value : item.Value,
                    End = item.Value,
                    EasingType = RePhiEdit.EasingNumber(item.Easing)
                };

                eventLayer.AlphaEvents.Add(eventItem);
            }

            // rotate
            eventLayer.RotateEvents.Capacity = lineItem.Props.Rotate.Count;
            for (var i = 0; i < lineItem.Props.Rotate.Count; i++)
            {
                var item = lineItem.Props.Rotate[i];
                Event eventItem = new()
                {
                    StartTime = item.Continuous
                        ? new Beat(lineItem.Props.Rotate[i - 1].Beat)
                        : new Beat(item.Beat),
                    EndTime = new Beat(item.Beat),
                    Start = item.Continuous ? lineItem.Props.Rotate[i - 1].Value : item.Value,
                    End = item.Value,
                    EasingType = RePhiEdit.EasingNumber(item.Easing)
                };

                eventLayer.RotateEvents.Add(eventItem);
            }
            
            // 检查每种事件的前两个事件开始数值是否相同，如果相同，删除第一个
            // Check if the first two events of each event type have the same start value, if so, delete the first one
            void CleanupRedundantEvents<T>(List<T> events) where T : Event
            {
                if (events.Count >= 2 && Math.Abs(events[0].StartTime - events[1].StartTime) < float.Epsilon)
                {
                    events.RemoveAt(0);
                }
                else if (events.Count > 0)
                {
                    events[0].EndTime = new Beat([1, 0, 1]);
                }
            }
            
            // Apply cleanup to each event type
            if (eventLayer.AlphaEvents.Count > 0)
                CleanupRedundantEvents(eventLayer.AlphaEvents);
            if (eventLayer.MoveXEvents.Count > 0)
                CleanupRedundantEvents(eventLayer.MoveXEvents);
            if (eventLayer.MoveYEvents.Count > 0)
                CleanupRedundantEvents(eventLayer.MoveYEvents);
            if (eventLayer.RotateEvents.Count > 0)
                CleanupRedundantEvents(eventLayer.RotateEvents);
            if (eventLayer.SpeedEvents.Count > 0)
                CleanupRedundantEvents(eventLayer.SpeedEvents);

            rpeChart.JudgeLineList.Add(judgeLine);
        }

        return rpeChart;
    }

    public static async Task<PhiFansChart> RePhiEditConverter(RpeChart chart)
    {
        var phiFansChart = new PhiFansChart();
        phiFansChart.Info = new()
        {
            Artist = chart.Meta.Composer,
            Designer = chart.Meta.Charter,
            Illustration = chart.Meta.Illustration,
            Level = chart.Meta.Level,
            Name = chart.Meta.Name
        };
        phiFansChart.Offset = chart.Meta.Offset;
        
        // Pre-allocate BPM list
        phiFansChart.Bpm = new List<PhiFansObject.BpmItem>(chart.BpmList.Count);
        foreach (var bpm in chart.BpmList)
        {
            phiFansChart.Bpm.Add(new PhiFansObject.BpmItem
            {
                Beat = bpm.StartTime,
                Bpm = bpm.Bpm
            });
        }

        // 提前删除所有判定线的空事件层
        // Delete all empty event layers in advance
        chart.JudgeLineList.ForEach(judgeline => judgeline.EventLayers.RemoveAll(layer => layer is null));
        
        // Pre-allocate judge lines
        phiFansChart.Lines = new List<PhiFansObject.LineItem>(chart.JudgeLineList.Count);
        
        var tasks = new List<Task<PhiFansObject.LineItem>>();
        foreach (var judgeline in chart.JudgeLineList)
        {
            tasks.Add(Task.Run(() => {
                if (judgeline.Texture != "line.png")
                {
                    Console.WriteLine("检测到了不支持的判定线纹理：" + judgeline.Texture);
                    // English
                    Console.WriteLine("Detected unsupported judge line texture: " + judgeline.Texture);
                }

                var lineItem = new PhiFansObject.LineItem()
                {
                    Props = new PhiFansObject.PropsObject(),
                    Notes = new List<PhiFansObject.Note>(judgeline.Notes.Count) // Pre-allocate notes
                };
            
                foreach (var note in judgeline.Notes)
                {
                    var phiNote = new PhiFansObject.Note
                    {
                        Beat = note.StartTime,
                        PositionX = RePhiEdit.TransformX(note.PositionX),
                        Speed = note.SpeedMultiplier,
                        IsAbove = note.Above == 1,
                        HoldEndBeat = note.EndTime,
                        Type = note.Type switch
                        {
                            4 => 2,
                            3 => 4,
                            2 => 3,
                            _ => 1
                        }
                    };

                    if (note.IsFake != 0)
                    {
                        Console.WriteLine("检查到了不支持的Fake属性：" + note.IsFake);
                        // English
                        Console.WriteLine("Detected unsupported Fake attribute: " + note.IsFake);
                    }

                    lineItem.Notes.Add(phiNote);
                }

                if (judgeline.EventLayers.Count > 1 || judgeline.Father != -1)
                {
                    L10n.Print("RePhiEditFeatureWarn",L10n.GetString("NestedParentChildLine"));
                    L10n.Print("RePhiEditFeatureWarn",L10n.GetString("Multilayer"));
                    // 求所有事件层级中，最后一个事件的结束时间
                    // Get the end time of the last event in all event layers
                    float maxBeat = judgeline.EventLayers.LastEventEndBeat();

                    // Pre-allocate event lists based on expected size
                    int estimatedEventCount = (int)Math.Ceiling(maxBeat / Precision);
                
                    // Use object pools for temporary lists to reduce allocations
                    var tempAlphaEvents = ObjectPools.PhiFansEventItemListPool.Rent();
                    var tempRotateEvents = ObjectPools.PhiFansEventItemListPool.Rent();
                    var tempPositionXEvents = ObjectPools.PhiFansEventItemListPool.Rent();
                    var tempPositionYEvents = ObjectPools.PhiFansEventItemListPool.Rent();
                
                    try
                    {
                        // 逐拍遍历
                        for (float beat = 0; beat < maxBeat; beat += Precision)
                        {
                            if (judgeline.EventLayers.HasAlphaEventAtBeat(beat))
                            {
                                var phiEventFrame = new PhiFansObject.EventItem
                                {
                                    Beat = BeatConverter.RestoreArray(beat),
                                    Value = judgeline.EventLayers.GetAlphaAtBeat(beat),
                                    Continuous = judgeline.EventLayers.HasAlphaEventAtBeat(beat - Precision),
                                    Easing = 0
                                };
                                tempAlphaEvents.Add(phiEventFrame);
                            }

                            // Rotate
                            if (judgeline.EventLayers.HasAngleEventAtBeat(beat))
                            {
                                var phiEventFrame = new PhiFansObject.EventItem
                                {
                                    Beat = BeatConverter.RestoreArray(beat),
                                    Value = judgeline.EventLayers.GetAngleAtBeat(beat),
                                    Continuous = judgeline.EventLayers.HasAngleEventAtBeat(beat - Precision),
                                    Easing = 0
                                };
                                tempRotateEvents.Add(phiEventFrame);
                            }

                            // X & Y
                            var lineIndex = chart.JudgeLineList.IndexOf(judgeline);
                            var hasEvent = chart.JudgeLineList.FatherAndTheLineHasXyEvent(lineIndex, beat);
                            var lastHasEvent = chart.JudgeLineList.FatherAndTheLineHasXyEvent(lineIndex, beat - Precision);
                            if (hasEvent)
                            {
                                // 获取这个判定线在判定线列表的索引
                                // Get the index of this judge line in the judge line list

                                // 调用GetLinePosition方法获取判定线的位置，返回x, y
                                // Call the GetLinePosition method to get the position of the judge line, returning x, y
                                var position = chart.JudgeLineList.GetLinePosition(lineIndex, beat);
                                // X
                                var phiEventFrameX = new PhiFansObject.EventItem
                                {
                                    Beat = BeatConverter.RestoreArray(beat),
                                    Value = RePhiEdit.TransformX(position.Item1),
                                    Continuous = lastHasEvent,
                                    Easing = 0
                                };
                                // Y
                                var phiEventFrameY = new PhiFansObject.EventItem
                                {
                                    Beat = BeatConverter.RestoreArray(beat),
                                    Value = RePhiEdit.TransformY(position.Item2),
                                    Continuous = lastHasEvent,
                                    Easing = 0
                                };
                                tempPositionXEvents.Add(phiEventFrameX);
                                tempPositionYEvents.Add(phiEventFrameY);
                            }
                        }
                    
                        // Copy to final lists
                        lineItem.Props.Alpha = new List<PhiFansObject.EventItem>(tempAlphaEvents);
                        lineItem.Props.Rotate = new List<PhiFansObject.EventItem>(tempRotateEvents);
                        lineItem.Props.PositionX = new List<PhiFansObject.EventItem>(tempPositionXEvents);
                        lineItem.Props.PositionY = new List<PhiFansObject.EventItem>(tempPositionYEvents);
                    }
                    finally
                    {
                        // Return objects to pool
                        ObjectPools.PhiFansEventItemListPool.Return(tempAlphaEvents);
                        ObjectPools.PhiFansEventItemListPool.Return(tempRotateEvents);
                        ObjectPools.PhiFansEventItemListPool.Return(tempPositionXEvents);
                        ObjectPools.PhiFansEventItemListPool.Return(tempPositionYEvents);
                    }

                    // Speed - pre-allocate based on actual events
                    lineItem.Props.Speed = new List<PhiFansObject.EventItem>(judgeline.EventLayers[0].SpeedEvents.Count * 2);
                    foreach (var eventItem in judgeline.EventLayers[0].SpeedEvents)
                    {
                        if (Math.Abs(eventItem.Start - eventItem.End) < float.Epsilon)
                        {
                            var phiEventFrame = new PhiFansObject.EventItem
                            {
                                Beat = eventItem.StartTime,
                                Value = eventItem.Start / SpeedRatio,
                                Continuous = false,
                                Easing = 0
                            };
                            lineItem.Props.Speed.Add(phiEventFrame);
                            continue;
                        }

                        var phiEventStart = new PhiFansObject.EventItem
                        {
                            Beat = eventItem.StartTime,
                            Value = eventItem.Start / SpeedRatio,
                            Continuous = false,
                            Easing = 0
                        };
                        var phiEventEnd = new PhiFansObject.EventItem
                        {
                            Beat = eventItem.EndTime,
                            Value = eventItem.End / SpeedRatio,
                            Continuous = true,
                            Easing = 0
                        };
                        lineItem.Props.Speed.Add(phiEventStart);
                        lineItem.Props.Speed.Add(phiEventEnd);
                    }

                    return lineItem;
                }

                // Pre-allocate event lists for single layer processing
                var totalEvents = judgeline.EventLayers.Sum(layer => 
                    layer.AlphaEvents.Count + layer.MoveXEvents.Count + 
                    layer.MoveYEvents.Count + layer.RotateEvents.Count + layer.SpeedEvents.Count);
            
                lineItem.Props.Alpha = new List<PhiFansObject.EventItem>();
                lineItem.Props.PositionX = new List<PhiFansObject.EventItem>();
                lineItem.Props.PositionY = new List<PhiFansObject.EventItem>();
                lineItem.Props.Rotate = new List<PhiFansObject.EventItem>();
                lineItem.Props.Speed = new List<PhiFansObject.EventItem>();

                foreach (var layer in judgeline.EventLayers)
                {
                    foreach (var eventItem in layer.AlphaEvents)
                    {
                        if (Math.Abs(eventItem.Start - eventItem.End) < float.Epsilon)
                        {
                            var phiEventFrame = new PhiFansObject.EventItem
                            {
                                Beat = eventItem.StartTime,
                                Value = eventItem.Start,
                                Continuous = false,
                                Easing = 0
                            };
                            lineItem.Props.Alpha.Add(phiEventFrame);
                            continue;
                        }

                        var phiEventStart = new PhiFansObject.EventItem
                        {
                            Beat = eventItem.StartTime,
                            Value = eventItem.Start,
                            Continuous = false,
                            Easing = PhiFans.EasingNumber(eventItem.EasingType)
                        };
                        var phiEventEnd = new PhiFansObject.EventItem
                        {
                            Beat = eventItem.EndTime,
                            Value = eventItem.End,
                            Continuous = true,
                            Easing = PhiFans.EasingNumber(eventItem.EasingType)
                        };
                        lineItem.Props.Alpha.Add(phiEventStart);
                        lineItem.Props.Alpha.Add(phiEventEnd);
                    }

                    foreach (var eventItem in layer.MoveXEvents)
                    {
                        if (Math.Abs(eventItem.Start - eventItem.End) < float.Epsilon)
                        {
                            var phiEventFrame = new PhiFansObject.EventItem
                            {
                                Beat = eventItem.StartTime,
                                Value = RePhiEdit.TransformX(eventItem.Start),
                                Continuous = false,
                                Easing = 0
                            };
                            lineItem.Props.PositionX.Add(phiEventFrame);
                            continue;
                        }

                        var phiEventStart = new PhiFansObject.EventItem
                        {
                            Beat = eventItem.StartTime,
                            Value = RePhiEdit.TransformX(eventItem.Start),
                            Continuous = false,
                            Easing = PhiFans.EasingNumber(eventItem.EasingType)
                        };
                        var phiEventEnd = new PhiFansObject.EventItem
                        {
                            Beat = eventItem.EndTime,
                            Value = RePhiEdit.TransformX(eventItem.End),
                            Continuous = true,
                            Easing = PhiFans.EasingNumber(eventItem.EasingType)
                        };
                        lineItem.Props.PositionX.Add(phiEventStart);
                        lineItem.Props.PositionX.Add(phiEventEnd);
                    }

                    foreach (var eventItem in layer.MoveYEvents)
                    {
                        if (Math.Abs(eventItem.Start - eventItem.End) < float.Epsilon)
                        {
                            var phiEventFrame = new PhiFansObject.EventItem
                            {
                                Beat = eventItem.StartTime,
                                Value = RePhiEdit.TransformY(eventItem.Start),
                                Continuous = false,
                                Easing = 0
                            };
                            lineItem.Props.PositionY.Add(phiEventFrame);
                            continue;
                        }

                        var phiEventStart = new PhiFansObject.EventItem
                        {
                            Beat = eventItem.StartTime,
                            Value = RePhiEdit.TransformY(eventItem.Start),
                            Continuous = false,
                            Easing = PhiFans.EasingNumber(eventItem.EasingType)
                        };
                        var phiEventEnd = new PhiFansObject.EventItem
                        {
                            Beat = eventItem.EndTime,
                            Value = RePhiEdit.TransformY(eventItem.End),
                            Continuous = true,
                            Easing = PhiFans.EasingNumber(eventItem.EasingType)
                        };
                        lineItem.Props.PositionY.Add(phiEventStart);
                        lineItem.Props.PositionY.Add(phiEventEnd);
                    }

                    foreach (var eventItem in layer.RotateEvents)
                    {
                        if (Math.Abs(eventItem.Start - eventItem.End) < float.Epsilon)
                        {
                            var phiEventFrame = new PhiFansObject.EventItem
                            {
                                Beat = eventItem.StartTime,
                                Value = eventItem.Start,
                                Continuous = false,
                                Easing = 0
                            };
                            lineItem.Props.Rotate.Add(phiEventFrame);
                            continue;
                        }

                        var phiEventStart = new PhiFansObject.EventItem
                        {
                            Beat = eventItem.StartTime,
                            Value = eventItem.Start,
                            Continuous = false,
                            Easing = PhiFans.EasingNumber(eventItem.EasingType)
                        };
                        var phiEventEnd = new PhiFansObject.EventItem
                        {
                            Beat = eventItem.EndTime,
                            Value = eventItem.End,
                            Continuous = true,
                            Easing = PhiFans.EasingNumber(eventItem.EasingType)
                        };
                        lineItem.Props.Rotate.Add(phiEventStart);
                        lineItem.Props.Rotate.Add(phiEventEnd);
                    }

                    foreach (var eventItem in layer.SpeedEvents)
                    {
                        if (Math.Abs(eventItem.Start - eventItem.End) < float.Epsilon)
                        {
                            var phiEventFrame = new PhiFansObject.EventItem
                            {
                                Beat = eventItem.StartTime,
                                Value = eventItem.Start / SpeedRatio,
                                Continuous = false,
                                Easing = 0
                            };
                            lineItem.Props.Speed.Add(phiEventFrame);
                            continue;
                        }

                        var phiEventStart = new PhiFansObject.EventItem
                        {
                            Beat = eventItem.StartTime,
                            Value = eventItem.Start / SpeedRatio,
                            Continuous = false,
                            Easing = 0
                        };
                        var phiEventEnd = new PhiFansObject.EventItem
                        {
                            Beat = eventItem.EndTime,
                            Value = eventItem.End / SpeedRatio,
                            Continuous = true,
                            Easing = 0
                        };
                        lineItem.Props.Speed.Add(phiEventStart);
                        lineItem.Props.Speed.Add(phiEventEnd);
                    }
                }

                return lineItem;
            }));
        }

        var results = await Task.WhenAll(tasks);
        phiFansChart.Lines.AddRange(results);

        return phiFansChart;
    }

    private static class PhiFans
    {
        /// <summary>
        /// Transform PhiFans X to RePhiEdit X
        /// </summary>
        /// <param name="x">PhiFans X</param>
        /// <returns>RePhiEdit X</returns>
        [Pure]
        public static float TransformX(float x)
        {
            // 转换目标是RPE坐标系，RPE坐标系x轴-675 ~ 675
            // PhiFans坐标系x轴-100 ~ 100
            return x * 6.75f;
        }

        /// <summary>
        /// Transform PhiFans Y to RePhiEdit Y
        /// </summary>
        /// <param name="y">PhiFans Y</param>
        /// <returns>RePhiEdit Y</returns>
        [Pure]
        public static float TransformY(float y)
        {
            // 转换目标是RPE坐标系，RPE坐标系y轴-450 ~ 450
            // PhiFans坐标系y轴-100 ~ 100
            return y * 4.5f;
        }

        /// <summary>
        /// Convert RePhiEdit easing number to PhiFans easing number
        /// </summary>
        /// <param name="rpeEasing">RePhiEdit easing number</param>
        /// <returns>PhiFans easing number</returns>
        [Pure]
        public static int EasingNumber(int rpeEasing)
        {
            // 返回PhiFans的缓动编号，输入RPE的缓动编号，也就是左侧的编号转换为右侧的编号
            int result = rpeEasing switch
            {
                1 => 0,
                2 => 2,
                3 => 1,
                4 => 5,
                5 => 4,
                6 => 3,
                7 => 6,
                8 => 8,
                9 => 7,
                10 => 11,
                11 => 10,
                12 => 9,
                13 => 12,
                14 => 14,
                15 => 13,
                16 => 17,
                17 => 16,
                18 => 20,
                19 => 19,
                20 => 23,
                21 => 22,
                22 => 21,
                23 => 24,
                24 => 26,
                25 => 25,
                26 => 29,
                27 => 28,
                28 => 30,
                29 => 27,
                _ => 0
            };
            return result;
        }
    }

    private static class RePhiEdit
    {
        /// <summary>
        /// Transform RePhiEdit X to PhiFans X
        /// </summary>
        /// <param name="x">RePhiEdit X</param>
        /// <returns>PhiFans X</returns>
        [Pure]
        public static float TransformX(float x)
        {
            // 转换目标是PhiFans坐标系，PhiFans坐标系x轴-100 ~ 100
            // RePhiEdit坐标系x轴-675 ~ 675
            return x / 6.75f;
        }

        /// <summary>
        /// Transform RePhiEdit Y to PhiFans Y
        /// </summary>
        /// <param name="y">RePhiEdit Y</param>
        /// <returns>PhiFans Y</returns>
        [Pure]
        public static float TransformY(float y)
        {
            // 转换目标是PhiFans坐标系，PhiFans坐标系y轴-100 ~ 100
            // RePhiEdit坐标系y轴-450 ~ 450
            return y / 4.5f;
        }

        /// <summary>
        /// Convert PhiFans easing number to RePhiEdit easing number
        /// </summary>
        /// <param name="pfEasing">PhiFans easing number</param>
        /// <returns>RePhiEdit easing number</returns>
        [Pure]
        public static int EasingNumber(int pfEasing)
        {
            // 返回RPE的缓动编号，输入PhiFans的缓动编号，也就是右侧的编号转换为左侧的编号
            int result = pfEasing switch
            {
                0 => 1,
                2 => 2,
                1 => 3,
                5 => 4,
                4 => 5,
                3 => 6,
                6 => 7,
                8 => 8,
                7 => 9,
                11 => 10,
                10 => 11,
                9 => 12,
                12 => 13,
                14 => 14,
                13 => 15,
                17 => 16,
                16 => 17,
                20 => 18,
                19 => 19,
                23 => 20,
                22 => 21,
                21 => 22,
                24 => 23,
                26 => 24,
                25 => 25,
                29 => 26,
                28 => 27,
                30 => 28,
                27 => 29,
                18 => 1,
                _ => 1
            };
            return result;
        }
    }
    
}