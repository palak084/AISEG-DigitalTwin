using OpenTK.Mathematics;
using AISEG.DigitalTwin.Core;

namespace AISEG.DigitalTwin.Environment;

/// <summary>
/// High-fidelity procedural FANUC M-900iA/350 industrial robot arm.
/// Geometry reconstructed from reference images of the STEP CAD model.
/// Key features: rectangular base, tapered shoulder housing with red E-stop,
/// parallel-bar linkage, large elbow drum, cylindrical upper arm, black wrist motors.
/// ~30 primitives arranged in a proper kinematic chain.
/// </summary>
public sealed class RoboticArm
{
    public Vector3 BasePosition { get; }
    public float BaseYawOffset { get; }
    
    // Animation state
    public bool IsPicking { get; private set; }
    private float _animTime = 0.0f;
    private const float AnimDuration = 2.0f;
    
    // Joint angles
    private float _j1Yaw = 0.0f;
    private float _j2Pitch = -20.0f;
    private float _j3Pitch = 40.0f;
    private float _j5Pitch = -20.0f;
    
    // ── M-900iA COLORWAY (from reference images) ──
    private readonly Vector3 _fanucYellow  = new(0.85f, 0.82f, 0.20f);  // Olive-yellow from renders
    private readonly Vector3 _baseSilver   = new(0.65f, 0.65f, 0.68f);  // Silver/grey base casting
    private readonly Vector3 _darkGrey     = new(0.18f, 0.18f, 0.18f);
    private readonly Vector3 _medGrey      = new(0.40f, 0.40f, 0.42f);
    private readonly Vector3 _motorBlack   = new(0.06f, 0.06f, 0.06f);
    private readonly Vector3 _eStopRed     = new(0.90f, 0.10f, 0.10f);  // Red e-stop / accent
    private readonly Vector3 _chrome       = new(0.70f, 0.72f, 0.74f);
    private readonly Vector3 _steel        = new(0.55f, 0.56f, 0.58f);
    private readonly Vector3 _jointPin     = new(0.50f, 0.50f, 0.52f);  // Grey pin color
    
    // ── J1: BASE (rectangular casting, visible in ref images) ──
    private readonly Cube     _basePlateBottom;    // Bottom mounting plate
    private readonly Cube     _basePlateTop;       // Upper stepped plate
    private readonly Cylinder _baseRing;           // Rotation ring between base & turret
    
    // ── J1: TURRET (the large rotating yellow body) ──
    private readonly Cylinder _turretBody;         // Main turret cylinder
    private readonly Cube     _turretShoulder;     // Tapered shoulder housing (wide trapezoid)
    private readonly Cube     _turretShoulderSide; // Side plate of shoulder
    private readonly Cylinder _eStopButton;        // Red emergency stop (visible in all ref images)
    
    // ── J2: SHOULDER JOINT (large pivot with visible pins) ──
    private readonly Cylinder _shoulderPinL;       // Left shoulder pivot pin
    private readonly Cylinder _shoulderPinR;       // Right shoulder pivot pin
    
    // ── J3: LOWER ARM (parallel-bar linkage structure) ──
    private readonly Cube _lowerArmMain;           // Main structural arm bar
    private readonly Cube _lowerArmLinkageBar;     // Parallel linkage bar (behind main arm)
    private readonly Cube _lowerArmWebL;           // Left triangular web plate
    private readonly Cube _lowerArmWebR;           // Right triangular web plate
    private readonly Cube _lowerArmBrace;          // Cross-brace stiffener mid-arm
    
    // ── J4: ELBOW (large drum joint, visible in refs) ──
    private readonly Cylinder _elbowDrum;          // Large elbow rotation drum
    private readonly Cylinder _elbowPinL;          // Left elbow pin
    private readonly Cylinder _elbowPinR;          // Right elbow pin
    
    // ── J5: UPPER ARM (cylindrical tube) ──
    private readonly Cube     _upperArmTube;       // Main upper arm structure
    private readonly Cube     _upperArmCover;      // Top cover/shroud
    private readonly Cylinder _upperArmCylinder;   // Cylindrical section (visible in ref)
    
    // ── J6: WRIST (black motor housings with red accent) ──
    private readonly Cylinder _wristMotor1;        // First wrist motor (black)
    private readonly Cylinder _wristMotor2;        // Second wrist motor (black)
    private readonly Cylinder _wristRedRing;       // Red accent ring
    private readonly Cylinder _wristFlange;        // Tool mounting flange
    
