using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;

namespace EntryScriptGenerator.Editor
{
    public class EntryScriptSettings : GeneratorUnit
    {
        [SerializeField] private List<string> interfaceFolderNames = new();
        [SerializeField] private List<string> classFolderNames = new();
        [SerializeField] private ToolTab selectedTab = ToolTab.ScriptGenerator;
        
        public IReadOnlyList<string> InterfaceFolderNames => interfaceFolderNames;
        public IReadOnlyList<string> ClassFolderNames => classFolderNames;
        
        public SerializedObject SerializedObject => So;
        
        public delegate void OnChangeFolderCount();
        public OnChangeFolderCount onChangeFolderCountEvent;

        public delegate void OnImportSettings();
        public OnImportSettings onImportSettingsEvent;

        private EntryScriptGenerator _entryScriptGenerator;

        public ToolTab SelectedTab => selectedTab;
        private static class Styles
        {
            private static GUIContent[] _tabToggles = null;
            public static GUIContent[] TabToggles{
                get {
                    if (_tabToggles == null) {
                        _tabToggles = Enum.GetNames(typeof(ToolTab)).Select(x => new GUIContent(x)).ToArray();
                    }
                    return _tabToggles;
                }
            }
        
            public static readonly GUIStyle TabButtonStyle = "LargeButton";

            // GUI.ToolbarButtonSize.FitToContentsも設定できる
            public static readonly GUI.ToolbarButtonSize TabButtonSize = GUI.ToolbarButtonSize.Fixed;
        }
        
        public static EntryScriptSettings CreateInstance(EntryScriptGenerator entryScriptGenerator)
        {
            var instance = CreateInstance<EntryScriptSettings>();
            instance.Initialize(entryScriptGenerator);
            return instance;
        }

        public void Initialize(EntryScriptGenerator entryScriptGenerator)
        {
            base.Initialize();
            _entryScriptGenerator = entryScriptGenerator;
        }
        
        public void OnFoldersListChanged()
        {
            onChangeFolderCountEvent?.Invoke();
        }
        
        protected override string SettingJsonPath => Constants.SaveDataFolderPath + "/EntryScriptSettings.json";

        public override void OnGUI()
        {
            So.Update();
            
            EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
            FoldersSetting();
            GUILayout.Box("", EditorWindowUtility.CreateLineColorStyle(),GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
            
            SettingsFileOperation();
            GUILayout.Box("", EditorWindowUtility.CreateLineColorStyle(),GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
            
            ToolMenuTab();
            
            EditorGUILayout.EndVertical();
            
            So.ApplyModifiedProperties();
        }

        private void FoldersSetting()
        {
            GUILayout.Label("フォルダ構成", EditorStyles.boldLabel);
            GUILayout.Box("", GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
            {
                var rect = EditorGUILayout.BeginVertical();
                var property = So.FindProperty("interfaceFolderNames");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(property, true);
                if (EditorGUI.EndChangeCheck())
                {
                    So.ApplyModifiedProperties();
                    OnFoldersListChanged();
                }
                EditorGUILayout.EndVertical();
                EditorWindowUtility.DragAndDropFilePaths(So, rect, "interfaceFolderNames", true);
            }
            GUILayout.Box("", GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
            {
                var rect = EditorGUILayout.BeginVertical();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(So.FindProperty("classFolderNames"), true);
                if (EditorGUI.EndChangeCheck())
                {
                    So.ApplyModifiedProperties();
                    OnFoldersListChanged();
                }
                EditorGUILayout.EndVertical();
                EditorWindowUtility.DragAndDropFilePaths(So, rect, "classFolderNames", true);
            }
            GUILayout.Box("", GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
        }

        private void SettingsFileOperation()
        {
            GUILayout.Label("設定ファイル操作", EditorStyles.boldLabel);
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export")) {
                ExportSettings();
            }
            if (GUILayout.Button("Import"))
            {
                ImportSettings();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void ExportSettings()
        {
            // 保存先のファイルパスを取得する
            var filePath = EditorUtility.SaveFilePanel("Export", "Assets", Constants.SettingFolderName, "zip");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            ZipFile.CreateFromDirectory(Constants.SaveDataFolderPath, filePath);
        }

        private void ImportSettings()
        {
            var filePath = EditorUtility.OpenFilePanel("Import", "Assets", "zip");
            ZipFile.ExtractToDirectory(filePath, Constants.SaveDataFolderPath, true);
            base.Initialize();
            onImportSettingsEvent?.Invoke();
        }

        private void ToolMenuTab()
        {
            GUILayout.Label("ツール選択", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope()) {
                GUILayout.FlexibleSpace();
                // タブを描画する
                selectedTab = (ToolTab)GUILayout.Toolbar((int)selectedTab, Styles.TabToggles, Styles.TabButtonStyle, Styles.TabButtonSize);
                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(10);
            GUILayout.Box("", EditorWindowUtility.CreateLineColorStyle(), GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
            
            GUILayout.Space(10);
        }
    }
}