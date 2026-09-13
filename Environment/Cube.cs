using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using AISEG.DigitalTwin.Core;

namespace AISEG.DigitalTwin.Environment;

public class Cube
{
    private readonly int _vao;
    private readonly int _vbo;

    public Vector3 Position { get; set; } = Vector3.Zero;

    public Vector3 Scale { get; set; } = Vector3.One;

    public Vector3 Rotation { get; set; } = Vector3.Zero;
    
    public Matrix4? TransformOverride { get; set; } = null;

    public Vector3 Color { get; set; } =
        new Vector3(0.5f, 0.5f, 0.5f);

    public bool IsFloor { get; set; }


    public Cube()
    {
        float[] vertices =
        {
            // Front
            -0.5f, -0.5f,  0.5f,
             0.5f, -0.5f,  0.5f,
             0.5f,  0.5f,  0.5f,

             0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f,  0.5f,
            -0.5f, -0.5f,  0.5f,

            // Back
             0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,
            -0.5f,  0.5f, -0.5f,

            -0.5f,  0.5f, -0.5f,
             0.5f,  0.5f, -0.5f,
             0.5f, -0.5f, -0.5f,

            // Left
            -0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f,  0.5f,
            -0.5f,  0.5f,  0.5f,

            -0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,

            // Right
             0.5f, -0.5f,  0.5f,
             0.5f, -0.5f, -0.5f,
             0.5f,  0.5f, -0.5f,

             0.5f,  0.5f, -0.5f,
             0.5f,  0.5f,  0.5f,
             0.5f, -0.5f,  0.5f,

            // Top
            -0.5f,  0.5f,  0.5f,
             0.5f,  0.5f,  0.5f,
             0.5f,  0.5f, -0.5f,

             0.5f,  0.5f, -0.5f,
            -0.5f,  0.5f, -0.5f,
            -0.5f,  0.5f,  0.5f,

            // Bottom
            -0.5f, -0.5f, -0.5f,
             0.5f, -0.5f, -0.5f,
             0.5f, -0.5f,  0.5f,

             0.5f, -0.5f,  0.5f,
            -0.5f, -0.5f,  0.5f,
            -0.5f, -0.5f, -0.5f
        };


        _vao = GL.GenVertexArray();

        _vbo = GL.GenBuffer();


        GL.BindVertexArray(_vao);

        GL.BindBuffer(
            BufferTarget.ArrayBuffer,
            _vbo
        );


        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertices.Length * sizeof(float),
            vertices,
            BufferUsageHint.StaticDraw
        );


        GL.VertexAttribPointer(
            0,
            3,
            VertexAttribPointerType.Float,
            false,
            3 * sizeof(float),
            0
        );


        GL.EnableVertexAttribArray(0);


        GL.BindBuffer(
            BufferTarget.ArrayBuffer,
            0
        );

        GL.BindVertexArray(0);
    }


    public void Draw(Shader shader)
    {
        Matrix4 model = TransformOverride ?? (
            Matrix4.CreateScale(Scale) *
            Matrix4.CreateRotationX(
                MathHelper.DegreesToRadians(Rotation.X)
            ) *
            Matrix4.CreateRotationY(
                MathHelper.DegreesToRadians(Rotation.Y)
            ) *
            Matrix4.CreateRotationZ(
                MathHelper.DegreesToRadians(Rotation.Z)
            ) *
            Matrix4.CreateTranslation(Position));

        shader.SetMatrix4(
            "model",
            model
        );


        shader.SetVector3(
            "objectColor",
            Color
        );

        shader.SetFloat("beltMotionEnabled", 0.0f);
        shader.SetFloat("materialSpecular", IsFloor ? 0.12f : 0.48f);
        shader.SetFloat("materialShininess", IsFloor ? 18.0f : 42.0f);
        shader.SetFloat("floorPass", IsFloor ? 1.0f : 0.0f);
        shader.SetFloat("structurePass", IsFloor ? 0.0f : 1.0f);


        GL.BindVertexArray(_vao);


        GL.DrawArrays(
            PrimitiveType.Triangles,
            0,
            36
        );


        GL.BindVertexArray(0);
    }
}