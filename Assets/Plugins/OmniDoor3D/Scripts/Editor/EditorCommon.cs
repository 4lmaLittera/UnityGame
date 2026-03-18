using EGL = UnityEditor.EditorGUILayout;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ABCodeworld.OmniDoor3D
{
    public class EditorCommon
    {
        public static Texture infoIcon { get { return GetInfoIcon(); } }

        public static GUILayoutOption[] iconButtonSmall = new GUILayoutOption[2] { GUILayout.Width(20), GUILayout.Height(20) };
        public static GUILayoutOption[] buttonLarge = new GUILayoutOption[2] { GUILayout.Width(120), GUILayout.Height(50) };
        public static GUIStyle labelEmph = new GUIStyle(GUI.skin.label) { wordWrap = true, fontStyle = FontStyle.Bold, padding = new RectOffset(0, 0, 5, 5) };
        public static GUIStyle labelNormal = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
        public static Color[] secColors { get { return SecCols; } }

        private static Texture txInfoIcon_s;

        private static readonly Color[] SecCols = new Color[5] { Color.cyan, new(1f, 0.8f, 0f), Color.magenta, new(0f, 0.7f, 0f), new(0.9f, 0.5f, 0.1f) };

        // Make a title bar with a background colour and black text
        public static GUIStyle TitleStyle(Color col)
        {
            return new GUIStyle()
            {
                normal = { textColor = Color.black, background = MakeTex(1, 1, col) },
                wordWrap = true,
                richText = true,
                padding = new RectOffset(5, 5, 5, 5),
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
        }

        // Make a coloured label
        public static GUIStyle ColLabel(Color col)
        {
            return new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = col },
                wordWrap = true,
                padding = new RectOffset(0, 0, 10, 2),
                fontStyle = FontStyle.Bold
            };
        }

        // A style to make a label that looks like a button
        public static GUIStyle ButtonLabel(Color textCol, Color bgCol)
        {
            return new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = textCol, background = MakeTex(1, 1, bgCol) },
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontStyle = FontStyle.Bold
            };
        }

        // This is used for drawing most properties, ones that do not need special handling. It draws the property name or a preferred name, and the property field, with a hint button, all on one line.
        public static void DrawProp(SerializedProperty prop, string sText = "", string sTitle = "", bool bUseTitleAsLabel = false, bool bLabelOnly = false)
        {
            EGL.BeginHorizontal();

            if (bLabelOnly)
                EGL.LabelField(sTitle);
            else
            {
                if (bUseTitleAsLabel)
                    EGL.PropertyField(prop, new GUIContent(sTitle));
                else
                    EGL.PropertyField(prop);
            }

            if ((sText.Length > 0) && (sTitle.Length > 0))
            {
                if (GUILayout.Button(new GUIContent(infoIcon, sText), iconButtonSmall))
                    CustomDialogWindow.Show(sTitle, sText);
            }

            EGL.EndHorizontal();
        }

        // Multiple layer selection field
        public static void DrawLayerMaskField(SerializedProperty layerMaskProperty, string sText = "", string sTitle = "")
        {
            EGL.BeginHorizontal();
            List<string> validLayerNames = new();
            List<int> validLayerIndices = new();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);

                if (!string.IsNullOrEmpty(layerName))
                {
                    validLayerNames.Add(layerName);
                    validLayerIndices.Add(i);
                }
            }

            GUIContent label = new GUIContent(string.IsNullOrEmpty(sTitle) ? layerMaskProperty.displayName : sTitle);
            int maskValue = 0;

            for (int i = 0; i < validLayerIndices.Count; i++)
            {
                if ((layerMaskProperty.intValue & (1 << validLayerIndices[i])) != 0)
                    maskValue |= (1 << i);
            }

            int newMaskValue = EGL.MaskField(label, maskValue, validLayerNames.ToArray());

            if (newMaskValue != maskValue)
            {
                int newLayerMask = 0;

                for (int i = 0; i < validLayerIndices.Count; i++)
                {
                    if ((newMaskValue & (1 << i)) != 0)
                        newLayerMask |= (1 << validLayerIndices[i]);
                }

                layerMaskProperty.intValue = newLayerMask;
            }

            if ((sText.Length > 0) && (sTitle.Length > 0))
            {
                if (GUILayout.Button(new GUIContent(infoIcon, sText), iconButtonSmall))
                    CustomDialogWindow.Show(sTitle, sText);
            }

            EGL.EndHorizontal();
        }

        // Single layer selection field
        public static void DrawLayerField(SerializedProperty layerProperty, string sText = "", string sTitle = "")
        {
            EGL.BeginHorizontal();
            List<string> validLayerNames = new();
            List<int> validLayerIndices = new();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);

                if (!string.IsNullOrEmpty(layerName))
                {
                    validLayerNames.Add(layerName);
                    validLayerIndices.Add(i);
                }
            }

            int currentIndex = validLayerIndices.IndexOf(layerProperty.intValue);

            if (currentIndex < 0)
                currentIndex = 0;

            GUIContent label = new GUIContent(string.IsNullOrEmpty(sTitle) ? layerProperty.displayName : sTitle);
            int newIndex = EditorGUILayout.Popup(label, currentIndex, validLayerNames.ToArray());

            if (newIndex != currentIndex)
                layerProperty.intValue = validLayerIndices[newIndex];

            if ((sText.Length > 0) && (sTitle.Length > 0))
            {
                if (GUILayout.Button(new GUIContent(infoIcon, sText), iconButtonSmall))
                    CustomDialogWindow.Show(sTitle, sText);
            }

            EGL.EndHorizontal();
        }

        // Icon for the hint button shown with every property
        private static Texture GetInfoIcon()
        {
            if (txInfoIcon_s == null)
                txInfoIcon_s = Resources.Load<Texture>("InfoIconLt");

            return txInfoIcon_s;
        }

        // Make a solid colour texture of requested size
        public static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }

    // Pop-up help window that shows a message and an OK button, non modal, always on top of editor, non dockable, auto-sizing but resizable, single instance. Used for the property hint buttons.
    public class CustomDialogWindow : EditorWindow
    {
        private static CustomDialogWindow curWindow;
        private string messageText;

        public static void Show(string title, string message)
        {
            if (curWindow != null)
            {
                curWindow.Close();
                curWindow = null;
            }

            curWindow = ScriptableObject.CreateInstance<CustomDialogWindow>();
            curWindow.messageText = message;
            curWindow.titleContent = new GUIContent(title);
            curWindow.ShowUtility();
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            float width = 350f;
            float height = EditorCommon.labelNormal.CalcHeight(new GUIContent(message), width) + 50f;
            float x = main.x + (main.width - width) * 0.5f;
            float y = main.y + (main.height - height) * 0.5f;
            curWindow.position = new Rect(x, y, width, height);
        }

        private void OnGUI()
        {
            EGL.LabelField(messageText, EditorCommon.labelNormal, GUILayout.ExpandHeight(true));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("OK", GUILayout.Width(60), GUILayout.Height(30)))
                Close();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}