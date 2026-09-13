using OpenTK.Mathematics;
using AISEG.DigitalTwin.Core;

namespace AISEG.DigitalTwin.Environment;

public sealed class RoboticArm
{
    public Vector3 BasePosition { get; }
    public float BaseYawOffset { get; }
    
    // Animation state
    public bool IsPicking { get; private set; }
    private float _animTime = 0.0f;
    private const float AnimDuration = 2.0f; // 2 seconds for a pick and drop cycle
    
    // Joint angles
    private float _baseYaw = 0.0f;
    private float _shoulderPitch = -20.0f;
    private float _elbowPitch = 40.0f;
    private float _wristPitch = -20.0f;
    
    // Structural parts
    private readonly Cylinder _baseHub;
    private readonly Cylinder _baseBody;
    private readonly Cube _lowerArmLink1;
    private readonly Cube _lowerArmLink2;
    private readonly Cylinder _shoulderMotorL;
    private readonly Cylinder _shoulderMotorR;
    private readonly Cylinder _elbowJoint;
    private readonly Cylinder _elbowMotor;
    private readonly Cube _upperArm;
    private readonly Cube _upperArmChamfer;
    private readonly Cylinder _wristJoint;
    private readonly Cube _wristRotator;
    private readonly Cube _gripperBase;
    private readonly Cube _gripperFingerL;
    private readonly Cube _gripperFingerR;
    
    private readonly Vector3 _fanucYellow = new Vector3(0.92f, 0.78f, 0.10f);
    private readonly Vector3 _darkGrey = new Vector3(0.2f, 0.2f, 0.2f);
    private readonly Vector3 _motorBlack = new Vector3(0.1f, 0.1f, 0.1f);
    private readonly Vector3 _steel = new Vector3(0.6f, 0.6f, 0.6f);

    public RoboticArm(Vector3 basePosition, float baseYawOffset)
    {
        BasePosition = basePosition;
        BaseYawOffset = baseYawOffset;
        
        _baseYaw = baseYawOffset;
        
        _baseHub = new Cylinder { Scale = new Vector3(0.7f, 0.15f, 0.7f), Color = _darkGrey };
        _baseBody = new Cylinder { Scale = new Vector3(0.5f, 0.5f, 0.5f), Color = _fanucYellow };
        
        _shoulderMotorL = new Cylinder { Scale = new Vector3(0.5f, 0.15f, 0.5f), Color = _motorBlack };
        _shoulderMotorR = new Cylinder { Scale = new Vector3(0.5f, 0.15f, 0.5f), Color = _motorBlack };
        
        _lowerArmLink1 = new Cube { Scale = new Vector3(0.15f, 1.3f, 0.35f), Color = _fanucYellow };
        _lowerArmLink2 = new Cube { Scale = new Vector3(0.15f, 1.3f, 0.35f), Color = _fanucYellow };
        
        _elbowJoint = new Cylinder { Scale = new Vector3(0.45f, 0.4f, 0.45f), Color = _fanucYellow };
        _elbowMotor = new Cylinder { Scale = new Vector3(0.35f, 0.45f, 0.35f), Color = _motorBlack };
        
        _upperArm = new Cube { Scale = new Vector3(0.25f, 1.1f, 0.25f), Color = _fanucYellow };
        _upperArmChamfer = new Cube { Scale = new Vector3(0.26f, 0.5f, 0.3f), Color = _fanucYellow };
        
        _wristJoint = new Cylinder { Scale = new Vector3(0.25f, 0.3f, 0.25f), Color = _darkGrey };
        _wristRotator = new Cube { Scale = new Vector3(0.2f, 0.2f, 0.2f), Color = _fanucYellow };
        
        _gripperBase = new Cube { Scale = new Vector3(0.3f, 0.1f, 0.2f), Color = _steel };
        _gripperFingerL = new Cube { Scale = new Vector3(0.05f, 0.3f, 0.05f), Color = _steel };
        _gripperFingerR = new Cube { Scale = new Vector3(0.05f, 0.3f, 0.05f), Color = _steel };
        
        UpdateTransforms();
    }
    
    public void StartPicking()
    {
        if (IsPicking) return;
        IsPicking = true;
        _animTime = 0.0f;
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
                float t = p / 0.25f;
                targetShoulder = MathHelper.Lerp(-20.0f, 40.0f, t);
                targetElbow = MathHelper.Lerp(40.0f, 60.0f, t);
                targetWrist = MathHelper.Lerp(-20.0f, 80.0f, t);
            }
            else if (p < 0.5f)
            {
                float t = (p - 0.25f) / 0.25f;
                targetShoulder = MathHelper.Lerp(40.0f, -10.0f, t);
                targetElbow = MathHelper.Lerp(60.0f, 20.0f, t);
                targetWrist = MathHelper.Lerp(80.0f, 0.0f, t);
                targetYaw = BaseYawOffset + MathHelper.Lerp(0, 45.0f, t);
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
            
            _baseYaw = targetYaw;
            _shoulderPitch = targetShoulder;
            _elbowPitch = targetElbow;
            _wristPitch = targetWrist;
        }
        else
        {
            float time = (float)DateTime.Now.TimeOfDay.TotalSeconds;
            _shoulderPitch = -20.0f + MathF.Sin(time * 2.0f) * 2.0f;
            _baseYaw = BaseYawOffset + MathF.Sin(time * 0.5f) * 5.0f;
        }
        
