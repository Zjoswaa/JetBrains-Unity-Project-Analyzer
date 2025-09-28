using YamlDotNet.Serialization;

namespace Scene_Analyzer;

public class UnityScene {
    public readonly string Path;
    public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);
    public readonly Dictionary<object, object?> SceneDict = new();

    public UnityScene(string path, string? content = null) {
        Path = path;

        if (content is null) {
            content = File.ReadAllText(path);
        }
        
        var yamlDeserializer = new DeserializerBuilder().Build();
        
        // Split the whole file text into groups, every group (except from the first one), will contain the following:
        // - Tag (eg. !u!123)
        // - ID (eg. &123)
        // - The actual YAML text
        string[] groups = content.Split("--- ");
        
        for (int i = 1; i < groups.Length; i++) {
            // Get the tag of this group, which is always the first line of the group
            // !u!29 &1 for example
            string groupTag = groups[i].Split('\n')[0];
            
            // The YAML text is the group text with the tag removed
            string yaml = groups[i].Replace(groupTag, "").Trim();
            // Console.WriteLine(yaml);
            
            // Deserialize the YAML text into an object
            var obj = yamlDeserializer.Deserialize(new StringReader(yaml));
            
            // Save the tag + ID string with the deserialized object in a dictionary
            SceneDict[groupTag] = obj;
        }
        
        Console.WriteLine($"Loaded Unity scene: {path}");
    }

    public void Print() {
        Util.PrintDictionary(SceneDict);
    }
    
    public static async Task<List<UnityScene>> GetAllScenesAsync(string rootDir) {
        // If the path ends with ".unity", like "/home/user/TestCase02/Assets/Scenes/SampleScene.unity" for example
        var files = Directory.GetFiles(rootDir, "*.unity");
        // Read the file content as tasks, the result is saved in the task object
        var tasks = files.Select(async filePath => {
            var content = await File.ReadAllTextAsync(filePath);
            return new UnityScene(filePath, content);
        });
        
        // Wait for all tasks to finish and return the contents as a list
        return (await Task.WhenAll(tasks)).ToList();
    }
    
    public async Task DumpHierarchyToFileAsync(string outputFile) {
        // If the file already exists, find a new name by adding ".1", ".2" to the extension.
        if (File.Exists(outputFile)) {
            int n = 1;
            string newOutputFile = outputFile + "." + n.ToString();
            while (File.Exists(newOutputFile)) {
                n++;
                newOutputFile = outputFile + "." + n.ToString();
            }
            outputFile = newOutputFile;
        }
        
        using var stream = new StreamWriter(File.Open(outputFile, FileMode.CreateNew, FileAccess.Write));
        
        Console.WriteLine($"Dumping {System.IO.Path.GetFileName(this.Path)} to {outputFile}");
        
        // Get the root object ID's from the "SceneRoots" object
        var rootIDs = GetSceneRootObjectIDs();
        // For every root object, dump it to the output, DumpObjectName will recursively also dump its children.
        foreach (var id in rootIDs) {
            await DumpObjectNameAsync(GetSceneObjectByID(id), 0, stream);
        }
        
        Console.WriteLine("Done");
    }
    
    // Get the Transform component of the object and get its m_GameObject field
    // Then find the GameObject with that ID and write its name to the output
    // If the transform has children, recursively visit every child of that Transform and do the same.
    private async Task DumpObjectNameAsync(object? obj, int depth, StreamWriter writer) {
        // First check if the input object is a dictionary that represents a "Transform"
        if (obj is not Dictionary<object, object> objDict || !objDict.TryGetValue("Transform", out var transformObj) || transformObj is not Dictionary<object, object> transformDict) {
            return;
        }

        // Get the m_GameObject > fileID field out of the Transform
        if (transformDict.TryGetValue("m_GameObject", out var goObj) && goObj is Dictionary<object, object> goDict && goDict.TryGetValue("fileID", out var gameObjectID) && gameObjectID is string goIdStr) {
            // Get the GameObject by its ID
            // This returns a dictionary with its name, like "GameObject" as a key
            // and the object fields (also a dictionary) as its values
            // So go to its GameObject > m_Name field
            if (GetSceneObjectByID(goIdStr) is Dictionary<object, object> gameObject &&
                gameObject.TryGetValue("GameObject", out var goValue) &&
                goValue is Dictionary<object, object> goValueDict &&
                goValueDict.TryGetValue("m_Name", out var nameObj) &&
                nameObj is string nameStr)
            {
                // Write the name to the output
                await writer.WriteLineAsync($"{new string('-', depth)}{nameStr}");
            }
        }

        // Every transform has a field "m_Children", which is a list of dictionaries
        // So get m_Children > fileID to get the ID's of its children and recursively visit them too
        if (transformDict.TryGetValue("m_Children", out object? childrenObj) && childrenObj is List<object> childIDs) {
            foreach (Dictionary<object, object> childID in childIDs) {
                if (childID.TryGetValue("fileID", out var childFileID) && childFileID is string childIdStr) {
                    await DumpObjectNameAsync(GetSceneObjectByID(childIdStr), depth + 2, writer);
                }
            }
        }
    }
    
    private List<string> GetSceneRootObjectIDs() {
        var rootIDs = new List<string>();
        // SceneRoots object is always the object with type ID !u!1660057539, at least in the provided sample scenes
        var sceneRoots = GetSceneObjectByTypeID("!u!1660057539");

        // Visit the SceneRoots > m_Roots field, which is a list of dictionaries like: {fileID: 963194228}
        if (sceneRoots is Dictionary<object, object> rootDict &&
            rootDict.TryGetValue("SceneRoots", out var sceneRootsObj) &&
            sceneRootsObj is Dictionary<object, object> sceneRootsDict &&
            sceneRootsDict.TryGetValue("m_Roots", out var rootsObj) &&
            rootsObj is List<object> rootsList)
        {
            // Get the fileID field value of every m_Roots member
            foreach (var obj in rootsList) {
                if (obj is Dictionary<object, object> objDict && objDict.TryGetValue("fileID", out var fileId) && fileId is string idStr) {
                    rootIDs.Add(idStr);
                }
            }
        }

        return rootIDs;
    }
    
    private object? GetSceneObjectByID(string ID) {
        foreach (var kv in SceneDict) {
            if (((kv.Key as string)!).Split(" ")[1].TrimStart('&') == ID) {
                return kv.Value;
            }
        }

        return null;
    }
    
    private object? GetSceneObjectByTypeID(string ID) {
        foreach (var kv in SceneDict) {
            if ((kv.Key as string)!.Split(" ")[0] == ID) {
                return kv.Value;
            }
        }

        return null;
    }
}
