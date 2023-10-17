using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Serialization;

namespace EntryScriptGenerator.Editor
{
    [Serializable]
    class AssemblyDefinitionJsonData
    {
        public string name;
        public string rootNamespace;
        public List<string> references = new();
        public List<string> includePlatforms = new();
        public List<string> excludePlatforms = new();
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public List<string> precompiledReferences = new();
        public bool autoReferenced;
        public List<string> defineConstraints = new();
        public List<string> versionDefines = new();
        public bool noEngineReferences = true;
    }
    
    public class FolderGeneratorUnit : GeneratorUnit
    {
        [SerializeField] private bool generateTarget = true;
        [SerializeField] private bool allowUnsafeCode;
        [SerializeField] private bool autoReferenced;
        [SerializeField] private bool noEngineReferences;
        [SerializeField] private string rootNamespace;
        [SerializeField] private List<AssemblyDefinitionAsset> references = new();
        [SerializeField] private List<string> referenceGuids = new();
        [SerializeField] private List<string> selectedInterfaceDependencies = new();
        [SerializeField] private List<string> selectedClassDependencies = new();

        private string _unitName;
        private FolderType _folderType;
        public string UnitName => _unitName;
        public FolderType FolderType => _folderType;
        private EntryScriptSettings _entryScriptSettings;
        private FolderGenerator _folderGenerator;
        private ReorderableList _referenceReorderableList;
        private ReorderableList _interfaceDependenciesReorderableList;
        private ReorderableList _classDependenciesReorderableList;

        private readonly List<string> _selectableInterfaceDependencies = new();
        private readonly List<string> _selectableClassDependencies = new();
        
        protected override string SettingJsonPath => Constants.SaveDataFolderPath + "/FolderGenerateUnit" + _folderType.ToString() + _unitName + "Settings.json";
        
        public static FolderGeneratorUnit CreateInstance(EntryScriptSettings entryScriptSettings, FolderGenerator folderGenerator, string unitName, FolderType folderType)
        {
            var instance = CreateInstance<FolderGeneratorUnit>();
            instance.Initialize(entryScriptSettings, folderGenerator, unitName, folderType);
            return instance;
        }

        private void Initialize(EntryScriptSettings entryScriptSettings, FolderGenerator folderGenerator, string unitName, FolderType folderType)
        {
            _entryScriptSettings = entryScriptSettings;
            _folderGenerator = folderGenerator;
            _unitName = unitName;
            _folderType = folderType;
            
            _selectableInterfaceDependencies.AddRange(_entryScriptSettings.InterfaceFolderNames);
            _selectableClassDependencies.AddRange(_entryScriptSettings.ClassFolderNames);
            
            base.Initialize();

            {
                So.Update();
                var referenceProperty = So.FindProperty("references");
                var referenceGuidsProperty = So.FindProperty("referenceGuids");
                
                if(referenceProperty.arraySize != referenceGuidsProperty.arraySize)
                {
                    referenceGuids.Clear();
                    for (var i=0; i<referenceProperty.arraySize; i++)
                    {
                        referenceGuids.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(referenceProperty.GetArrayElementAtIndex(i).objectReferenceValue)));
                    }
                    So.Update();
                }
                else
                {
                    for (var i = 0; i < referenceGuidsProperty.arraySize; i++)
                    {
                        references[i] = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(AssetDatabase.GUIDToAssetPath(referenceGuidsProperty.GetArrayElementAtIndex(i).stringValue));
                    }
                    So.Update();
                }
                