    // ── END EFFECTOR ──
    private readonly Cube _toolPlate;              // Gripper adapter plate
    private readonly Cylinder _toolCylinder;       // Cylindrical tool connector

    public RoboticArm(Vector3 basePosition, float baseYawOffset)
    {
        BasePosition = basePosition;
        BaseYawOffset = baseYawOffset;
        _j1Yaw = baseYawOffset;
        
        // ── J1: BASE (rectangular, silver, stepped) ──
        _basePlateBottom = new Cube     { Scale = new(1.0f, 0.08f, 0.8f),  Color = _baseSilver };
        _basePlateTop    = new Cube     { Scale = new(0.85f, 0.10f, 0.7f), Color = _baseSilver };
        _baseRing        = new Cylinder { Scale = new(0.55f, 0.08f, 0.55f), Color = _darkGrey };
        
        // ── J1: TURRET ──
        _turretBody          = new Cylinder { Scale = new(0.50f, 0.50f, 0.50f), Color = _fanucYellow };
        _turretShoulder      = new Cube     { Scale = new(0.65f, 0.70f, 0.50f), Color = _fanucYellow };
        _turretShoulderSide  = new Cube     { Scale = new(0.10f, 0.55f, 0.42f), Color = _fanucYellow };
        _eStopButton         = new Cylinder { Scale = new(0.06f, 0.06f, 0.06f), Color = _eStopRed };
        
        // ── J2: SHOULDER PINS ──
        _shoulderPinL = new Cylinder { Scale = new(0.10f, 0.08f, 0.10f), Color = _jointPin };
        _shoulderPinR = new Cylinder { Scale = new(0.10f, 0.08f, 0.10f), Color = _jointPin };
        
        // ── J3: LOWER ARM (M-900iA has thick parallel bars) ──
        _lowerArmMain       = new Cube { Scale = new(0.16f, 1.50f, 0.30f), Color = _fanucYellow };
        _lowerArmLinkageBar = new Cube { Scale = new(0.10f, 1.30f, 0.15f), Color = _fanucYellow };
        _lowerArmWebL       = new Cube { Scale = new(0.30f, 0.12f, 0.08f), Color = _fanucYellow };
        _lowerArmWebR       = new Cube { Scale = new(0.30f, 0.12f, 0.08f), Color = _fanucYellow };
        _lowerArmBrace      = new Cube { Scale = new(0.35f, 0.08f, 0.25f), Color = _fanucYellow };
        
        // ── J4: ELBOW ──
        _elbowDrum = new Cylinder { Scale = new(0.30f, 0.40f, 0.30f), Color = _fanucYellow };
        _elbowPinL = new Cylinder { Scale = new(0.08f, 0.06f, 0.08f), Color = _jointPin };
        _elbowPinR = new Cylinder { Scale = new(0.08f, 0.06f, 0.08f), Color = _jointPin };
        
        // ── J5: UPPER ARM (tube + cover) ──
        _upperArmTube     = new Cube     { Scale = new(0.20f, 1.10f, 0.22f), Color = _fanucYellow };
        _upperArmCover    = new Cube     { Scale = new(0.24f, 0.60f, 0.26f), Color = _fanucYellow };
        _upperArmCylinder = new Cylinder { Scale = new(0.14f, 0.30f, 0.14f), Color = _fanucYellow };
        
        // ── J6: WRIST (black motors + red ring, visible in ref images) ──
        _wristMotor1  = new Cylinder { Scale = new(0.14f, 0.22f, 0.14f), Color = _motorBlack };
        _wristMotor2  = new Cylinder { Scale = new(0.12f, 0.18f, 0.12f), Color = _motorBlack };
        _wristRedRing = new Cylinder { Scale = new(0.13f, 0.04f, 0.13f), Color = _eStopRed };
        _wristFlange  = new Cylinder { Scale = new(0.10f, 0.05f, 0.10f), Color = _chrome };
        
        // ── END EFFECTOR ──
        _toolPlate    = new Cube     { Scale = new(0.20f, 0.04f, 0.16f), Color = _steel };
        _toolCylinder = new Cylinder { Scale = new(0.08f, 0.10f, 0.08f), Color = _chrome };
        
        UpdateTransforms();
    }
    
