﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EntryScriptGenerator.Editor
{
    public class ScriptGenerator : ScriptableObject, IDisposable
    {
        [SerializeField] private string prefix = "";
        [SerializeField] private string interfaceRootPath = "";
        [SerializeField] private List<string> interfaceSuffixNames = new();
        [SerializeField] private List<int> interfaceInheritanceIndexes = new();
        [SerializeField] private List<bool> interfaceActivates = new();
        [SerializeField] private string classRootPath = "";
        [SerializeField] private List<string> classSuffixNames = new();
        [SerializeField] private List<int> classInterfaceIndexes = new();
        [SerializeField] private List<bool> classActivates = new();
        
        [SerializeField] private bool isAllCheckInterfaces;
        [SerializeField] private bool isAllCheckClasses;
        [SerializeField] private bool isAutoSelectInterface;

        private EntryScriptGenerator _entryScriptGenerator;
        private EntryScriptSettings _entryScriptSettings;
        private SerializedObject _so;
        private Vector2 _scrollPosition = Vector2.zero;
        
        private const string SettingJsonPath = "Assets/EntryScriptGenerator/Editor/SaveData/ScriptGeneratorSettings.json";

        private void Initialize(EntryScriptGenerator entryScriptGenerator, EntryScriptSettings entryScriptSettings)
        {
            _entryScriptGenerator = entryScriptGenerator;
            _entryScriptSettings = entryScriptSettings;
            _so = new SerializedObject(this);

            if (!File.Exists(SettingJsonPath)) return;
            using var sr = new StreamReader(SettingJsonPath);
            JsonUtility.FromJsonOverwrite(sr.ReadToEnd(), this);
        }

        public void Dispose()
        {
            using var sw = new StreamWriter(SettingJsonPath, false);
            var json = JsonUtility.ToJson(this, false);
            sw.Write(json);
            sw.Flush();
        }

        public static ScriptGenerator CreateInstance(EntryScriptGenerator entryScriptGenerator, EntryScriptSettings entryScriptSettings)
        {
            var instance = CreateInstance<ScriptGenerator>();
            instance.Initialize(entryScriptGenerator, entryScriptSettings);
            return instance;
        }
        
        public void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            _so.Update();
            
            // Prefix
            GUILayout.Label("スクリプト出力設定", EditorStyles.boldLabel);
            {
                EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
                GUILayout.Label("ソースコード名", EditorStyles.boldLabel);
                {
                    EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
                    EditorGUILayout.PropertyField(_so.FindProperty("prefix"), true);
                    EditorGUILayout.EndVertical();
                }
                
                {
                    var rect = EditorGUILayout.BeginVertical();
                    EditorGUILayout.PropertyField(_so.FindProperty("interfaceRootPath"), true);
                    EditorGUILayout.EndVertical();
                    EditorWindowUtility.DragAndDropFilePaths(_so, rect, "interfaceRootPath", false);
                }
                {
                    var rect = EditorGUILayout.BeginVertical();
                    EditorGUILayout.PropertyField(_so.FindProperty("classRootPath"), true);
                    EditorGUILayout.EndVertical();
                    EditorWindowUtility.DragAndDropFilePaths(_so, rect, "classRootPath", false);
                }
                
                // interface activates
                GUILayout.Label("Interface 出力の有効/無効", EditorStyles.boldLabel);
                EditorWindowUtility.SyncArraySize(_entryScriptSettings.SerializedObject,"interfaceFolderNames", _so, "interfaceActivates", true);
                {
                    var activates = _so.FindProperty("interfaceActivates");
                    var paths = _entryScriptSettings.SerializedObject.FindProperty("interfaceFolderNames");
                    var allChecked = true;
                    for (var i = 0; i < paths.arraySize; i++)
                    {
                        activates.GetArrayElementAtIndex(i).boolValue = 
                            GUILayout.Toggle(activates.GetArrayElementAtIndex(i).boolValue, paths.GetArrayElementAtIndex(i).stringValue);
                        if (!activates.GetArrayElementAtIndex(i).boolValue)
                        {
                            allChecked = false;
                        }
                    }
                    GUILayout.Space(1);

                    if (!isAllCheckInterfaces && allChecked)
                    {
                        isAllCheckInterfaces = true;
                    } 
                    else if (isAllCheckInterfaces && !allChecked)
                    {
                        isAllCheckInterfaces = false;
                    }
                    isAllCheckInterfaces = GUILayout.Toggle(isAllCheckInterfaces, "全選択/解除");
                    if (isAllCheckInterfaces != allChecked)
                    {
                        for (int i = 0; i < paths.arraySize; i++)
                        {
                            activates.GetArrayElementAtIndex(i).boolValue = isAllCheckInterfaces;
                        }   
                    }
                }
                GUILayout.Box("", GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
            
                // class activates
                GUILayout.Label("Class 出力の有効/無効", EditorStyles.boldLabel);
                EditorWindowUtility.SyncArraySize(_entryScriptSettings.SerializedObject, "classFolderNames", _so, "classActivates", true);
                {
                    var activates = _so.FindProperty("classActivates");
                    var paths = _entryScriptSettings.SerializedObject.FindProperty("classFolderNames");
                    var allChecked = true;
                    for (int i = 0; i < paths.arraySize; i++)
                    {
                        activates.GetArrayElementAtIndex(i).boolValue = 
                            GUILayout.Toggle(activates.GetArrayElementAtIndex(i).boolValue, paths.GetArrayElementAtIndex(i).stringValue);
                        if (!activates.GetArrayElementAtIndex(i).boolValue)
                        {
                            allChecked = false;
                        }
                    }
                    GUILayout.Space(1);

                    if (!isAllCheckClasses && allChecked)
                    {
                        isAllCheckClasses = true;
                    } 
                    else if (isAllCheckClasses && !allChecked)
                    {
                        isAllCheckClasses = false;
                    }
                    isAllCheckClasses = GUILayout.Toggle(isAllCheckClasses, "全選択/解除");
                    if (isAllCheckClasses != allChecked)
                    {
                        for (int i = 0; i < paths.arraySize; i++)
                        {
                            activates.GetArrayElementAtIndex(i).boolValue = isAllCheckClasses;
                        }   
                    }
                    
                    GUILayout.Space(2);
                    isAutoSelectInterface = GUILayout.Toggle(isAutoSelectInterface, "Interface自動選択");
                    if (isAutoSelectInterface)
                    {
                        var interfaceActivatesProperty = _so.FindProperty("interfaceActivates");
                        for (int i = 0; i < activates.arraySize; i++)
                        {
                            var index = classInterfaceIndexes[i];
                            if (index < 0 || this.interfaceActivates.Count <= index)
                            {
                                continue;
                            }
                            interfaceActivatesProperty.GetArrayElementAtIndex(index).boolValue = activates.GetArrayElementAtIndex(i).boolValue;
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            
            GUILayout.Box("", EditorWindowUtility.CreateLineColorStyle(), GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));

            // interface paths
            GUILayout.Label("Interface 関連設定", EditorStyles.boldLabel);
            {
                EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
                
                // interface suffixes
                EditorWindowUtility.SyncArraySize(_entryScriptSettings.SerializedObject, "interfaceFolderNames", _so, "interfaceSuffixNames", "");
                EditorGUILayout.PropertyField(_so.FindProperty("interfaceSuffixNames"), true);
                GUILayout.Box("", GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
                
                // Interface Inheritance Indexes
                EditorWindowUtility.SyncArraySize(_entryScriptSettings.SerializedObject, "interfaceFolderNames", _so, "interfaceInheritanceIndexes", -1);
                EditorGUILayout.PropertyField(_so.FindProperty("interfaceInheritanceIndexes"), true);
                GUILayout.Box("", GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
                
                // class interface references
                GUILayout.Label("Interface間の継承関係", EditorStyles.boldLabel);
                for (int i=0; i<interfaceInheritanceIndexes.Count; i++)
                {
                    var label = interfaceSuffixNames[i] + "：";
                    if (interfaceInheritanceIndexes[i] >= _entryScriptSettings.InterfaceFolderNames.Count)
                    {
                        label += "Index Error!";
                    }
                    else if (interfaceInheritanceIndexes[i] >= 0)
                    {
                        label += interfaceRootPath + "/" + _entryScriptSettings.InterfaceFolderNames[interfaceInheritanceIndexes[i]];
                    }
                    else
                    {
                        label += "None";
                    }
                    GUILayout.Label(label, EditorStyles.label);
                }
                GUILayout.Box("", GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
                
                EditorGUILayout.EndVertical();
            }
            GUILayout.Box("", EditorWindowUtility.CreateLineColorStyle(), GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
            
            // class paths
            GUILayout.Label("Class 関連設定", EditorStyles.boldLabel);
            {
                EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
                
                // class suffixes
                EditorWindowUtility.SyncArraySize(_entryScriptSettings.SerializedObject, "classFolderNames", _so, "classSuffixNames", "");
                EditorGUILayout.PropertyField(_so.FindProperty("classSuffixNames"), true);
                GUILayout.Box("", GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
                
                // class interfaces
                EditorWindowUtility.SyncArraySize(_entryScriptSettings.SerializedObject, "classFolderNames", _so, "classInterfaceIndexes", -1);
                EditorGUILayout.PropertyField(_so.FindProperty("classInterfaceIndexes"), true);
                GUILayout.Box("", GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
                
                // class interface references
                GUILayout.Label("Interfaceとの関連表示（Class Interface Indexesの参照結果）", EditorStyles.boldLabel);
                for (int i=0; i<classInterfaceIndexes.Count; i++)
                {
                    var label = classSuffixNames[i] + "：";
                    if (classInterfaceIndexes[i] >= _entryScriptSettings.InterfaceFolderNames.Count)
                    {
                        label += "Index Error!";
                    }
                    else if (classInterfaceIndexes[i] >= 0)
                    {
                        label += interfaceRootPath + "/" + _entryScriptSettings.InterfaceFolderNames[classInterfaceIndexes[i]];
                    }
                    else
                    {
                        label += "None";
                    }
                    GUILayout.Label(label, EditorStyles.label);
                }
                GUILayout.Box("", GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
                
                EditorGUILayout.EndVertical();
            }
            GUILayout.Box("", GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(5));
            
            _so.ApplyModifiedProperties();
            
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);
            if (GUILayout.Button("出力する")) {
                Execute();
            }
        }
        
        private void Execute()
        {
            GenerateInterfaces();
            GenerateClasses();
            
            AssetDatabase.Refresh();
        }

        private void GenerateInterfaces()
        {
            const string interfaceNameSpaceStr = @"#INTERFACE_NAME_SPACE#

";
            const string interfaceScript = @"namespace #NAME_SPACE# {
    public interface #INTERFACE_NAME# #INHERITED_INTERFACE_NAME#
    {
    }
}
";
            for (var i=0; i<_entryScriptSettings.InterfaceFolderNames.Count; i++)
            {
                if (!interfaceActivates[i])
                {
                    continue;
                }

                var inheritedInterfaceIndex = interfaceInheritanceIndexes[i];
                var path = interfaceRootPath + "/" + _entryScriptSettings.InterfaceFolderNames[i];
                var inheritedInterfaceNameSpace = 0 <= inheritedInterfaceIndex && inheritedInterfaceIndex < _entryScriptSettings.InterfaceFolderNames.Count ? "using " + (interfaceRootPath + "/" + _entryScriptSettings.InterfaceFolderNames[inheritedInterfaceIndex]).Replace("Assets/", "").Replace("Scripts/", "").Replace("/", ".") + ";" : "";
                var nameSpace = path.Replace("Assets/", "").Replace("Scripts/", "").Replace("/", ".");
                var interfaceName = "I" + prefix + interfaceSuffixNames[i];
                var inheritedInterfaceName = 0 <= inheritedInterfaceIndex && inheritedInterfaceIndex < _entryScriptSettings.InterfaceFolderNames.Count ? ": " + "I" + prefix + interfaceSuffixNames[inheritedInterfaceIndex] : "";
                var fileName = interfaceName + ".cs";
                var assetPath = path+"/"+fileName;
                var script = "";
                if (inheritedInterfaceNameSpace.Length > 0)
                {
                    script += interfaceNameSpaceStr.Replace("#INTERFACE_NAME_SPACE#", inheritedInterfaceNameSpace);
                }
                script += interfaceScript.Replace("#INTERFACE_NAME#", interfaceName).Replace("#INHERITED_INTERFACE_NAME#", inheritedInterfaceName).Replace("#NAME_SPACE#", nameSpace);
                if (!File.Exists(assetPath))
                {
                    File.WriteAllText(assetPath, script);
                }
            }
        }
        
        private void GenerateClasses()
        {
            const string interfaceNameSpaceStr = @"#INTERFACE_NAME_SPACE#

";
            const string classScript = @"namespace #NAME_SPACE# {
    public class #CLASS_NAME# #INTERFACE_NAME#
    {
    }
}
";
            for (var i=0; i<_entryScriptSettings.ClassFolderNames.Count; i++)
            {
                if (!classActivates[i])
                {
                    continue;
                }

                var interfaceIndex = classInterfaceIndexes[i];
                var path = classRootPath + "/" + _entryScriptSettings.ClassFolderNames[i];
                var interfaceNameSpace = 0 <= interfaceIndex && interfaceIndex < _entryScriptSettings.InterfaceFolderNames.Count ? "using " + (interfaceRootPath + "/" + _entryScriptSettings.InterfaceFolderNames[interfaceIndex]).Replace("Assets/", "").Replace("Scripts/", "").Replace("/", ".") + ";" : "";
                var nameSpace = path.Replace("Assets/", "").Replace("Scripts/", "").Replace("/", ".");
                var interfaceName = 0 <= interfaceIndex && interfaceIndex < _entryScriptSettings.InterfaceFolderNames.Count ? ": " + "I" + prefix + interfaceSuffixNames[interfaceIndex] : "";
                var className = prefix + classSuffixNames[i];
                var fileName = className + ".cs";
                var assetPath = path+"/"+fileName;
                var script = "";
                if (interfaceNameSpace.Length > 0)
                {
                    script += interfaceNameSpaceStr.Replace("#INTERFACE_NAME_SPACE#", interfaceNameSpace);
                }
                script += classScript.Replace("#CLASS_NAME#", className).Replace("#INTERFACE_NAME#", interfaceName).Replace("#NAME_SPACE#", nameSpace);
                
                if (!File.Exists(assetPath))
                {
                    File.WriteAllText(assetPath, script);
                }
            }
        }
    }
}