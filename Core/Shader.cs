using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace AISEG.DigitalTwin.Core;

public class Shader
{
    private readonly int _handle;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public Shader(
        string vertexSource,
        string fragmentSource
    )
    {
        // =====================================================
        // VERTEX SHADER
        // =====================================================

        int vertexShader =
            GL.CreateShader(
                ShaderType.VertexShader
            );

        GL.ShaderSource(
            vertexShader,
            vertexSource
        );

        GL.CompileShader(
            vertexShader
        );

        GL.GetShader(
            vertexShader,
            ShaderParameter.CompileStatus,
            out int vertexSuccess
        );

        if (vertexSuccess == 0)
        {
            string infoLog =
                GL.GetShaderInfoLog(vertexShader);

            throw new Exception(
                "Vertex shader compilation failed:\n"
                + infoLog
            );
        }

        // =====================================================
        // FRAGMENT SHADER
        // =====================================================

        int fragmentShader =
            GL.CreateShader(
                ShaderType.FragmentShader
            );

        GL.ShaderSource(
            fragmentShader,
            fragmentSource
        );

        GL.CompileShader(
            fragmentShader
        );

        GL.GetShader(
            fragmentShader,
            ShaderParameter.CompileStatus,
            out int fragmentSuccess
        );

        if (fragmentSuccess == 0)
        {
            string infoLog =
                GL.GetShaderInfoLog(fragmentShader);

            throw new Exception(
                "Fragment shader compilation failed:\n"
                + infoLog
            );
        }

        // =====================================================
        // CREATE PROGRAM
        // =====================================================

        _handle =
            GL.CreateProgram();

        GL.AttachShader(
            _handle,
            vertexShader
        );

        GL.AttachShader(
            _handle,
            fragmentShader
        );

        GL.LinkProgram(
            _handle
        );

        GL.GetProgram(
            _handle,
            GetProgramParameterName.LinkStatus,
            out int programSuccess
        );

        if (programSuccess == 0)
        {
            string infoLog =
                GL.GetProgramInfoLog(_handle);

            throw new Exception(
                "Shader program linking failed:\n"
                + infoLog
            );
        }

        // =====================================================
        // SHADERS NO LONGER NEEDED
        // =====================================================

        GL.DetachShader(
            _handle,
            vertexShader
        );

        GL.DetachShader(
            _handle,
            fragmentShader
        );

        GL.DeleteShader(
            vertexShader
        );

        GL.DeleteShader(
            fragmentShader
        );
    }

    // =========================================================
    // USE SHADER
    // =========================================================

    public void Use()
    {
        GL.UseProgram(
            _handle
        );
    }

    // =========================================================
    // SET MATRIX4
    // =========================================================

    public void SetMatrix4(
        string name,
        Matrix4 matrix
    )
    {
        int location =
            GL.GetUniformLocation(
                _handle,
                name
            );

        if (location == -1)
        {
            Console.WriteLine(
                $"WARNING: Uniform '{name}' not found."
            );

            return;
        }

        GL.UniformMatrix4(
            location,
            false,
            ref matrix
        );
    }

    // =========================================================
    // SET VECTOR3
    // =========================================================

    public void SetVector3(
        string name,
        Vector3 value
    )
    {
        int location =
            GL.GetUniformLocation(
                _handle,
                name
            );

        if (location == -1)
        {
            Console.WriteLine(
                $"WARNING: Uniform '{name}' not found."
            );

            return;
        }

        GL.Uniform3(
            location,
            value
        );
    }

    public void SetFloat(
        string name,
        float value
    )
    {
        int location = GL.GetUniformLocation(_handle, name);

        if (location == -1)
        {
            return;
        }

        GL.Uniform1(location, value);
    }
}