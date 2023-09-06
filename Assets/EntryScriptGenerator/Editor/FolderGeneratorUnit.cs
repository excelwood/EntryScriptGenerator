﻿using System;
using System.Collections.Generic;
using System.IO;
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
        [SerializeField] private List<string> selectedDependencies = new();

        private string _unitName;
        public string UnitName => _unitName;
        private EntryScriptSettings _entryScriptSettings;
        private FolderGenerator _folderGenerator;
        private ReorderableList _referenceReorderableList;
        private ReorderableList _dependenciesReorderableList;

        private readonly List<string> _selectableDependencies = new();
        
        protected override string SettingJsonPath => "Assets/EntryScriptGenerator/Editor/SaveData/FolderGenerateUnit" + _unitName + "Settings.json";
        
        public static FolderGeneratorUnit CreateInstance(EntryScriptSettings entryScriptSettings, FolderGenerator folderGenerator, string unitName)
        {
            var instance = CreateInstance<FolderGeneratorUnit>();
            instance.Initialize(entryScriptSettings, folderGenerator, unitName);
            return instance;
        }

        private void Initialize(EntryScriptSettings entryScriptSettings, FolderGenerator folderGenerator, string unitName)
        {
            _entryScriptSettings = entryScriptSettings;
            _folderGenerator = folderGenerator;
            _unitName = unitName;
            
            _selectableDependencies.AddRange(_entryScriptSettings.InterfaceFolderNames);
            _selectableDependencies.AddRange(_entryScriptSettings.ClassFolderNames);
            
            base.Initialize();

            {
                _so.Update();
                var referenceProperty = _so.FindProperty("references");
                var referenceGuidsProperty = _so.FindProperty("referenceGuids");
                
                if(referenceProperty.arraySize != referenceGuidsProperty.arraySize)
                {
                    referenceGuids.Clear();
                    for (var i=0; i<referenceProperty.arraySize; i++)
                    {
                        referenceGuids.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(referenceProperty.GetArrayElementAtIndex(i).objectReferenceValue)));
                    }
                    _so.Update();
                }
                else
                {
                    for (var i = 0; i < referenceGuidsProperty.arraySize; i++)
                    {
                        references[i] = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(AssetDatabase.GUIDToAssetPath(referenceGuidsProperty.GetArrayElementAtIndex(i).stringValue));
                    }
                    _so.Update();
                }
                
                _referenceReorderableList = new ReorderableList(_so, referenceProperty)
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
                        _so.Update();
                    }
                };
            }

            {
                _dependenciesReorderableList = new ReorderableList(selectedDependencies, typeof(string))
                {
                    drawHeaderCallback = (rect) =>
                    {
                        EditorGUI.LabelField(rect, "Layer Dependencies");
                    },
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var selectedIndex = EditorGUI.Popup(rect, _selectableDependencies.IndexOf(selectedDependencies[index]), _selectableDependencies.ToArray());
                        if (0 <= selectedIndex && selectedIndex < _selectableDependencies.Count)
                        {
                            selectedDependencies[index] = _selectableDependencies[selectedIndex];
                        }
                    }
                };
            }
        }

        public override void OnGUI()
        {
            _so.Update();
            
            GUILayout.Label(_unitName, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
            EditorGUILayout.PropertyField(_so.FindProperty("generateTarget"), true);
            EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
            EditorGUILayout.PropertyField(_so.FindProperty("allowUnsafeCode"), true);
            EditorGUILayout.PropertyField(_so.FindProperty("autoReferenced"), true);
            EditorGUILayout.PropertyField(_so.FindProperty("noEngineReferences"), true);
            EditorGUILayout.PropertyField(_so.FindProperty("rootNamespace"), true);
            EditorGUILayout.Space(10);
            _referenceReorderableList.DoLayoutList();
            _dependenciesReorderableList.DoLayoutList();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            _so.ApplyModifiedProperties();
        }

        public void PublishAssemblyDefinition(string targetPath)
        {
            if (!_so.FindProperty("generateTarget").boolValue)
            {
                return;
            }
            
            var asmdefJson = new AssemblyDefinitionJsonData();
            var fileName = _folderGenerator.GenerateAsmdefPrefix.Length > 0 ? _folderGenerator.GenerateAsmdefPrefix + "." : "";
            fileName += _entryScriptSettings.InterfaceFolderNames.Exists(t => t == UnitName) ? "Interfaces." + UnitName : UnitName;
            asmdefJson.name = fileName;
            asmdefJson.allowUnsafeCode = _so.FindProperty("allowUnsafeCode").boolValue;
            asmdefJson.autoReferenced = _so.FindProperty("autoReferenced").boolValue;
            asmdefJson.noEngineReferences = _so.FindProperty("noEngineReferences").boolValue;
            asmdefJson.rootNamespace = _so.FindProperty("rootNamespace").stringValue;
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
                    foreach (var selectedDependency in selectedDependencies)
                    {
                        var dependencyFileName = _entryScriptSettings.InterfaceFolderNames.Exists(t => t == selectedDependency) ? "Interfaces." + selectedDependency : selectedDependency;
                        
                        var dependencyAsmdefFilePath = _folderGenerator.GenerateFolderRoot.Length > 0 ? _folderGenerator.GenerateFolderRoot + "/" : _folderGenerator.GenerateFolderRoot;
                        if (_entryScriptSettings.InterfaceFolderNames.Exists(t => t == selectedDependency))
                        {
                            dependencyAsmdefFilePath += "Interfaces/";
                        }
                        
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
    }
}