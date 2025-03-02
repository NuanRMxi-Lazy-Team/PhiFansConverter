using Newtonsoft.Json;
using PhiFansConverter;

// 要求选取一个文件
const float precision = 1f / 8f;
hey:
Console.WriteLine("请选择一个文件：");
string path = Console.ReadLine();
if (!File.Exists(path))
{
    Console.WriteLine("文件不存在！");
    return;
}

// 询问这是什么类型的文件
Console.WriteLine("这是什么类型的文件？（1. PhiFans 2. RPE）");
int type = int.Parse(Console.ReadLine());
if (type == 1)
{
    // 读取文件
    string json = File.ReadAllText(path);
    // 序列化为 PhiFansChart 对象
    PhiFansChart chart = JsonConvert.DeserializeObject<PhiFansChart>(json);
    // 转换为 RPEChart 对象
    RpeChart rpeChart = PhiFansConverter(chart);
    // 保存在rpe.json
    File.WriteAllText("rpe.json", JsonConvert.SerializeObject(rpeChart, Formatting.Indented));
    // 打印完整文件路径
    Console.WriteLine("已保存在" + Path.GetFullPath("rpe.json"));
    // 按回车键退出
    Console.WriteLine("按回车键退出");
    Console.ReadLine();
}
else if (type == 2)
{
    // 读取文件
    string json = File.ReadAllText(path);
    // 序列化为 RpeChart 对象
    RpeChart chart = JsonConvert.DeserializeObject<RpeChart>(json);
    // 转换为 PhiFansChart 对象
    PhiFansChart phiFansChart = RePhiEditConverter(chart);
    // 保存在phifans.json
    File.WriteAllText("phifans.json", JsonConvert.SerializeObject(phiFansChart, Formatting.Indented));
    // 打印完整文件路径
    Console.WriteLine("已保存在" + Path.GetFullPath("phifans.json"));
    // 按回车键退出
    Console.WriteLine("按回车键退出");
    Console.ReadLine();
}
else
{
    Console.WriteLine("未知的类型！");
    goto hey;
}


RePhiEditObject.Beat IntArrayToBeat(int[] array)
{
    var beat = new RePhiEditObject.Beat(array);
    return beat;
}

