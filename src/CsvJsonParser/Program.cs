using System.Text;
using System.Text.RegularExpressions;
using CommandLine;
using Csv;
using LitJson;
using Renci.SshNet;


Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Parser.Default.ParseArguments<Options>(args).WithParsed(CSVUtility.ConvertParam);

public class Options {
    [Value(0, Required = true, MetaName = "csvfile", HelpText = "csv folder path")]
    public string InputFolder { get; set; } = "";

    [Value(1, Required = false, MetaName = "outfile", HelpText = "output json folder path")]
    public string OutputFolder { get; set; } = "";

    [Value(2, Required = false, MetaName = "updateToServer", HelpText = "update to server")]
    public string UpdateToServer { get; set; } = "";
    
    [Value(3, Required = false, MetaName = "path", HelpText = "remote filePath")]
    public string RemoteFilePath { get; set; } = "";
    
    // [Value(4, Required = false, MetaName = "host", HelpText = "host")]
    // public string Host { get; set; } = "";
    //
    // [Value(5, Required = false, MetaName = "port", HelpText = "port")]
    // public string Port { get; set; } = "";
    //
    // [Value(6, Required = false, MetaName = "username", HelpText = "username")]
    // public string UserName { get; set; } = "";
    //
    // [Value(7, Required = false, MetaName = "psw", HelpText = "psw")]
    // public string PSW { get; set; } = "";
}

