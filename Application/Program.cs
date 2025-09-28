using Scene_Analyzer;

namespace Application;

class Program {
    public static async Task Main(string[] args) {
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

        // Get all scenes asynchronously
        List<UnityScene> scenes = await UnityScene.GetAllScenesAsync(Path.Combine(args[0], "Assets/Scenes/"));
        
        // Wait for all the tasks (dumping the file hierarchies) to finish
        await Task.WhenAll(scenes.Select(scene => scene.DumpHierarchyToFileAsync(Path.Combine(args[1], Path.GetFileName(scene.Path)) + ".dump")));

        // Get all scripts asynchrinously
        List<UnityScript> scripts = await UnityScript.GetAllScriptsAsync(Path.Combine(args[0], "Assets/Scripts/"));

        // Get all unused scripts asynchronously
        var unusedScripts = await Util.GetUnusedScriptsAsync(scenes, scripts);

        // Write the unused scripts to the output asynchronously
        await Util.WriteScriptsInfoToFileAsync(unusedScripts, Path.Combine(args[1], "UnusedScripts.csv"), args[0]);
    }
}