RpeChart PhiFansConverter(PhiFansChart chart)
{
    RpeChart rpeChart = new();
    rpeChart.Meta.RpeVersion = 150;
    rpeChart.Meta.Illustration = chart.info.illustration;
    rpeChart.Meta.Composer = chart.info.artist;
    rpeChart.Meta.Charter = chart.info.designer;
    rpeChart.Meta.Name = chart.info.name;
    rpeChart.Meta.Level = chart.info.level;
    rpeChart.Meta.Offset = chart.offset;
    rpeChart.JudgeLineList = new();
    rpeChart.bpmlist = new();
    foreach (var bpmItem in chart.bpm)
    {
        rpeChart.bpmlist.Add(new RePhiEditObject.RpeBpm
        {
            StartTime = IntArrayToBeat(bpmItem.beat),
            Bpm = bpmItem.bpm
        });
    }

    foreach (var lineItem in chart.lines)
    {
        RePhiEditObject.JudgeLine judgeLine = new();
        judgeLine.Notes = new();
        foreach (var note in lineItem.notes)
        {
            RePhiEditObject.Note rpeNote = new(true);
            rpeNote.Type = note.type switch
            {
                2 => 4, // Drag
                4 => 3, // Flick
                3 => 2, // Hold
                _ => 1 // Default to Tap
            };
            rpeNote.StartTime = IntArrayToBeat(note.beat);
            rpeNote.PositionX = PhiFansTransformX(note.positionX);
            rpeNote.SpeedMultiplier = note.speed;
            rpeNote.Above = note.isAbove ? 1 : 0;
            rpeNote.Size = 1f;
            rpeNote.EndTime = IntArrayToBeat(note.holdEndBeat);
            judgeLine.Notes.Add(rpeNote);
        }

        judgeLine.EventLayers = new RePhiEditObject.EventLayers();
        judgeLine.EventLayers.Add(new RePhiEditObject.EventLayer());
        for (int i = 0; i < lineItem.props.speed.Count; i++)
        {
            RePhiEditObject.SpeedEvent eventItem = new();
            var item = lineItem.props.speed[i];
            if (lineItem.props.speed[i].continuous)
            {
                eventItem.StartTime = IntArrayToBeat(lineItem.props.speed[i - 1].beat);
                eventItem.EndTime = IntArrayToBeat(item.beat);
                eventItem.Start = lineItem.props.speed[i - 1].value * 8;
                eventItem.End = item.value * 8;
                eventItem.EasingType = 1;
            }
            else
            {
                eventItem.StartTime = IntArrayToBeat(item.beat);
                eventItem.EndTime = IntArrayToBeat(item.beat);
                eventItem.Start = item.value * 8;
                eventItem.End = item.value * 8;
                eventItem.EasingType = 1;
            }

            judgeLine.EventLayers[0].SpeedEvents.Add(eventItem);
        }

        for (int i = 0; i < lineItem.props.positionX.Count; i++)
        {
            RePhiEditObject.Event eventItem = new();
            var item = lineItem.props.positionX[i];
            if (lineItem.props.positionX[i].continuous)
            {
                eventItem.StartTime = IntArrayToBeat(lineItem.props.positionX[i - 1].beat);
                eventItem.EndTime = IntArrayToBeat(item.beat);
                var value = PhiFansTransformX(item.value);
                eventItem.Start = value;
                eventItem.End = value;
                eventItem.EasingType = RePhiEditEasing(item.easing);
            }
            else
            {
                eventItem.StartTime = IntArrayToBeat(item.beat);
                eventItem.EndTime = IntArrayToBeat(item.beat);
                var value = PhiFansTransformX(item.value);
                eventItem.Start = value;
                eventItem.End = value;
                eventItem.EasingType = RePhiEditEasing(item.easing);
            }

            judgeLine.EventLayers[0].MoveXEvents.Add(eventItem);
        }

        for (int i = 0; i < lineItem.props.positionY.Count; i++)
        {
            RePhiEditObject.Event eventItem = new();
            var item = lineItem.props.positionY[i];
            if (lineItem.props.positionY[i].continuous)
            {
                eventItem.StartTime = IntArrayToBeat(lineItem.props.positionY[i - 1].beat);
                eventItem.EndTime = IntArrayToBeat(item.beat);
                var value = PhiFansTransformY(item.value);
                eventItem.Start = value;
                eventItem.End = value;
                eventItem.EasingType = RePhiEditEasing(item.easing);
            }
            else
            {
                eventItem.StartTime = IntArrayToBeat(item.beat);
                eventItem.EndTime = IntArrayToBeat(item.beat);
                var value = PhiFansTransformY(item.value);
                eventItem.Start = value;
                eventItem.End = value;
                eventItem.EasingType = RePhiEditEasing(item.easing);
            }

            judgeLine.EventLayers[0].MoveYEvents.Add(eventItem);
        }

        // alpha
        for (int i = 0; i < lineItem.props.alpha.Count; i++)
        {
            RePhiEditObject.Event eventItem = new();
            var item = lineItem.props.alpha[i];
            if (lineItem.props.alpha[i].continuous)
            {
                eventItem.StartTime = IntArrayToBeat(lineItem.props.alpha[i - 1].beat);
                eventItem.EndTime = IntArrayToBeat(item.beat);
                eventItem.Start = item.value;
                eventItem.End = item.value;
                eventItem.EasingType = RePhiEditEasing(item.easing);
            }
            else
            {
                eventItem.StartTime = IntArrayToBeat(item.beat);
                eventItem.EndTime = IntArrayToBeat(item.beat);
                eventItem.Start = item.value;
                eventItem.End = item.value;
                eventItem.EasingType = RePhiEditEasing(item.easing);
            }

            judgeLine.EventLayers[0].AlphaEvents.Add(eventItem);
        }

        // rotate
        for (int i = 0; i < lineItem.props.rotate.Count; i++)
        {
            RePhiEditObject.Event eventItem = new();
            var item = lineItem.props.rotate[i];
            if (lineItem.props.rotate[i].continuous)
            {
                eventItem.StartTime = IntArrayToBeat(lineItem.props.rotate[i - 1].beat);
                eventItem.EndTime = IntArrayToBeat(item.beat);
                eventItem.Start = item.value;
                eventItem.End = item.value;
                eventItem.EasingType = RePhiEditEasing(item.easing);
            }
            else
            {
                eventItem.StartTime = IntArrayToBeat(item.beat);
                eventItem.EndTime = IntArrayToBeat(item.beat);
                eventItem.Start = item.value;
                eventItem.End = item.value;
                eventItem.EasingType = RePhiEditEasing(item.easing);
            }

            judgeLine.EventLayers[0].RotateEvents.Add(eventItem);
        }

        rpeChart.JudgeLineList.Add(judgeLine);
    }

    return rpeChart;
}

