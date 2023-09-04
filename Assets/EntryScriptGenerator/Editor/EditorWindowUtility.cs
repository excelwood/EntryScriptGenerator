using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EntryScriptGenerator.Editor
{
    public static class EditorWindowUtility
    {
        public static GUIStyle CreateLineColorStyle()
        {
            var style = new GUIStyle();
            style.normal.background = MakeTex(2, 2, Color.gray);
            return style;
        }
        
        private static Texture2D MakeTex( int width, int height, Color col )
        {
            Color[] pix = new Color[width * height];
            for( int i = 0; i < pix.Length; ++i )
            {
                pix[ i ] = col;
            }
            Texture2D result = new Texture2D( width, height );
            result.SetPixels( pix );
            result.Apply();
            return result;
        }
        
        public static void DragAndDropFilePaths(SerializedObject so, Rect rect, string propertyName, bool isOnlyFileName)
        {
            if (!rect.Contains (Event.current.mousePosition))
            {
                return;
            }
                
            var list = new List<Object> ();
            //現在のイベントを取得
            EventType eventType = Event.current.type;

            //ドラッグ＆ドロップで操作が 更新されたとき or 実行したとき
            if(eventType == EventType.DragUpdated  || eventType == EventType.DragPerform){
                //カーソルに+のアイコンを表示
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                //ドロップされたオブジェクトをリストに登録
                if(eventType == EventType.DragPerform){
                    list = new List<Object> (DragAndDrop.objectReferences);

                    //ドラッグを受け付ける(ドラッグしてカーソルにくっ付いてたオブジェクトが戻らなくなる)
                    DragAndDrop.AcceptDrag ();
                }

                //イベントを使用済みにする
                Event.current.Use();
            }
            
            foreach (var obj in list)
            {
                var resultValue = AssetDatabase.GetAssetPath(obj);
                if (isOnlyFileName)
                {
                    var splitStrings = resultValue.Split("/");
                    resultValue = splitStrings.LastOrDefault();
                }
                
                var property = so.FindProperty(propertyName);
                if (property.propertyType == SerializedPropertyType.String)
                {
                    property.stringValue = resultValue;
                }
                else if(property.isArray)
                {
                    property.InsertArrayElementAtIndex(property.arraySize);
                    property.GetArrayElementAtIndex(property.arraySize - 1).stringValue = resultValue;
                }
                else
                {
                    throw new InvalidOperationException("無効な型のプロパティにドロップしようとしています");
                }
            }
        }

        public static void SyncArraySize<T>(
            SerializedObject so1, string propertyName1, 
            SerializedObject so2, string propertyName2, T defaultValue)
        {
            var paths = so1.FindProperty(propertyName1);
            var suffixes = so2.FindProperty(propertyName2);
            var needCount = paths.arraySize - suffixes.arraySize;
            if (needCount > 0)
            {
                for (var i=0; i<needCount; i++)
                {
                    suffixes.InsertArrayElementAtIndex(suffixes.arraySize);

                    switch (defaultValue)
                    {
                        case int:
                            suffixes.GetArrayElementAtIndex(suffixes.arraySize-1).intValue = Convert.ToInt32(defaultValue);
                            break;
                        case string:
                            suffixes.GetArrayElementAtIndex(suffixes.arraySize-1).stringValue = Convert.ToString(defaultValue);
                            break;
                        case bool:
                            suffixes.GetArrayElementAtIndex(suffixes.arraySize-1).boolValue = Convert.ToBoolean(defaultValue);
                            break;
                    }
                }   
            }
            else
            {
                for (var i = 0; i < Mathf.Abs(needCount); i++)
                {
                    suffixes.DeleteArrayElementAtIndex(suffixes.arraySize-1);
                }
            }
        }
    }
}