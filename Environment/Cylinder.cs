using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using AISEG.DigitalTwin.Core;

namespace AISEG.DigitalTwin.Environment;

public sealed class Cylinder
{
    private readonly int _vao;
    private readonly int _indexCount;

    public Vector3 Position { get; set; } = Vector3.Zero;

    public Vector3 Scale { get; set; } = Vector3.One;

    public Vector3 Rotation { get; set; } = Vector3.Zero;
    
    public Matrix4? TransformOverride { get; set; } = null;

    public Vector3 Color { get; set; } = new Vector3(0.4f);

    public Cylinder(int segments = 24)
    {
        List<float> vertices = new();
        List<uint> indices = new();

        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * MathF.PI * 2.0f;
            float x = MathF.Cos(angle) * 0.5f;
            float z = MathF.Sin(angle) * 0.5f;

            AddVertex(vertices, x, -0.5f, z);
            AddVertex(vertices, x, 0.5f, z);
        }

        uint bottomCenter = (uint)(vertices.Count / 3);
        AddVertex(vertices, 0.0f, -0.5f, 0.0f);

        uint topCenter = (uint)(vertices.Count / 3);
        AddVertex(vertices, 0.0f, 0.5f, 0.0f);

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            uint bottom = (uint)(i * 2);
            uint top = bottom + 1;
            uint nextBottom = (uint)(next * 2);
            uint nextTop = nextBottom + 1;

            AddTriangle(indices, bottom, nextBottom, top);
            AddTriangle(indices, top, nextBottom, nextTop);
            AddTriangle(indices, bottomCenter, nextBottom, bottom);
            AddTriangle(indices, topCenter, top, nextTop);
        }

        _indexCount = indices.Count;
        _vao = GL.GenVertexArray();
        int vbo = GL.GenBuffer();
        int ebo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertices.Count * sizeof(float),
            vertices.ToArray(),
            BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        GL.BufferData(
            BufferTarget.ElementArrayBuffer,
            indices.Count * sizeof(uint),
            indices.ToArray(),
            BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(
            0,
            3,
            VertexAttribPointerType.Float,
            false,
            3 * sizeof(float),
            0);
        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    private static void AddVertex(
        List<float> vertices,
        float x,
        float y,
        float z)
    {
        vertices.Add(x);
        vertices.Add(y);
        vertices.Add(z);
    }

    private static void AddTriangle(
        List<uint> indices,
        uint first,
        uint second,
        uint third)
    {
        indices.Add(first);
        indices.Add(second);
        indices.Add(third);
    }

    public void Draw(Shader shader)
    {
        Matrix4 model = TransformOverride ?? (
            Matrix4.CreateScale(Scale) *
            Matrix4.CreateRotationX(
                MathHelper.DegreesToRadians(Rotation.X)) *
            Matrix4.CreateRotationY(
                MathHelper.DegreesToRadians(Rotation.Y)) *
            Matrix4.CreateRotationZ(
                MathHelper.DegreesToRadians(Rotation.Z)) *
            Matrix4.CreateTranslation(Position));

        shader.SetMatrix4("model", model);
        shader.SetVector3("objectColor", Color);
        shader.SetFloat("beltMotionEnabled", 0.0f);
        shader.SetFloat("materialSpecular", 0.62f);
        shader.SetFloat("materialShininess", 48.0f);
        shader.SetFloat("floorPass", 0.0f);
        shader.SetFloat("structurePass", 0.0f);

        GL.BindVertexArray(_vao);
        GL.DrawElements(
            PrimitiveType.Triangles,
            _indexCount,
            DrawElementsType.UnsignedInt,
            0);
        GL.BindVertexArray(0);
    }
}