    private Vector3 _pickTarget;
    private float _ikTargetJ1;
    private float _ikTargetJ2;
    private float _ikTargetJ3;
    private float _ikTargetJ5;
    
    public void StartPicking(Vector3 targetPos)
    {
        if (IsPicking) return;
        IsPicking = true;
        _animTime = 0.0f;
        _pickTarget = targetPos;
        
        // --- INVERSE KINEMATICS ---
        // Base J2 pivot is at Y = 1.20f relative to base
        Vector3 baseJ2 = BasePosition + new Vector3(0, 1.20f, 0);
        
        // Wrist points straight down. End effector length from J6 is 0.55f.
        // We want the tip to be at targetPos, so J6 must be at targetPos + (0, 0.55, 0).
        Vector3 targetJ6 = targetPos + new Vector3(0, 0.55f, 0);
        
        Vector3 delta = targetJ6 - baseJ2;
        
        // J1 Yaw (Angle in XZ plane)
        _ikTargetJ1 = MathHelper.RadiansToDegrees(MathF.Atan2(delta.X, delta.Z));
        
        // Planar reach
        float r = MathF.Sqrt(delta.X * delta.X + delta.Z * delta.Z);
        float y = delta.Y; // y is positive if target is above J2
        
        float L1 = 1.50f; // J2 to J4
        float L2 = 1.10f; // J4 to J6
        float d = MathF.Sqrt(r * r + y * y);
        
        d = Math.Clamp(d, 0.01f, L1 + L2 - 0.01f); // clamp to reachable workspace
        
        // Cosine rule
        float alpha = MathF.Atan2(r, y); // Angle to target from vertical up
        float beta = MathF.Acos((L1 * L1 + d * d - L2 * L2) / (2.0f * L1 * d));
        
        // Elbow up configuration: J2 pitches less than the target vector (alpha - beta)
        _ikTargetJ2 = MathHelper.RadiansToDegrees(alpha - beta);
        
        float gamma = MathF.Acos((L1 * L1 + L2 * L2 - d * d) / (2.0f * L1 * L2));
        // Elbow bends forward relative to L1 to point down at the target
        _ikTargetJ3 = 180.0f - MathHelper.RadiansToDegrees(gamma);
        
        // Wrist should point straight down (absolute angle 180 from vertical)
        _ikTargetJ5 = 180.0f - (_ikTargetJ2 + _ikTargetJ3);
    }
    
    public void Update(double deltaTime)
    {
        if (IsPicking)
        {
            _animTime += (float)deltaTime;
            float p = _animTime / AnimDuration;
            if (p >= 1.0f)
            {
                IsPicking = false;
                _animTime = 0.0f;
                p = 0.0f;
            }
            
            float targetYaw = BaseYawOffset;
            float targetShoulder = -20.0f;
            float targetElbow = 40.0f;
            float targetWrist = -20.0f;
            
            if (p < 0.25f)
            {
                // Reach down to IK target
                float t = p / 0.25f;
                targetYaw = MathHelper.Lerp(BaseYawOffset, _ikTargetJ1, t);
                targetShoulder = MathHelper.Lerp(-20.0f, _ikTargetJ2, t);
                targetElbow = MathHelper.Lerp(40.0f, _ikTargetJ3, t);
                targetWrist = MathHelper.Lerp(-20.0f, _ikTargetJ5, t);
            }
            else if (p < 0.5f)
            {
                // Lift item up
                float t = (p - 0.25f) / 0.25f;
                targetYaw = _ikTargetJ1;
                targetShoulder = MathHelper.Lerp(_ikTargetJ2, -10.0f, t);
                targetElbow = MathHelper.Lerp(_ikTargetJ3, 20.0f, t);
                targetWrist = MathHelper.Lerp(_ikTargetJ5, 0.0f, t);
            }
            else if (p < 0.75f)
            {
                targetYaw = BaseYawOffset + 45.0f;
                targetShoulder = -10.0f;
                targetElbow = 20.0f;
                targetWrist = 0.0f;
            }
            else
            {
                float t = (p - 0.75f) / 0.25f;
                targetYaw = MathHelper.Lerp(BaseYawOffset + 45.0f, BaseYawOffset, t);
                targetShoulder = MathHelper.Lerp(-10.0f, -20.0f, t);
                targetElbow = MathHelper.Lerp(20.0f, 40.0f, t);
                targetWrist = MathHelper.Lerp(0.0f, -20.0f, t);
            }
            
            _j1Yaw = targetYaw;
            _j2Pitch = targetShoulder;
            _j3Pitch = targetElbow;
            _j5Pitch = targetWrist;
        }
        else
        {
            float time = (float)DateTime.Now.TimeOfDay.TotalSeconds;
            _j2Pitch = -20.0f + MathF.Sin(time * 2.0f) * 2.0f;
            _j1Yaw = BaseYawOffset + MathF.Sin(time * 0.5f) * 5.0f;
        }
        
        UpdateTransforms();
    }
    
