namespace ABCodeworld.Examples
{
    using EC = ABCodeworld.OmniDoor3D.EditorCommon;
    using EGL = UnityEditor.EditorGUILayout;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(DoorControllerExample))]
    public class ExampleInspector : Editor
    {
        // This custom inspector script interacts with a door controller script in the example scene. It allows you to control the door from the inspector.
        // Select the buttons that control the Wood Fences in the example scene to see how it works. (The buttons are also interactive in Play mode - don't be distracted by that)

        SerializedProperty mySpaceHint;

        private string sSpaceHint;

        private static readonly Color charcoal = new(0.15f, 0.15f, 0.15f);
        private static readonly Color midGreen = new(0f, 0.5f, 0f);
        private static readonly Color midRed = new(0.5f, 0f, 0f);

        private void OnEnable()
        {
            mySpaceHint = serializedObject.FindProperty("spaceHint");
            sSpaceHint = "Points to the UI \"PressSpace\" sprite";
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DoorControllerExample dc = (DoorControllerExample)target;
            EC.DrawProp(mySpaceHint, sSpaceHint, "Space Hint");
            EGL.LabelField("These buttons work during runtime only, when an OmniDoor3D has this GameObject in its \"Controlling Objects\" list. They cause events to fire that the door is listening " +
                "for. You can use the same technique in your scripts to control any OmniDoor3D. You can set more than one door to respond to the same OmniDoor3DController.", EC.labelEmph);
            EGL.BeginHorizontal();

            if (GUILayout.Button("Open", EC.ButtonLabel(charcoal, midGreen), EC.buttonLarge))
                dc.OpenDoor();

            if (GUILayout.Button("Close", EC.ButtonLabel(Color.white, midRed), EC.buttonLarge))
                dc.CloseDoor();

            EGL.EndHorizontal();
            EGL.BeginHorizontal();

            if (GUILayout.Button("Unlock", EC.ButtonLabel(charcoal, midGreen), EC.buttonLarge))
                dc.UnlockDoor();

            if (GUILayout.Button("Lock", EC.ButtonLabel(Color.white, midRed), EC.buttonLarge))
                dc.LockDoor();

            EGL.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
    }
}
