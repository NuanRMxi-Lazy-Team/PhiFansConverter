using System.Globalization;

namespace PhiFansConverter;

public static class L10n
{
    // ReSharper disable InconsistentNaming
    private static readonly Dictionary<string, string> zh_CN = new()
    {
        ["SelectFile"] = "请选择一个文件：",
        ["FileNotFound"] = "文件不存在！",
        ["SavedTo"] = "转换的文件已保存在{0}",
        ["FormatError"] = "文件格式不正确！",
        ["PressEnterToSelectAgain"] = "按回车键重新选择文件",
        ["PressEnterToExit"] = "按回车键退出本程序。",
        ["DoYouNeedAutomaticPackaging"] = "是否需要自动打包？（y/n）",
        ["SelectIllustration"] = "请选择插画文件：",
        ["SelectMusic"] = "选择音乐文件",
        ["RePhiEditFeatureWarn"] = "检测到RPE独有特性{0}",
        ["Multilayer"] = "多层级",
        ["NestedParentChildLine"] = "嵌套父子线",
    };

    private static readonly Dictionary<string, string> en_US = new()
    {
        ["SelectFile"] = "Please select a file:",
        ["FileNotFound"] = "File not found!",
        ["SavedTo"] = "Converted file has saved to {0}",
        ["FormatError"] = "File format is incorrect!",
        ["PressEnterToSelectAgain"] = "Press Enter to select a file again",
        ["PressEnterToExit"] = "Press Enter to exit",
        ["DoYouNeedAutomaticPackaging"] = "Do you need automatic packaging?(y/n)",
        ["SelectIllustration"] = "Please select an illustration file:",
        ["SelectMusic"] = "Select a music file",
        ["RePhiEditFeatureWarn"] = "Detected RePhiEdit unique feature {0}",
        ["Multilayer"] = "Multilayer",
        ["NestedParentChildLine"] = "Nested parent-child line",
    };

    private static readonly Dictionary<string, string> ja_JP = new()
    {
        ["SelectFile"] = "ファイルを選択してください：",
        ["FileNotFound"] = "ファイルが見つかりません！",
        ["SavedTo"] = "変換されたファイルは{0}に保存されました",
        ["FormatError"] = "ファイル形式が正しくありません！",
        ["PressEnterToSelectAgain"] = "Enterキーを押して再度ファイルを選択してください",
        ["PressEnterToExit"] = "Enterキーを押してこのプログラムを終了します。",
        ["DoYouNeedAutomaticPackaging"] = "自動パッケージングが必要ですか？（y/n）",
        ["SelectIllustration"] = "イラストファイルを選択してください：",
        ["SelectMusic"] = "音楽ファイルを選択してください",
        ["RePhiEditFeatureWarn"] = "RePhiEdit独自の機能{0}が検出されました"
    };

    private static readonly Dictionary<string, string> zh_Hant = new()
    {
        // 繁體中文翻譯
        ["SelectFile"] = "请開啓一個檔案：",
        ["FileNotFound"] = "檔案不存在！",
        ["SavedTo"] = "轉換的檔案已保存在{0}",
        ["FormatError"] = "檔案格式不正确！",
        ["PressEnterToSelectAgain"] = "按Enter鍵重新開啓檔案",
        ["PressEnterToExit"] = "按Enter鍵退出本程式。",
        ["DoYouNeedAutomaticPackaging"] = "是否需要自動打包？（y/n）",
        ["SelectIllustration"] = "請選取插画檔案：",
        ["SelectMusic"] = "請選取音乐文件",
        ["RePhiEditFeatureWarn"] = "檢測到RPE獨有特性{0}",
        ["Multilayer"] = "多層級",
        ["NestedParentChildLine"] = "嵌套父子線",
    };

