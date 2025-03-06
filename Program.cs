using Newtonsoft.Json;
using PhiFansConverter;
using System.IO.Compression;

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
    L10n.Print("DoYouNeedAutomaticPackaging");
    if (Console.ReadLine().ToLower() == "y")
    {
        L10n.Print("SelectIllustration");
        string? illustrationPath = Console.ReadLine();
        L10n.Print("SelectMusic");
        string? musicPath = Console.ReadLine();
        if (File.Exists(illustrationPath) && File.Exists(musicPath))
        {
            convertedRpeChart.Meta.Background = Path.GetFileName(illustrationPath);
            convertedRpeChart.Meta.Song = Path.GetFileName(musicPath);
            // 将三个文件打包成一个 zip 文件，保存在程序目录
            string zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pack.zip");
            using (var zip = new ZipArchive(File.Create(zipPath), ZipArchiveMode.Create))
            {
                ZipFileExtensions.CreateEntryFromFile(zip, path, Path.GetFileName(path));
                ZipFileExtensions.CreateEntryFromFile(zip, illustrationPath, Path.GetFileName(illustrationPath));
                ZipFileExtensions.CreateEntryFromFile(zip, musicPath, Path.GetFileName(musicPath));
            }
            // 重命名 zip 文件为 pack.pez
            string pezPath = Path.ChangeExtension(zipPath, ".pez");
            File.Move(zipPath, pezPath);
            L10n.Print("SavedTo", Path.GetFullPath(pezPath));
        }
        else
        {
            L10n.Print("FileNotFound");
        }
    }
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