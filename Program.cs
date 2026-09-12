using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

using AISEG.DigitalTwin.Core;
using AISEG.DigitalTwin.Environment;
using AISEG.DigitalTwin.Conveyor;

// ============================================================
// AI-SEG DIGITAL TWIN
// PHASE 1
//
// Current objective:
// 3D OpenGL environment + conveyor
//
// Next:
// Waste Objects → Robots → Sensors → AI
// ============================================================


// ============================================================
// 1. GAME WINDOW SETTINGS
// ============================================================

var gameWindowSettings = new GameWindowSettings
{
    UpdateFrequency = 60.0
};


// ============================================================
// 2. NATIVE WINDOW SETTINGS
// ============================================================

var nativeWindowSettings = new NativeWindowSettings
{
    ClientSize = new Vector2i(
        1280,
        720
    ),

    Title = "AI-SEG Digital Twin - Phase 1",

    NumberOfSamples = 4
};


// ============================================================
// 3. CREATE WINDOW
// ============================================================

using var window = new GameWindow(
    gameWindowSettings,
    nativeWindowSettings
);


// ============================================================
// 4. CAMERA
// ============================================================

Camera camera =
    new Camera();


// ============================================================
// 5. OBJECT REFERENCES
//
// OpenGL resources are created inside Load because the
// OpenGL context must already exist.
// ============================================================

Shader? shader = null;

Conveyor? conveyor = null;

Cube? floor = null;

BackgroundRenderer? background = null;


// ============================================================
// 6. VERTEX SHADER
// ============================================================

string vertexShaderSource = """

#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in float aBeltCoordinate;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec3 worldPosition;
out float beltCoordinate;

void main()
{
    vec4 world = model * vec4(aPosition, 1.0);
    worldPosition = world.xyz;
    beltCoordinate = aBeltCoordinate;

    gl_Position =
        projection *
        view *
        world;
}

""";


// ============================================================
// 7. FRAGMENT SHADER
// ============================================================

string fragmentShaderSource = """

#version 330 core

out vec4 FragColor;

uniform vec3 objectColor;
uniform vec3 lightDirection;
uniform vec3 fillLightDirection;
uniform vec3 viewPosition;
uniform float ambientStrength;
uniform float specularStrength;
uniform float fillLightStrength;
uniform float materialSpecular;
uniform float materialShininess;
uniform float floorPass;
uniform float structurePass;
uniform vec3 pointLightPositions[3];
uniform vec3 pointLightColors[3];
uniform float pointLightIntensities[3];
uniform float pointLightLinear;
uniform float pointLightQuadratic;
uniform float beltMotionEnabled;
uniform float beltMotionOffset;

in vec3 worldPosition;
in float beltCoordinate;

void main()
{
    vec3 normal = normalize(cross(dFdx(worldPosition), dFdy(worldPosition)));
    vec3 viewDirection = normalize(viewPosition - worldPosition);

    vec3 light = normalize(lightDirection);
    float diffuse = max(dot(normal, light), 0.0) * 1.35;
    vec3 fillLight = normalize(fillLightDirection);
    float fillDiffuse = max(dot(normal, fillLight), 0.0);
    vec3 halfway = normalize(light + viewDirection);
    float specular = pow(
        max(dot(normal, halfway), 0.0),
        materialShininess);
    vec3 skyAmbient = vec3(0.18, 0.20, 0.24);
    vec3 groundAmbient = vec3(0.08, 0.075, 0.065);
    float hemisphere = normal.y * 0.5 + 0.5;
    vec3 ambient = mix(groundAmbient, skyAmbient, hemisphere) * ambientStrength;
    vec3 litColor = objectColor * (ambient + vec3(diffuse));
    litColor += objectColor * fillLightStrength * fillDiffuse;
    litColor += vec3(materialSpecular * specularStrength * specular);

    for (int i = 0; i < 3; i++)
    {
        vec3 toPoint = pointLightPositions[i] - worldPosition;
        float distanceToPoint = length(toPoint);
        vec3 pointDirection = normalize(toPoint);
        float attenuation = 1.0 / (1.0 +
            pointLightLinear * distanceToPoint +
            pointLightQuadratic * distanceToPoint * distanceToPoint);
        float pointDiffuse = max(dot(normal, pointDirection), 0.0);
        vec3 pointHalfway = normalize(pointDirection + viewDirection);
        float pointSpecular = pow(
            max(dot(normal, pointHalfway), 0.0),
            48.0);

        litColor += objectColor * pointLightColors[i] *
            pointLightIntensities[i] * attenuation * pointDiffuse;
        litColor += pointLightColors[i] *
            pointLightIntensities[i] * attenuation *
            specularStrength * pointSpecular;
    }

    float edgeHighlight = pow(1.0 - max(dot(normal, viewDirection), 0.0), 3.0);
    litColor += vec3(0.10, 0.12, 0.14) * edgeHighlight;
    float beltVariation = 1.0;

    if (beltMotionEnabled > 0.5)
    {
        float pattern = sin((beltCoordinate - beltMotionOffset) * 240.0);
        beltVariation = 0.985 + 0.015 * (0.5 + 0.5 * pattern);
    }

    vec3 exposedColor = litColor * beltVariation * 1.15;

    if (floorPass > 0.5)
    {
        float nearestX = min(
            min(abs(worldPosition.x + 3.7), abs(worldPosition.x + 1.85)),
            min(abs(worldPosition.x), abs(worldPosition.x - 1.85)));
        nearestX = min(nearestX, abs(worldPosition.x - 3.7));
        float nearestZ = min(abs(abs(worldPosition.z) - 0.68),
            abs(abs(worldPosition.z) - 2.12));
        float contact = exp(-nearestX * nearestX * 18.0 -
            nearestZ * nearestZ * 8.0);
        exposedColor *= 1.0 - 0.22 * contact;
    }

    if (structurePass > 0.5)
    {
        float lowerJoint = exp(-abs(worldPosition.y - 1.0) * 12.0);
        float upperJoint = exp(-abs(worldPosition.y - 1.18) * 18.0);
        exposedColor *= 1.0 - 0.10 * max(lowerJoint, upperJoint);
    }

    vec3 displayColor = vec3(1.0) - exp(-exposedColor);
    displayColor = pow(displayColor, vec3(1.0 / 2.2));

    FragColor = vec4(displayColor, 1.0);
}

""";


