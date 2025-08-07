using Newtonsoft.Json;
using PhiFansConverter;
using System.IO.Compression;

Console.WriteLine("PhiFans Converter v1.0.6");

// Check for benchmark argument
if (args.Length > 0 && args[0].ToLower() == "--benchmark")
{
    PerformanceBenchmark.RunBenchmarks();
    return;
}

Console.WriteLine("Please Choose a Language");// 选择语言英文
Console.WriteLine("Language: 1. English (US) (Default) 2. 简体中文 （中国大陆） 3. 日本語 （日本國） 4. 繁體中文 5. ???");
Console.WriteLine("Or type 'benchmark' to run performance tests");
string langNumStr = Console.ReadLine()!;

if (string.IsNullOrEmpty(langNumStr))
    langNumStr = "1";

// Check if user wants to run benchmark
if (langNumStr.ToLower().Contains("benchmark"))
{
    PerformanceBenchmark.RunBenchmarks();
    Console.WriteLine("\nPress Enter to continue to converter...");
    Console.ReadLine();
}

try
{
    _ = int.Parse(langNumStr);
}
catch (Exception)
{
    langNumStr = "1";
}
int langNum = int.Parse(langNumStr);
switch (langNum)
{
    case 1:
        L10n.CurrentLanguage = "en-US";
        break;
    case 2:
        L10n.CurrentLanguage = "zh-CN";
        break;
    case 3:
        L10n.CurrentLanguage = "ja-JP";
        break;
    case 4:
        L10n.CurrentLanguage = "zh-Hant";
        break;
    case 5:
        L10n.CurrentLanguage = "zh-ST";
        break;
    default:
        L10n.CurrentLanguage = "en-US";
        break;
}

selectFile:
L10n.Print("SelectFile");
string? path = Console.ReadLine();
if (!File.Exists(path))
{
    L10n.Print("FileNotFound");
    return;
}
var json = File.ReadAllText(path);
var rpeChart = JsonConvert.DeserializeObject<RpeChart>(json);
if (json.Contains("props"))
{
    var pfChart = JsonConvert.DeserializeObject<PhiFansChart>(json);
    // Is PhiFans file
    var convertedRpeChart = Converters.PhiFansConverter(pfChart!);
    L10n.Print("DoYouNeedAutomaticPackaging");
    if (Console.ReadLine()?.ToLower() == "y")
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
                zip.CreateEntryFromFile(path, Path.GetFileName(path));
                zip.CreateEntryFromFile(illustrationPath, Path.GetFileName(illustrationPath));
                zip.CreateEntryFromFile(musicPath, Path.GetFileName(musicPath));
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
if (rpeChart.Meta.Name is not null)
{
    // Is RePhiEdit file
    var phiFansChart = Converters.RePhiEditConverter(rpeChart);
    File.WriteAllText("phifans.json", JsonConvert.SerializeObject(phiFansChart, Formatting.None));
    L10n.Print("SavedTo", Path.GetFullPath("phifans.json"));
}
else
{
    L10n.Print("FormatError");
    L10n.Print("PressEnterToSelectAgain");
    Console.ReadLine();
    goto selectFile;
}
L10n.Print("PressEnterToExit");
Console.ReadLine();