public partial class CSVUtility {
    public static void ConvertParam(Options o) {
        var inPath = Path.GetFullPath(o.InputFolder);
        var outPath = Path.GetFullPath(o.OutputFolder);

//清空目录
        if (Directory.Exists(outPath)) Directory.Delete(outPath, true);
        if (!Directory.Exists($"{outPath}/Client")) Directory.CreateDirectory($"{outPath}/Client");
        if (!Directory.Exists($"{outPath}/Server")) Directory.CreateDirectory($"{outPath}/Server");
        var idContent = string.Empty;
        var fieldContent = string.Empty;
        var classContent = $"---@class Configs Configs{Environment.NewLine}";
        var defineKey = 0;
        var defineClass = 0;
        var files = Directory.GetFiles(inPath, "*.csv", SearchOption.AllDirectories);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        foreach (var file in files) {
            //Console.WriteLine(Path.GetFullPath(file));
            var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var sr = new StreamReader(fs, Encoding.GetEncoding("GB2312"));
            var csv = sr.ReadToEnd();
            sr.Close();

            var options = new CsvOptions {
                RowsToSkip = 0, // Allows skipping of initial rows without csv data
                SkipRow = (row, _) => string.IsNullOrEmpty(row) || row[0] == '#',
                Separator = '\0', // Autodetects based on first row
                TrimData = false, // Can be used to trim each cell
                Comparer = null, // Can be used for case-insensitive comparison for names
                HeaderMode = HeaderMode.HeaderAbsent, // Assumes first row is a header row
                ValidateColumnCount = false, // Checks each row immediately for column count
                ReturnEmptyForMissingColumn = false, // Allows for accessing invalid column names
                Aliases = null, // A collection of alternative column names
                AllowNewLineInEnclosedFieldValues = false, // Respects new line (either \r\n or \n) characters inside field values enclosed in double quotes.
                AllowBackSlashToEscapeQuote = false, // Allows the sequence "\"" to be a valid quoted value (in addition to the standard """")
                AllowSingleQuoteToEncloseFieldValues = false, // Allows the single-quote character to be used to enclose field values
                NewLine =
                    Environment.NewLine // The new line string to use when multiline field values are read (Requires "AllowNewLineInEnclosedFieldValues" to be set to "true" for this to have any effect.)
            };

            var jsonDataC = new JsonData();
            var jsonDataS = new JsonData();
            var exportType = new List<string>();
            var keyName = new List<string>();
            var keyType = new List<string>();
            var keyComment = new List<string>();
            foreach (var line in CsvReader.ReadFromText(csv, options)) {
                switch (line.Index) {
                    case 1:
                        exportType.AddRange(line.Values);
                        exportType = exportType.Select(v => v.ToUpper()).ToList();
                        break;
                    case 2:
                        keyComment.AddRange(line.Values);
                        break;
                    case 3:
                        continue;
                    case 4:
                        keyName.AddRange(line.Values.ToList().ConvertAll(v => v.Trim()));
                        break;
                    case 5:
                        keyType.AddRange(line.Values);
                        keyType = keyType.Select(v => v.ToUpper()).ToList();
                        break;
                    default: {
                        if (string.IsNullOrEmpty(line.Values[0])) continue;
                        JsonData? rowDataC = null;
                        JsonData? rowDataS = null;
                        for (var i = 0; i < line.Values.Length; i++) {
                            var parserValue = line.Values[i];
                            try {
                                JsonData? value = null;
                                var nullArray = JsonMapper.ToObject("[]");
                                var nullObject = JsonMapper.ToObject("{}");
                                if (string.IsNullOrEmpty(parserValue)) {
                                    value = keyType[i] switch {
                                        "STRING[]" => nullArray,
                                        "INT[]" => nullArray,
                                        "DOUBLE[]" => nullArray,
                                        "FLOAT[]" => nullArray,
                                        "BOOL[]" => nullArray,
                                        "STRING[][]" => nullArray,
                                        "INT[][]" => nullArray,
                                        "DOUBLE[][]" => nullArray,
                                        "FLOAT[][]" => nullArray,
                                        "BOOL[][]" => nullArray,
                                        "STRING<>" => nullObject,
                                        "INT<>" => nullObject,
                                        "DOUBLE<>" => nullObject,
                                        "FLOAT<>" => nullObject,
                                        "BOOL<>" => nullObject,
                                        "STRING<[]>" => nullObject,
                                        "INT<[]>" => nullObject,
                                        "DOUBLE<[]>" => nullObject,
                                        "FLOAT<[]>" => nullObject,
                                        "BOOL<[]>" => nullObject,
                                        "STRING[<>]" => nullArray,
                                        "INT[<>]" => nullArray,
                                        "DOUBLE[<>]" => nullArray,
                                        "FLOAT[<>]" => nullArray,
                                        "BOOL[<>]" => nullArray,
                                        "STRING" => "",
                                        "DESC" => "",
                                        "INT" => 0,
                                        "BOOL" => false,
                                        _ => 0f
                                    };
                                }
                                else {
                                    value = keyType[i] switch {
                                        "INT" => int.Parse(parserValue),
                                        "BOOL" => bool.Parse(parserValue),
                                        "FLOAT" => double.Parse(parserValue),
                                        "DOUBLE" => double.Parse(parserValue),
                                        "DESC" => string.Empty,
                                        "STRING" => parserValue,
                                        "STRING[]" => GetSplitData<string>(parserValue, "[]"),
                                        "INT[]" => GetSplitData<int>(parserValue, "[]"),
                                        "DOUBLE[]" => GetSplitData<double>(parserValue, "[]"),
                                        "FLOAT[]" => GetSplitData<double>(parserValue, "[]"),
                                        "BOOL[]" => GetSplitData<bool>(parserValue, "[]"),
                                        "STRING[][]" => GetSplitData<string>(parserValue, "[][]"),
                                        "INT[][]" => GetSplitData<int>(parserValue, "[][]"),
                                        "DOUBLE[][]" => GetSplitData<double>(parserValue, "[][]"),
                                        "FLOAT[][]" => GetSplitData<double>(parserValue, "[][]"),
                                        "BOOL[][]" => GetSplitData<bool>(parserValue, "[][]"),
                                        "STRING<>" => GetSplitData<string>(parserValue, "<>"),
                                        "INT<>" => GetSplitData<int>(parserValue, "<>"),
                                        "DOUBLE<>" => GetSplitData<double>(parserValue, "<>"),
                                        "FLOAT<>" => GetSplitData<double>(parserValue, "<>"),
                                        "BOOL<>" => GetSplitData<bool>(parserValue, "<>"),
                                        "STRING<[]>" => GetSplitData<string>(parserValue, "<[]>"),
                                        "INT<[]>" => GetSplitData<int>(parserValue, "<[]>"),
                                        "DOUBLE<[]>" => GetSplitData<double>(parserValue, "<[]>"),
                                        "FLOAT<[]>" => GetSplitData<double>(parserValue, "<[]>"),
                                        "BOOL<[]>" => GetSplitData<bool>(parserValue, "<[]>"),
                                        "STRING[<>]" => GetSplitData<string>(parserValue, "[<>]"),
                                        "INT[<>]" => GetSplitData<int>(parserValue, "[<>]"),
                                        "DOUBLE[<>]" => GetSplitData<double>(parserValue, "[<>]"),
                                        "FLOAT[<>]" => GetSplitData<double>(parserValue, "[<>]"),
                                        "BOOL[<>]" => GetSplitData<bool>(parserValue, "[<>]"),
                                        _ => null
                                    };
                                }

                                if (exportType[i] == "CS") {
                                    rowDataC ??= new JsonData();
                                    rowDataS ??= new JsonData();
                                    rowDataC[keyName[i]] = value;
                                    rowDataS[keyName[i]] = value;
                                }
                                else if (exportType[i] == "C") {
                                    rowDataC ??= new JsonData();
                                    rowDataC[keyName[i]] = value;
                                }
                                else if (exportType[i] == "S") {
                                    rowDataS ??= new JsonData();
                                    rowDataS[keyName[i]] = value;
                                }
                            }
                            catch (Exception) {
                                Console.WriteLine(
                                    $"File: [{Path.GetFileNameWithoutExtension(file)}] Row: [{line.Values[0]}] Column: [{keyName[i]}] Value: [{parserValue}] Type: [{keyType[i]}] parser error!");
                                return;
                            }
                        }

                        if (rowDataC != null) jsonDataC[line.Values[0]] = rowDataC;
                        if (rowDataS != null) jsonDataS[line.Values[0]] = rowDataS;
                        break;
                    }
                }
            }

            var CSType = "";
            var fileName = Path.GetFileNameWithoutExtension(file);
            var fileComment = string.Empty;
            if (fileName.Contains('_')) {
                var sp = fileName.Split('_');
                fileComment = sp[0];
                if (sp.Length <= 2) fileName = sp[1];
                else {
                    fileName = sp[1];
                    for (int i = 2; i < sp.Length; i++) {
                        fileName += $"{(i <= sp.Length - 1 ? "_" : "")}{sp[i]}";
                    }
                }
            }

            //写出文件
            if (jsonDataC.Keys.Count > 0) {
                //加入到luadefine
                idContent += $"        \"{fileName}\",{Environment.NewLine}";
                classContent += $"---@field {fileName} table<string, {fileName}>{Environment.NewLine}";
                fieldContent += $"---@class {fileName} {fileComment}{Environment.NewLine}";
                for (int i = 0; i < keyName.Count; i++) {
                    if (keyName[i] == "nil" || exportType[i].IndexOf('C') == -1) continue;
                    fieldContent += $"---@field {keyName[i]} {LuaDefineTypeConvert(keyType[i])} {keyComment[i]}{Environment.NewLine}";
                    defineKey++;
                }

                fieldContent += Environment.NewLine;
                defineClass++;
                var sbc = new StringBuilder();
                var jrc = new JsonWriter(sbc);
                jrc.PrettyPrint = false;
                JsonMapper.ToJson(jsonDataC, jrc);
                File.WriteAllText($"{outPath}/Client/{fileName}.json", Regex.Unescape(sbc.ToString()));
                CSType += "C";
            }

            if (jsonDataS.Keys.Count > 0) {
                var sbs = new StringBuilder();
                var jrs = new JsonWriter(sbs);
                jrs.PrettyPrint = false;
                JsonMapper.ToJson(jsonDataS, jrs);
                File.WriteAllText($"{outPath}/Server/{fileName}.json", Regex.Unescape(sbs.ToString()));
                CSType += "S";
            }

            Console.WriteLine($"导出Json --> {fileName} [{CSType}]");
        }

        if (defineClass > 0 && defineKey > 0) {
            const string idHead = "return {\n    id = {\n";
            const string idTail = "\n    }\n}";
            var complex = $"{fieldContent} {Environment.NewLine}{classContent} {Environment.NewLine}{idHead}{idContent}{idTail}";
            File.WriteAllText($"{outPath}/Define.txt", Regex.Unescape(complex));
            Console.WriteLine($"导出Define --> Class: {defineClass} Keys: {defineKey}");
        }

        if (o.UpdateToServer.Equals("UpdateToServer")) {
            UpdateToServer(o.OutputFolder, o.RemoteFilePath, "115.159.211.214", 22, "root", "eastbest180410");
        }
        
    }