// ============================================================
// 8. LOAD EVENT
// ============================================================

window.Load += () =>
{
    Console.WriteLine(
        "========================================"
    );

    Console.WriteLine(
        "Starting AI-SEG Digital Twin..."
    );

    Console.WriteLine(
        "========================================"
    );


    // ========================================================
    // OPENGL INFORMATION
    // ========================================================

    Console.WriteLine(
        "OpenGL Version: "
        + GL.GetString(StringName.Version)
    );

    Console.WriteLine(
        "GPU: "
        + GL.GetString(StringName.Renderer)
    );

    Console.WriteLine(
        "GLSL: "
        + GL.GetString(StringName.ShadingLanguageVersion)
    );


    // ========================================================
    // VIEWPORT
    // ========================================================

    GL.Viewport(
        0,
        0,
        window.Size.X,
        window.Size.Y
    );


    // ========================================================
    // BACKGROUND
    // ========================================================

    GL.ClearColor(
        0.04f,
        0.04f,
        0.06f,
        1.0f
    );


    // ========================================================
    // DEPTH TESTING
    // ========================================================

    GL.Enable(
        EnableCap.DepthTest
    );

    GL.Enable(
        EnableCap.Multisample
    );


    // ========================================================
    // CREATE SHADER
    // ========================================================

    shader = new Shader(
        vertexShaderSource,
        fragmentShaderSource
    );

    background = new BackgroundRenderer();


    // ========================================================
    // CREATE CONVEYOR PARAMETERS
    // ========================================================

    ConveyorParameters conveyorParameters =
        new ConveyorParameters();


    // ========================================================
    // DISPLAY CONVEYOR PARAMETERS
    // ========================================================

    Console.WriteLine(
        $"Conveyor Length: {conveyorParameters.Length} m"
    );

    Console.WriteLine(
        $"Conveyor Width: {conveyorParameters.Width} m"
    );

    Console.WriteLine(
        $"Conveyor Speed: {conveyorParameters.Speed} m/s"
    );

    Console.WriteLine(
        $"Maximum Item Weight: {conveyorParameters.MaximumItemWeight} kg"
    );


    // ========================================================
    // CREATE CONVEYOR
    // ========================================================

    conveyor =
        new Conveyor(
            conveyorParameters
        );

    floor = new Cube
    {
        Position = new Vector3(0.0f, -0.14f, 0.0f),
        Scale = new Vector3(22.0f, 0.20f, 14.0f),
        Color = new Vector3(0.22f, 0.23f, 0.24f),
        IsFloor = true
    };


    Console.WriteLine(
        "OpenGL initialization completed."
    );

    Console.WriteLine(
        "Conveyor created successfully."
    );

    conveyor.PrintState();
};


