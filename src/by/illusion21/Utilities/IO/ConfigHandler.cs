using System.Xml.Linq;

namespace by.illusion21.Utilities.IO;

public class ConfigSection {
    public Dictionary<string, object?> Values { get; } = new();
}

public class ConfigHandler {
    private static readonly Dictionary<string, ConfigSection> ConfigSections = new();

    private static readonly Dictionary<string, Func<string, object?>> KeyHandlers = new() {
        { "KookEnable", s => CheckBool(s, false) },
        { "BaseUrl", s => CheckUrl(s, "www.kookapp.cn") },
        { "RequestPath", s => CheckString(s, "api/v3/message/create") },
        { "UseSSL", s => CheckBool(s, true) },
        { "Authorization", CheckRequiredString },
        { "PostType", s => CheckRequiredInt(s) },
        { "TargetID", CheckRequiredString },
        { "CustomContent", s => CheckString(s, null) },
        { "SrcFolder", CheckRequiredPath },
        { "TgtFolder", CheckRequiredPath },
        { "SteamCmdExecPath", CheckRequiredPath },
        { "PalWorldExecPath", CheckRequiredPath },
        { "MemThreshold", s => CheckDouble(s, 0.9) },
        { "CustomPort", s => CheckNullableInt(s, 8211) },
        { "DoUpdate", s => CheckBool(s, true) }
    };

    public ConfigHandler() {
        if (!File.Exists("config.xml")) throw new FileNotFoundException("Config file not found.");

        var xDoc = XDocument.Load("config.xml");
        var config = xDoc.Element("Config");

        foreach (var section in config!.Elements()) {
            var sectionName = section.Name.LocalName;
            if (!ConfigSections.TryGetValue(sectionName, out var sectionValues)) {
                sectionValues = new ConfigSection();
                ConfigSections[sectionName] = sectionValues;
            }

            foreach (var element in section.Elements()) {
                var key = element.Name.LocalName;
                var value = element.Value;

                if (KeyHandlers.TryGetValue(key, out var handler)) sectionValues.Values[key] = handler(value);
            }
        }
    }

    public T ValueOf<T>(string section, string key) {
        if (!ConfigSections.TryGetValue(section, out var sectionValues)) throw new KeyNotFoundException($"Config section '{section}' not found.");

        if (sectionValues.Values.TryGetValue(key, out var value)) return (T)Convert.ChangeType(value, typeof(T))!;

        throw new KeyNotFoundException($"Config key '{key}' not found in section '{section}'.");
    }

    private static string CheckRequiredString(string value) {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Config key requires a non-empty string value.");

        return value;
    }

    private static int CheckRequiredInt(string value) {
        if (int.TryParse(value, out var result))
            return result;

        throw new ArgumentException("Config key requires a non-null integer value.");
    }

    private static string CheckRequiredPath(string value) {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Config key requires a non-empty path.");

        return value;
    }


    private static string CheckUrl(string value, string defaultValue) {
        if ((Uri.TryCreate(value, UriKind.Absolute, out var uriResult) && uriResult.Scheme == Uri.UriSchemeHttp) ||
            uriResult?.Scheme == Uri.UriSchemeHttps) return value;

        return defaultValue;
    }

    private static string CheckString(string value, string? defaultValue) {
        return (string.IsNullOrWhiteSpace(value) ? defaultValue : value) ?? string.Empty;
    }

    private static bool CheckBool(string value, bool defaultValue) {
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    private static double CheckDouble(string value, double defaultValue) {
        if (double.TryParse(value, out var result) && result >= 0.0 && result <= 1.0) return result;

        return defaultValue;
    }

    private static int? CheckNullableInt(string value, int defaultValue) {
        if (int.TryParse(value, out var result)) return result;

        return defaultValue;
    }
}