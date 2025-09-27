# JetBrains Unity Project Analyzer
This project is part of my application for the internship [Unity Game Development tooling in JetBrains Rider](https://internship.jetbrains.com/projects/1667).

## How to run
### Clone
```bash
git clone git@github.com:Zjoswaa/JetBrains-Unity-Project-Analyzer.git
cd JetBrains-Unity-Project-Analyzer/Application/
```
### Run
To run using dotnet, the provided test cases from the solution are included in this repository at `JetBrains-Unity-Project-Analyzer/TestCases/TestCase[num]/`, this path can be used as `unity_project_path`.
```bash
dotnet run <unity_project_path> <output_folder_path>
```
### Build
You can also build the application to an executable
```bash
dotnet publish -c Release
```
The executable can then be found in `JetBrains-Unity-Project-Analyzer/Application/bin/Release/net9.0/`

To build a self-contained executable run the following
```bash
dotnet publish -c Release --self-contained true
```
The executable can then be found in `JetBrains-Unity-Project-Analyzer/Application/bin/Release/net9.0/<platform>/publish/`

The executable can then be run using
#### Linux
```bash
./Application <unity_project_path> <output_folder_path>
```
#### Windows
```bash
./Application.exe <unity_project_path> <output_folder_path>
```
## Task description
Write a console tool that analysis a Unity project directory and dumps the following information about the project:

### Primary task
- For each scene file you need to dump scene hierarchy in a file named `<SceneName>.unity.dump`
    ```text
    Main Camera
    Directional Light
    Cube
    Plane
    Cylinder
    ```
    ```text
    Main Camera
    Directional Light
    Parent
    --Child 1
    ----ChildNested
    --Child 2
    ```
- All unused scripts (`.cs`) should be dumped to `UnusedScripts.csv` file
    ```text
    Relative Path,GUID
    Assets/Scripts/UnusedScript.cs,0111ada5c04694881b4ea1c5adfed99f
    Assets/Scripts/Nested/UnusedScript2.cs,4851f847002ac48c487adaab15c4350c
    ```
    | Relative Path | GUID |
    |---------------|------|
    | Assets/Scripts/UnusedScript.cs | 0111ada5c04694881b4ea1c5adfed99f |
    | Assets/Scripts/Nested/UnusedScript2.cs | 4851f847002ac48c487adaab15c4350c |

As part of your task completion, it will be necessary for you to gain an understanding of how Unity stores scene hierarchies and connects assets to Unity scenes.

### Command Line
```shell
./tool.exe unity_project_path output_folder_path
```

## Research
First I analyzed the provided Unity scene files called `SampleScene.unity` and `SecondScene.unity` to make the following conclusions:
- Scene files use the YAML syntax to describe the scene hierarchy.
- It consists of multiple blocks like this:
    ```unityyaml
    --- !u!1 &350617203
    GameObject:
      m_ObjectHideFlags: 0
      m_CorrespondingSourceObject: {fileID: 0}
      m_PrefabInstance: {fileID: 0}
      m_PrefabAsset: {fileID: 0}
      serializedVersion: 6
      m_Component:
      - component: {fileID: 350617206}
      - component: {fileID: 350617205}
      - component: {fileID: 350617204}
      m_Layer: 0
      m_Name: Main Camera
      m_TagString: MainCamera
      m_Icon: {fileID: 0}
      m_NavMeshLayer: 0
      m_StaticEditorFlags: 0
      m_IsActive: 1
    ```
  - Every block starts with `---`.
  - This is followed by a `!u![number]`, I think `number` indicates some sort of Object type code, GameObjects have the code `!u!1`, Transform objects have the code `!u!4`, MonoBehaviour objects have the code `!u!114`, etc. This is consistent for both provided sample scene files.
  - This is followed by a `&[number]`, this seems to indicate the objects ID of some kind, and are used to reference other objects from one object, in the above example the GameObject with name "Main Camera" has 3 components with ID's `350617206`, `350617205` and `350617204`.
- The last block in every scene file is called `SceneRoots`, and has a structure like this:
    ```unityyaml
    --- !u!1660057539 &9223372036854775807
    SceneRoots:
      m_ObjectHideFlags: 0
      m_Roots:
      - {fileID: 963194228}
      - {fileID: 705507995}
      - {fileID: 2118425386}
      - {fileID: 2115756241}
      - {fileID: 136406838}
    ```
  - This `SceneRoots` has the type code `!u!1660057539` and ID `9223372036854775807`
  - It has a `m_Roots` field, which holds a list of ID's, these ID's are of Transform objects. this is consistent across both sample scene files. These are all the objects that don't have a parent which explains the "Root" name. This is useful to keep in mind when analyzing the scene hierarchy.
- Looking for the first root object with ID `963194228` gives us this object:
    ```unityyaml
    --- !u!4 &963194228
    Transform:
      m_ObjectHideFlags: 0
      m_CorrespondingSourceObject: {fileID: 0}
      m_PrefabInstance: {fileID: 0}
      m_PrefabAsset: {fileID: 0}
      m_GameObject: {fileID: 963194225}
      serializedVersion: 2
      m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
      m_LocalPosition: {x: 0, y: 1, z: -10}
      m_LocalScale: {x: 1, y: 1, z: 1}
      m_ConstrainProportionsScale: 0
      m_Children: []
      m_Father: {fileID: 0}
      m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
    ```
  - As mentioned before, it is a Transform object.
  - It links to the GameObject that it controls through the `m_GameObject` field, the GameObject with ID `963194225` in this case, which has the name "Main Camera".
    ```unityyaml
    --- !u!1 &963194225
    GameObject:
      m_ObjectHideFlags: 0
      m_CorrespondingSourceObject: {fileID: 0}
      m_PrefabInstance: {fileID: 0}
      m_PrefabAsset: {fileID: 0}
      serializedVersion: 6
      m_Component:
      - component: {fileID: 963194228}
      - component: {fileID: 963194227}
      - component: {fileID: 963194226}
      m_Layer: 0
      m_Name: Main Camera
      m_TagString: MainCamera
      m_Icon: {fileID: 0}
      m_NavMeshLayer: 0
      m_StaticEditorFlags: 0
      m_IsActive: 1
    ```
  - The Transform objects have fields called `m_Children` and `m_Father`, these contain a list of ID's of its children and a single ID of its father respectively. Root objects always have the ID `0` as father.
- In Unity, Transform objects seem to form the scene hierarchy. Some Transform objects are marked as root objects through the `SceneRoots` object. Every Transform object holds the ID's of its children and parent and the GameObject it is attached to (also other information like position, rotation, scale, etc. but that is not relevant for this task).
- Using this information, it is easier to create the scene hierarchy analyzer tool.
