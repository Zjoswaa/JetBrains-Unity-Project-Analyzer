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
        
        Console.WriteLine($"Loaded Unity script: {sourceFilePath}");
    }

    public void Print() {
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
    
    public static async Task<List<UnityScript>> GetAllScriptsAsync(string rootDir) {
        var fileNames = GetAllScriptFileNames(rootDir);
        fileNames.Sort();
        // Create the script object as tasks, the result is saved in the task objects
        var tasks = Enumerable.Range(0, fileNames.Count / 2)
            .Select(i => Task.FromResult(new UnityScript(fileNames[2*i], fileNames[2*i + 1])));

        // Wait for all tasks to finish and return the results as a list
        return (await Task.WhenAll(tasks)).ToList();
    }
}
