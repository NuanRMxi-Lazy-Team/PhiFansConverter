using Newtonsoft.Json;
using PhiFansConverter;

const float precision = 1f / 8f;
const float speedRatio = 8f;
hey:
Console.WriteLine("请选择一个文件：");
Console.WriteLine("Please select a file:");
string? path = Console.ReadLine();
if (!File.Exists(path))
{
    Console.WriteLine("文件不存在！");
    Console.WriteLine("File not found!");
    return;
}

// 询问这是什么类型的文件
Console.WriteLine("这是什么类型的文件？（1. PhiFans 2. RPE）");
// English
Console.WriteLine("What type of file is this? (1. PhiFans 2. RPE)");
int type = int.Parse(Console.ReadLine());
if (type == 1)
{
    string json = File.ReadAllText(path);
    PhiFansChart? chart = JsonConvert.DeserializeObject<PhiFansChart>(json);
    if (chart == null)
    {
        Console.WriteLine("无法解析文件！");
        // English
        Console.WriteLine("Failed to parse file!");
        goto hey;
    }
    RpeChart rpeChart = Converters.PhiFansConverter(chart);
    File.WriteAllText("rpe.json", JsonConvert.SerializeObject(rpeChart, Formatting.Indented));
    Console.WriteLine("已保存在" + Path.GetFullPath("rpe.json"));
    Console.WriteLine("Saved to " + Path.GetFullPath("rpe.json"));
    Console.WriteLine("按回车键退出");
    Console.WriteLine("Press Enter to exit");
    Console.ReadLine();
}
else if (type == 2)
{
    string json = File.ReadAllText(path);
    RpeChart chart = JsonConvert.DeserializeObject<RpeChart>(json);
    PhiFansChart phiFansChart = Converters.RePhiEditConverter(chart);
    File.WriteAllText("phifans.json", JsonConvert.SerializeObject(phiFansChart, Formatting.Indented));
    Console.WriteLine("已保存在" + Path.GetFullPath("phifans.json"));
    Console.WriteLine("Saved to " + Path.GetFullPath("phifans.json"));
    Console.WriteLine("按回车键退出");
    Console.WriteLine("Press Enter to exit");
    Console.ReadLine();
}
else
{
    Console.WriteLine("未知的类型！");
    Console.WriteLine("Unknown type!");
    goto hey;
}