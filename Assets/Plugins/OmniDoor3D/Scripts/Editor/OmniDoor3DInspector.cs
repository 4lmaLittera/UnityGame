namespace ABCodeworld.OmniDoor3D
{
    using EC = EditorCommon;
    using EGL = UnityEditor.EditorGUILayout;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    [CustomEditor(typeof(OmniDoor3D))]
    [CanEditMultipleObjects]
    public class OmniDoor3DInspector : Editor
    {
        SerializedProperty myStartLocked;
        SerializedProperty myOpeningLayers;
        SerializedProperty myWindowed;
        SerializedProperty mySwingDirection;
        SerializedProperty myDoorKind;
        SerializedProperty myPlayAudio;
        SerializedProperty myHandleKind;
        SerializedProperty myPostLayout;
        SerializedProperty myMotionKind;
        SerializedProperty mySecondsToOpen;
        SerializedProperty mySecondsToClose;
        SerializedProperty myControllingObjects;
        SerializedProperty myCloseOvrSound;
        SerializedProperty myOpenOvrSound;
        SerializedProperty myLockedOvrSound;
        SerializedProperty myLockingOvrSound;
        SerializedProperty myUnlockingOvrSound;
        SerializedProperty myCustomMaterials;
        SerializedProperty myHandleMaterials;
        SerializedProperty myPostMaterials;
        SerializedProperty mySetTravelDistance;
        SerializedProperty mySlideDirection;
        SerializedProperty mySwingAmountX;
        SerializedProperty mySwingAmountY;
        SerializedProperty mySwingAmountZ;
        SerializedProperty myOffsetSlide;
        SerializedProperty myOffsetDirection;
        SerializedProperty mySetOffsetDistance;
        SerializedProperty myOffsetPortion;
        SerializedProperty myAutoOpen;
        SerializedProperty myAutoClose;
        SerializedProperty myTimedClose;
        SerializedProperty myCloseAfter;
        SerializedProperty myTriggerExtents;
        SerializedProperty myTriggerShape;
        SerializedProperty myTriggerRadius;
        SerializedProperty myReverseSwing;
        SerializedProperty myDoorLayer;
        SerializedProperty myTriggerOffset;
        SerializedProperty myLinkedDoors;
        SerializedProperty myOpenDelay;
        SerializedProperty myCloseDelay;

        private DoorKind previousDoorKind;
        private List<int> previousCustomMatIds = new List<int>(), previousHandleMatIds = new List<int>(), previousPostMatIds = new List<int>();
        private string sStartLocked, sOpeningLayers, sWindowed, sSwingDirection, sDoorKind, sPlayAudio, sHandleKind, sPostLayout, sMotionKind, sSecondsToOpen, sSecondsToClose, sControllingObjects, 
            sDoorLayer, sCloseOvrSound, sOpenOvrSound, sLockedOvrSound, sLockingOvrSound, sUnlockingOvrSound, sCustomMaterials, sHandleMaterials, sPostMaterials, sSetTravelDistance, sSlideDirection, 
            sSwingAmountX, sSwingAmountY, sSwingAmountZ, sOffsetSlide, sOffsetDirection, sSetOffsetDistance, sOffsetPortion, sAutoOpen, sAutoClose, sTimedClose, sCloseAfter, sTriggerExtents, 
            sTriggerShape, sTriggerRadius, sReverseSwing, sTriggerOffset, sLinkedDoors, sOpenDelay, sCloseDelay;

        private void OnEnable()
        {
            previousDoorKind = ((OmniDoor3D)target).doorKind;

            Undo.undoRedoPerformed += OnUndoRedo;
            StoreCurrentMaterialReferences();
            myStartLocked = serializedObject.FindProperty("startLocked");
            myOpeningLayers = serializedObject.FindProperty("openingLayers");
            myWindowed = serializedObject.FindProperty("windowed");
            mySwingDirection = serializedObject.FindProperty("swingDirection");
            myDoorKind = serializedObject.FindProperty("doorKind");
            myPlayAudio = serializedObject.FindProperty("playAudio");
            myHandleKind = serializedObject.FindProperty("handleKind");
            myPostLayout = serializedObject.FindProperty("postLayout");
            myMotionKind = serializedObject.FindProperty("motionKind");
            mySecondsToOpen = serializedObject.FindProperty("secondsToOpen");
            mySecondsToClose = serializedObject.FindProperty("secondsToClose");
            myControllingObjects = serializedObject.FindProperty("controllingObjects");
            myCloseOvrSound = serializedObject.FindProperty("closeOvrSound");
            myOpenOvrSound = serializedObject.FindProperty("openOvrSound");
            myLockedOvrSound = serializedObject.FindProperty("lockedOvrSound");
            myLockingOvrSound = serializedObject.FindProperty("lockingOvrSound");
            myUnlockingOvrSound = serializedObject.FindProperty("unlockingOvrSound");
            myCustomMaterials = serializedObject.FindProperty("customMaterials");
            myHandleMaterials = serializedObject.FindProperty("handleMaterials");
            myPostMaterials = serializedObject.FindProperty("postMaterials");
            mySetTravelDistance = serializedObject.FindProperty("setTravelDistance");
            mySlideDirection = serializedObject.FindProperty("slideDirection");
            mySwingAmountX = serializedObject.FindProperty("swingAmountX");
            mySwingAmountY = serializedObject.FindProperty("swingAmountY");
            mySwingAmountZ = serializedObject.FindProperty("swingAmountZ");
            myOffsetSlide = serializedObject.FindProperty("offsetSlide");
            myOffsetDirection = serializedObject.FindProperty("offsetDirection");
            mySetOffsetDistance = serializedObject.FindProperty("setOffsetDistance");
            myOffsetPortion = serializedObject.FindProperty("offsetPortion");
            myAutoOpen = serializedObject.FindProperty("autoOpen");
            myAutoClose = serializedObject.FindProperty("autoClose");
            myTimedClose = serializedObject.FindProperty("timedClose");
            myCloseAfter = serializedObject.FindProperty("closeAfter");
            myTriggerExtents = serializedObject.FindProperty("triggerExtents");
            myTriggerShape = serializedObject.FindProperty("triggerShape");
            myTriggerRadius = serializedObject.FindProperty("triggerRadius");
            myReverseSwing = serializedObject.FindProperty("reverseSwing");
            myDoorLayer = serializedObject.FindProperty("doorLayer");
            myTriggerOffset = serializedObject.FindProperty("triggerOffset");
            myLinkedDoors = serializedObject.FindProperty("linkedDoors");
            myOpenDelay = serializedObject.FindProperty("openDelay");
            myCloseDelay = serializedObject.FindProperty("closeDelay");

            sStartLocked = "Locking is only supported in script. You can have the door be in the locked state when Play mode is entered, by checking this checkbox. To lock or unlock an OmniDoor3D at runtime, you must use an OmniDoor3DController.";
            sOpeningLayers = "You can specify which layers will be allowed to trigger the door. The same Triggering Layers are used for opening and closing.";
            sWindowed = "The ‘Standard’ door presets have an optional window. Toggle it using this checkbox.";
            sSwingDirection = "Auto: The door will swing away from the triggering object.\r\nForward: The door will swing toward positive local axes.\r\nBackward: The door will swing toward negative local axes.";
            sDoorKind = "Select from this drop-down list to choose a preset door, which are fully working doors with customization options. The type of door may affect the availability of other choices in this section, e.g. post related options are only shown for fence gates.";
            sPlayAudio = "Check this checkbox to automatically play audio for the door events. Default sounds are included - you do not need to set custom sounds. For even more control over the sound, disable this checkbox and use the door events as a starting point. Note that audio plays at the beginning of an event, that is, the opening sound will be played when the door starts to open, not after it’s fully open.";
            sHandleKind = "Select from this drop-down list to choose the handles, if any, that you want for the door.";
            sPostLayout = "Choose which fence posts to add, if any.";
            sMotionKind = "Choose from this drop-down list to pick a style of motion for opening and closing the door. Further options shown in this section will depend on the Motion Type.";
            sSecondsToOpen = "The number of seconds it will take for the door to go from fully closed to fully opened.";
            sSecondsToClose = "The number of seconds it will take for the door to go from fully opened to fully closed.";
            sControllingObjects = "Controlling objects are optional. If specified, this door will subscribe itself to the events of the chosen game objects having an OmniDoor3DController component. The events are OpenDoor, CloseDoor, ToggleDoor, LockDoor, and UnlockDoor. Your scripts can invoke these events from an OmniDoor3DController to perform the indicated action on all doors controlled by that controller.";
            sCloseOvrSound = "Specific audio clip to play when closing.";
            sOpenOvrSound = "Specific audio clip to play when opening.";
            sLockedOvrSound = "Specific audio clip to play when the door is attempting to open, but is locked.";
            sLockingOvrSound = "Specific audio clip to play when the door is being locked.";
            sUnlockingOvrSound = "Specific audio clip to play when the door is being unlocked.";
            sCustomMaterials = "Default materials to be used for the door. You can choose other materials to alter the look of the door.";
            sHandleMaterials = "Default materials to be used for the handles. You can choose other materials to alter the look of the handle.";
            sPostMaterials = "Default materials to be used for the fence posts. You can choose other materials to alter the look of the posts.";
            sSetTravelDistance = "This is the number of local units that the door will move in the Slide Direction when opening.";
            sSlideDirection = "This is a Vector3 that specifies the direction the door will move when opening.";
            sSwingAmountX = "This is the amount out of one full rotation that the door will rotate in the given axis, in the open state. For example, a typical swinging door would have 0 X and 0.25-0.45 Y and 0 Z rotation.";
            sSwingAmountY = sSwingAmountX;
            sSwingAmountZ = sSwingAmountX;
            sOffsetSlide = "If this checkbox is checked, you can have the door move to an offset from its position before sliding.";
            sOffsetDirection = "Set this Vector3 to specify the direction of the offset.";
            sSetOffsetDistance = "Set this value to specify the number of local units that the door will move in the Offset Direction before sliding.";
            sOffsetPortion = "Set this value to specify how much of the opening or closing time is taken by the offset part of the motion. For example, if your Seconds to Open is 2, and Offset Portion is 0.2, the offset motion will be done in 0.4s and the main part of the slide in 1.6s.";
            sAutoOpen = "If checked, the door will automatically open when a collider in the Triggering Layers enters the trigger. Note that a triggered open door does not automatically close with a lack of trigger presence, unless you set that option as well.";
            sAutoClose = "If checked, the door will automatically close when it is open and there are no colliders in the Triggering Layers present within the trigger.";
            sTimedClose = "If checked, the door can be set to close after a specified number of seconds. This is independent of controlled or triggered closing, i.e., if set, it will happen even if the door can also close by trigger or controller.";
            sCloseAfter = "Number of seconds to wait after the door becomes fully open, upon which the door will begin closing.";
            sTriggerExtents = "Set the extents of the box. This is how far the box extends from the center of the door mesh's bounds, in the given axes. (N.B.: size = 2 * extents). The same Trigger Extents are used for opening and closing.";
            sTriggerShape = "The desired shape of the trigger - Sphere or Box. The same Trigger Shape is used for opening and closing.";
            sTriggerRadius = "Set the radius of the sphere. The center of the sphere will be the center of the door mesh’s bounds. The same Trigger Radius is used for opening and closing.";
            sReverseSwing = "If checked, the door will swing in the opposite direction calculated. This is useful for quick fixes due to unusual rotation or without having to modify a mesh.";
            sDoorLayer = "This is the layer to use for door, handle, and fence post meshes.";
            sTriggerOffset = "Set this Vector3 to offset the center of the trigger from the center of the door mesh's bounds. The same Trigger Offset is used for opening and closing.";
            sLinkedDoors = "Add other OmniDoor3Ds to this list to have them open, close, lock, and unlock at the same time as this door.";
            sOpenDelay = "If a value is set here, when the door successfully opens, it is delayed by this number of seconds.";
            sCloseDelay = "If a value is set here, when the door successfully closes, it is delayed by this number of seconds.";
        }


        // Util methods to track material changes

        private void StoreCurrentMaterialReferences()
        {
            previousCustomMatIds = GetMaterialIds(myCustomMaterials);
            previousHandleMatIds = GetMaterialIds(myHandleMaterials);
            previousPostMatIds = GetMaterialIds(myPostMaterials);
        }

        private List<int> GetMaterialIds(SerializedProperty materialArray)
        {
            List<int> ids = new List<int>();

            if (materialArray != null && materialArray.isArray)
            {
                for (int i = 0; i < materialArray.arraySize; i++)
                {
                    SerializedProperty element = materialArray.GetArrayElementAtIndex(i);

                    if (element.objectReferenceValue != null)
                        ids.Add(element.objectReferenceValue.GetInstanceID());
                    else
                        ids.Add(0);
                }
            }

            return ids;
        }

        private bool CheckMaterialsChanged()
        {
            List<int> currentCustomMatIds = GetMaterialIds(myCustomMaterials);
            List<int> currentHandleMatIds = GetMaterialIds(myHandleMaterials);
            List<int> currentPostMatIds = GetMaterialIds(myPostMaterials);
            bool customChanged = !AreEqual(previousCustomMatIds, currentCustomMatIds);
            bool handleChanged = !AreEqual(previousHandleMatIds, currentHandleMatIds);
            bool postChanged = !AreEqual(previousPostMatIds, currentPostMatIds);

            if (customChanged) 
                previousCustomMatIds = currentCustomMatIds;

            if (handleChanged) 
                previousHandleMatIds = currentHandleMatIds;

            if (postChanged) 
                previousPostMatIds = currentPostMatIds;

            return customChanged || handleChanged || postChanged;
        }

        private bool AreEqual(List<int> list1, List<int> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i] != list2[i])
                    return false;
            }

            return true;
        }

        // End material util methods

        // Let all selected doors know they should update themselves visually
        private void UpdateAllDoors()
        {
            foreach (var target in targets)
            {
                OmniDoor3D door = (OmniDoor3D)target;
                door.OnDoorVisChanged();
                EditorUtility.SetDirty(door);

                if (!Application.isPlaying)
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(door.gameObject.scene);
            }
        }

        // Let all selected doors know they should update their layers
        private void UpdateLayers()
        {
            foreach (var target in targets)
            {
                OmniDoor3D door = (OmniDoor3D)target;
                door.OnLayerChanged();
                EditorUtility.SetDirty(door);

                if (!Application.isPlaying)
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(door.gameObject.scene);
            }
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo; // Unsubscribe from the event or face the consequences (MissingReferenceException?)
        }

        // Undoing a change is a change, so update all doors
        private void OnUndoRedo()
        {
            serializedObject.Update();

            foreach (var target in targets)
            {
                OmniDoor3D door = (OmniDoor3D)target;
                door.OnDoorVisChanged();
                EditorUtility.SetDirty(door);
            }

            Repaint();
        }

        // Draw the inspector GUI. This happens when the game object is selected and the inspector is open.
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EGL.LabelField("Door Setup", EC.TitleStyle(EC.secColors[0]));
            var door = (OmniDoor3D)target;
            previousDoorKind = door.doorKind;
            EditorGUI.BeginChangeCheck();
            DrawSortedDoorTypes(myDoorKind, sDoorKind, "Door Preset");

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                ((OmniDoor3D)target).lastDoorKind = previousDoorKind;
                UpdateAllDoors();
            }

            EditorGUI.BeginChangeCheck();
            EC.DrawProp(myCustomMaterials, sCustomMaterials, "Door Materials", true);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                UpdateAllDoors();
                StoreCurrentMaterialReferences();
            }

            EC.DrawProp(myControllingObjects, sControllingObjects, "Controlling Objects");
            EC.DrawProp(myLinkedDoors, sLinkedDoors, "Linked Doors");
            EditorGUI.BeginChangeCheck();
            EC.DrawLayerField(myDoorLayer, sDoorLayer, "Door Layer");

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                UpdateLayers();
            }

            EditorGUI.BeginChangeCheck();
            DrawFilteredHandleTypes(myHandleKind, sHandleKind, "Handle Type");

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                UpdateAllDoors();
            }

            if ((HandleKind)myHandleKind.enumValueIndex != HandleKind.None)
            {
                EditorGUI.BeginChangeCheck();
                EC.DrawProp(myHandleMaterials, sHandleMaterials, "Handle Materials");

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    UpdateAllDoors();
                    StoreCurrentMaterialReferences();
                }
            }

            if (OmniDoor3DData.IsStandard((DoorKind)myDoorKind.enumValueIndex))
            {
                EditorGUI.BeginChangeCheck();
                EC.DrawProp(myWindowed, sWindowed, "Windowed");

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    UpdateAllDoors();
                }
            }
            else if (OmniDoor3DData.IsFence((DoorKind)myDoorKind.enumValueIndex))
            {
                EditorGUI.BeginChangeCheck();
                EC.DrawProp(myPostLayout, sPostLayout, "Posts");

                if ((PostLayout)myPostLayout.enumValueIndex != PostLayout.None)
                {
                    EditorGUI.BeginChangeCheck();
                    EC.DrawProp(myPostMaterials, sPostMaterials, "Post Materials");

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        UpdateAllDoors();
                        StoreCurrentMaterialReferences();
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    UpdateAllDoors();
                }
            }

            EGL.Separator();
            EGL.Separator();

            EGL.LabelField("Opening Options", EC.TitleStyle(EC.secColors[3]));
            EC.DrawProp(mySecondsToOpen, sSecondsToOpen, "Seconds To Open");
            EC.DrawProp(myOpenDelay, sOpenDelay, "Open Delay (s)", true);
            EC.DrawProp(myAutoOpen, sAutoOpen, "Triggered Open", true);

            if (myAutoOpen.boolValue)
                DrawTriggerOptions();

            EC.DrawProp(myStartLocked, sStartLocked, "Start Locked");
            EGL.Separator();
            EGL.Separator();

            EGL.LabelField("Closing Options", EC.TitleStyle(EC.secColors[1]));
            EC.DrawProp(mySecondsToClose, sSecondsToClose, "Seconds To Close");
            EC.DrawProp(myCloseDelay, sCloseDelay, "Close Delay (s)", true);
            EC.DrawProp(myAutoClose, sAutoClose, "Triggered Close", true);

            if (myAutoClose.boolValue)
                DrawTriggerOptions();

            EC.DrawProp(myTimedClose, sTimedClose, "Timed Close");

            if (myTimedClose.boolValue)
                EC.DrawProp(myCloseAfter, sCloseAfter, "Close After (s)", true);

            EGL.Separator();
            EGL.Separator();

            EGL.LabelField("Motion Options", EC.TitleStyle(EC.secColors[2]));
            DrawFilteredMotionTypes(myMotionKind, sMotionKind, "Motion Type");

            if ((MotionKind)myMotionKind.enumValueIndex == MotionKind.Swing)
            {
                EC.DrawProp(mySwingDirection, sSwingDirection, "Swing Direction");

                if ((SwDir)mySwingDirection.enumValueIndex == SwDir.Auto)
                    EC.DrawProp(myReverseSwing, sReverseSwing, "Reverse Swing");

                EC.DrawProp(mySwingAmountX, sSwingAmountX, "X Swing Amount");
                EC.DrawProp(mySwingAmountY, sSwingAmountY, "Y Swing Amount");
                EC.DrawProp(mySwingAmountZ, sSwingAmountZ, "Z Swing Amount");
            }
            else if ((MotionKind)myMotionKind.enumValueIndex == MotionKind.Slide)
            {
                EC.DrawProp(mySlideDirection, sSlideDirection, "Slide Direction");
                EC.DrawProp(mySetTravelDistance, sSetTravelDistance, "Slide Distance", true);
                EC.DrawProp(myOffsetSlide, sOffsetSlide, "Offset Slide");

                if (myOffsetSlide.boolValue)
                {
                    EC.DrawProp(myOffsetDirection, sOffsetDirection, "Offset Direction");
                    EC.DrawProp(mySetOffsetDistance, sSetOffsetDistance, "Offset Distance", true);
                    EC.DrawProp(myOffsetPortion, sOffsetPortion, "Offset Time Portion");
                }
            }

            EGL.Separator();
            EGL.Separator();
            EGL.LabelField("Sound Options", EC.TitleStyle(EC.secColors[4])); EGL.Separator();
            EC.DrawProp(myPlayAudio, sPlayAudio, "Play Audio");
            EC.DrawProp(myCloseOvrSound, sCloseOvrSound, "Custom Close Sound", true);
            EC.DrawProp(myOpenOvrSound, sOpenOvrSound, "Custom Open Sound", true);
            EC.DrawProp(myLockedOvrSound, sLockedOvrSound, "Custom Locked Sound", true);
            EC.DrawProp(myLockingOvrSound, sLockingOvrSound, "Custom Locking Sound", true);
            EC.DrawProp(myUnlockingOvrSound, sUnlockingOvrSound, "Custom Unlocking Sound", true);
            EGL.Separator();
            EGL.Separator();
            serializedObject.ApplyModifiedProperties();

            // This covers changes to materials by drag & drop, right-click paste, ctrl+v, etc.
            if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.KeyUp)
            {
                EditorApplication.delayCall += () => 
                {
                    if (this == null || target == null) 
                        return;

                    serializedObject.Update();

                    if (CheckMaterialsChanged())
                        UpdateAllDoors();
                };
            }

            base.OnInspectorGUI();

            // Since this section is potentially drawn twice, reuse code
            void DrawTriggerOptions()
            {
                EditorGUI.BeginChangeCheck();
                EC.DrawProp(myTriggerShape, sTriggerShape, "Trigger Shape");

                if ((TriggerShape)myTriggerShape.enumValueIndex == TriggerShape.Box)
                    EC.DrawProp(myTriggerExtents, sTriggerExtents, "Trigger Extents");
                else
                    EC.DrawProp(myTriggerRadius, sTriggerRadius, "Trigger Radius");

                EC.DrawProp(myTriggerOffset, sTriggerOffset, "Trigger Offset");

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();

                    foreach (var target in targets)
                    {
                        OmniDoor3D door = (OmniDoor3D)target;
                        door.UpdateTriggerShapeAndSize();
                        EditorUtility.SetDirty(door);

                        if (!Application.isPlaying)
                            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(door.gameObject.scene);
                    }
                }

                EC.DrawLayerMaskField(myOpeningLayers, sOpeningLayers, "Triggering Layers");
            }
        }

        // Sorting and special filtering methods

        private void DrawSortedDoorTypes(SerializedProperty enumProperty, string sText, string sTitle)
        {
            Type enumType = typeof(DoorKind);
            var enumValues = Enum.GetValues(enumType).Cast<DoorKind>().ToArray();
            string[] displayNames = new string[enumValues.Length];

            for (int i = 0; i < enumValues.Length; i++)
            {
                var fieldInfo = enumType.GetField(enumValues[i].ToString());
                var attrs = fieldInfo.GetCustomAttributes(typeof(InspectorNameAttribute), false);

                if (attrs.Length > 0)
                    displayNames[i] = ((InspectorNameAttribute)attrs[0]).displayName;
                else
                    displayNames[i] = enumValues[i].ToString();
            }

            var sortedPairs = new List<KeyValuePair<string, int>>();

            for (int i = 0; i < displayNames.Length; i++)
                sortedPairs.Add(new KeyValuePair<string, int>(displayNames[i], i));
            
            sortedPairs.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));
            string[] sortedNames = new string[sortedPairs.Count];
            int[] sortedValues = new int[sortedPairs.Count];

            for (int i = 0; i < sortedPairs.Count; i++)
            {
                sortedNames[i] = sortedPairs[i].Key;
                sortedValues[i] = (int)enumValues[sortedPairs[i].Value];
            }

            int currentIndex = 0;

            for (int i = 0; i < sortedValues.Length; i++)
            {
                if (sortedValues[i] == enumProperty.enumValueIndex)
                {
                    currentIndex = i;
                    break;
                }
            }

            EGL.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int newSortedIndex = EGL.Popup(sTitle, currentIndex, sortedNames);

            if (EditorGUI.EndChangeCheck())
                enumProperty.enumValueIndex = sortedValues[newSortedIndex];

            if (GUILayout.Button(new GUIContent(EC.infoIcon, sText), EC.iconButtonSmall))
                CustomDialogWindow.Show(sTitle, sText);

            EGL.EndHorizontal();
        }

        private void DrawFilteredHandleTypes(SerializedProperty enumProperty, string sText, string sTitle)
        {
            Type enumType = typeof(HandleKind);
            var enumValues = Enum.GetValues(enumType).Cast<HandleKind>().ToArray();
            string[] displayNames = new string[enumValues.Length];

            for (int i = 0; i < enumValues.Length; i++)
            {
                var fieldInfo = enumType.GetField(enumValues[i].ToString());
                var attrs = fieldInfo.GetCustomAttributes(typeof(InspectorNameAttribute), false);

                if ((i == 0) || (((OmniDoor3D)target).GetHandlePlacement(((OmniDoor3D)target).GetDoorInfo((DoorKind)myDoorKind.enumValueIndex), (HandleKind)i, out _)))
                {
                    if (attrs.Length > 0)
                        displayNames[i] = ((InspectorNameAttribute)attrs[0]).displayName;
                    else
                        displayNames[i] = enumValues[i].ToString();
                }
                else
                    displayNames[i] = "~";
            }

            var sortedPairs = new List<KeyValuePair<string, int>>();

            for (int i = 0; i < displayNames.Length; i++)
            {
                if (displayNames[i] != "~")
                    sortedPairs.Add(new KeyValuePair<string, int>(displayNames[i], i));
            }

            sortedPairs.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));
            string[] sortedNames = new string[sortedPairs.Count];
            int[] sortedValues = new int[sortedPairs.Count];

            for (int i = 0; i < sortedPairs.Count; i++)
            {
                sortedNames[i] = sortedPairs[i].Key;
                sortedValues[i] = (int)enumValues[sortedPairs[i].Value];
            }

            int currentIndex = 0;

            for (int i = 0; i < sortedValues.Length; i++)
            {
                if (sortedValues[i] == enumProperty.enumValueIndex)
                {
                    currentIndex = i;
                    break;
                }
            }

            EGL.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int newSortedIndex = EGL.Popup(sTitle, currentIndex, sortedNames);

            if (EditorGUI.EndChangeCheck())
                enumProperty.enumValueIndex = sortedValues[newSortedIndex];

            if (GUILayout.Button(new GUIContent(EC.infoIcon, sText), EC.iconButtonSmall))
                CustomDialogWindow.Show(sTitle, sText);

            EGL.EndHorizontal();
        }

        private void DrawFilteredMotionTypes(SerializedProperty enumProperty, string sText, string sTitle)
        {
            Type enumType = typeof(MotionKind);
            var enumValues = Enum.GetValues(enumType).Cast<MotionKind>().ToArray();
            string[] displayNames = new string[enumValues.Length];

            for (int i = 0; i < enumValues.Length; i++)
            {
                var fieldInfo = enumType.GetField(enumValues[i].ToString());
                var attrs = fieldInfo.GetCustomAttributes(typeof(InspectorNameAttribute), false);

                if (((OmniDoor3D)target).MotionAllowed((MotionKind)i, (DoorKind)myDoorKind.enumValueIndex))
                {
                    if (attrs.Length > 0)
                        displayNames[i] = ((InspectorNameAttribute)attrs[0]).displayName;
                    else
                        displayNames[i] = enumValues[i].ToString();
                }
                else
                    displayNames[i] = "~";
            }

            var sortedPairs = new List<KeyValuePair<string, int>>();

            for (int i = 0; i < displayNames.Length; i++)
            {
                if (displayNames[i] != "~")
                    sortedPairs.Add(new KeyValuePair<string, int>(displayNames[i], i));
            }

            sortedPairs.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));
            string[] sortedNames = new string[sortedPairs.Count];
            int[] sortedValues = new int[sortedPairs.Count];

            for (int i = 0; i < sortedPairs.Count; i++)
            {
                sortedNames[i] = sortedPairs[i].Key;
                sortedValues[i] = (int)enumValues[sortedPairs[i].Value];
            }

            int currentIndex = 0;

            for (int i = 0; i < sortedValues.Length; i++)
            {
                if (sortedValues[i] == enumProperty.enumValueIndex)
                {
                    currentIndex = i;
                    break;
                }
            }

            EGL.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int newSortedIndex = EGL.Popup(sTitle, currentIndex, sortedNames);

            if (EditorGUI.EndChangeCheck())
                enumProperty.enumValueIndex = sortedValues[newSortedIndex];

            if (GUILayout.Button(new GUIContent(EC.infoIcon, sText), EC.iconButtonSmall))
                CustomDialogWindow.Show(sTitle, sText);

            EGL.EndHorizontal();
        }
    }
}