    private static readonly Dictionary<string, string> zh_ST = new()
    {
        // Translation goal: Stuff and humor
        // 由NRLT-萌雨社沙雕翻译包团队制作
        ["SelectFile"] = "请选举一个存档：",
        ["FileNotFound"] = "存档哪去嘞？！",
        ["SavedTo"] = "转换的存档已放到了{0}",
        ["FormatError"] = "存档格式不对，这不是我们的问题！",
        ["PressEnterToSelectAgain"] = "按换行键重新选择存档",
        ["PressEnterToExit"] = "按换行键退出本程序。",
        ["DoYouNeedAutomaticPackaging"] = "是否需要自动偷懒？（y/n）",
        ["SelectIllustration"] = "请选择绘画文件：",
        ["SelectMusic"] = "选择波形文件",
        ["RePhiEditFeatureWarn"] = "侦测到RPE独占BUG{0}",
        ["Multilayer"] = "堆积成山的输出层",
        ["NestedParentChildLine"] = "堆积成山的线",
    };

    private static readonly List<string> SupportedLanguages =
        new() { "zh-CN", "en-US", "ja-JP", "zh-ST", "zh-TW", "zh-HK", "zh-Hant" };

    // Get system language and fall back to en-US if not supported
    private static string _currentLanguage = GetDefaultLanguage();

    public static string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (SupportedLanguages.Contains(value))
                _currentLanguage = value;
            else
                _currentLanguage = "en-US"; // Default to English if unsupported
        }
    }

    // For backward compatibility
    public static string Language
    {
        get => _currentLanguage;
        set => CurrentLanguage = value.Split(',')[0].Trim(); // Take only the first language
    }

    // For bilingual output
    public static bool BilingualMode { get; set; } = false;

    private static string GetDefaultLanguage()
    {
        // Get system language
        string systemLang = CultureInfo.CurrentUICulture.Name;

        // Check if system language is supported
        if (systemLang.StartsWith("zh-"))
        {
            if (systemLang == "zh-HK" || systemLang == "zh-TW")
                return "zh-Hant";
            return "zh-CN";
        }
        if (systemLang.StartsWith("ja-"))
            return "ja-JP";
        return "en-US"; // Default fallback
    }

    public static void Print(string key, params object[] args)
    {
        // If in bilingual mode, print in both languages
        if (BilingualMode)
        {
            PrintInLanguage("zh-CN", key, args);
            PrintInLanguage("en-US", key, args);
            PrintInLanguage("ja-JP", key, args);
            PrintInLanguage("zh-ST", key, args);
            PrintInLanguage("zh-Hant", key, args);
            return;
        }

        // Otherwise print in the current language
        PrintInLanguage(CurrentLanguage, key, args);
    }

    private static void PrintInLanguage(string language, string key, params object[] args)
    {
        string text = language switch
        {
            "zh-CN" => zh_CN.GetValueOrDefault(key, key),
            "ja-JP" => ja_JP.GetValueOrDefault(key, key),
            "zh-ST" => zh_ST.GetValueOrDefault(key, key),
            "zh-Hant" => zh_Hant.GetValueOrDefault(key, key),
            _ => en_US.GetValueOrDefault(key, key)
        };

        Console.WriteLine(string.Format(text, args));
    }

    public static string GetString(string key)
    {
        return CurrentLanguage switch
        {
            "zh-CN" => zh_CN.GetValueOrDefault(key, key),
            "en-US" => en_US.GetValueOrDefault(key, key),
            "ja-JP" => ja_JP.GetValueOrDefault(key, key),
            "zh-ST" => zh_ST.GetValueOrDefault(key, key),
            "zh-Hant" => zh_Hant.GetValueOrDefault(key, key),
            _ => key
        };
    }

    // Optional utility methods
    public static void SetToSystemLanguage() => CurrentLanguage = GetDefaultLanguage();
    public static void SetToChinese() => CurrentLanguage = "zh-CN";
    public static void SetToEnglish() => CurrentLanguage = "en-US";
    public static void EnableBilingualMode() => BilingualMode = true;
    public static void DisableBilingualMode() => BilingualMode = false;
}