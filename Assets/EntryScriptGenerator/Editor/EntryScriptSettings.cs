using System;
using System.Collections.Generic;
using System.Linq;
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
        
        public SerializedObject SerializedObject => _so;
        
        public delegate void OnChangeFolderCount();
        public OnChangeFolderCount OnChangeFolderCountEvent;

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
            OnChangeFolderCountEvent?.Invoke();
        }
        
        protected override string SettingJsonPath => "Assets/EntryScriptGenerator/Editor/SaveData/EntryScriptSettings.json";

        public override void OnGUI()
        {
            _so.Update();
            
            // Common
            EditorGUILayout.BeginVertical(StyleData.CategoryGuiStyle);
            GUILayout.Label("フォルダ構成", EditorStyles.boldLabel);
            GUILayout.Box("", GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
            {
                var rect = EditorGUILayout.BeginVertical();
                var property = _so.FindProperty("interfaceFolderNames");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(property, true);
                if (EditorGUI.EndChangeCheck())
                {
                    _so.ApplyModifiedProperties();
                    OnFoldersListChanged();
                }
                EditorGUILayout.EndVertical();
                EditorWindowUtility.DragAndDropFilePaths(_so, rect, "interfaceFolderNames", true);
            }
            GUILayout.Box("", GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
            {
                var rect = EditorGUILayout.BeginVertical();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_so.FindProperty("classFolderNames"), true);
                if (EditorGUI.EndChangeCheck())
                {
                    _so.ApplyModifiedProperties();
                    OnFoldersListChanged();
                }
                EditorGUILayout.EndVertical();
                EditorWindowUtility.DragAndDropFilePaths(_so, rect, "classFolderNames", true);
            }
            GUILayout.Box("", GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
            
            // Select Tool
            using (new EditorGUILayout.HorizontalScope()) {
                GUILayout.FlexibleSpace();
                // タブを描画する
                selectedTab = (ToolTab)GUILayout.Toolbar((int)selectedTab, Styles.TabToggles, Styles.TabButtonStyle, Styles.TabButtonSize);
                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(10);
            GUILayout.Box("", EditorWindowUtility.CreateLineColorStyle(), GUILayout.Width(_entryScriptGenerator.position.width), GUILayout.Height(1));
            
            GUILayout.Space(10);
            
            EditorGUILayout.EndVertical();
            
            _so.ApplyModifiedProperties();
        }
    }
}