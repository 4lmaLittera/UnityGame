using UnityEngine;

namespace ABCodeworld.OmniDoor3D
{
    // Handles that will appear in the inspector's Handle Type drop-down, depending on the door
    public enum HandleKind { None, Bar, Knob, Lever }

    // Doors that will appear in the inspector's Door Type drop-down
    public enum DoorKind
    {
        [InspectorName("Standard - Wooden")] StandardWooden,
        [InspectorName("Standard - Metal")] StandardMetal,
        [InspectorName("Fence - Tall Wooden")] FenceWooden,
        [InspectorName("Fence - Chain Link")] FenceChain,
        [InspectorName("Standard - Plastic")] StandardPlastic,
        [InspectorName("Garage")] GarageA,
        [InspectorName("Stall - Metal")] StallA,
        [InspectorName("Stall - Plastic")] StallB,
        [InspectorName("Paneled - Wooden")] PaneledA,
        [InspectorName("Paneled - Plastic")] PaneledB,
        [InspectorName("Fence - Picket (RH)")] FencePicketRH,
        [InspectorName("Hatch - Octagonal")] HatchOctagon,
        [InspectorName("Window (Top Center)")] WindowTop,
        [InspectorName("Fence - Picket (LH)")] FencePicketLH,
        [InspectorName("Slab - Metal")] SlabMetal,
        [InspectorName("Circular (Centered)")] CircularCent,
        [InspectorName("Circular")] Circular,
        [InspectorName("Window (Side Center)")] WindowSide,
        // Add your door type above this line. Never change the order of the enum values.
    }

    public static class OmniDoor3DData
    {
        // Standard door kinds
        public static readonly DoorKind[] standardKinds = new DoorKind[3]
            { DoorKind.StandardWooden, DoorKind.StandardMetal, DoorKind.StandardPlastic };

        // Fence door kinds
        public static readonly DoorKind[] fenceKinds = new DoorKind[4]
            { DoorKind.FenceChain, DoorKind.FenceWooden, DoorKind.FencePicketRH, DoorKind.FencePicketLH };

        // Is the door in the list of standard doors? This is important to know because that type of door has a special window feature
        public static bool IsStandard(DoorKind doorKind)
        {
            foreach (DoorKind kind in OmniDoor3DData.standardKinds)
            {
                if (doorKind == kind)
                    return true;
            }

            return false;
        }

        // Is the door in the list of fence doors? This is important to know because that type of door has optional posts
        public static bool IsFence(DoorKind doorKind)
        {
            foreach (DoorKind kind in OmniDoor3DData.fenceKinds)
            {
                if (doorKind == kind)
                    return true;
            }

            return false;
        }
    }
}