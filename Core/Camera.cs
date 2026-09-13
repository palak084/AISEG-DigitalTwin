using OpenTK.Mathematics;

namespace AISEG.DigitalTwin.Core;

public class Camera
{
    // ============================================================
    // CAMERA TARGET
    // ============================================================

    public Vector3 Target { get; private set; }

    // ============================================================
    // CAMERA POSITION
    // ============================================================

    public Vector3 Position { get; private set; }

    // ============================================================
    // CAMERA SETTINGS
    // ============================================================

    public float Fov { get; set; } = 45.0f;

    private float _distance;

    private float _yaw;

    private float _pitch;

    // ============================================================
    // CAMERA LIMITS
    // ============================================================

    // Prevent the camera from entering the conveyor itself.
    private const float MinimumDistance = 6.0f;

    private const float MaximumDistance = 25.0f;

    // The camera can look from low side angles
    // up to almost directly above the conveyor.
    //
    // We do NOT allow -90 degrees because that would
    // put the camera underneath the floor.
    private const float MinimumPitch = -5.0f;

    private const float MaximumPitch = 82.0f;

    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public Camera()
    {
        // Center of the conveyor.
        Target = new Vector3(
            0.0f,
            0.75f,
            0.0f
        );

        // Distance from conveyor.
        _distance = 12.5f;

        // Initial horizontal angle.
        //
        // 0 degrees = looking from +Z.
        //
        // There is NO limit on yaw.
        // Therefore the user can rotate a complete
        // 360 degrees and continue rotating.
        _yaw = 0.0f;

        // Low side perspective.
        _pitch = 13.0f;

        UpdatePosition();
    }

    // ============================================================
    // UPDATE CAMERA POSITION
    // ============================================================

    private void UpdatePosition()
    {
        float yawRadians =
            MathHelper.DegreesToRadians(
                _yaw
            );

        float pitchRadians =
            MathHelper.DegreesToRadians(
                _pitch
            );


        // Horizontal distance from target.
        float horizontalDistance =
            MathF.Cos(
                pitchRadians
            )
            *
            _distance;


        // Vertical distance from target.
        float verticalDistance =
            MathF.Sin(
                pitchRadians
            )
            *
            _distance;


        // ========================================================
        // ORBIT AROUND Y AXIS
        // ========================================================

        float x =
            MathF.Sin(
                yawRadians
            )
            *
            horizontalDistance;


        float z =
            MathF.Cos(
                yawRadians
            )
            *
            horizontalDistance;


        float y =
            Target.Y +
            verticalDistance;


        Position = new Vector3(
            Target.X + x,
            y,
            Target.Z + z
        );


        // ========================================================
        // FLOOR SAFETY
        // ========================================================

        // The floor is approximately at Y = 0.
        //
        // Never allow the camera itself to go below
        // the floor.
        if (Position.Y < 0.35f)
        {
            Position = new Vector3(
                Position.X,
                0.35f,
                Position.Z
            );
        }
    }

    // ============================================================
    // ORBIT CAMERA
    // ============================================================

    public void Orbit(
        float deltaX,
        float deltaY)
    {
        // ========================================================
        // HORIZONTAL ROTATION
        // ========================================================

        // IMPORTANT:
        //
        // There is intentionally NO Clamp on _yaw.
        //
        // This gives unlimited horizontal rotation:
        //
        // 0° → 90° → 180° → 270° → 360°
        //
        // and then it continues.
        _yaw -= deltaX * 0.35f;


        // ========================================================
        // VERTICAL ROTATION
        // ========================================================

        _pitch +=
            deltaY * 0.25f;


        // Prevent camera from flipping upside down
        // or going beneath the floor.
        _pitch =
            MathHelper.Clamp(
                _pitch,
                MinimumPitch,
                MaximumPitch
            );


        UpdatePosition();
    }

    // ============================================================
    // ZOOM
    // ============================================================

    public void Zoom(
        float scrollAmount)
    {
        _distance -=
            scrollAmount * 0.8f;


        _distance =
            MathHelper.Clamp(
                _distance,
                MinimumDistance,
                MaximumDistance
            );


        UpdatePosition();
    }

    // ============================================================
    // PAN CAMERA
    // ============================================================

    public void Pan(
        float deltaX,
        float deltaY)
    {
        Vector3 forward =
            Vector3.Normalize(
                Target - Position
            );


        Vector3 right =
            Vector3.Normalize(
                Vector3.Cross(
                    forward,
                    Vector3.UnitY
                )
            );


        Vector3 up =
            Vector3.Normalize(
                Vector3.Cross(
                    right,
                    forward
                )
            );


        float panSpeed =
            _distance * 0.0025f;


        Target +=
            (
                -right * deltaX
                +
                up * deltaY
            )
            *
            panSpeed;


        // Keep target above the floor.
        if (Target.Y < 0.45f)
        {
            Target = new Vector3(
                Target.X,
                0.45f,
                Target.Z
            );
        }


        UpdatePosition();
    }

    // ============================================================
    // VIEW MATRIX
    // ============================================================

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(
            Position,
            Target,
            Vector3.UnitY
        );
    }

    // ============================================================
    // PROJECTION MATRIX
    // ============================================================

    public Matrix4 GetProjectionMatrix(
        float aspectRatio)
    {
        return Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(
                Fov
            ),
            aspectRatio,
            0.1f,
            100.0f
        );
    }
}