                _referenceReorderableList = new ReorderableList(So, referenceProperty)
                {
                    drawHeaderCallback = (rect) =>
                    {
                        EditorGUI.LabelField(rect, "Assembly Definition References");
                    },
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var assemblyDefinitionAsset = referenceProperty.GetArrayElementAtIndex(index);
                        if (index >= referenceGuidsProperty.arraySize)
                        {
                            return;
                        }
                        var assemblyDefinitionFileGuid = referenceGuidsProperty.GetArrayElementAtIndex(index);
                        assemblyDefinitionAsset.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(AssetDatabase.GUIDToAssetPath(assemblyDefinitionFileGuid.stringValue));
                        rect.height = EditorGUIUtility.singleLineHeight;
                        var label = "(Missing Reference)";
                        if (assemblyDefinitionAsset.objectReferenceValue != null)
                        {
                            var nameSo = new SerializedObject(assemblyDefinitionAsset.objectReferenceValue);
                            var nameProperty = nameSo.targetObject.name;
                            if (!string.IsNullOrEmpty(nameProperty))
                            {
                                label = nameProperty;
                            }
                        }
                        EditorGUI.PropertyField(rect, assemblyDefinitionAsset, new GUIContent(label));
                        assemblyDefinitionFileGuid.stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(assemblyDefinitionAsset.objectReferenceValue));
                    },
                    onChangedCallback = (list) =>
                    {
                        var property = list.serializedProperty;
                        references.Clear();
                        referenceGuids.Clear();
                        for (var i=0; i<property.arraySize; i++)
                        {
                            references.Add(property.GetArrayElementAtIndex(i).objectReferenceValue as AssemblyDefinitionAsset);
                            referenceGuids.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(property.GetArrayElementAtIndex(i).objectReferenceValue)));
                        }
                        So.Update();
                    }
                };
            }
            
            {
                _interfaceDependenciesReorderableList = new ReorderableList(selectedInterfaceDependencies, typeof(string))
                {
                    drawHeaderCallback = (rect) =>
                    {
                        EditorGUI.LabelField(rect, "Interface Dependencies");
                    },
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var selectedIndex = EditorGUI.Popup(rect, _selectableInterfaceDependencies.IndexOf(selectedInterfaceDependencies[index]), _selectableInterfaceDependencies.ToArray());
                        if (0 <= selectedIndex && selectedIndex < _selectableInterfaceDependencies.Count)
                        {
                            selectedInterfaceDependencies[index] = _selectableInterfaceDependencies[selectedIndex];
                        }
                    }
                };
            }

            {
                _classDependenciesReorderableList = new ReorderableList(selectedClassDependencies, typeof(string))
                {
                    drawHeaderCallback = (rect) =>
                    {
                        EditorGUI.LabelField(rect, "Class Dependencies");
                    },
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var selectedIndex = EditorGUI.Popup(rect, _selectableClassDependencies.IndexOf(selectedClassDependencies[index]), _selectableClassDependencies.ToArray());
                        if (0 <= selectedIndex && selectedIndex < _selectableClassDependencies.Count)
                        {
                            selectedClassDependencies[index] = _selectableClassDependencies[selectedIndex];
                        }
                    }
                };
            }
        }

        public override void OnGUI()
        {
            So.Update();
            
            GUILayout.Label(_unitName, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
            EditorGUILayout.PropertyField(So.FindProperty("generateTarget"), true);
            EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
            EditorGUILayout.PropertyField(So.FindProperty("allowUnsafeCode"), true);
            EditorGUILayout.PropertyField(So.FindProperty("autoReferenced"), true);
            EditorGUILayout.PropertyField(So.FindProperty("noEngineReferences"), true);
            EditorGUILayout.PropertyField(So.FindProperty("rootNamespace"), true);
            EditorGUILayout.Space(10);
            _referenceReorderableList.DoLayoutList();
            _interfaceDependenciesReorderableList.DoLayoutList();
            _classDependenciesReorderableList.DoLayoutList();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            So.ApplyModifiedProperties();
        }

        private string GenerateAssemblyDefinitionFileName(bool isIncludeExt)
        {
            var fileName = _folderGenerator.GenerateAsmdefPrefix.Length > 0 ? _folderGenerator.GenerateAsmdefPrefix + "." : "";
            fileName += _folderType == FolderType.Interface ? "Interfaces." + UnitName : UnitName;
            if (isIncludeExt)
            {
                fileName += ".asmdef";
            }

            return fileName;
        }

        public void PublishAssemblyDefinition(string targetPath)
        {
            if (!So.FindProperty("generateTarget").boolValue)
            {
                return;
            }
            
            var asmdefJson = new AssemblyDefinitionJsonData();
            var fileName = GenerateAssemblyDefinitionFileName(false);
            asmdefJson.name = fileName;
            asmdefJson.allowUnsafeCode = So.FindProperty("allowUnsafeCode").boolValue;
            asmdefJson.autoReferenced = So.FindProperty("autoReferenced").boolValue;
            asmdefJson.noEngineReferences = So.FindProperty("noEngineReferences").boolValue;
            asmdefJson.rootNamespace = So.FindProperty("rootNamespace").stringValue;
            {
                var guids = new List<string>();
                {
                    var property = _referenceReorderableList.serializedProperty;
                    for (var i=0; i<property.arraySize; i++)
                    {
                        guids.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(property.GetArrayElementAtIndex(i).objectReferenceValue)));
                    }
                }
                {
                    foreach (var selectedDependency in selectedInterfaceDependencies)
                    {
                        var dependencyFileName = "Interfaces." + selectedDependency;
                        
                        var dependencyAsmdefFilePath = _folderGenerator.GenerateFolderRoot.Length > 0 ? _folderGenerator.GenerateFolderRoot + "/" : _folderGenerator.GenerateFolderRoot;
                        dependencyAsmdefFilePath += "Interfaces/";
                        
                        dependencyAsmdefFilePath += selectedDependency + "/";
                        dependencyAsmdefFilePath += _folderGenerator.GenerateAsmdefPrefix.Length > 0 ? _folderGenerator.GenerateAsmdefPrefix + "." + dependencyFileName : dependencyFileName;
                        dependencyAsmdefFilePath += ".asmdef";
                        if (File.Exists(dependencyAsmdefFilePath))
                        {
                            var guid = AssetDatabase.AssetPathToGUID(dependencyAsmdefFilePath);
                            if (!guids.Exists(t => t == guid))
                            {
                                guids.Add(guid);
                            }
                        }
                    }
                }
                {
                    foreach (var selectedDependency in selectedClassDependencies)
                    {
                        var dependencyFileName = selectedDependency;
                        
                        var dependencyAsmdefFilePath = _folderGenerator.GenerateFolderRoot.Length > 0 ? _folderGenerator.GenerateFolderRoot + "/" : _folderGenerator.GenerateFolderRoot;
                        dependencyAsmdefFilePath += selectedDependency + "/";
                        dependencyAsmdefFilePath += _folderGenerator.GenerateAsmdefPrefix.Length > 0 ? _folderGenerator.GenerateAsmdefPrefix + "." + dependencyFileName : dependencyFileName;
                        dependencyAsmdefFilePath += ".asmdef";
                        if (File.Exists(dependencyAsmdefFilePath))
                        {
                            var guid = AssetDatabase.AssetPathToGUID(dependencyAsmdefFilePath);
                            if (!guids.Exists(t => t == guid))
                            {
                                guids.Add(guid);
                            }
                        }
                    }
                }

                foreach (var guid in guids)
                {
                    asmdefJson.references.Add("GUID:" + guid);
                }
            }
            var json = JsonUtility.ToJson(asmdefJson, true);
            var path = targetPath + "/" + fileName + ".asmdef";
            var writer = new StreamWriter(path, false);
            writer.Write(json);
            writer.Close();
        }

        public void LoadAssemblyDefinition(string targetPath)
        {
            var assemblyDefinitionPath = targetPath + "/" + GenerateAssemblyDefinitionFileName(true);
            if (!File.Exists(assemblyDefinitionPath))
            {
                return;
            }
            var reader = new StreamReader(assemblyDefinitionPath);
            var assemblyDefinitionJsonData = JsonUtility.FromJson<AssemblyDefinitionJsonData>(reader.ReadToEnd());
            allowUnsafeCode = assemblyDefinitionJsonData.allowUnsafeCode;
            autoReferenced = assemblyDefinitionJsonData.autoReferenced;
            noEngineReferences = assemblyDefinitionJsonData.noEngineReferences;
            rootNamespace = assemblyDefinitionJsonData.rootNamespace;

            referenceGuids.Clear();
            references.Clear();
            selectedInterfaceDependencies.Clear();
            selectedClassDependencies.Clear();
            foreach (var referenceGuid in assemblyDefinitionJsonData.references)
            {
                var path = AssetDatabase.GUIDToAssetPath(referenceGuid.Replace("GUID:", ""));
                if (path.Contains(_folderGenerator.GenerateFolderRoot))
                {
                    if (path.Contains("Interfaces"))
                    {
                        foreach (var interfaceName in _entryScriptSettings.InterfaceFolderNames)
                        {
                            if (!path.Contains(interfaceName)) continue;
                            selectedInterfaceDependencies.Add(interfaceName);
                            break;
                        }
                    }
                    else
                    {
                        foreach (var className in _entryScriptSettings.ClassFolderNames)
                        {
                            if (!path.Contains(className)) continue;
                            selectedClassDependencies.Add(className);
                            break;
                        }
                    }
                }
                else
                {
                    referenceGuids.Add(referenceGuid);
                    references.Add(AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(AssetDatabase.GUIDToAssetPath(referenceGuid)));
                }
            }
        }
    }
}