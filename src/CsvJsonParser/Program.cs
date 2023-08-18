using System.Text;
using System.Text.RegularExpressions;
using Csv;
using LitJson;

if (args.Length > 0) {
    var outPath = Path.GetFullPath(args[0]);

    //清空目录
    if (Directory.Exists(outPath)) Directory.Delete(outPath, true);
    if (!Directory.Exists($"{outPath}/Client")) Directory.CreateDirectory($"{outPath}/Client");
    if (!Directory.Exists($"{outPath}/Server")) Directory.CreateDirectory($"{outPath}/Server");
    var idContent = string.Empty;
    var fieldContent = string.Empty;
    var classContent = $"---@class Configs Configs{Environment.NewLine}";
    var defineKey = 0;
    var defineClass = 0;
    var files = Directory.GetFiles(".", "*.csv", SearchOption.AllDirectories);
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
            NewLine = Environment
                .NewLine // The new line string to use when multiline field values are read (Requires "AllowNewLineInEnclosedFieldValues" to be set to "true" for this to have any effect.)
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
                                    "STRING" => "",
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
                                    "STRING" => parserValue,
                                    "STRING[]" => GetSplitData<string>(parserValue),
                                    "INT[]" => GetSplitData<int>(parserValue),
                                    "DOUBLE[]" => GetSplitData<double>(parserValue),
                                    "FLOAT[]" => GetSplitData<double>(parserValue),
                                    "BOOL[]" => GetSplitData<bool>(parserValue),
                                    "STRING[][]" => GetSplitData<string[]>(parserValue),
                                    "INT[][]" => GetSplitData<int[]>(parserValue),
                                    "DOUBLE[][]" => GetSplitData<double[]>(parserValue),
                                    "FLOAT[][]" => GetSplitData<double[]>(parserValue),
                                    "BOOL[][]" => GetSplitData<bool[]>(parserValue),
                                    "STRING<>" => GetSplitData<string>(parserValue),
                                    "INT<>" => GetSplitData<int>(parserValue),
                                    "DOUBLE<>" => GetSplitData<double>(parserValue),
                                    "FLOAT<>" => GetSplitData<double>(parserValue),
                                    "BOOL<>" => GetSplitData<bool>(parserValue),
                                    "STRING<[]>" => GetSplitData<string[]>(parserValue),
                                    "INT<[]>" => GetSplitData<int[]>(parserValue),
                                    "DOUBLE<[]>" => GetSplitData<double[]>(parserValue),
                                    "FLOAT<[]>" => GetSplitData<double[]>(parserValue),
                                    "BOOL<[]>" => GetSplitData<bool[]>(parserValue),
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
        if (fileName.IndexOf("_", StringComparison.Ordinal) != -1) {
            fileComment = fileName.Split('_')[0];
            fileName = fileName.Split('_')[1];
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
            jrc.PrettyPrint = args.Length < 2 || bool.Parse(args[1]);
            JsonMapper.ToJson(jsonDataC, jrc);
            File.WriteAllText($"{outPath}/Client/{fileName}.json", Regex.Unescape(sbc.ToString()));
            CSType += "C";
        }

        if (jsonDataS.Keys.Count > 0) {
            var sbs = new StringBuilder();
            var jrs = new JsonWriter(sbs);
            jrs.PrettyPrint = args.Length < 2 || bool.Parse(args[1]);
            JsonMapper.ToJson(jsonDataS, jrs);
            File.WriteAllText($"{outPath}/Server/{fileName}.json", Regex.Unescape(sbs.ToString()));
            CSType += "S";
        }

        Console.WriteLine($"导出Json --> {fileName} [{CSType}]");
    }

    if (defineClass > 0 && defineKey > 0) {
        const string idHead = @"return {
    id = {
";
        const string idTail = @"
    }
}";
        var complex = $"{fieldContent} {Environment.NewLine}{classContent} {Environment.NewLine}{idHead}{idContent}{idTail}";
        File.WriteAllText($"{outPath}/Define.txt", Regex.Unescape(complex));
        Console.WriteLine($"导出Define --> Class: {defineClass} Keys: {defineKey}");
    }
}
else {
    Console.WriteLine("导出路径参数不能为空");
}

string LuaDefineTypeConvert(string type) {
    return type switch {
        "INT" => "number",
        "BOOL" => "boolean",
        "FLOAT" => "number",
        "DOUBLE" => "number",
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
        _ => "nil"
    };
}

JsonData GetSplitData<T>(string input) {
    var typeArray = typeof(T).ToString().Contains("[]");
    var newJD = new JsonData();
    input += "|";
    var sp = input.Split('|');
    foreach (var v in sp) {
        if (string.IsNullOrEmpty(v)) continue;
        var newJD2 = new JsonData();
        if (v.Contains('=')) {
            var sp2 = v.Split('=');
            var key = sp2[0];
            var value = sp2[1];
            if (typeArray) {
                value += "~";
                var sp3 = value.Split('~');
                foreach (var v2 in sp3) {
                    if (string.IsNullOrEmpty(v2)) continue;
                    if (typeof(T) == typeof(string[])) newJD2.Add(v2);
                    else if (typeof(T) == typeof(bool[])) newJD2.Add(bool.Parse(v2));
                    else if (typeof(T) == typeof(int[])) newJD2.Add(int.Parse(v2));
                    else if (typeof(T) == typeof(double[])) newJD2.Add(double.Parse(v2));
                }

                newJD[key] = newJD2;
            }
            else {
                if (typeof(T) == typeof(string)) newJD[key] = value;
                else if (typeof(T) == typeof(bool)) newJD[key] = bool.Parse(value);
                else if (typeof(T) == typeof(int)) newJD[key] = int.Parse(value);
                else if (typeof(T) == typeof(double)) newJD[key] = double.Parse(value);
            }
        }
        else if (typeArray) {
            var value = v + "~";
            var sp2 = value.Split('~');
            foreach (var v2 in sp2) {
                if (string.IsNullOrEmpty(v2)) continue;
                if (typeof(T) == typeof(string[])) newJD2.Add(v2);
                else if (typeof(T) == typeof(bool[])) newJD2.Add(bool.Parse(v2));
                else if (typeof(T) == typeof(int[])) newJD2.Add(int.Parse(v2));
                else if (typeof(T) == typeof(double[])) newJD2.Add(double.Parse(v2));
            }

            newJD.Add(newJD2);
        }
        else {
            if (typeof(T) == typeof(string)) newJD.Add(v);
            else if (typeof(T) == typeof(bool)) newJD.Add(bool.Parse(v));
            else if (typeof(T) == typeof(int)) newJD.Add(int.Parse(v));
            else if (typeof(T) == typeof(double)) newJD.Add(double.Parse(v));
        }
    }

    return newJD;
}