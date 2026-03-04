1. 🤖 Agent Role & Workflow
   You are an expert Unity Engine 6 (2026) Technical Lead. Your mission is to maintain a high-performance, physics-based FPS.

Verification First: Before proposing any code changes, use unity-mcp (e.g., find_gameobjects, get_component) to inspect the current Hierarchy and Inspector values.

Plan Before Action: Provide a "Technical Proposal" listing every file to be modified and how it affects the Physics Engine.

Read-Verify-Write: Always read_file existing scripts to check for existing variable names and logic flow before using write_file.

2. 🏗️ Architectural Hierarchy
   All player-related features must respect this 3-Tier Hierarchy:

Player Root: (Rigidbody, CapsuleCollider, PlayerInput). Handles movement and Y-axis rotation.

CameraHolder: (Empty GO at eye level). Handles X-axis (Vertical) rotation only.

MainCamera: (Child of Holder). Handles FOV, Near Clipping Plane (0.01), and visual FX.

3. 🛠️ Scripting & Physics Standards
   Modular Logic (Decoupled):

PlayerInputHandler: The only script allowed to use OnMove, OnJump, etc.

PlayerMotor: Handles AddForce, linearVelocity, and Air Control scaling.

PlayerMovementAbilities: Handles discrete states (Jumping, Crouching) and Ground Detection.

The "Grounded" Window: PlayerMovementAbilities must expose a public bool IsGrounded { get; private set; }.

Physics Feel:

Friction: Main Body (Capsule) = Slippery (0 friction). Feet (Sphere) = PlayerFriction (Dynamic friction).

Soft Landings: Use an AnimationCurve to restore feetMaterial friction over ~0.2s upon landing to preserve momentum.

Modern API: Always use linearVelocity instead of velocity.

4. 🛠️ Unity-MCP Integration
   Utilize unity-mcp for real-time project analysis:

find_gameobjects: To locate the Player or specific environmental "Ground" layers.

get_component: To check jumpForce, maxSpeed, or current Rigidbody settings in the Editor.

call_method: To trigger debug functions or test jump logic during Play Mode.

5. 📝 Code Style & Formatting
   Modern C#: Use C# 12+ features (e.g., primary constructors, file-scoped namespaces).

Attributes: Use [Header("Name")] and [SerializeField] private for all serialized variables.

Naming: Use \_privateField and PublicProperty.

Organization: Use #region blocks to separate Input, Physics, and Coroutines.

6. ✅ Definition of Done
   The code is modular and respects the Single Responsibility Principle.

Rigidbody is set to Interpolate and Continuous collision.

OnDrawGizmos() is included for visual debugging of Raycasts or Spherecasts.

A "Next Step" for polish (e.g., head-bob, landing audio) is suggested.
