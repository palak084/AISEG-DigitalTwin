using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace AISEG.DigitalTwin.Core;

public sealed class BackgroundRenderer
{
    private readonly int _vao;
    private readonly Shader _shader;

    public BackgroundRenderer()
    {
        float[] vertices =
        {
            -1.0f, -1.0f,
             1.0f, -1.0f,
             1.0f,  1.0f,
            -1.0f, -1.0f,
             1.0f,  1.0f,
            -1.0f,  1.0f
        };

        _vao = GL.GenVertexArray();
        int vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertices.Length * sizeof(float),
            vertices,
            BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(
            0,
            2,
            VertexAttribPointerType.Float,
            false,
            2 * sizeof(float),
            0);
        GL.EnableVertexAttribArray(0);
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        _shader = new Shader(
            """
            #version 330 core
            layout (location = 0) in vec2 aPosition;
            out vec2 screenPosition;
            void main()
            {
                screenPosition = aPosition;
                gl_Position = vec4(aPosition, 0.999, 1.0);
            }
            """,
            """
            #version 330 core
            in vec2 screenPosition;
            out vec4 FragColor;
            uniform vec3 zenithColor;
            uniform vec3 horizonColor;
            void main()
            {
                float horizon = smoothstep(-0.75, 0.65, screenPosition.y);
                vec3 color = mix(horizonColor, zenithColor, horizon);
                float glow = exp(-pow((screenPosition.y + 0.18) * 3.0, 2.0));
                color += vec3(0.035, 0.028, 0.020) * glow;
                FragColor = vec4(color, 1.0);
            }
            """);
    }

    public void Draw()
    {
        GL.DepthMask(false);
        GL.Disable(EnableCap.DepthTest);
        _shader.Use();
        _shader.SetVector3("zenithColor", new Vector3(0.018f, 0.025f, 0.045f));
        _shader.SetVector3("horizonColor", new Vector3(0.20f, 0.19f, 0.17f));
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindVertexArray(0);
        GL.DepthMask(true);
        GL.Enable(EnableCap.DepthTest);
    }
}
