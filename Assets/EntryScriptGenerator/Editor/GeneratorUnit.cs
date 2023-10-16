using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using File = UnityEngine.Windows.File;

namespace EntryScriptGenerator.Editor
{
    public abstract class GeneratorUnit : ScriptableObject, IDisposable
    {
        protected SerializedObject So
        {
            get
            {
                if (_so == null)
                {
                    _so = new SerializedObject(this);
                }
                return _so;
            }   
        }
        private SerializedObject _so;
        protected abstract string SettingJsonPath { get; }

        protected void Initialize()
        {
            if (!File.Exists(SettingJsonPath)) return;
            using var sr = new StreamReader(SettingJsonPath);
            JsonUtility.FromJsonOverwrite(sr.ReadToEnd(), this);
        }
        
        public virtual void Dispose()
        {
            if (!Directory.Exists(Constants.SaveDataFolderPath))
            {
                FileUtility.CreateDirectory(Constants.SaveDataFolderPath);
            }
            
            using var sw = new StreamWriter(SettingJsonPath, false);
            var json = JsonUtility.ToJson(this, true);
            sw.Write(json);
            sw.Flush();
        }

        public abstract void OnGUI();
    }
}