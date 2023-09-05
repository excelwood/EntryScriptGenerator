using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace EntryScriptGenerator.Editor
{
    public class FolderGenerator : GeneratorUnit
    {
        [SerializeField] private string generateTargetPath;
        [SerializeField] private string generateFolderName;
        [SerializeField] private List<string> generateFolderNameExcludes = new ()
        {
            "Assets", "Scripts", "Script"
        };

        private EntryScriptGenerator _entryScriptGenerator;
        private EntryScriptSettings _entryScriptSettings;
        private Vector2 _scrollPosition = Vector2.zero;

        private readonly List<FolderGeneratorUnit> _interfaceFolderGeneratorUnits = new();
        private readonly List<FolderGeneratorUnit> _classFolderGeneratorUnits = new();

        protected override string SettingJsonPath => "Assets/EntryScriptGenerator/Editor/SaveData/FolderGeneratorSettings.json";

        public string GenerateAsmdefPrefix
        {
            get
            {
                var path = "";
                var splits = generateTargetPath.Split("/");

                {
                    var excludes = new List<string>();
                    var property = _so.FindProperty("generateFolderNameExcludes");
                    for (var i=0; i<property.arraySize; i++)
                    {
                        excludes.Add(property.GetArrayElementAtIndex(i).stringValue);
                    }
                
                    foreach (var split in splits)
                    {
                        if (excludes.Exists(t => t == split))
                        {
                            continue;
                        }
                        if (path.Length > 0)
                        {
                            path += ".";
                        }
                        path += split;
                    }
                }
                {
                    var property = _so.FindProperty("generateFolderName");
                    if (path.Length > 0)
                    {
                        path += ".";
                    }
                    path += property.stringValue;
                }
                
                return path;
            }
        }

        public string GenerateFolderRoot => generateTargetPath + "/" + generateFolderName;

        private void Initialize(EntryScriptGenerator entryScriptGenerator, EntryScriptSettings entryScriptSettings)
        {
            base.Initialize();

            _entryScriptGenerator = entryScriptGenerator;
            _entryScriptSettings = entryScriptSettings;
            ResetFolderUnits();

            _entryScriptSettings.OnChangeFolderCountEvent += ResetFolderUnits;
        }

        private void ResetFolderUnits()
        {
            _interfaceFolderGeneratorUnits.Clear();
            _classFolderGeneratorUnits.Clear();
            foreach (var interfaceFolderName in _entryScriptSettings.InterfaceFolderNames)
            {
                var folderGeneratorUnit = FolderGeneratorUnit.CreateInstance(_entryScriptSettings, this, interfaceFolderName);
                _interfaceFolderGeneratorUnits.Add(folderGeneratorUnit);
            }
            foreach (var classFolderName in _entryScriptSettings.ClassFolderNames)
            {
                var folderGeneratorUnit = FolderGeneratorUnit.CreateInstance(_entryScriptSettings, this, classFolderName);
                _classFolderGeneratorUnits.Add(folderGeneratorUnit);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            
            _interfaceFolderGeneratorUnits.ForEach(t => t.Dispose());
            _classFolderGeneratorUnits.ForEach(t => t.Dispose());
            
            _entryScriptSettings.OnChangeFolderCountEvent -= ResetFolderUnits;
        }

        public static FolderGenerator CreateInstance(EntryScriptGenerator entryScriptGenerator, EntryScriptSettings entryScriptSettings)
        {
            var instance = CreateInstance<FolderGenerator>();
            instance.Initialize(entryScriptGenerator, entryScriptSettings);
            return instance;
        }

        public override void OnGUI()
        {
            _so.Update();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                GUILayout.Label("フォルダー出力設定", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
                {
                    {
                        var rect = EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
                        EditorGUILayout.PropertyField(_so.FindProperty("generateTargetPath"), true);
                        EditorGUILayout.EndVertical();
                        EditorWindowUtility.DragAndDropFilePaths(_so, rect, "generateTargetPath", false);
                    }

                    {
                        EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
                        EditorGUILayout.PropertyField(_so.FindProperty("generateFolderName"), true);
                        EditorGUILayout.EndVertical();
                    }
                    
                    {
                        EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
                        EditorGUILayout.PropertyField(_so.FindProperty("generateFolderNameExcludes"), true);
                        EditorGUILayout.EndVertical();
                    }
                }
                EditorGUILayout.EndVertical();
            }
            
            GUILayout.Box("", EditorWindowUtility.CreateLineColorStyle(), GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
            {
                GUILayout.Label("Interfaces Assembly Definition設定", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
                foreach (var folderGeneratorUnit in _interfaceFolderGeneratorUnits)
                {
                    folderGeneratorUnit.OnGUI();
                }
                EditorGUILayout.EndVertical();
            }
            
            GUILayout.Box("", EditorWindowUtility.CreateLineColorStyle(), GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
            {
                GUILayout.Label("Classes Assembly Definition設定", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
                foreach (var folderGeneratorUnit in _classFolderGeneratorUnits)
                {
                    folderGeneratorUnit.OnGUI();
                }
                EditorGUILayout.EndVertical();
            }
            
            _so.ApplyModifiedProperties();
            
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);
            if (GUILayout.Button("出力する")) {
                Execute();
            }
        }

        private void Execute()
        {
            var targetPathRoot = GenerateFolderRoot;
            
            if (Directory.Exists(targetPathRoot))
            {
                FileUtility.DeleteDirectory(targetPathRoot, true);
            }

            // フォルダー作成とAsmdefの書き出しを行う
            {
                FileUtility.CreateDirectory(targetPathRoot);
                if (_interfaceFolderGeneratorUnits.Count > 0)
                {
                    FileUtility.CreateDirectory(targetPathRoot + "/Interfaces");
                    foreach (var interfaceUnit in _interfaceFolderGeneratorUnits)
                    {
                        var path = targetPathRoot + "/Interfaces/" + interfaceUnit.UnitName;
                        FileUtility.CreateDirectory(path);
                        interfaceUnit.PublishAssemblyDefinition(path);
                    }
                }
            
                foreach (var classUnit in _classFolderGeneratorUnits)
                {
                    var path = targetPathRoot + "/" + classUnit.UnitName;
                    FileUtility.CreateDirectory(path);
                    classUnit.PublishAssemblyDefinition(path);
                }
            }

            // Asmdef生成後に依存関係を解決するために再書き出し
            {
                foreach (var interfaceUnit in _interfaceFolderGeneratorUnits)
                {
                    var path = targetPathRoot + "/Interfaces/" + interfaceUnit.UnitName;
                    interfaceUnit.PublishAssemblyDefinition(path);
                }
            
                foreach (var classUnit in _classFolderGeneratorUnits)
                {
                    var path = targetPathRoot + "/" + classUnit.UnitName;
                    classUnit.PublishAssemblyDefinition(path);
                }
            }
            
            AssetDatabase.Refresh();
        }
    }
}