using UnityEditor;
using UnityEngine;

namespace EntryScriptGenerator.Editor
{
    public class MessageWindow : EditorWindow
    {
        private string _message;

        public static void ShowMessageWindow(string message, Vector2 position)
        {
            ShowMessageWindow(message, position, new Vector2(200, 50));
        }

        public static void ShowMessageWindow(string message, Vector2 position, Vector2 size)
        {
            var window = CreateInstance<MessageWindow>();
            window.SetMessage(message);
            window.position = new Rect(position, size);
            window.ShowPopup();
        }
        
        public void SetMessage(string message)
        {
            _message = message;
        }

        private void OnGUI()
        {
            GUILayout.Label(_message, EditorStyles.boldLabel);
            if (GUILayout.Button("OK"))
            {
                Close();
            }
        }

        private void OnLostFocus()
        {
            Close();
        }
    }
}