        UpdateTransforms();
    }
    
    private void UpdateTransforms()
    {
        // 1. Base
        Matrix4 baseFrame = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_baseYaw)) * Matrix4.CreateTranslation(BasePosition + new Vector3(0, 0.075f, 0));
        _baseHub.TransformOverride = Matrix4.CreateScale(_baseHub.Scale) * baseFrame;
        
        Matrix4 baseBodyFrame = Matrix4.CreateTranslation(0, 0.3f, 0) * baseFrame;
        _baseBody.TransformOverride = Matrix4.CreateScale(_baseBody.Scale) * baseBodyFrame;

        // 2. Shoulder
        Matrix4 shoulderFrame = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_shoulderPitch)) * Matrix4.CreateTranslation(0, 0.25f, 0) * baseBodyFrame;
        
        _shoulderMotorL.TransformOverride = Matrix4.CreateScale(_shoulderMotorL.Scale) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90)) * Matrix4.CreateTranslation(-0.25f, 0, 0) * shoulderFrame;
        _shoulderMotorR.TransformOverride = Matrix4.CreateScale(_shoulderMotorR.Scale) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90)) * Matrix4.CreateTranslation(0.25f, 0, 0) * shoulderFrame;
        
        _lowerArmLink1.TransformOverride = Matrix4.CreateScale(_lowerArmLink1.Scale) * Matrix4.CreateTranslation(-0.15f, 0.65f, 0) * shoulderFrame;
        _lowerArmLink2.TransformOverride = Matrix4.CreateScale(_lowerArmLink2.Scale) * Matrix4.CreateTranslation(0.15f, 0.65f, 0) * shoulderFrame;

        // 3. Elbow
        Matrix4 elbowJointFrame = Matrix4.CreateTranslation(0, 1.3f, 0) * shoulderFrame;
        _elbowJoint.TransformOverride = Matrix4.CreateScale(_elbowJoint.Scale) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90)) * elbowJointFrame;

        Matrix4 elbowFrame = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_elbowPitch)) * elbowJointFrame;
        _elbowMotor.TransformOverride = Matrix4.CreateScale(_elbowMotor.Scale) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90)) * Matrix4.CreateTranslation(-0.25f, 0, 0) * elbowFrame;
        
        _upperArm.TransformOverride = Matrix4.CreateScale(_upperArm.Scale) * Matrix4.CreateTranslation(0, 0.55f, 0) * elbowFrame;
        _upperArmChamfer.TransformOverride = Matrix4.CreateScale(_upperArmChamfer.Scale) * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(15)) * Matrix4.CreateTranslation(0, 0.2f, 0.05f) * elbowFrame;

        // 4. Wrist & Gripper
        Matrix4 wristJointFrame = Matrix4.CreateTranslation(0, 1.1f, 0) * elbowFrame;
        _wristJoint.TransformOverride = Matrix4.CreateScale(_wristJoint.Scale) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90)) * wristJointFrame;

        Matrix4 wristFrame = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_wristPitch)) * wristJointFrame;
        _wristRotator.TransformOverride = Matrix4.CreateScale(_wristRotator.Scale) * Matrix4.CreateTranslation(0, 0.1f, 0) * wristFrame;
        
        _gripperBase.TransformOverride = Matrix4.CreateScale(_gripperBase.Scale) * Matrix4.CreateTranslation(0, 0.25f, 0) * wristFrame;
        _gripperFingerL.TransformOverride = Matrix4.CreateScale(_gripperFingerL.Scale) * Matrix4.CreateTranslation(-0.1f, 0.45f, 0) * wristFrame;
        _gripperFingerR.TransformOverride = Matrix4.CreateScale(_gripperFingerR.Scale) * Matrix4.CreateTranslation(0.1f, 0.45f, 0) * wristFrame;
    }

    public void Draw(Shader shader)
    {
        _baseHub.Draw(shader);
        _baseBody.Draw(shader);
        _shoulderMotorL.Draw(shader);
        _shoulderMotorR.Draw(shader);
        _lowerArmLink1.Draw(shader);
        _lowerArmLink2.Draw(shader);
        _elbowJoint.Draw(shader);
        _elbowMotor.Draw(shader);
        _upperArm.Draw(shader);
        _upperArmChamfer.Draw(shader);
        _wristJoint.Draw(shader);
        _wristRotator.Draw(shader);
        _gripperBase.Draw(shader);
        _gripperFingerL.Draw(shader);
        _gripperFingerR.Draw(shader);
    }
}
