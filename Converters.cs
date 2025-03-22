using System.Diagnostics.Contracts;
using static PhiFansConverter.RePhiEditObject;

namespace PhiFansConverter;

public static class Converters
{
    private const float Precision = 1f / 8f;
    private const float SpeedRatio = 8f;

    public static RpeChart PhiFansConverter(PhiFansChart chart)
    {
        RpeChart rpeChart = new();
        rpeChart.Meta.RpeVersion = 150;
        rpeChart.Meta.Illustration = chart.info.illustration;
        rpeChart.Meta.Composer = chart.info.artist;
        rpeChart.Meta.Charter = chart.info.designer;
        rpeChart.Meta.Name = chart.info.name;
        rpeChart.Meta.Level = chart.info.level;
        rpeChart.Meta.Offset = chart.offset;
        rpeChart.JudgeLineList = [];
        rpeChart.bpmlist = [];
        foreach (var bpmItem in chart.bpm)
        {
            rpeChart.bpmlist.Add(new RpeBpm
            {
                StartTime = IntArrayToBeat(bpmItem.beat),
                Bpm = bpmItem.bpm
            });
        }

        foreach (var lineItem in chart.lines)
        {
            JudgeLine judgeLine = new()
            {
                Notes = []
            };
            foreach (var note in lineItem.notes)
            {
                Note rpeNote = new(true)
                {
                    Type = note.type switch
                    {
                        2 => 4, // Drag
                        4 => 3, // Flick
                        3 => 2, // Hold
                        _ => 1 // Default to Tap
                    },
                    StartTime = IntArrayToBeat(note.beat),
                    PositionX = PhiFans.TransformX(note.positionX),
                    SpeedMultiplier = note.speed,
                    Above = note.isAbove ? 1 : 0,
                    Size = 1f,
                    EndTime = IntArrayToBeat(note.holdEndBeat)
                };
                judgeLine.Notes.Add(rpeNote);
            }

            judgeLine.EventLayers = [new EventLayer()];
            for (var i = 0; i < lineItem.props.speed.Count; i++)
            {
                var item = lineItem.props.speed[i];
                var value = PhiFans.TransformX(item.value);
                var start = item.continuous ? lineItem.props.positionY[i - 1].value : value;
                SpeedEvent eventItem = new()
                {
                    StartTime = item.continuous
                        ? IntArrayToBeat(lineItem.props.positionX[i - 1].beat)
                        : IntArrayToBeat(item.beat),
                    EndTime = IntArrayToBeat(item.beat),
                    Start = start * SpeedRatio,
                    End = item.value * SpeedRatio,
                    EasingType = 1
                };
                judgeLine.EventLayers[0].SpeedEvents.Add(eventItem);
            }

            for (var i = 0; i < lineItem.props.positionX.Count; i++)
            {
                var item = lineItem.props.positionX[i];
                var value = PhiFans.TransformX(item.value);

                Event eventItem = new()
                {
                    StartTime = item.continuous
                        ? IntArrayToBeat(lineItem.props.positionX[i - 1].beat)
                        : IntArrayToBeat(item.beat),
                    EndTime = IntArrayToBeat(item.beat),
                    Start = item.continuous ? lineItem.props.positionY[i - 1].value : value,
                    End = value,
                    EasingType = RePhiEdit.EasingNumber(item.easing)
                };

                judgeLine.EventLayers[0].MoveXEvents.Add(eventItem);
            }

            for (var i = 0; i < lineItem.props.positionY.Count; i++)
            {
                var item = lineItem.props.positionY[i];
                var value = PhiFans.TransformY(item.value);
                Event eventItem = new()
                {
                    StartTime = item.continuous
                        ? IntArrayToBeat(lineItem.props.positionY[i - 1].beat)
                        : IntArrayToBeat(item.beat),
                    EndTime = IntArrayToBeat(item.beat),
                    Start = item.continuous ? lineItem.props.positionY[i - 1].value : value,
                    End = value,
                    EasingType = RePhiEdit.EasingNumber(item.easing)
                };
                judgeLine.EventLayers[0].MoveYEvents.Add(eventItem);
            }

            // alpha
            for (var i = 0; i < lineItem.props.alpha.Count; i++)
            {
                var item = lineItem.props.alpha[i];
                Event eventItem = new()
                {
                    StartTime = lineItem.props.alpha[i].continuous
                        ? IntArrayToBeat(lineItem.props.alpha[i - 1].beat)
                        : IntArrayToBeat(item.beat),
                    EndTime = IntArrayToBeat(item.beat),
                    Start = item.continuous ? lineItem.props.positionY[i - 1].value : item.value,
                    End = item.value,
                    EasingType = RePhiEdit.EasingNumber(item.easing)
                };

                judgeLine.EventLayers[0].AlphaEvents.Add(eventItem);
            }

            // rotate
            for (var i = 0; i < lineItem.props.rotate.Count; i++)
            {
                var item = lineItem.props.rotate[i];
                Event eventItem = new()
                {
                    StartTime = item.continuous
                        ? IntArrayToBeat(lineItem.props.rotate[i - 1].beat)
                        : IntArrayToBeat(item.beat),
                    EndTime = IntArrayToBeat(item.beat),
                    Start = item.continuous ? lineItem.props.positionY[i - 1].value : item.value,
                    End = item.value,
                    EasingType = RePhiEdit.EasingNumber(item.easing)
                };

                judgeLine.EventLayers[0].RotateEvents.Add(eventItem);
            }
            // 检查每种事件的前两个事件开始数值是否相同，如果相同，删除第一个
            // Check if the first two events of each event type have the same start value, if so, delete the first one
            void CleanupRedundantEvents<T>(List<T> events) where T : Event
            {
                if (events.Count >= 2 && Math.Abs(events[0].StartTime.CurBeat - events[1].StartTime.CurBeat) < float.Epsilon)
                {
                    events.RemoveAt(0);
                }
                else
                {
                    events[0].EndTime = new Beat([1, 0, 1]);
                }
            }
            // Apply cleanup to each event type
            if (judgeLine.EventLayers[0].AlphaEvents.Count > 0)
                CleanupRedundantEvents(judgeLine.EventLayers[0].AlphaEvents);
            if (judgeLine.EventLayers[0].MoveXEvents.Count > 0)
                CleanupRedundantEvents(judgeLine.EventLayers[0].MoveXEvents);
            if (judgeLine.EventLayers[0].MoveYEvents.Count > 0)
                CleanupRedundantEvents(judgeLine.EventLayers[0].MoveYEvents);
            if (judgeLine.EventLayers[0].RotateEvents.Count > 0)
                CleanupRedundantEvents(judgeLine.EventLayers[0].RotateEvents);
            if (judgeLine.EventLayers[0].SpeedEvents.Count > 0)
                CleanupRedundantEvents(judgeLine.EventLayers[0].SpeedEvents);

            rpeChart.JudgeLineList.Add(judgeLine);
        }

        return rpeChart;
    }

