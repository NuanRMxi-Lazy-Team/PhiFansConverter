using Newtonsoft.Json;
using PhiFansConverter;

hey:
L10n.Print("SelectFile");
string? path = Console.ReadLine();
if (!File.Exists(path))
{
    L10n.Print("FileNotFound");
    return;
}
string json = File.ReadAllText(path);
var pfChart = JsonConvert.DeserializeObject<PhiFansChart>(json);
var rpeChart = JsonConvert.DeserializeObject<RpeChart>(json);
if (pfChart.info is not null)
{
    // 是 PhiFans 文件
    var convertedRpeChart = Converters.PhiFansConverter(pfChart);
    File.WriteAllText("rpe.json", JsonConvert.SerializeObject(convertedRpeChart, Formatting.None));
    L10n.Print("SavedTo", Path.GetFullPath("rpe.json"));
}
if (rpeChart.bpmlist is not null)
{
    // 是 RPE 文件
    var phiFansChart = Converters.RePhiEditConverter(rpeChart);
    File.WriteAllText("phifans.json", JsonConvert.SerializeObject(phiFansChart, Formatting.None));
    L10n.Print("SavedTo", Path.GetFullPath("phifans.json"));
}
else
{
    L10n.Print("FormatError");
    L10n.Print("PressEnterToSelectAgain");
    Console.ReadLine();
    goto hey;
}
L10n.Print("PressEnterToExit");
Console.ReadLine();