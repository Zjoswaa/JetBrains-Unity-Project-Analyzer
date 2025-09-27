using Scene_Analyzer;

namespace Application;

class Program {
    public static void Main(string[] args) {
        // Make sure at least 2 arguments are provided
        if (args.Length < 2) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"2 arguments required, {args.Length} provided");
            Console.WriteLine("Usage: <executable> unity_project_path output_folder_path");
            Console.ResetColor();
            return;
        }

        // Make sure the input and output path exist
        if (!Path.Exists(args[0])) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Invalid input path: {args[0]}");
            Console.ResetColor();
            return;
        } if (!Path.Exists(args[1])) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Invalid output path: {args[1]}");
            Console.ResetColor();
            return;
        }
        
        // Get all the scenes from the Assets/Scenes/ directory
        List<UnityScene> scenes = UnityScene.GetAllScenes(Path.Combine(args[0], "Assets/Scenes/"));
        // Dump every collected scene to an output file
        foreach (var scene in scenes) {
            scene.DumpHierarchyToFile(Path.Combine(args[1], Path.GetFileName(scene.Path)) + ".dump");
        }
        
        // Get all the scripts from the Assets/Scripts/ directory
        List<UnityScript> scripts = UnityScript.GetAllScripts(Path.Combine(args[0], "Assets/Scripts/"));
        // Get all the scripts that are not used in any of the scenes
        List<UnityScript> unusedScripts = Util.GetUnusedScripts(scenes, scripts);
        // Write the result to an output file
        Util.WriteScriptsInfoToFile(unusedScripts, Path.Combine(args[1], "UnusedScripts.csv"), args[0]);
    }
}