    static string LuaDefineTypeConvert(string type) {
        return type switch {
            "INT" => "number",
            "BOOL" => "boolean",
            "FLOAT" => "number",
            "DOUBLE" => "number",
            "DESC" => "string",
            "STRING" => "string",
            "STRING[]" => "string[]",
            "INT[]" => "number[]",
            "DOUBLE[]" => "number[]",
            "FLOAT[]" => "number[]",
            "BOOL[]" => "boolean[]",
            "STRING[][]" => "string[][]",
            "INT[][]" => "number[][]",
            "DOUBLE[][]" => "number[][]",
            "FLOAT[][]" => "number[][]",
            "BOOL[][]" => "boolean[][]",
            "STRING<>" => "table<string, string>",
            "INT<>" => "table<string, number>",
            "DOUBLE<>" => "table<string, number>",
            "FLOAT<>" => "table<string, number>",
            "BOOL<>" => "table<string, boolean>",
            "STRING<[]>" => "table<string, string[]>",
            "INT<[]>" => "table<string, number[]>",
            "DOUBLE<[]>" => "table<string, number[]>",
            "FLOAT<[]>" => "table<string, number[]>",
            "BOOL<[]>" => "table<string, boolean[]>",
            "STRING[<>]" => "table<string, string>[]",
            "INT[<>]" => "table<string, number>[]",
            "DOUBLE[<>]" => "table<string, number>[]",
            "FLOAT[<>]" => "table<string, number>[]",
            "BOOL[<>]" => "table<string, boolean>[]",
            _ => "nil"
        };
    }

