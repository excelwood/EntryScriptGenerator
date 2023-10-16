using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EntryScriptGenerator.Editor
{
    public class EntryScriptGenerator : EditorWindow
    {
        private EntryScriptSettings _entryScriptSettings;
        private ScriptGenerator _scriptGenerator;
        private FolderGenerator _folderGenerator;

        [MenuItem("Tools/EntryScriptGenerator")]
        private static void OpenWindow() {
            var window = GetWindow(typeof(EntryScriptGenerator), false, "EntryScriptGenerator");
            window.Show();
        }

        private void OnEnable()
        {
            CheckAndInitialize();
        }

        private void OnDisable()
        {
            Dispose();
        }

        private void Dispose()
        {
            SaveSettings();
        }

        public void SaveSettings()
        {
            if (_entryScriptSettings != null) 
            {
                _entryScriptSettings.onImportSettingsEvent -= OnImportSettings;
                _entryScriptSettings.Dispose();
            }

            if (_scriptGenerator != null)
            {
                _scriptGenerator.Dispose();
            }

            if (_folderGenerator != null)
            {
                _folderGenerator.Dispose();
            }
        }

        private void CheckAndInitialize()
        {
            if (_entryScriptSettings == null)
            {
                _entryScriptSettings = EntryScriptSettings.CreateInstance(this);
                _entryScriptSettings.onImportSettingsEvent += OnImportSettings;
            }
            if (_scriptGenerator == null)
            {
                _scriptGenerator = ScriptGenerator.CreateInstance(this, _entryScriptSettings);
            }
            if (_folderGenerator == null)
            {
                _folderGenerator = FolderGenerator.CreateInstance(this, _entryScriptSettings);
            }
        }

        private void OnImportSettings()
        {
            if (_scriptGenerator != null)
            {
                _scriptGenerator.Dispose();
            }
            _scriptGenerator = ScriptGenerator.CreateInstance(this, _entryScriptSettings);
            
            if (_folderGenerator != null)
            {
                _folderGenerator.Dispose();
            }
            _folderGenerator = FolderGenerator.CreateInstance(this, _entryScriptSettings);
        }
        
        private void OnGUI()
        {
            CheckAndInitialize();
            
            _entryScriptSettings.OnGUI();
            
            switch (_entryScriptSettings.SelectedTab)
            {
                // ScriptGenerator
                case ToolTab.ScriptGenerator:
                    _scriptGenerator.OnGUI();
                    break;
                case ToolTab.FolderGenerator:
                    _folderGenerator.OnGUI();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}