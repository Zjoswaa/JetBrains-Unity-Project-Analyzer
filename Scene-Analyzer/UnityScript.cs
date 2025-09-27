using YamlDotNet.Serialization;

namespace Scene_Analyzer;

public class UnityScript {
    public readonly string SourceFilePath;
    public readonly string MetaFilePath;
    public readonly string GUID;
    public string Name => System.IO.Path.GetFileNameWithoutExtension(SourceFilePath);
    
    public readonly Dictionary<object, object?> ScriptDict;

    public UnityScript(string sourceFilePath, string metaFilePath) {
        SourceFilePath = sourceFilePath;
        MetaFilePath = metaFilePath;
        
        string allText = File.ReadAllText(metaFilePath);
        var yamlDeserializer = new DeserializerBuilder().Build();

        ScriptDict = (yamlDeserializer.Deserialize(new StringReader(allText)) as Dictionary<object, object?>)!;
        GUID = (ScriptDict["guid"] as string)!;
        
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"Loaded Unity script: {sourceFilePath}");
        Console.ResetColor();
    }

    public void Print() {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"{Name} {GUID}");
        Console.ResetColor();
        Util.PrintDictionary(ScriptDict);
    }

    public static List<string> GetAllScriptFileNames(string rootDir) {
        List<string> fileNames = new List<string>();
        try {
            foreach (string file in Directory.GetFiles(rootDir)) {
                if (file.EndsWith(".cs") || file.EndsWith(".cs.meta")) {
                    fileNames.Add(file);
                }
            }
            foreach (string dir in Directory.GetDirectories(rootDir)) {
                fileNames.AddRange(GetAllScriptFileNames(dir));
            }
        }
        catch (Exception e) {
            Console.WriteLine(e.Message);
        }
        return fileNames;
    }

    public static List<UnityScript> GetAllScripts(string rootDir) {
        List<string> fileNames = GetAllScriptFileNames(rootDir);
        fileNames.Sort();
        List<UnityScript> scripts = new List<UnityScript>();
        
        for (int i = 0; i < fileNames.Count; i += 2) {
            scripts.Add(new UnityScript(fileNames[i], fileNames[i+1]));
        }

        return scripts;
    }
}