    public static PhiFansChart RePhiEditConverter(RpeChart chart)
    {
        var phiFansChart = new PhiFansChart();
        phiFansChart.info = new()
        {
            artist = chart.Meta.Composer,
            designer = chart.Meta.Charter,
            illustration = chart.Meta.Illustration,
            level = chart.Meta.Level,
            name = chart.Meta.Name
        };
        phiFansChart.offset = chart.Meta.Offset;
        foreach (var bpm in chart.bpmlist)
        {
            phiFansChart.bpm.Add(new PhiFansObject.BpmItem
            {
                beat = bpm.StartTime.Array,
                bpm = bpm.Bpm
            });
        }

        // 提前删除所有判定线的空事件层
        // Delete all empty event layers in advance
        chart.JudgeLineList.ForEach(judgeline => judgeline.EventLayers.RemoveAll(layer => layer is null));
        foreach (var judgeline in chart.JudgeLineList)
        {
            if (judgeline.Texture != "line.png")
            {
                Console.WriteLine("检测到了不支持的判定线纹理：" + judgeline.Texture);
                // English
                Console.WriteLine("Detected unsupported judge line texture: " + judgeline.Texture);
            }

            var lineItem = new PhiFansObject.LineItem();
            lineItem.props = new PhiFansObject.PropsObject();
            lineItem.notes = new();
            foreach (var note in judgeline.Notes)
            {
                var phiNote = new PhiFansObject.Note
                {
                    beat = note.StartTime.Array,
                    positionX = RePhiEdit.TransformX(note.PositionX),
                    speed = note.SpeedMultiplier,
                    isAbove = note.Above == 1,
                    holdEndBeat = note.EndTime.Array,
                    type = note.Type switch
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

                lineItem.notes.Add(phiNote);
            }

            if (judgeline.EventLayers.Count > 1 || judgeline.Father != -1)
            {
                L10n.Print("RePhiEditFeatureWarn",L10n.GetString("NestedParentChildLine"));
                L10n.Print("RePhiEditFeatureWarn",L10n.GetString("Multilayer"));
                // 求所有事件层级中，最后一个事件的结束时间
                // Get the end time of the last event in all event layers
                float maxBeat = judgeline.EventLayers.LastEventEndBeat();

                // 逐拍遍历
                for (float beat = 0; beat < maxBeat; beat += Precision)
                {
                    if (judgeline.EventLayers.HasAlphaEventAtBeat(beat))
                    {
                        var phiEventFrame = new PhiFansObject.EventItem
                        {
                            beat = BeatConverter.BeatToBeatArray(beat),
                            value = judgeline.EventLayers.GetAlphaAtBeat(beat),
                            continuous = judgeline.EventLayers.HasAlphaEventAtBeat(beat - Precision),
                            easing = 0
                        };
                        lineItem.props.alpha.Add(phiEventFrame);
                    }

                    // Rotate
                    if (judgeline.EventLayers.HasAngleEventAtBeat(beat))
                    {
                        var phiEventFrame = new PhiFansObject.EventItem
                        {
                            beat = BeatConverter.BeatToBeatArray(beat),
                            value = judgeline.EventLayers.GetAngleAtBeat(beat),
                            continuous = judgeline.EventLayers.HasAngleEventAtBeat(beat - Precision),
                            easing = 0
                        };
                        lineItem.props.rotate.Add(phiEventFrame);
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
                            beat = BeatConverter.BeatToBeatArray(beat),
                            value = RePhiEdit.TransformX(position.Item1),
                            continuous = lastHasEvent,
                            easing = 0
                        };
                        // Y
                        var phiEventFrameY = new PhiFansObject.EventItem
                        {
                            beat = BeatConverter.BeatToBeatArray(beat),
                            value = RePhiEdit.TransformY(position.Item2),
                            continuous = lastHasEvent,
                            easing = 0
                        };
                        lineItem.props.positionX.Add(phiEventFrameX);
                        lineItem.props.positionY.Add(phiEventFrameY);
                    }
                }

                // Speed
                foreach (var eventItem in judgeline.EventLayers[0].SpeedEvents)
                {
                    if (eventItem.Start == eventItem.End)
                    {
                        var phiEventFrame = new PhiFansObject.EventItem
                        {
                            beat = eventItem.StartTime.Array,
                            value = eventItem.Start / SpeedRatio,
                            continuous = false,
                            easing = 0
                        };
                        lineItem.props.speed.Add(phiEventFrame);
                        continue;
                    }

                    var phiEventStart = new PhiFansObject.EventItem
                    {
                        beat = eventItem.StartTime.Array,
                        value = eventItem.Start / SpeedRatio,
                        continuous = false,
                        easing = 0
                    };
                    var phiEventEnd = new PhiFansObject.EventItem
                    {
                        beat = eventItem.EndTime.Array,
                        value = eventItem.End / SpeedRatio,
                        continuous = true,
                        easing = 0
                    };
                    lineItem.props.speed.Add(phiEventStart);
                    lineItem.props.speed.Add(phiEventEnd);
                }

                phiFansChart.lines.Add(lineItem);
                continue;
            }

            foreach (var layer in judgeline.EventLayers)
            {
                foreach (var eventItem in layer.AlphaEvents)
                {
                    if (eventItem.Start == eventItem.End)
                    {
                        var phiEventFrame = new PhiFansObject.EventItem
                        {
                            beat = eventItem.StartTime.Array,
                            value = eventItem.Start,
                            continuous = false,
                            easing = 0
                        };
                        lineItem.props.alpha.Add(phiEventFrame);
                        continue;
                    }

                    var phiEventStart = new PhiFansObject.EventItem
                    {
                        beat = eventItem.StartTime.Array,
                        value = eventItem.Start,
                        continuous = false,
                        easing = PhiFans.EasingNumber(eventItem.EasingType)
                    };
                    var phiEventEnd = new PhiFansObject.EventItem
                    {
                        beat = eventItem.EndTime.Array,
                        value = eventItem.End,
                        continuous = true,
                        easing = PhiFans.EasingNumber(eventItem.EasingType)
                    };
                    lineItem.props.alpha.Add(phiEventStart);
                    lineItem.props.alpha.Add(phiEventEnd);
                }

                foreach (var eventItem in layer.MoveXEvents)
                {
                    if (eventItem.Start == eventItem.End)
                    {
                        var phiEventFrame = new PhiFansObject.EventItem
                        {
                            beat = eventItem.StartTime.Array,
                            value = RePhiEdit.TransformX(eventItem.Start),
                            continuous = false,
                            easing = 0
                        };
                        lineItem.props.positionX.Add(phiEventFrame);
                        continue;
                    }

                    var phiEventStart = new PhiFansObject.EventItem
                    {
                        beat = eventItem.StartTime.Array,
                        value = RePhiEdit.TransformX(eventItem.Start),
                        continuous = false,
                        easing = PhiFans.EasingNumber(eventItem.EasingType)
                    };
                    var phiEventEnd = new PhiFansObject.EventItem
                    {
                        beat = eventItem.EndTime.Array,
                        value = RePhiEdit.TransformX(eventItem.End),
                        continuous = true,
                        easing = PhiFans.EasingNumber(eventItem.EasingType)
                    };
                    lineItem.props.positionX.Add(phiEventStart);
                    lineItem.props.positionX.Add(phiEventEnd);
                }

                foreach (var eventItem in layer.MoveYEvents)
                {
                    if (eventItem.Start == eventItem.End)
                    {
                        var phiEventFrame = new PhiFansObject.EventItem
                        {
                            beat = eventItem.StartTime.Array,
                            value = RePhiEdit.TransformY(eventItem.Start),
                            continuous = false,
                            easing = 0
                        };
                        lineItem.props.positionY.Add(phiEventFrame);
                        continue;
                    }

                    var phiEventStart = new PhiFansObject.EventItem
                    {
                        beat = eventItem.StartTime.Array,
                        value = RePhiEdit.TransformY(eventItem.Start),
                        continuous = false,
                        easing = PhiFans.EasingNumber(eventItem.EasingType)
                    };
                    var phiEventEnd = new PhiFansObject.EventItem
                    {
                        beat = eventItem.EndTime.Array,
                        value = RePhiEdit.TransformY(eventItem.End),
                        continuous = true,
                        easing = PhiFans.EasingNumber(eventItem.EasingType)
                    };
                    lineItem.props.positionY.Add(phiEventStart);
                    lineItem.props.positionY.Add(phiEventEnd);
                }

                foreach (var eventItem in layer.RotateEvents)
                {
                    if (eventItem.Start == eventItem.End)
                    {
                        var phiEventFrame = new PhiFansObject.EventItem
                        {
                            beat = eventItem.StartTime.Array,
                            value = eventItem.Start,
                            continuous = false,
                            easing = 0
                        };
                        lineItem.props.rotate.Add(phiEventFrame);
                        continue;
                    }

                    var phiEventStart = new PhiFansObject.EventItem
                    {
                        beat = eventItem.StartTime.Array,
                        value = eventItem.Start,
                        continuous = false,
                        easing = PhiFans.EasingNumber(eventItem.EasingType)
                    };
                    var phiEventEnd = new PhiFansObject.EventItem
                    {
                        beat = eventItem.EndTime.Array,
                        value = eventItem.End,
                        continuous = true,
                        easing = PhiFans.EasingNumber(eventItem.EasingType)
                    };
                    lineItem.props.rotate.Add(phiEventStart);
                    lineItem.props.rotate.Add(phiEventEnd);
                }

                foreach (var eventItem in layer.SpeedEvents)
                {
                    if (eventItem.Start == eventItem.End)
                    {
                        var phiEventFrame = new PhiFansObject.EventItem
                        {
                            beat = eventItem.StartTime.Array,
                            value = eventItem.Start / SpeedRatio,
                            continuous = false,
                            easing = 0
                        };
                        lineItem.props.speed.Add(phiEventFrame);
                        continue;
                    }

                    var phiEventStart = new PhiFansObject.EventItem
                    {
                        beat = eventItem.StartTime.Array,
                        value = eventItem.Start / SpeedRatio,
                        continuous = false,
                        easing = 0
                    };
                    var phiEventEnd = new PhiFansObject.EventItem
                    {
                        beat = eventItem.EndTime.Array,
                        value = eventItem.End / SpeedRatio,
                        continuous = true,
                        easing = 0
                    };
                    lineItem.props.speed.Add(phiEventStart);
                    lineItem.props.speed.Add(phiEventEnd);
                }
            }

            phiFansChart.lines.Add(lineItem);
        }

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

    /// <summary>
    /// Convert int[] to RePhiEdit Beat
    /// </summary>
    /// <param name="array">Any array</param>
    /// <returns>RePhiEdit Beat</returns>
    private static Beat IntArrayToBeat(int[] array)
    {
        if (array.Length != 3)
            throw new ArgumentException("Array length must be 3");
        var beat = new Beat(array);
        return beat;
    }
}