    static JsonData GetArrayData<T>(IEnumerable<string> sp) {
        var newJD = new JsonData();
        foreach (var v in sp) {
            if (string.IsNullOrEmpty(v)) continue;
            if (typeof(T) == typeof(string)) newJD.Add(v);
            else if (typeof(T) == typeof(bool)) newJD.Add(bool.Parse(v));
            else if (typeof(T) == typeof(int)) newJD.Add(int.Parse(v));
            else if (typeof(T) == typeof(double)) newJD.Add(double.Parse(v));
        }

        return newJD;
    }
    
    static JsonData GetObjData<T>(IEnumerable<string> sp) {
        var newJD = new JsonData();
        foreach (var v in sp) {
            if (string.IsNullOrEmpty(v)) continue;
            var sp2 = v.Split('=');
            var key = sp2[0];
            var value = sp2[1];
            if (typeof(T) == typeof(string)) newJD[key] = value;
            else if (typeof(T) == typeof(bool)) newJD[key] = bool.Parse(value);
            else if (typeof(T) == typeof(int)) newJD[key] = int.Parse(value);
            else if (typeof(T) == typeof(double)) newJD[key] = double.Parse(value);
        }

        return newJD;
    }
    static JsonData GetSplitData<T>(string input, string type) {
        JsonData newJD;
        string[] arrays;
        switch (type) {
            case "[]":
                input += "|";
                return GetArrayData<T>(input.Split('|'));
            case "<>":
                input += "|";
                return GetObjData<T>(input.Split('|'));
            case "[][]":
                newJD = new JsonData();
                input += "|";
                arrays = input.Split('|');
                foreach (var array in arrays) {
                    if (string.IsNullOrEmpty(array)) continue;
                    var newArray = array + "~";
                    newJD.Add(GetArrayData<T>(newArray.Split('~')));
                }
                return newJD;
            case "<[]>":
                newJD = new JsonData();
                input += "|";
                arrays = input.Split('|');
                foreach (var array in arrays) {
                    if (string.IsNullOrEmpty(array)) continue;
                    var sp = array.Split('=');
                    var newArray = sp[1] + "~";
                    newJD[sp[0]] = GetArrayData<T>(newArray.Split('~'));
                }
                return newJD;
            case "[<>]":
                newJD = new JsonData();
                input += "|";
                arrays = input.Split('|');
                foreach (var array in arrays) {
                    if (string.IsNullOrEmpty(array)) continue;
                    var newArray = array + "~";
                    newJD.Add(GetObjData<T>(newArray.Split('~')));
                }
                return newJD;
            default:
                return null;
        }
    }

    
    static string ByteConversionGBMBKB(ulong KSize)
    {
        const int GB = 1073741824; //定义GB的计算常量
        const int MB = 1048576; //定义MB的计算常量
        const int KB = 1024; //定义KB的计算常量
        if (KSize / GB >= 1) //如果当前Byte的值大于等于1GB
            return $"{Math.Round(KSize / (float)GB, 2):F}GB"; //将其转换成GB
        if (KSize / MB >= 1) //如果当前Byte的值大于等于1MB
            return $"{Math.Round(KSize / (float)MB, 2):F}MB"; //将其转换成MB
        return KSize / KB >= 1 ? //如果当前Byte的值大于等于1KB
            $"{Math.Round(KSize / (float)KB, 2):F}KB" : //将其转换成KGB
            $"{KSize}Byte"; //显示Byte值
    }
    static void UpdateToServer(string output, string remotePath, string host, int port, string username, string psw) { 
        try {
            var files = Directory.GetFiles($@"{output}\Server", "*.json", SearchOption.AllDirectories);

            using var ssftp = new SftpClient(host, port, username, psw);
            ssftp.Connect();
            foreach (var file in files) {
                using var fileStream = File.OpenRead(file);
                FileInfo info = new(file);
                string serverName = remotePath + info.Name;
                ssftp.UploadFile(fileStream, serverName, true, size => { Console.WriteLine($"UploadFile: {serverName} [{ByteConversionGBMBKB(size)}]"); });
            }

            ssftp.Disconnect();

            using var sshClient = new SshClient(host, port, username, psw);
            sshClient.Connect();

            //sh / data / twentyfour / exec.sh

            Console.WriteLine($"执行 /data/twentyfour/exec.sh");

            var commandResult = sshClient.RunCommand("sh /data/twentyfour/exec.sh");
            Console.WriteLine(commandResult.Result);
        }
        catch (Exception ex) {
            Console.WriteLine("上传文件失败" + ex);
            Console.ReadKey();
        }
    }
}