// ============================================================
// 9. FRAMEBUFFER RESIZE
// ============================================================

window.FramebufferResize += args =>
{
    GL.Viewport(
        0,
        0,
        args.Width,
        args.Height
    );
};


// ============================================================
// 10. UPDATE LOOP
// ============================================================

window.UpdateFrame += args =>
{
    if (conveyor == null)
    {
        return;
    }

    if (window.KeyboardState.IsKeyPressed(Keys.Space))
    {
        conveyor.ToggleRunning();
    }

    if (window.KeyboardState.IsKeyPressed(Keys.R))
    {
        conveyor.ReverseDirection();
    }

    conveyor.Update(args.Time);
};


// ============================================================
// 11. RENDER LOOP
// ============================================================

window.RenderFrame += args =>
{
    // ========================================================
    // CLEAR SCREEN
    // ========================================================

    GL.Clear(
        ClearBufferMask.ColorBufferBit |
        ClearBufferMask.DepthBufferBit
    );

    background?.Draw();


    // ========================================================
    // MAKE SURE OBJECTS EXIST
    // ========================================================

    if (
        shader != null &&
        conveyor != null
    )
    {
        // ====================================================
        // ACTIVATE SHADER
        // ====================================================

        shader.Use();


        // ====================================================
        // ASPECT RATIO
        // ====================================================

        float aspectRatio =
            window.Size.X /
            (float)window.Size.Y;


        // ====================================================
        // CAMERA VIEW
        // ====================================================

        Matrix4 view =
            camera.GetViewMatrix();


        // ====================================================
        // CAMERA PROJECTION
        // ====================================================

        Matrix4 projection =
            camera.GetProjectionMatrix(
                aspectRatio
            );


        // ====================================================
        // SEND VIEW MATRIX
        // ====================================================

        shader.SetMatrix4(
            "view",
            view
        );


        // ====================================================
        // SEND PROJECTION MATRIX
        // ====================================================

        shader.SetMatrix4(
            "projection",
            projection
        );

        shader.SetVector3(
            "lightDirection",
            new Vector3(-0.55f, 0.85f, 0.45f)
        );

        shader.SetVector3(
            "fillLightDirection",
            new Vector3(0.55f, 0.40f, -0.65f)
        );

        shader.SetVector3(
            "viewPosition",
            camera.Position
        );

        shader.SetFloat(
            "ambientStrength",
            0.20f
        );

        shader.SetFloat(
            "specularStrength",
            0.34f
        );

        shader.SetFloat(
            "fillLightStrength",
            0.24f
        );

        shader.SetVector3(
            "pointLightPositions[0]",
            new Vector3(-2.5f, 4.5f, 2.0f)
        );
        shader.SetVector3(
            "pointLightPositions[1]",
            new Vector3(2.5f, 4.0f, -1.5f)
        );
        shader.SetVector3(
            "pointLightPositions[2]",
            new Vector3(0.0f, 3.5f, 3.0f)
        );

        shader.SetVector3(
            "pointLightColors[0]",
            new Vector3(1.0f, 0.78f, 0.58f)
        );
        shader.SetVector3(
            "pointLightColors[1]",
            new Vector3(0.72f, 0.84f, 1.0f)
        );
        shader.SetVector3(
            "pointLightColors[2]",
            new Vector3(1.0f, 0.88f, 0.70f)
        );

        shader.SetFloat("pointLightIntensities[0]", 1.8f);
        shader.SetFloat("pointLightIntensities[1]", 1.4f);
        shader.SetFloat("pointLightIntensities[2]", 1.2f);
        shader.SetFloat("pointLightLinear", 0.08f);
        shader.SetFloat("pointLightQuadratic", 0.025f);

        floor?.Draw(shader);


        // ====================================================
        // DRAW CONVEYOR
        // ====================================================

        conveyor.Draw(
            shader
        );
    }


    // ========================================================
    // OPENGL ERROR CHECK
    // ========================================================

    ErrorCode error =
        GL.GetError();

    if (error != ErrorCode.NoError)
    {
        Console.WriteLine(
            "OpenGL Error: "
            + error
        );
    }


    // ========================================================
    // DISPLAY FRAME
    // ========================================================

    window.SwapBuffers();
};


// ============================================================
// 12. START APPLICATION
// ============================================================

Console.WriteLine(
    "Starting render loop..."
);

window.Run();

Console.WriteLine(
    "Application closed."
);  