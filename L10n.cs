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
        ["PressEnterToExit"] = "按回车键退出本程序。"
    };

    private static readonly Dictionary<string, string> en_US = new()
    {
        ["SelectFile"] = "Please select a file:",
        ["FileNotFound"] = "File not found!",
        ["SavedTo"] = "Saved to {0}",
        ["FormatError"] = "File format is incorrect!",
        ["PressEnterToSelectAgain"] = "Press Enter to select a file again",
        ["PressEnterToExit"] = "Press Enter to exit"
    };

    private static readonly List<string> SupportedLanguages = new() { "zh-CN", "en-US" };
    
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
            return "zh-CN";
            
        return "en-US"; // Default fallback
    }

    public static void Print(string key, params object[] args)
    {
        // If in bilingual mode, print in both languages
        if (BilingualMode)
        {
            PrintInLanguage("zh-CN", key, args);
            PrintInLanguage("en-US", key, args);
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
            _ => en_US.GetValueOrDefault(key, key)
        };

        Console.WriteLine(string.Format(text, args));
    }
    
    // Optional utility methods
    public static void SetToSystemLanguage() => CurrentLanguage = GetDefaultLanguage();
    public static void SetToChinese() => CurrentLanguage = "zh-CN";
    public static void SetToEnglish() => CurrentLanguage = "en-US";
    public static void EnableBilingualMode() => BilingualMode = true;
    public static void DisableBilingualMode() => BilingualMode = false;
}