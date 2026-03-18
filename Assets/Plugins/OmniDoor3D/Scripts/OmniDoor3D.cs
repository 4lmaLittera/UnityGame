using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ABCodeworld.OmniDoor3D
{
    public class OmniDoor3D : MonoBehaviour
    {
        // These events are provided for your scripts to listen to
        public delegate void DoorOpenedEvt(OmniDoor3D door);
        public DoorOpenedEvt DoorOpened;
        public delegate void DoorOpenFailedEvt(OmniDoor3D door);
        public DoorOpenFailedEvt DoorOpenFailed;
        public delegate void DoorClosedEvt(OmniDoor3D door);
        public DoorClosedEvt DoorClosed;
        public delegate void DoorWillOpenEvt(OmniDoor3D door);
        public DoorWillOpenEvt DoorWillOpen;
        public delegate void DoorWillCloseEvt(OmniDoor3D door);
        public DoorWillCloseEvt DoorWillClose;
        public delegate void DoorLockedEvt(OmniDoor3D door);
        public DoorLockedEvt DoorLocked;
        public delegate void DoorUnlockedEvt(OmniDoor3D door);
        public DoorUnlockedEvt DoorUnlocked;

        // Inspector properties - drawn by OmniDoor3DInspector - see the tooltips in the editor for info about each property
        [HideInInspector, SerializeField] public bool autoClose, autoOpen, offsetSlide, playAudio = true, reverseSwing, startLocked, timedClose, windowed = true;
        [HideInInspector, SerializeField] public DoorKind doorKind = DoorKind.StandardWooden;
        [HideInInspector, SerializeField, Min(0f)] public float closeAfter, closeDelay, openDelay;
        [HideInInspector, SerializeField, Range(0f, 1f)] public float swingAmountX = 0f, swingAmountY = 0.25f, swingAmountZ = 0f;
        [HideInInspector, SerializeField, Min(0.05f)] public float secondsToClose = 0.5f, secondsToOpen = 0.5f, setOffsetDistance = 0.25f, setTravelDistance = 1f, triggerRadius = 1f;
        [HideInInspector, SerializeField, Range(0.05f, 0.95f)] public float offsetPortion = 0.2f;
        [HideInInspector, SerializeField] public LayerMask openingLayers;
        [HideInInspector, SerializeField] public TriggerShape triggerShape;
        [HideInInspector, SerializeField] public SwDir swingDirection;
        [HideInInspector, SerializeField] public HandleKind handleKind = HandleKind.Lever;
        [HideInInspector, SerializeField] public PostLayout postLayout;
        [HideInInspector, SerializeField] public MotionKind motionKind;
        [HideInInspector, SerializeField] public OmniDoor3DController[] controllingObjects;
        [HideInInspector, SerializeField] public OmniDoor3D[] linkedDoors;
        [HideInInspector, SerializeField] public AudioClip openOvrSound, closeOvrSound, lockedOvrSound, lockingOvrSound, unlockingOvrSound;
        [HideInInspector, SerializeField] public Material[] customMaterials = new Material[0], handleMaterials = new Material[0], postMaterials = new Material[0];
        [HideInInspector, SerializeField] public Vector3 offsetDirection = Vector3.back, slideDirection = Vector3.right, triggerExtents = 2f * Vector3.one, triggerOffset = Vector3.zero;
        [HideInInspector, SerializeField] public int doorLayer = 0;

        public bool needClose { get { return _needClose; } set { _needClose = value; } } // This is 
        public DoorKind lastDoorKind { get { return _lastDoorKind; } set { _lastDoorKind = value; } } // This is used to determine if the door type has changed, so we can update the visuals

        // Working variables that don't need persistence
        private AudioClip openClip, closeClip, lockedClip, lockingClip, unlockingClip;
        private AudioSource aSrc;
        private bool closeDelayComplete = true, isLocked, isMoving, isOpen, motionOpen, _needClose, negZ, openDelayComplete, openFwd;
        private Coroutine closeAfterRoutine, closeRoutine, openRoutine;
        private DoorKind _lastDoorKind;
        private float motionStartPct, motionStartTime, percentComplete = 1f, rollUpPosition = 0f, targetAngleX, targetAngleY, targetAngleZ, timeElapsed, swingStartAngleX, swingStartAngleY,
            swingStartAngleZ;
        private List<Collider> openColliders = new();
        private OmniDoor3DConfig config;
        private Vector3 targetPos, slideOffset, slideStartPos;

        // These working variables are not properties but need to persist their values in and out of Play mode
        [HideInInspector, SerializeField] private Bounds doorBounds;
        [HideInInspector, SerializeField] private HandleKind lastHandleKind;
        [HideInInspector, SerializeField] private float autoOffsetDistance, autoTravelDistance;
        [HideInInspector, SerializeField] private List<GameObject> internalPivots = new();
        [HideInInspector, SerializeField] private MeshRenderer theDoor;

        // These sets of waypoints are for the sections of the garage door when it rolls up and down
        private static readonly Vector3[,] section0Waypoints = {
    { new Vector3(0, 0.00f, 0.00f), new Vector3(0, 0, 0) },    // 0%
    { new Vector3(0, 0.15f, 0.00f), new Vector3(0, 0, 0) },    // 5%
    { new Vector3(0, 0.30f, 0.00f), new Vector3(0, 0, 0) },    // 10%
    { new Vector3(0, 0.45f, 0.00f), new Vector3(0, 0, 0) },    // 15%
    { new Vector3(0, 0.60f, 0.00f), new Vector3(0, 0, 0) },    // 20%
    { new Vector3(0, 0.75f, 0.00f), new Vector3(0, 0, 0) },    // 25%
    { new Vector3(0, 0.90f, 0.00f), new Vector3(0, 0, 0) },    // 30%
    { new Vector3(0, 1.05f, 0.00f), new Vector3(0, 0, 0) },    // 35%
    { new Vector3(0, 1.20f, 0.00f), new Vector3(0, 0, 0) },    // 40%
    { new Vector3(0, 1.35f, 0.00f), new Vector3(0, 0, 0) },    // 45%
    { new Vector3(0, 1.50f, 0.00f), new Vector3(0, 0, 0) },    // 50%
    { new Vector3(0, 1.65f, 0.00f), new Vector3(0, 0, 0) },    // 55%
    { new Vector3(0, 1.80f, 0.00f), new Vector3(5, 0, 0) },    // 60%
    { new Vector3(0, 1.90f, 0.01f), new Vector3(15, 0, 0) },   // 65%
    { new Vector3(0, 1.98f, 0.03f), new Vector3(30, 0, 0) },   // 70%
    { new Vector3(0, 2.05f, 0.10f), new Vector3(45, 0, 0) },   // 75%
    { new Vector3(0, 2.10f, 0.20f), new Vector3(60, 0, 0) },   // 80%
    { new Vector3(0, 2.14f, 0.32f), new Vector3(75, 0, 0) },   // 85%
    { new Vector3(0, 2.16f, 0.48f), new Vector3(85, 0, 0) },   // 90%
    { new Vector3(0, 2.16f, 0.56f), new Vector3(88, 0, 0) },   // 95%
    { new Vector3(0, 2.16f, 0.64f), new Vector3(90, 0, 0) }    // 100%
};

        private static readonly Vector3[,] section1Waypoints = {
    { new Vector3(0, 0.54f, 0.00f), new Vector3(0, 0, 0) },    // 0%
    { new Vector3(0, 0.69f, 0.00f), new Vector3(0, 0, 0) },    // 5%
    { new Vector3(0, 0.84f, 0.00f), new Vector3(0, 0, 0) },    // 10%
    { new Vector3(0, 0.99f, 0.00f), new Vector3(0, 0, 0) },    // 15%
    { new Vector3(0, 1.14f, 0.00f), new Vector3(0, 0, 0) },    // 20%
    { new Vector3(0, 1.29f, 0.00f), new Vector3(0, 0, 0) },    // 25%
    { new Vector3(0, 1.44f, 0.00f), new Vector3(0, 0, 0) },    // 30%
    { new Vector3(0, 1.59f, 0.00f), new Vector3(0, 0, 0) },    // 35%
    { new Vector3(0, 1.74f, 0.00f), new Vector3(0, 0, 0) },    // 40%
    { new Vector3(0, 1.89f, 0.00f), new Vector3(5, 0, 0) },    // 45%
    { new Vector3(0, 1.98f, 0.01f), new Vector3(15, 0, 0) },   // 50%
    { new Vector3(0, 2.05f, 0.03f), new Vector3(30, 0, 0) },   // 55%
    { new Vector3(0, 2.10f, 0.10f), new Vector3(45, 0, 0) },   // 60%
    { new Vector3(0, 2.14f, 0.20f), new Vector3(60, 0, 0) },   // 65%
    { new Vector3(0, 2.16f, 0.32f), new Vector3(75, 0, 0) },   // 70%
    { new Vector3(0, 2.16f, 0.48f), new Vector3(85, 0, 0) },   // 75%
    { new Vector3(0, 2.16f, 0.64f), new Vector3(90, 0, 0) },   // 80%
    { new Vector3(0, 2.16f, 0.82f), new Vector3(90, 0, 0) },   // 85%
    { new Vector3(0, 2.16f, 1.00f), new Vector3(90, 0, 0) },   // 90%
    { new Vector3(0, 2.16f, 1.10f), new Vector3(90, 0, 0) },   // 95%
    { new Vector3(0, 2.16f, 1.18f), new Vector3(90, 0, 0) }    // 100%
};

        private static readonly Vector3[,] section2Waypoints = {
    { new Vector3(0, 1.08f, 0.00f), new Vector3(0, 0, 0) },    // 0%
    { new Vector3(0, 1.23f, 0.00f), new Vector3(0, 0, 0) },    // 5%
    { new Vector3(0, 1.38f, 0.00f), new Vector3(0, 0, 0) },    // 10%
    { new Vector3(0, 1.53f, 0.00f), new Vector3(0, 0, 0) },    // 15%
    { new Vector3(0, 1.68f, 0.00f), new Vector3(0, 0, 0) },    // 20%
    { new Vector3(0, 1.83f, 0.00f), new Vector3(0, 0, 0) },    // 25%
    { new Vector3(0, 1.98f, 0.00f), new Vector3(5, 0, 0) },    // 30%
    { new Vector3(0, 2.05f, 0.01f), new Vector3(15, 0, 0) },   // 35%
    { new Vector3(0, 2.10f, 0.03f), new Vector3(30, 0, 0) },   // 40%
    { new Vector3(0, 2.14f, 0.10f), new Vector3(45, 0, 0) },   // 45%
    { new Vector3(0, 2.16f, 0.20f), new Vector3(60, 0, 0) },   // 50%
    { new Vector3(0, 2.16f, 0.32f), new Vector3(75, 0, 0) },   // 55%
    { new Vector3(0, 2.16f, 0.48f), new Vector3(85, 0, 0) },   // 60%
    { new Vector3(0, 2.16f, 0.64f), new Vector3(90, 0, 0) },   // 65%
    { new Vector3(0, 2.16f, 0.82f), new Vector3(90, 0, 0) },   // 70%
    { new Vector3(0, 2.16f, 1.00f), new Vector3(90, 0, 0) },   // 75%
    { new Vector3(0, 2.16f, 1.18f), new Vector3(90, 0, 0) },   // 80%
    { new Vector3(0, 2.16f, 1.36f), new Vector3(90, 0, 0) },   // 85%
    { new Vector3(0, 2.16f, 1.54f), new Vector3(90, 0, 0) },   // 90%
    { new Vector3(0, 2.16f, 1.64f), new Vector3(90, 0, 0) },   // 95%
    { new Vector3(0, 2.16f, 1.72f), new Vector3(90, 0, 0) }    // 100%
};

        private static readonly Vector3[,] section3Waypoints = {
    { new Vector3(0, 1.62f, 0.00f), new Vector3(0, 0, 0) },    // 0%
    { new Vector3(0, 1.77f, 0.00f), new Vector3(0, 0, 0) },    // 5%
    { new Vector3(0, 1.92f, 0.00f), new Vector3(5, 0, 0) },    // 10%
    { new Vector3(0, 2.02f, 0.01f), new Vector3(15, 0, 0) },   // 15%
    { new Vector3(0, 2.09f, 0.03f), new Vector3(30, 0, 0) },   // 20%
    { new Vector3(0, 2.14f, 0.10f), new Vector3(45, 0, 0) },   // 25%
    { new Vector3(0, 2.16f, 0.20f), new Vector3(60, 0, 0) },   // 30%
    { new Vector3(0, 2.16f, 0.32f), new Vector3(75, 0, 0) },   // 35%
    { new Vector3(0, 2.16f, 0.48f), new Vector3(85, 0, 0) },   // 40%
    { new Vector3(0, 2.16f, 0.64f), new Vector3(90, 0, 0) },   // 45%
    { new Vector3(0, 2.16f, 0.82f), new Vector3(90, 0, 0) },   // 50%
    { new Vector3(0, 2.16f, 1.00f), new Vector3(90, 0, 0) },   // 55%
    { new Vector3(0, 2.16f, 1.18f), new Vector3(90, 0, 0) },   // 60%
    { new Vector3(0, 2.16f, 1.36f), new Vector3(90, 0, 0) },   // 65%
    { new Vector3(0, 2.16f, 1.54f), new Vector3(90, 0, 0) },   // 70%
    { new Vector3(0, 2.16f, 1.72f), new Vector3(90, 0, 0) },   // 75%
    { new Vector3(0, 2.16f, 1.90f), new Vector3(90, 0, 0) },   // 80%
    { new Vector3(0, 2.16f, 2.08f), new Vector3(90, 0, 0) },   // 85%
    { new Vector3(0, 2.16f, 2.18f), new Vector3(90, 0, 0) },   // 90%
    { new Vector3(0, 2.16f, 2.22f), new Vector3(90, 0, 0) },   // 95%
    { new Vector3(0, 2.16f, 2.26f), new Vector3(90, 0, 0) }    // 100%
};

        private void Start()
        {
            // Attach to controllers
            foreach (OmniDoor3DController controllingObject in controllingObjects)
            {
                if (controllingObject != null)
                {
                    controllingObject.OpenDoor += OnControllerOpen;
                    controllingObject.CloseDoor += OnControllerClose;
                    controllingObject.ToggleDoor += OnControllerToggle;
                    controllingObject.UnlockDoor += OnControllerUnlock;
                    controllingObject.LockDoor += OnControllerLock;
                }
            }

            DoorOpened += OnDoorOpened; // Listen to own door opened event, so I can do timed close
            aSrc = GetComponent<AudioSource>();
            isLocked = startLocked;
            _lastDoorKind = doorKind;
            lastHandleKind = handleKind;
            UpdateDoorVisuals(); // Visual update on start (ensures the door looks right according to property settings)
        }

        // This deletes extra door pieces from the door during a visual update.
        // The door is then responsible for creating any extras it needs based on current property settings, at a later point in the same visual update.
        private void Cleanup()
        {
            // Garage door sections and fence posts are children of this transform
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform t = transform.GetChild(i);

                if ((t.name == "SecondPivot") || (t.name == "ThirdPivot") || (t.name == "FourthPivot") || (t.name == "FencePost"))
                {
                    if (Application.isPlaying)
                        Destroy(t.gameObject);
                    else
                        DestroyImmediate(t.gameObject);
                }
            }

            // Handles are children of the door mesh
            for (int i = theDoor.transform.childCount - 1; i >= 0; i--)
            {
                Transform t = theDoor.transform.GetChild(i);

                if (t.name == "DoorHandles")
                {
                    if (Application.isPlaying)
                        Destroy(t.gameObject);
                    else
                        DestroyImmediate(t.gameObject);
                }
            }
        }

        private void UpdateDoorVisuals()
        {
            if (theDoor == null) // Grab a door ref if needed
                theDoor = GetComponentInChildren<MeshRenderer>();

            DoorInfo di = GetDoorInfo(doorKind); // Get the main info record for this door type

            if ((_lastDoorKind != doorKind)) // If door type changes, we need to reset materials and extras
            {
                customMaterials = new Material[di.doorMaterials.Length];
                System.Array.Copy(di.doorMaterials, customMaterials, di.doorMaterials.Length);
                handleKind = HandleKind.None;
                postLayout = PostLayout.None;
            }

            // Door visuals
            internalPivots.Clear();
            internalPivots.Add(theDoor.transform.parent.gameObject);
            theDoor.GetComponent<MeshFilter>().mesh = di.doorMesh;
            theDoor.GetComponent<MeshCollider>().sharedMesh = di.doorMesh;
            theDoor.SetSharedMaterials(new(customMaterials));
            theDoor.transform.localPosition = di.localPosition;
            theDoor.transform.localRotation = Quaternion.Euler(di.localRotation);
            theDoor.transform.localScale = di.localScale;
            doorBounds = theDoor.bounds;
            Cleanup();

            // Store default sounds (may be overridden)
            openClip = di.defaultOpenSound;
            closeClip = di.defaultCloseSound;
            lockedClip = di.defaultLockedSound;
            lockingClip = di.defaultLockingSound;
            unlockingClip = di.defaultUnlockingSound;

            if (doorKind == DoorKind.GarageA) // If garage door, replicate into 4 sections
            {
                MakeNewSection("SecondPivot", "Section 2", new(0f, 0.54f, 0f));
                MakeNewSection("ThirdPivot", "Section 3", new(0f, 1.08f, 0f));
                MeshRenderer mr = MakeNewSection("FourthPivot", "Section 4", new(0f, 1.62f, 0f));
                doorBounds.Encapsulate(mr.bounds);
            }
            else if (OmniDoor3DData.IsStandard(doorKind)) // If "standard" door, check window option
            {
                if (!windowed && (theDoor.sharedMaterials.Length < 4))
                {
                    List<Material> mats = new(customMaterials);
                    mats.Add(mats[0]); // In the mesh used for standard doors, the 4th material slot is the solid panel that blocks the window.
                    theDoor.SetSharedMaterials(mats);
                }
                else if (windowed && (theDoor.sharedMaterials.Length > 3))
                {
                    List<Material> mats = new(customMaterials);
                    mats.RemoveAt(3); // Take off the last material to enable the window.
                    theDoor.SetSharedMaterials(mats);
                }
            }

            // Update handles
            if (handleKind != HandleKind.None)
            {
                if (lastHandleKind != handleKind)
                    handleMaterials = GetHandleMaterials(handleKind);

                // The handles will always be deleted by Cleanup(), so they will always be instantiated
                if (GetHandlePlacement(di, handleKind, out HandlePlacement hp))
                {
                    GameObject handleObj = Instantiate(GetHandlePrefab(handleKind), theDoor.transform);
                    handleObj.name = "DoorHandles";
                    handleObj.transform.localPosition = hp.handlePosition;
                    handleObj.transform.localRotation = Quaternion.Euler(hp.handleRotation);
                    handleObj.transform.localScale = hp.handleScale;
                    handleObj.GetComponent<MeshRenderer>().SetSharedMaterials(new(handleMaterials));
                    handleObj.isStatic = true;
                }
            }

            // Update fence posts
            if (postLayout != PostLayout.None)
            {
                if ((postLayout == PostLayout.Right) || (postLayout == PostLayout.Both))
                    MakeFencePost(GetRightPostPos(doorKind));

                if ((postLayout == PostLayout.Left) || (postLayout == PostLayout.Both))
                    MakeFencePost(GetLeftPostPos(doorKind));
            }
            else
                postMaterials = GetPostMaterials(doorKind);

            UpdateTriggerShapeAndSize();
            UpdateMeshLayers();
            _lastDoorKind = doorKind;
            lastHandleKind = handleKind;
        }

        // All the meshes should go in the layer specified by the user
        public void UpdateMeshLayers()
        {
            foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
            {
                if (mr.gameObject.layer != doorLayer)
                    mr.gameObject.layer = doorLayer;
            }

            foreach (GameObject go in internalPivots)
            {
                if (go.layer != doorLayer)
                    go.layer = doorLayer;

                if (go.transform.parent.gameObject.layer != doorLayer)
                    go.transform.parent.gameObject.layer = doorLayer;
            }
        }

        // Update trigger shape and size
        public void UpdateTriggerShapeAndSize()
        {
            if (triggerShape == TriggerShape.Box)
            {
                if (!TryGetComponent<BoxCollider>(out BoxCollider eCol))
                {
                    DestroyImmediate(GetComponent<Collider>());
                    eCol = gameObject.AddComponent<BoxCollider>();
                    eCol.isTrigger = true;
                    eCol.includeLayers = openingLayers;
                }

                eCol.center = transform.worldToLocalMatrix.MultiplyPoint3x4(doorBounds.center) + triggerOffset;
                eCol.size = triggerExtents * 2f;
            }
            else if (triggerShape == TriggerShape.Sphere)
            {
                if (!TryGetComponent<SphereCollider>(out SphereCollider eCol))
                {
                    DestroyImmediate(GetComponent<Collider>());
                    eCol = gameObject.AddComponent<SphereCollider>();
                    eCol.isTrigger = true;
                    eCol.includeLayers = openingLayers;
                }

                eCol.center = transform.worldToLocalMatrix.MultiplyPoint3x4(doorBounds.center) + triggerOffset;
                eCol.radius = triggerRadius;
            }
        }

        // Spawn fence posts if requested
        private void MakeFencePost(Vector3 pos)
        {
            GameObject postObj = Instantiate(GetPostPrefab(doorKind), transform);
            postObj.name = "FencePost";
            postObj.transform.localPosition = pos;
            postObj.GetComponent<MeshRenderer>().SetSharedMaterials(new(postMaterials));
            postObj.isStatic = true;
        }

        // Spawn new garage door section
        private MeshRenderer MakeNewSection(string sPivot, string sSection, Vector3 localPos)
        {
            GameObject go = new(sPivot);
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.isStatic = true;

            GameObject goPiv = new("Internal Pivot");
            internalPivots.Add(goPiv);
            goPiv.transform.SetParent(go.transform);
            goPiv.transform.localPosition = Vector3.zero;
            goPiv.transform.localScale = Vector3.one;
            goPiv.transform.localRotation = Quaternion.identity;

            go = new(sSection, typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider));
            go.transform.SetParent(goPiv.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = theDoor.transform.localScale;
            go.transform.localRotation = Quaternion.identity;
            go.GetComponent<MeshFilter>().sharedMesh = theDoor.GetComponent<MeshFilter>().sharedMesh;
            go.GetComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            go.isStatic = true;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.SetSharedMaterials(new(theDoor.sharedMaterials));
            return mr;
        }

        // Handle window setting change from inspector
        public void OnWindowedChanged()
        {
#if UNITY_EDITOR
            if (!gameObject.scene.IsValid() || PrefabUtility.IsPartOfPrefabAsset(gameObject)) // Don't react to a prefab in the Project window, just one in the scene
                return;
#endif
            if (!OmniDoor3DData.IsStandard(doorKind))
                return;

            UpdateDoorVisuals();
        }

        // Handle door visuals change from inspector
        public void OnDoorVisChanged()
        {
#if UNITY_EDITOR
            if (!gameObject.scene.IsValid() || PrefabUtility.IsPartOfPrefabAsset(gameObject)) // Don't react to a prefab in the Project window, just one in the scene
                return;
#endif
            UpdateDoorVisuals();
        }

        // Handle layer change from inspector
        public void OnLayerChanged()
        {
            UpdateMeshLayers();
        }

        // Triggered open
        private void OnTriggerEnter(Collider other)
        {
            if (autoOpen && InOpeningLayers(other.gameObject))
            {
                negZ = transform.InverseTransformPoint(other.transform.position).z < 0; // Used for auto swing direction
                openColliders.Add(other);
                TryOpen();
            }
        }

        // Triggered close
        private void OnTriggerExit(Collider other)
        {
            if (autoClose && InOpeningLayers(other.gameObject))
            {
                openColliders.Remove(other);

                if (openColliders.Count == 0)
                {
                    CloseLinkedDoors();

                    if (isOpen)
                    {
                        isOpen = false;
                        needClose = true;
                    }
                }
            }
        }

        // Controller events

        private void OnControllerToggle()
        {
            if (isOpen)
            {
                isOpen = false;
                needClose = true;
            }
            else
                TryOpen();
        }

        private void OnControllerOpen()
        {
            TryOpen();
        }

        private void OnControllerClose()
        {
            CloseLinkedDoors();

            if (isOpen)
            {
                isOpen = false;
                needClose = true;
            }
        }

        private void OnControllerLock()
        {
            TryLock();
            LockLinkedDoors();
        }

        public void TryLock()
        {
            if (!isLocked)
            {
                if (playAudio)
                {
                    aSrc.clip = lockingOvrSound == null ? lockingClip : lockingOvrSound;
                    aSrc.Play();
                }

                DoorLocked?.Invoke(this);
                isLocked = true;
            }
        }

        private void OnControllerUnlock()
        {
            TryUnlock();
            UnlockLinkedDoors();
        }

        public void TryUnlock()
        {
            if (isLocked)
            {
                if (playAudio)
                {
                    aSrc.clip = unlockingOvrSound == null ? unlockingClip : unlockingOvrSound;
                    aSrc.Play();
                }

                DoorUnlocked?.Invoke(this);
                isLocked = false;
            }
        }

        // Actually open, once decided
        private IEnumerator Open()
        {
            if (closeDelayComplete)
            {
                bool wasMoving = isMoving; // wasMoving is important for reversing from the correct position
                isMoving = false;
                openDelayComplete = false;
                yield return new WaitForSeconds(openDelay); // Wait for open delay
                openDelayComplete = true;
                isMoving = wasMoving;
                DoorWillOpen?.Invoke(this);

                if (playAudio) // Play the open sound
                {
                    aSrc.clip = openOvrSound == null ? openClip : openOvrSound;
                    aSrc.Play();
                }

                // Depending on motion type, calculate starting values and begin opening
                if (motionKind != MotionKind.None) // It's not possible by normal means to have motion type None, but check anyway
                {
                    if (motionKind == MotionKind.RollUp)
                    {
                        if (isMoving && !motionOpen)
                            rollUpPosition = 1f - rollUpPosition;
                        else if (!isMoving)
                            rollUpPosition = 0f;
                    }
                    else
                        motionStartPct = percentComplete;

                    if (motionKind == MotionKind.Swing)
                    {
                        targetAngleX = (openFwd ? 360f : -360f) * swingAmountX;
                        swingStartAngleX = Mathf.Lerp(0f, targetAngleX, 1f - percentComplete);
                        targetAngleY = (openFwd ? 360f : -360f) * swingAmountY;
                        swingStartAngleY = Mathf.Lerp(0f, targetAngleY, 1f - percentComplete);
                        targetAngleZ = (openFwd ? 360f : -360f) * swingAmountZ;
                        swingStartAngleZ = Mathf.Lerp(0f, targetAngleZ, 1f - percentComplete);
                    }
                    else if (motionKind == MotionKind.Slide)
                    {
                        targetPos = slideDirection.normalized * setTravelDistance;
                        slideStartPos = Vector3.Lerp(Vector3.zero, targetPos, 1f - percentComplete);

                        if (offsetSlide)
                            slideOffset = offsetDirection * setOffsetDistance;
                    }

                    motionStartTime = Time.time;
                    motionOpen = true;
                    isMoving = true;
                }
            }
        }

        // Actually close, once decided
        private IEnumerator Close()
        {
            if (closeAfterRoutine != null) // If something closes the door and the timed close is counting down, stop it
            {
                StopCoroutine(closeAfterRoutine);
                closeAfterRoutine = null;
            }

            if (openDelayComplete)
            {
                bool wasMoving = isMoving; // wasMoving is important for reversing from the correct position
                isMoving = false;
                closeDelayComplete = false;
                yield return new WaitForSeconds(closeDelay); // Wait for close delay
                closeDelayComplete = true;
                isMoving = wasMoving;
                DoorWillClose?.Invoke(this);

                if (playAudio) // Play the close sound
                {
                    aSrc.clip = closeOvrSound == null ? closeClip : closeOvrSound;
                    aSrc.Play();
                }

                // Depending on motion type, calculate starting values and begin closing
                if (motionKind != MotionKind.None)
                {
                    if (motionKind == MotionKind.RollUp)
                    {
                        if (isMoving && motionOpen)
                            rollUpPosition = 1f - rollUpPosition;
                        else if (!isMoving)
                            rollUpPosition = 0f;
                    }
                    else
                        motionStartPct = percentComplete;

                    if (motionKind == MotionKind.Swing)
                    {
                        targetAngleX = 0f;
                        swingStartAngleX = Mathf.Lerp((openFwd ? 360f : -360f) * swingAmountX, targetAngleX, 1f - percentComplete);
                        targetAngleY = 0f;
                        swingStartAngleY = Mathf.Lerp((openFwd ? 360f : -360f) * swingAmountY, targetAngleY, 1f - percentComplete);
                        targetAngleZ = 0f;
                        swingStartAngleZ = Mathf.Lerp((openFwd ? 360f : -360f) * swingAmountZ, targetAngleZ, 1f - percentComplete);
                    }
                    else if (motionKind == MotionKind.Slide)
                    {
                        targetPos = Vector3.zero;
                        slideStartPos = Vector3.Lerp(slideDirection.normalized * setTravelDistance, targetPos, 1f - percentComplete);
                    }

                    motionStartTime = Time.time;
                    motionOpen = false;
                    isMoving = true;
                }
            }
        }

        public OmniDoor3DConfig GetConfig() // Access the config object
        {
            if (config == null)
                config = Resources.Load<OmniDoor3DConfig>("OmniDoor3DConfig");

            return config;
        }

        // Attempt to open. Success may vary.
        private void TryOpen()
        {
            if (!isOpen)
            {
                if (isLocked) // It's locked
                {
                    DoorOpenFailed?.Invoke(this);

                    if (playAudio)
                    {
                        aSrc.clip = lockedOvrSound == null ? lockedClip : lockedOvrSound;
                        aSrc.Play();
                    }
                }
                else
                {
                    if (closeRoutine != null)
                    {
                        StopCoroutine(closeRoutine);
                        closeRoutine = null;
                    }

                    if (motionKind == MotionKind.Swing) // Determine swing direction
                    {
                        if (autoOpen)
                        {
                            openFwd = (swingDirection == SwDir.Forward) || ((swingDirection == SwDir.Auto) && negZ);

                            if (reverseSwing)
                                openFwd = !openFwd;
                        }
                        else
                            openFwd = swingDirection != SwDir.Back;
                    }

                    isOpen = true;
                    needClose = false;
                    openRoutine = StartCoroutine(Open()); // Now it can open
                }
            }

            // Trigger linked doors
            foreach (OmniDoor3D door in linkedDoors)
            {
                if (door != null)
                    door.TryOpen();
            }
        }

        // Check if the object is in the opening layers
        protected bool InOpeningLayers(GameObject go)
        {
            return ((1 << go.layer) & openingLayers) != 0;
        }

        // Trigger linked doors to close
        private void CloseLinkedDoors()
        {
            foreach (OmniDoor3D door in linkedDoors)
            {
                if (door != null)
                {
                    door.isOpen = false;
                    door.needClose = true;
                }
            }
        }

        // Trigger linked doors to lock
        private void LockLinkedDoors()
        {
            foreach (OmniDoor3D door in linkedDoors)
            {
                if (door != null)
                    door.TryLock();
            }
        }

        // Trigger linked doors to unlock
        private void UnlockLinkedDoors()
        {
            foreach (OmniDoor3D door in linkedDoors)
            {
                if (door != null)
                    door.TryUnlock();
            }
        }

        private void Update()
        {
            timeElapsed += Time.deltaTime;

            if (timeElapsed > GetConfig().checkTime)
            {
                timeElapsed = 0f;

                if (needClose)
                {
                    if (openRoutine != null)
                    {
                        StopCoroutine(openRoutine);
                        openRoutine = null;
                    }

                    needClose = false;
                    closeRoutine = StartCoroutine(Close()); // Now it can close
                    return;
                }
            }

            // Keep motions going to completion
            if (isMoving)
            {
                if (motionKind == MotionKind.RollUp)
                {
                    float fullDuration = motionOpen ? secondsToOpen : secondsToClose;
                    float deltaProgress = Time.deltaTime / fullDuration;

                    if (motionOpen)
                        rollUpPosition += deltaProgress;
                    else
                        rollUpPosition += deltaProgress;

                    rollUpPosition = Mathf.Clamp01(rollUpPosition);

                    if (rollUpPosition >= 1.0f)
                    {
                        rollUpPosition = 1.0f;
                        isMoving = false;

                        if (motionOpen)
                            DoorOpened?.Invoke(this);
                        else
                            DoorClosed?.Invoke(this);
                    }

                    int waypointCount = section0Waypoints.GetLength(0);
                    float position = motionOpen ? rollUpPosition : (1f - rollUpPosition);
                    float indexFloat = position * (waypointCount - 1);
                    int index = Mathf.FloorToInt(indexFloat);
                    float lerpFactor = indexFloat - index;
                    index = Mathf.Clamp(index, 0, waypointCount - 2);

                    // This works on multiple pieces because of the internal pivots; they are all at 0,0,0 locally but in different places in the world.
                    for (int i = 0; i < internalPivots.Count; i++)
                        ApplyGarageDoorWaypoint(i, index, lerpFactor);
                }
                else
                {
                    float duration = motionStartPct * (motionOpen ? secondsToOpen : secondsToClose);
                    float elapsedTime = Time.time - motionStartTime;
                    percentComplete = elapsedTime / duration;

                    if (percentComplete >= 1.0f)
                    {
                        percentComplete = 1.0f;
                        isMoving = false;

                        if (motionOpen)
                            DoorOpened?.Invoke(this);
                        else
                            DoorClosed?.Invoke(this);
                    }

                    foreach (GameObject pivot in internalPivots)
                    {
                        if (motionKind == MotionKind.Slide)
                        {
                            if (offsetSlide)
                            {
                                if (motionOpen)
                                {
                                    if (percentComplete < offsetPortion)
                                        pivot.transform.localPosition = Vector3.Lerp(slideStartPos, slideStartPos + slideOffset, percentComplete * (1f / offsetPortion));
                                    else
                                        pivot.transform.localPosition = Vector3.Lerp(slideStartPos + slideOffset, targetPos + slideOffset, (percentComplete - offsetPortion) *
                                            (1f / (1f - offsetPortion)));
                                }
                                else
                                {
                                    if (percentComplete < (1f - offsetPortion))
                                        pivot.transform.localPosition = Vector3.Lerp(slideStartPos + slideOffset, targetPos + slideOffset, percentComplete * (1f / (1f - offsetPortion)));
                                    else
                                        pivot.transform.localPosition = Vector3.Lerp(targetPos + slideOffset, targetPos, (percentComplete - (1f - offsetPortion)) * (1f / offsetPortion));
                                }
                            }
                            else
                                pivot.transform.localPosition = Vector3.Lerp(slideStartPos, targetPos, percentComplete);
                        }
                        else if (motionKind == MotionKind.Swing)
                            pivot.transform.localRotation = Quaternion.Euler(Vector3.Lerp(new(swingStartAngleX, swingStartAngleY, swingStartAngleZ),
                                new(targetAngleX, targetAngleY, targetAngleZ), percentComplete));
                    }
                }
            }
        }

        // Apply the garage door waypoints to the internal pivots
        private void ApplyGarageDoorWaypoint(int sectionIndex, int waypointIndex, float lerpFactor)
        {
            if (sectionIndex >= 0 && sectionIndex < internalPivots.Count)
            {
                Vector3[,] waypoints;

                switch (sectionIndex)
                {
                    case 0: waypoints = section0Waypoints; break;
                    case 1: waypoints = section1Waypoints; break;
                    case 2: waypoints = section2Waypoints; break;
                    case 3: waypoints = section3Waypoints; break;
                    default: return;
                }

                Transform parentTransform = internalPivots[sectionIndex].transform.parent;

                if (parentTransform != null)
                {
                    Vector3 currentPos = waypoints[waypointIndex, 0];
                    Vector3 nextPos = waypoints[waypointIndex + 1, 0];
                    parentTransform.localPosition = Vector3.Lerp(currentPos, nextPos, lerpFactor);

                    Vector3 currentRot = waypoints[waypointIndex, 1];
                    Vector3 nextRot = waypoints[waypointIndex + 1, 1];
                    parentTransform.localRotation = Quaternion.Euler(Vector3.Lerp(currentRot, nextRot, lerpFactor));
                }
            }
        }

        // Timed close is applied after door opened

        private void OnDoorOpened(OmniDoor3D door)
        {
            if (door != this)
                return;

            if (timedClose)
                closeAfterRoutine = StartCoroutine(CloseAfter(closeAfter));
        }

        private IEnumerator CloseAfter(float fSec)
        {
            yield return new WaitForSeconds(fSec);
            CloseLinkedDoors();

            if (isOpen)
            {
                isOpen = false;
                needClose = true;
            }
        }

        // Housekeeping
        private void OnDestroy()
        {
            StopAllCoroutines();

            foreach (OmniDoor3DController controllingObject in controllingObjects)
            {
                if (controllingObject != null)
                {
                    controllingObject.OpenDoor -= OnControllerOpen;
                    controllingObject.CloseDoor -= OnControllerClose;
                    controllingObject.ToggleDoor -= OnControllerToggle;
                    controllingObject.UnlockDoor -= OnControllerUnlock;
                    controllingObject.LockDoor -= OnControllerLock;
                }
            }
        }

        // Database stuff

        // Get the default info about doorKind
        public DoorInfo GetDoorInfo(DoorKind doorKind)
        {
            foreach (OmniDoorDB odb in GetConfig().DBList)
            {
                foreach (DoorInfo di in odb.DoorList)
                {
                    if (di.doorKind == doorKind)
                        return di;
                }
            }

            return default;
        }

        // Get the prefab for instantiating a handle
        private GameObject GetHandlePrefab(HandleKind handleKind)
        {
            foreach (OmniDoorDB odb in GetConfig().DBList)
            {
                foreach (HandleInfo hi in odb.HandleList)
                {
                    if (hi.handleKind == handleKind)
                        return hi.handlePrefab;
                }
            }

            return null;
        }

        // Get the prefab for instantiating a post
        private GameObject GetPostPrefab(DoorKind dk)
        {
            foreach (OmniDoorDB odb in GetConfig().DBList)
            {
                foreach (PostInfo pi in odb.PostList)
                {
                    if (pi.doorKind == dk)
                        return pi.postPrefab;
                }
            }

            return null;
        }

        // Retrieve info on how to place a specific handle on a door
        public bool GetHandlePlacement(DoorInfo di, HandleKind hk, out HandlePlacement hpOut)
        {
            foreach (HandlePlacement hp in di.handles)
            {
                if (hp.handleKind == hk)
                {
                    hpOut = hp;
                    return true;
                }
            }

            hpOut = default;
            return false;
        }

        // Retrieve the materials for a specific handle
        private Material[] GetHandleMaterials(HandleKind hk)
        {
            foreach (OmniDoorDB odb in GetConfig().DBList)
            {
                foreach (HandleInfo hi in odb.HandleList)
                {
                    if (hi.handleKind == hk)
                        return hi.handlePrefab.GetComponent<MeshRenderer>().sharedMaterials;
                }
            }

            return null;
        }

        // Retrieve the materials for a specific post
        private Material[] GetPostMaterials(DoorKind dk)
        {
            foreach (OmniDoorDB odb in GetConfig().DBList)
            {
                foreach (PostInfo pinf in odb.PostList)
                {
                    if (pinf.doorKind == dk)
                        return pinf.postPrefab.GetComponent<MeshRenderer>().sharedMaterials;
                }
            }

            return null;
        }

        // Get the position of the right-side post for a given fence
        private Vector3 GetRightPostPos(DoorKind dk)
        {
            foreach (OmniDoorDB odb in GetConfig().DBList)
            {
                foreach (PostInfo pinf in odb.PostList)
                {
                    if (pinf.doorKind == dk)
                        return pinf.rightPostPosition;
                }
            }

            return Vector3.zero;
        }

        // Get the position of the left-side post for a given fence
        private Vector3 GetLeftPostPos(DoorKind dk)
        {
            foreach (OmniDoorDB odb in GetConfig().DBList)
            {
                foreach (PostInfo pinf in odb.PostList)
                {
                    if (pinf.doorKind == dk)
                        return pinf.leftPostPosition;
                }
            }

            return Vector3.zero;
        }

        // Is the motionKind allowed for the doorKind?
        public bool MotionAllowed(MotionKind mk, DoorKind dk)
        {
            foreach (OmniDoorDB odb in GetConfig().DBList)
            {
                foreach (MotionInfo mi in odb.MotionList)
                {
                    if (mi.motionKind == mk)
                    {
                        foreach (DoorKind kind in mi.allowedDoorKinds)
                        {
                            if (kind == dk)
                                return true;
                        }
                    }
                }
            }

            return false;
        }
    }

    [Serializable]
    public struct DoorInfo
    {
        public DoorKind doorKind;
        public Mesh doorMesh;
        public Material[] doorMaterials;
        public AudioClip defaultOpenSound, defaultCloseSound, defaultLockedSound, defaultLockingSound, defaultUnlockingSound;
        public Vector3 localPosition, localRotation, localScale;
        public HandlePlacement[] handles;
    }

    public enum TriggerShape { Box, Sphere }

    public enum SwDir { Auto, Forward, Back }

    [Serializable]
    public struct HandleInfo
    {
        public HandleKind handleKind;
        public GameObject handlePrefab;
    }

    [Serializable]
    public struct HandlePlacement
    {
        public HandleKind handleKind;
        public Vector3 handlePosition, handleRotation, handleScale;
    }

    public enum PostLayout { None, Both, Left, Right }

    [Serializable]
    public struct PostInfo
    {
        public DoorKind doorKind;
        public Vector3 leftPostPosition, rightPostPosition;
        public GameObject postPrefab;
    }

    public enum MotionKind { None, Swing, Slide, [InspectorName("Roll Up")] RollUp }

    [Serializable]
    public struct MotionInfo
    {
        public MotionKind motionKind;
        public DoorKind[] allowedDoorKinds;
    }
}