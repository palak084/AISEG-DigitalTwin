using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using AISEG.DigitalTwin.Core;

namespace AISEG.DigitalTwin.Conveyor;

public sealed class ConveyorBeltMesh
{
    private readonly int _vao;
    private readonly int _indexCount;

    public ConveyorBeltMesh(
        UConveyorPath path,
        float height,
        float thickness)
    {
        float[] vertices =
            CreateVertices(
                path,
                height,
                thickness
            );

        uint[] indices =
            CreateIndices(
                path.Points.Count
            );

        _indexCount =
            indices.Length;


        // =====================================================
        // VAO
        // =====================================================

        _vao =
            GL.GenVertexArray();


        // =====================================================
        // VBO
        // =====================================================

        int vbo =
            GL.GenBuffer();


        // =====================================================
        // EBO
        // =====================================================

        int ebo =
            GL.GenBuffer();


        GL.BindVertexArray(
            _vao
        );


        // =====================================================
        // VERTEX BUFFER
        // =====================================================

        GL.BindBuffer(
            BufferTarget.ArrayBuffer,
            vbo
        );

        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertices.Length * sizeof(float),
            vertices,
            BufferUsageHint.StaticDraw
        );


        // =====================================================
        // INDEX BUFFER
        // =====================================================

        GL.BindBuffer(
            BufferTarget.ElementArrayBuffer,
            ebo
        );

        GL.BufferData(
            BufferTarget.ElementArrayBuffer,
            indices.Length * sizeof(uint),
            indices,
            BufferUsageHint.StaticDraw
        );


        // =====================================================
        // POSITION
        //
        // location 0 = vec3 position
        // =====================================================

        GL.VertexAttribPointer(
            0,
            3,
            VertexAttribPointerType.Float,
            false,
            4 * sizeof(float),
            0
        );

        GL.EnableVertexAttribArray(0);


        // =====================================================
        // BELT COORDINATE
        //
        // location 1 = float
        //
        // This tells the shader where a point exists along
        // the conveyor path.
        // =====================================================

        GL.VertexAttribPointer(
            1,
            1,
            VertexAttribPointerType.Float,
            false,
            4 * sizeof(float),
            3 * sizeof(float)
        );

        GL.EnableVertexAttribArray(1);


        GL.BindVertexArray(0);

