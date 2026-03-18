using System;
using UnityEngine;

namespace ABCodeworld.OmniDoor3D
{
    [CreateAssetMenu(fileName = "OmniDoorDB", menuName = "Scriptable Objects/ABCodeworld/OmniDoorDB")]
    public class OmniDoorDB : ScriptableObject
    {
        public DoorInfo[] DoorList; // List of all doors
        public HandleInfo[] HandleList; // List of all handles
        public PostInfo[] PostList; // List of fence and post connections
        public MotionInfo[] MotionList; // List of motion types to control which door types can use that motion
    }
}