    private void UpdateTransforms()
    {
        var baseOrigin = BasePosition;
        
        // ── J1: BASE (static rectangular plates) ──
        _basePlateBottom.TransformOverride = Matrix4.CreateScale(_basePlateBottom.Scale) * Matrix4.CreateTranslation(baseOrigin + new Vector3(0, 0.04f, 0));
        _basePlateTop.TransformOverride = Matrix4.CreateScale(_basePlateTop.Scale) * Matrix4.CreateTranslation(baseOrigin + new Vector3(0, 0.13f, 0));
        _baseRing.TransformOverride = Matrix4.CreateScale(_baseRing.Scale) * Matrix4.CreateTranslation(baseOrigin + new Vector3(0, 0.22f, 0));
        
        // ── J1: TURRET (rotates with J1 yaw) ──
        var j1Frame = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_j1Yaw)) * Matrix4.CreateTranslation(baseOrigin + new Vector3(0, 0.50f, 0));
        _turretBody.TransformOverride = Matrix4.CreateScale(_turretBody.Scale) * j1Frame;
        
        // Tapered shoulder housing (the big trapezoidal body visible in refs)
        var shoulderHousingPos = Matrix4.CreateTranslation(0, 0.35f, 0) * j1Frame;
        _turretShoulder.TransformOverride = Matrix4.CreateScale(_turretShoulder.Scale) * shoulderHousingPos;
        _turretShoulderSide.TransformOverride = Matrix4.CreateScale(_turretShoulderSide.Scale) * Matrix4.CreateTranslation(0.30f, 0, 0) * shoulderHousingPos;
        
        // Red E-stop button on side of turret (distinctive feature)
        _eStopButton.TransformOverride = Matrix4.CreateScale(_eStopButton.Scale) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90)) * Matrix4.CreateTranslation(0.28f, 0.0f, 0.15f) * shoulderHousingPos;
        
        // ── J2: SHOULDER PIVOT (pitch rotation) ──
        var j2Pivot = Matrix4.CreateTranslation(0, 0.70f, 0) * j1Frame;
        _shoulderPinL.TransformOverride = Matrix4.CreateScale(_shoulderPinL.Scale) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90)) * Matrix4.CreateTranslation(-0.35f, 0, 0) * j2Pivot;
        _shoulderPinR.TransformOverride = Matrix4.CreateScale(_shoulderPinR.Scale) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90)) * Matrix4.CreateTranslation(0.35f, 0, 0) * j2Pivot;
        
        var j2Frame = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_j2Pitch)) * j2Pivot;
        
        // ── J3: LOWER ARM ──
        _lowerArmMain.TransformOverride = Matrix4.CreateScale(_lowerArmMain.Scale) * Matrix4.CreateTranslation(0, 0.75f, 0) * j2Frame;
        _lowerArmLinkageBar.TransformOverride = Matrix4.CreateScale(_lowerArmLinkageBar.Scale) * Matrix4.CreateTranslation(0, 0.70f, -0.20f) * j2Frame;
        
        // Web plates connecting main bar to linkage bar (triangular structure)
        _lowerArmWebL.TransformOverride = Matrix4.CreateScale(_lowerArmWebL.Scale) * Matrix4.CreateTranslation(0, 0.15f, -0.10f) * j2Frame;
        _lowerArmWebR.TransformOverride = Matrix4.CreateScale(_lowerArmWebR.Scale) * Matrix4.CreateTranslation(0, 1.35f, -0.10f) * j2Frame;
        
        // Mid-arm brace
        _lowerArmBrace.TransformOverride = Matrix4.CreateScale(_lowerArmBrace.Scale) * Matrix4.CreateTranslation(0, 0.75f, -0.08f) * j2Frame;
        
        // ── J4: ELBOW ──
        var j4JointPos = Matrix4.CreateTranslation(0, 1.50f, 0) * j2Frame;
        _elbowDrum.TransformOverride = Matrix4.CreateScale(_elbowDrum.Scale) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90)) * j4JointPos;
        _elbowPinL.TransformOverride = Matrix4.CreateScale(_elbowPinL.Scale) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90)) * Matrix4.CreateTranslation(-0.22f, 0, 0) * j4JointPos;
        _elbowPinR.TransformOverride = Matrix4.CreateScale(_elbowPinR.Scale) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90)) * Matrix4.CreateTranslation(0.22f, 0, 0) * j4JointPos;
        
        var j4Frame = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_j3Pitch)) * j4JointPos;
        
        // ── J5: UPPER ARM ──
        _upperArmTube.TransformOverride = Matrix4.CreateScale(_upperArmTube.Scale) * Matrix4.CreateTranslation(0, 0.55f, 0) * j4Frame;
        _upperArmCover.TransformOverride = Matrix4.CreateScale(_upperArmCover.Scale) * Matrix4.CreateTranslation(0, 0.40f, 0.02f) * j4Frame;
        _upperArmCylinder.TransformOverride = Matrix4.CreateScale(_upperArmCylinder.Scale) * Matrix4.CreateTranslation(0, 0.85f, 0) * j4Frame;
        
        // ── J6: WRIST ──
        var j6JointPos = Matrix4.CreateTranslation(0, 1.10f, 0) * j4Frame;
        var j6Frame = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_j5Pitch)) * j6JointPos;
        
        _wristMotor1.TransformOverride = Matrix4.CreateScale(_wristMotor1.Scale) * Matrix4.CreateTranslation(0, 0.10f, 0) * j6Frame;
        _wristRedRing.TransformOverride = Matrix4.CreateScale(_wristRedRing.Scale) * Matrix4.CreateTranslation(0, 0.22f, 0) * j6Frame;
        _wristMotor2.TransformOverride = Matrix4.CreateScale(_wristMotor2.Scale) * Matrix4.CreateTranslation(0, 0.30f, 0) * j6Frame;
        _wristFlange.TransformOverride = Matrix4.CreateScale(_wristFlange.Scale) * Matrix4.CreateTranslation(0, 0.42f, 0) * j6Frame;
        
        // ── END EFFECTOR ──
        _toolPlate.TransformOverride = Matrix4.CreateScale(_toolPlate.Scale) * Matrix4.CreateTranslation(0, 0.48f, 0) * j6Frame;
        _toolCylinder.TransformOverride = Matrix4.CreateScale(_toolCylinder.Scale) * Matrix4.CreateTranslation(0, 0.55f, 0) * j6Frame;
    }

    public void Draw(Shader shader)
    {
        // J1 Base
        _basePlateBottom.Draw(shader);
        _basePlateTop.Draw(shader);
        _baseRing.Draw(shader);
        
        // J1 Turret
        _turretBody.Draw(shader);
        _turretShoulder.Draw(shader);
        _turretShoulderSide.Draw(shader);
        _eStopButton.Draw(shader);
        
        // J2 Shoulder
        _shoulderPinL.Draw(shader);
        _shoulderPinR.Draw(shader);
        
        // J3 Lower Arm
        _lowerArmMain.Draw(shader);
        _lowerArmLinkageBar.Draw(shader);
        _lowerArmWebL.Draw(shader);
        _lowerArmWebR.Draw(shader);
        _lowerArmBrace.Draw(shader);
        
        // J4 Elbow
        _elbowDrum.Draw(shader);
        _elbowPinL.Draw(shader);
        _elbowPinR.Draw(shader);
        
        // J5 Upper Arm
        _upperArmTube.Draw(shader);
        _upperArmCover.Draw(shader);
        _upperArmCylinder.Draw(shader);
        
        // J6 Wrist
        _wristMotor1.Draw(shader);
        _wristMotor2.Draw(shader);
        _wristRedRing.Draw(shader);
        _wristFlange.Draw(shader);
        
        // End Effector
        _toolPlate.Draw(shader);
        _toolCylinder.Draw(shader);
    }
}