        GL.BindBuffer(
            BufferTarget.ArrayBuffer,
            0
        );
    }


    // =========================================================
    // CREATE VERTICES
    // =========================================================

    private static float[] CreateVertices(
        UConveyorPath path,
        float height,
        float thickness)
    {
        List<float> vertices =
            new();


        float halfWidth =
            path.BeltWidth / 2.0f;


        // =====================================================
        // TOP SURFACE
        // =====================================================

        float distance =
            0.0f;


        for (
            int i = 0;
            i < path.Points.Count;
            i++)
        {
            UConveyorPoint point =
                path.Points[i];


            Vector2 outer =
                point.Position +
                point.Normal *
                halfWidth;


            Vector2 inner =
                point.Position -
                point.Normal *
                halfWidth;


            float coordinate =
                path.TotalLength <= 0.0f
                    ? 0.0f
                    : distance /
                      path.TotalLength;


            AddVertex(
                vertices,
                new Vector3(
                    outer.X,
                    height,
                    outer.Y
                ),
                coordinate
            );


            AddVertex(
                vertices,
                new Vector3(
                    inner.X,
                    height,
                    inner.Y
                ),
                coordinate
            );


            if (
                i <
                path.Points.Count - 1)
            {
                distance +=
                    (
                        path.Points[i + 1].Position -
                        point.Position
                    ).Length;
            }
        }


        // =====================================================
        // BOTTOM SURFACE
        // =====================================================

        distance = 0.0f;


        for (
            int i = 0;
            i < path.Points.Count;
            i++)
        {
            UConveyorPoint point =
                path.Points[i];


            Vector2 outer =
                point.Position +
                point.Normal *
                halfWidth;


            Vector2 inner =
                point.Position -
                point.Normal *
                halfWidth;


            float coordinate =
                path.TotalLength <= 0.0f
                    ? 0.0f
                    : distance /
                      path.TotalLength;


            AddVertex(
                vertices,
                new Vector3(
                    outer.X,
                    height - thickness,
                    outer.Y
                ),
                coordinate
            );


            AddVertex(
                vertices,
                new Vector3(
                    inner.X,
                    height - thickness,
                    inner.Y
                ),
                coordinate
            );


            if (
                i <
                path.Points.Count - 1)
            {
                distance +=
                    (
                        path.Points[i + 1].Position -
                        point.Position
                    ).Length;
            }
        }


        return vertices.ToArray();
    }


    // =========================================================
    // CREATE INDICES
    // =========================================================

    private static uint[] CreateIndices(
        int count)
    {
        List<uint> indices =
            new();


        int bottomOffset =
            count * 2;


        for (
            int i = 0;
            i < count - 1;
            i++)
        {
            int next =
                i + 1;


            uint topOuter =
                (uint)(i * 2);


            uint topInner =
                topOuter + 1;


            uint nextTopOuter =
                (uint)(next * 2);


            uint nextTopInner =
                nextTopOuter + 1;


            uint bottomOuter =
                (uint)(
                    bottomOffset +
                    i * 2
                );


            uint bottomInner =
                bottomOuter + 1;


            uint nextBottomOuter =
                (uint)(
                    bottomOffset +
                    next * 2
                );


            uint nextBottomInner =
                nextBottomOuter + 1;


            // Top
            AddQuad(
                indices,
                topOuter,
                nextTopOuter,
                topInner,
                nextTopInner
            );


            // Bottom
            AddQuad(
                indices,
                bottomOuter,
                bottomInner,
                nextBottomOuter,
                nextBottomInner
            );


            // Outer side
            AddQuad(
                indices,
                topOuter,
                bottomOuter,
                nextTopOuter,
                nextBottomOuter
            );


            // Inner side
            AddQuad(
                indices,
                topInner,
                nextTopInner,
                bottomInner,
                nextBottomInner
            );
        }


        // First end
        AddQuad(
            indices,
            0,
            1,
            (uint)bottomOffset,
            (uint)(bottomOffset + 1)
        );


        // Last end
        uint lastTopOuter =
            (uint)(
                (count - 1) * 2
            );


        uint lastTopInner =
            lastTopOuter + 1;


        uint lastBottomOuter =
            (uint)(
                bottomOffset +
                (count - 1) * 2
            );


        uint lastBottomInner =
            lastBottomOuter + 1;


        AddQuad(
            indices,
            lastTopOuter,
            lastBottomOuter,
            lastTopInner,
            lastBottomInner
        );


        return indices.ToArray();
    }


    // =========================================================
    // ADD QUAD
    // =========================================================

    private static void AddQuad(
        List<uint> indices,
        uint first,
        uint second,
        uint third,
        uint fourth)
    {
        indices.Add(first);
        indices.Add(second);
        indices.Add(third);

        indices.Add(third);
        indices.Add(second);
        indices.Add(fourth);
    }


    // =========================================================
    // ADD VERTEX
    // =========================================================

    private static void AddVertex(
        List<float> vertices,
        Vector3 position,
        float coordinate)
    {
        vertices.Add(position.X);
        vertices.Add(position.Y);
        vertices.Add(position.Z);

        vertices.Add(coordinate);
    }


    // =========================================================
    // DRAW
    // =========================================================

    public void Draw(
        Shader shader,
        Vector3 color,
        float motionOffset = 0.0f)
    {
        // =====================================================
        // MODEL
        // =====================================================

        shader.SetMatrix4(
            "model",
            Matrix4.Identity
        );


        // =====================================================
        // BASE COLOR
        // =====================================================

        shader.SetVector3(
            "objectColor",
            color
        );


        // =====================================================
        // ENABLE BELT ANIMATION
        // =====================================================

        shader.SetFloat(
            "beltMotionEnabled",
            1.0f
        );


        // =====================================================
        // CURRENT BELT POSITION
        // =====================================================

        shader.SetFloat(
            "beltMotionOffset",
            motionOffset
        );


        // =====================================================
        // MATERIAL
        // =====================================================

        shader.SetFloat(
            "materialSpecular",
            0.22f
        );


        shader.SetFloat(
            "materialShininess",
            24.0f
        );


        shader.SetFloat(
            "floorPass",
            0.0f
        );


        shader.SetFloat(
            "structurePass",
            0.0f
        );


        // =====================================================
        // DRAW
        // =====================================================

        GL.BindVertexArray(
            _vao
        );


        GL.DrawElements(
            PrimitiveType.Triangles,
            _indexCount,
            DrawElementsType.UnsignedInt,
            0
        );


        GL.BindVertexArray(0);
    }
}