PhiFansChart RePhiEditConverter(RpeChart chart)
{
    var phiFansChart = new PhiFansChart();
    phiFansChart.info.artist = chart.Meta.Composer;
    phiFansChart.info.designer = chart.Meta.Charter;
    phiFansChart.info.illustration = chart.Meta.Illustration;
    phiFansChart.info.level = chart.Meta.Level;
    phiFansChart.info.name = chart.Meta.Name;
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
    chart.JudgeLineList.ForEach(judgeline => judgeline.EventLayers.RemoveAll(layer => layer is null));
    foreach (var judgeline in chart.JudgeLineList)
    {
        if (judgeline.Texture != "line.png")
        {
            Console.WriteLine("检测到了不支持的判定线纹理：" + judgeline.Texture);
        }

        var lineItem = new PhiFansObject.LineItem();
        lineItem.props = new PhiFansObject.PropsObject();
        lineItem.notes = new();
        foreach (var note in judgeline.Notes)
        {
            var phiNote = new PhiFansObject.Note();
            phiNote.beat = note.StartTime.Array;
            phiNote.positionX = (int)RePhiEditTransformX(note.PositionX);
            phiNote.speed = note.SpeedMultiplier;
            phiNote.isAbove = note.Above == 1;
            phiNote.holdEndBeat = note.EndTime.Array;
            phiNote.type = note.Type switch
            {
                4 => 2,
                3 => 4,
                2 => 3,
                _ => 1
            };
            if (note.IsFake != 0)
            {
                Console.WriteLine("检查到了不支持的Fake属性：" + note.IsFake);
            }

            lineItem.notes.Add(phiNote);
        }
        
        if (judgeline.EventLayers.Count > 1 || judgeline.Father != -1)
        {
            Console.WriteLine("天哪！！！多层事件或父子线！！！这将需要很长的处理时间！");
            // 求所有事件层级中，最后一个事件的结束时间
            float maxBeat = judgeline.EventLayers.LastEventEndBeat();
            
            
            // 逐拍遍历
            for (float beat = 0; beat < maxBeat; beat += precision)
            {
                if (judgeline.EventLayers.HasAlphaEventAtBeat(beat))
                {
                    var phiEventFrame = new PhiFansObject.EventItem
                    {
                        beat = BeatConverter.BeatToBeatArray(beat),
                        value = judgeline.EventLayers.GetAlphaAtBeat(beat),
                        continuous = judgeline.EventLayers.HasAlphaEventAtBeat(beat - precision),
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
                        continuous = judgeline.EventLayers.HasAngleEventAtBeat(beat - precision),
                        easing = 0
                    };
                    lineItem.props.rotate.Add(phiEventFrame);
                }
                
                // X & Y
                var lineIndex = chart.JudgeLineList.IndexOf(judgeline);
                var hasEvent = chart.JudgeLineList.FatherAndTheLineHasXyEvent(lineIndex, beat);
                var lastHasEvent = chart.JudgeLineList.FatherAndTheLineHasXyEvent(lineIndex, beat - precision);
                if (hasEvent)
                {
                    // 获取这个判定线在判定线列表的索引

                    // 调用GetLinePosition方法获取判定线的位置，返回x, y
                    var position = chart.JudgeLineList.GetLinePosition(lineIndex, beat);
                    // X
                    var phiEventFrameX = new PhiFansObject.EventItem
                    {
                        beat = BeatConverter.BeatToBeatArray(beat),
                        value = RePhiEditTransformX(position.Item1),
                        continuous = lastHasEvent,
                        easing = 0
                    };
                    var phiEventFrameY = new PhiFansObject.EventItem
                    {
                        beat = BeatConverter.BeatToBeatArray(beat),
                        value = RePhiEditTransformY(position.Item2),
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
                        value = eventItem.Start / 8f,
                        continuous = false,
                        easing = 0
                    };
                    lineItem.props.speed.Add(phiEventFrame);
                    continue;
                }
                var phiEventStart = new PhiFansObject.EventItem
                {
                    beat = eventItem.StartTime.Array,
                    value = eventItem.Start / 8f,
                    continuous = false,
                    easing = 0
                };
                var phiEventEnd = new PhiFansObject.EventItem
                {
                    beat = eventItem.EndTime.Array,
                    value = eventItem.End / 8f,
                    continuous = true,
                    easing = 0
                };
                lineItem.props.speed.Add(phiEventStart);
                lineItem.props.speed.Add(phiEventEnd);
            }
            //Console.WriteLine("这次做了！");
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
                    easing = PhiFansEasing(eventItem.EasingType)
                };
                var phiEventEnd = new PhiFansObject.EventItem
                {
                    beat = eventItem.EndTime.Array,
                    value = eventItem.End,
                    continuous = true,
                    easing = PhiFansEasing(eventItem.EasingType)
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
                        value = RePhiEditTransformX(eventItem.Start),
                        continuous = false,
                        easing = 0
                    };
                    lineItem.props.positionX.Add(phiEventFrame);
                    continue;
                }
                var phiEventStart = new PhiFansObject.EventItem
                {
                    beat = eventItem.StartTime.Array,
                    value = RePhiEditTransformX(eventItem.Start),
                    continuous = false,
                    easing = PhiFansEasing(eventItem.EasingType)
                };
                var phiEventEnd = new PhiFansObject.EventItem
                {
                    beat = eventItem.EndTime.Array,
                    value = RePhiEditTransformX(eventItem.End),
                    continuous = true,
                    easing = PhiFansEasing(eventItem.EasingType)
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
                        value = RePhiEditTransformY(eventItem.Start),
                        continuous = false,
                        easing = 0
                    };
                    lineItem.props.positionY.Add(phiEventFrame);
                    continue;
                }
                var phiEventStart = new PhiFansObject.EventItem
                {
                    beat = eventItem.StartTime.Array,
                    value = RePhiEditTransformY(eventItem.Start),
                    continuous = false,
                    easing = PhiFansEasing(eventItem.EasingType)
                };
                var phiEventEnd = new PhiFansObject.EventItem
                {
                    beat = eventItem.EndTime.Array,
                    value = RePhiEditTransformY(eventItem.End),
                    continuous = true,
                    easing = PhiFansEasing(eventItem.EasingType)
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
                    easing = PhiFansEasing(eventItem.EasingType)
                };
                var phiEventEnd = new PhiFansObject.EventItem
                {
                    beat = eventItem.EndTime.Array,
                    value = eventItem.End,
                    continuous = true,
                    easing = PhiFansEasing(eventItem.EasingType)
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
                        value = eventItem.Start / 8f,
                        continuous = false,
                        easing = 0
                    };
                    lineItem.props.speed.Add(phiEventFrame);
                    continue;
                }
                var phiEventStart = new PhiFansObject.EventItem
                {
                    beat = eventItem.StartTime.Array,
                    value = eventItem.Start / 8f,
                    continuous = false,
                    easing = 0
                };
                var phiEventEnd = new PhiFansObject.EventItem
                {
                    beat = eventItem.EndTime.Array,
                    value = eventItem.End / 8f,
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

#region 坐标系转换

// X坐标转换
float PhiFansTransformX(float x)
{
    // 转换目标是RPE坐标系，RPE坐标系x轴-675 ~ 675
    // PhiFans坐标系x轴-100 ~ 100
    return x * 6.75f;
}

// Y坐标转换
float PhiFansTransformY(float y)
{
    // 转换目标是RPE坐标系，RPE坐标系y轴-450 ~ 450
    // PhiFans坐标系y轴-100 ~ 100
    return y * 4.5f;
}

// RePhiEditTransformX
float RePhiEditTransformX(float x)
{
    // 转换目标是PhiFans坐标系，PhiFans坐标系x轴-100 ~ 100
    // RePhiEdit坐标系x轴-675 ~ 675
    return x / 6.75f;
}

// RePhiEditTransformY
float RePhiEditTransformY(float y)
{
    // 转换目标是PhiFans坐标系，PhiFans坐标系y轴-100 ~ 100
    // RePhiEdit坐标系y轴-450 ~ 450
    return y / 4.5f;
}

#endregion

#region 缓动编号转换

int RePhiEditEasing(int pfEasing)
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
int PhiFansEasing(int rpeEasing)
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


#endregion