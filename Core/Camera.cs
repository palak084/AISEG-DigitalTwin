using OpenTK.Mathematics;

namespace AISEG.DigitalTwin.Core;

public class Camera
{
    // =========================================================
    // CAMERA POSITION
    // =========================================================
    // Positioned diagonally in front of the conveyor.
    //
    // X = distance along the conveyor
    // Y = camera height
    // Z = distance from the conveyor
    //
    // This is intentionally lower than the previous camera
    // so the conveyor looks more like an industrial machine
    // instead of a top-down CAD model.

    public Vector3 Position { get; set; }


    // =========================================================
    // CAMERA TARGET
    // =========================================================

    public Vector3 Target { get; set; }


    // =========================================================
    // FIELD OF VIEW
    // =========================================================

    public float Fov { get; set; } = 40.0f;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public Camera()
    {
        // Camera position for the 10 m conveyor.
        //
        // Lower camera height gives a stronger 3D perspective.
        // The camera is far enough away to see the complete
        // conveyor without making it look tiny.

        Position = new Vector3(
            7.4f,
            3.8f,
            7.6f
        );


        // Aim toward the central working area of the conveyor.

        Target = new Vector3(
            0.0f,
            0.65f,
            0.0f
        );
    }


    // =========================================================
    // VIEW MATRIX
    // =========================================================

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(
            Position,
            Target,
            Vector3.UnitY
        );
    }


    // =========================================================
    // PROJECTION MATRIX
    // =========================================================

    public Matrix4 GetProjectionMatrix(
        float aspectRatio
    )
    {
        return Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(Fov),
            aspectRatio,
            0.1f,
            200.0f
        );
    }
}