using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Scene_Analyzer;

public static class Util {
    public static void PrintDictionary(Dictionary<object, object?>? dict, int depth = 0) {
        if (dict is null) {
            return;
        }
        foreach (var kv in dict) {
            Console.Write($"{new string(' ', depth)}");
            if (kv.Value is Dictionary<object, object?> nestedDict) {
                Console.WriteLine(kv.Key);
                PrintDictionary(nestedDict, depth + 2);
            } else if (kv.Value is List<object> nestedList) {
                Console.WriteLine(kv.Key);
                PrintList(nestedList, depth + 2);
            } else {
                Console.WriteLine($"{kv.Key}: {kv.Value}");
            }
        }
    }
    
    public static void PrintList(List<object>? list, int depth = 0) {
        if (list is null) {
            return;
        }
        foreach (var item in list) {
            if (item is Dictionary<object, object?> nestedDict) {
                PrintDictionary(nestedDict, depth + 2);
            } else if (item is List<object> nestedList) {
                PrintList(nestedList, depth + 2);
            } else {
                Console.WriteLine($"{new string(' ', depth)}{item}");
            }
        }
    }

    public static async Task<List<UnityScript>> GetUnusedScriptsAsync(List<UnityScene> scenes, List<UnityScript> scripts) {
        var usedGuids = new HashSet<object?>();

        // Loop over every scene
        await Parallel.ForEachAsync(scenes, async (scene, ct) => {
            // tagObject is the dictionary that holds the tag, like "!u!114 &136406840" as a key
            // and the object itself (also a dictionary) as a value
            foreach (Dictionary<object, object>? tagObject in scene.SceneDict.Values) {
                // Search into its scene hierarchy dictionary to find MonoBehaviour > m_Script > guid
                if (tagObject.TryGetValue("MonoBehaviour", out var monoVal) &&
                    monoVal is Dictionary<object, object?> monoDict &&
                    monoDict.TryGetValue("m_Script", out var scriptObj) &&
                    scriptObj is Dictionary<object, object?> scriptDict &&
                    scriptDict.TryGetValue("guid", out var guidObj) &&
                    guidObj is string guid)
                {
                    // Get the script with that guid
                    UnityScript? script = scripts.FirstOrDefault(s => s.GUID == guid);

                    // Use Roslyn to get the member variable names from each script
                    var code = await File.ReadAllTextAsync(script.SourceFilePath, ct);
                    var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: ct);
                    var root = await tree.GetRootAsync(ct);
                    var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
                    var variableNames = classDecl.Members.OfType<FieldDeclarationSyntax>()
                        .SelectMany(f => f.Declaration.Variables)
                        .Select(v => v.Identifier.Text)
                        .ToHashSet();

                    // If the script has no variables, just add the GUID to the used script list
                    if (variableNames.Count == 0) {
                        usedGuids.Add(guid);
                    }
                    // Else the script has variables, so then we try to find the field of the MonoBehaviour object
                    // that has a matching name and if true, add the GUID to the used script list
                    else {
                        foreach (string variableName in variableNames) {
                            if (monoDict.TryGetValue(variableName, out _)) {
                                usedGuids.Add(guid);
                            }
                        }
                    }
                }
            }
        });
        
        // Finally return all the scripts where its GUID is not in the usedGuids list
        return scripts.Where(s => !usedGuids.Contains(s.GUID)).ToList();
    }
    
    public static async Task WriteScriptsInfoToFileAsync(List<UnityScript> scripts, string outputFile, string absolutePathPrefix = "") {
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
        
        Console.WriteLine($"Writing unused scripts to {outputFile}");
        
        using var writer = new StreamWriter(File.Open(outputFile, FileMode.CreateNew, FileAccess.Write));
        // Write header row
        await writer.WriteLineAsync("Relative Path,GUID");
        // Write relative path and GUID
        foreach (UnityScript script in scripts) {
            await writer.WriteLineAsync($"{script.SourceFilePath.Replace(absolutePathPrefix, "")},{script.GUID}");
        }
        
        Console.WriteLine("Done");
    }
}
