using System;
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
        [SerializeField] private List<AssemblyDefinitionAsset> references;

        private string _unitName;
        public string UnitName => _unitName;
        private FolderGenerator _folderGenerator;

        private ReorderableList _referenceReorderableList;
        
        protected override string SettingJsonPath => "Assets/EntryScriptGenerator/Editor/SaveData/FolderGenerateUnit" + _unitName + "Settings.json";
        
        public static FolderGeneratorUnit CreateInstance(FolderGenerator folderGenerator, string unitName)
        {
            var instance = CreateInstance<FolderGeneratorUnit>();
            instance.Initialize(folderGenerator, unitName);
            return instance;
        }

        private void Initialize(FolderGenerator folderGenerator, string unitName)
        {
            _folderGenerator = folderGenerator;
            _unitName = unitName;
            
            base.Initialize();
            
            {
                var property = _so.FindProperty("references");
                _referenceReorderableList = new ReorderableList(_so, property, true, false, true, true)
                {
                    drawElementCallback = (rect, index, isActive, isFocused) => 
                    {
                        var assemblyDefinitionFile = property.GetArrayElementAtIndex(index);
                        rect.height = EditorGUIUtility.singleLineHeight;
                        var label = "(Missing Reference)";
                        if (assemblyDefinitionFile.objectReferenceValue != null)
                        {
                            var nameSo = new SerializedObject(assemblyDefinitionFile.objectReferenceValue);
                            var nameProperty = nameSo.targetObject.name;
                            if (!string.IsNullOrEmpty(nameProperty))
                            {
                                label = nameProperty;
                            }
                        }
                        EditorGUI.PropertyField(rect, assemblyDefinitionFile, new GUIContent(label));
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
            _referenceReorderableList.DoLayoutList();
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
            var fileName = asmdefJson.name = _folderGenerator.GenerateAsmdefPrefix.Length > 0 ? _folderGenerator.GenerateAsmdefPrefix + "." + UnitName : UnitName;
            
            asmdefJson.allowUnsafeCode = _so.FindProperty("allowUnsafeCode").boolValue;
            asmdefJson.autoReferenced = _so.FindProperty("autoReferenced").boolValue;
            asmdefJson.noEngineReferences = _so.FindProperty("noEngineReferences").boolValue;
            asmdefJson.rootNamespace = _so.FindProperty("rootNamespace").stringValue;
            {
                var references = _referenceReorderableList.serializedProperty;
                for (var i=0; i<references.arraySize; i++)
                {
                    var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(references.GetArrayElementAtIndex(i).objectReferenceValue as UnityEngine.Object));
                    asmdefJson.references.Add("GUID:" + guid);
                }
            }
            var json = JsonUtility.ToJson(asmdefJson);

            var path = targetPath + "/" + fileName + ".asmdef";
            StreamWriter writer = new StreamWriter(path, true);
            writer.Write(json);
            writer.Close();
        }
    }
}