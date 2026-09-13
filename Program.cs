using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;
using MouseButton = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;

using AISEG.DigitalTwin.Core;
using AISEG.DigitalTwin.Environment;
using AISEG.DigitalTwin.Conveyor;


// ============================================================
// AI-SEG DIGITAL TWIN
// PHASE 1
//
// 3D OpenGL Environment
// Interactive Camera
// Conveyor Simulation
// Digital Twin Information Layer
// ============================================================


// ============================================================
// 1. GAME WINDOW SETTINGS
// ============================================================

var gameWindowSettings = new GameWindowSettings
{
    UpdateFrequency = 60.0
};


// ============================================================
// 2. WINDOW SETTINGS
// ============================================================

var nativeWindowSettings = new NativeWindowSettings
{
    ClientSize = new Vector2i(
        1280,
        720
    ),

    Title =
        "AI-SEG Digital Twin - Phase 1",

    NumberOfSamples = 4
};


// ============================================================
// 3. CREATE WINDOW
// ============================================================

using var window =
    new GameWindow(
        gameWindowSettings,
        nativeWindowSettings
    );


// ============================================================
// 4. CAMERA
// ============================================================

Camera camera =
    new Camera();


// ============================================================
// 5. MOUSE STATE
// ============================================================

Vector2 lastMousePosition =
    Vector2.Zero;

bool leftMouseDragging =
    false;

bool rightMouseDragging =
    false;


// ============================================================
// 6. OBJECT REFERENCES
// ============================================================

Shader? shader = null;

Conveyor? conveyor = null;

BackgroundRenderer? background = null;


// ============================================================
// 7. ENVIRONMENT
// ============================================================

List<Cube> environmentObjects =
    new();


// ============================================================
// 8. INFORMATION SIGNS
// ============================================================

// List<InformationSign> informationSigns =
//     new();


// ============================================================
// 9. VERTEX SHADER
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
    vec4 world =
        model *
        vec4(
            aPosition,
            1.0
        );

    worldPosition =
        world.xyz;

    beltCoordinate =
        aBeltCoordinate;

    gl_Position =
        projection *
        view *
        world;
}

""";


// ============================================================
// 10. FRAGMENT SHADER
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
    // ========================================================
    // SURFACE NORMAL
    // ========================================================

    vec3 normal =
        normalize(
            cross(
                dFdx(worldPosition),
                dFdy(worldPosition)
            )
        );


    // ========================================================
    // VIEW DIRECTION
    // ========================================================

    vec3 viewDirection =
        normalize(
            viewPosition -
            worldPosition
        );


    // ========================================================
    // MAIN LIGHT
    // ========================================================

    vec3 light =
        normalize(
            lightDirection
        );

    float diffuse =
        max(
            dot(
                normal,
                light
            ),
            0.0
        );


    // ========================================================
    // FILL LIGHT
    // ========================================================

    vec3 fillLight =
        normalize(
            fillLightDirection
        );

    float fillDiffuse =
        max(
            dot(
                normal,
                fillLight
            ),
            0.0
        );


    // ========================================================
    // SPECULAR
    // ========================================================

    vec3 halfway =
        normalize(
            light +
            viewDirection
        );

    float specular =
        pow(
            max(
                dot(
                    normal,
                    halfway
                ),
                0.0
            ),
            materialShininess
        );


    // ========================================================
    // AMBIENT
    // ========================================================

    vec3 skyAmbient =
        vec3(
            0.20,
            0.22,
            0.25
        );

    vec3 groundAmbient =
        vec3(
            0.055,
            0.055,
            0.055
        );


    float hemisphere =
        normal.y *
        0.5 +
        0.5;


    vec3 ambient =
        mix(
            groundAmbient,
            skyAmbient,
            hemisphere
        )
        *
        ambientStrength;


    // ========================================================
    // BASE LIGHTING
    // ========================================================

    vec3 litColor =
        objectColor *
        (
            ambient +
            vec3(diffuse)
        );


    litColor +=
        objectColor *
        fillLightStrength *
        fillDiffuse;


    litColor +=
        vec3(
            materialSpecular *
            specularStrength *
            specular
        );


    // ========================================================
    // POINT LIGHTS
    // ========================================================

    for (int i = 0; i < 3; i++)
    {
        vec3 toPoint =
            pointLightPositions[i] -
            worldPosition;

        float distanceToPoint =
            length(
                toPoint
            );

        vec3 pointDirection =
            normalize(
                toPoint
            );


        float attenuation =
            1.0 /
            (
                1.0 +
                pointLightLinear *
                distanceToPoint +
                pointLightQuadratic *
                distanceToPoint *
                distanceToPoint
            );


        float pointDiffuse =
            max(
                dot(
                    normal,
                    pointDirection
                ),
                0.0
            );


        vec3 pointHalfway =
            normalize(
                pointDirection +
                viewDirection
            );


        float pointSpecular =
            pow(
                max(
                    dot(
                        normal,
                        pointHalfway
                    ),
                    0.0
                ),
                48.0
            );


        litColor +=
            objectColor *
            pointLightColors[i] *
            pointLightIntensities[i] *
            attenuation *
            pointDiffuse;


        litColor +=
            pointLightColors[i] *
            pointLightIntensities[i] *
            attenuation *
            specularStrength *
            pointSpecular;
    }


    // ========================================================
    // EDGE HIGHLIGHT
    // ========================================================

    float edgeHighlight =
        pow(
            1.0 -
            max(
                dot(
                    normal,
                    viewDirection
                ),
                0.0
            ),
            3.0
        );


    litColor +=
        vec3(
            0.07,
            0.08,
            0.09
        )
        *
        edgeHighlight;


    // ========================================================
    // FLOOR CONTACT
    // ========================================================

    vec3 exposedColor =
        litColor;


    if (
        floorPass > 0.5
    )
    {
        float contact =
            exp(
                -worldPosition.y *
                worldPosition.y *
                18.0
            );

        exposedColor *=
            1.0 -
            0.10 *
            contact;
    }


    // ========================================================
    // STRUCTURAL DARKENING
    // ========================================================

    if (
        structurePass > 0.5
    )
    {
        float lowerJoint =
            exp(
                -abs(
                    worldPosition.y -
                    1.0
                )
                *
                12.0
            );


        float upperJoint =
            exp(
                -abs(
                    worldPosition.y -
                    1.18
                )
                *
                18.0
            );


        exposedColor *=
            1.0 -
            0.08 *
            max(
                lowerJoint,
                upperJoint
            );
    }


    // ========================================================
    // TONE MAPPING
    // ========================================================

    vec3 displayColor =
        vec3(1.0) -
        exp(
            -exposedColor
        );


    displayColor =
        pow(
            displayColor,
            vec3(
                1.0 / 2.2
            )
        );


    FragColor =
        vec4(
            displayColor,
            1.0
        );
}

""";


// ============================================================
// 11. LOAD
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
        "OpenGL Version: " +
        GL.GetString(
            StringName.Version
        )
    );


    Console.WriteLine(
        "GPU: " +
        GL.GetString(
            StringName.Renderer
        )
    );


    Console.WriteLine(
        "GLSL: " +
        GL.GetString(
            StringName.ShadingLanguageVersion
        )
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
        0.025f,
        0.030f,
        0.035f,
        1.0f
    );


    // ========================================================
    // DEPTH
    // ========================================================

    GL.Enable(
        EnableCap.DepthTest
    );


    GL.Enable(
        EnableCap.Multisample
    );


    // ========================================================
    // SHADER
    // ========================================================

    shader =
        new Shader(
            vertexShaderSource,
            fragmentShaderSource
        );


    // ========================================================
    // BACKGROUND
    // ========================================================

    background =
        new BackgroundRenderer();


    // ========================================================
    // CONVEYOR PARAMETERS
    // ========================================================

    ConveyorParameters parameters =
        new ConveyorParameters();


    Console.WriteLine(
        $"Conveyor Length: " +
        $"{parameters.Length} m"
    );


    Console.WriteLine(
        $"Conveyor Width: " +
        $"{parameters.Width} m"
    );


    Console.WriteLine(
        $"Conveyor Speed: " +
        $"{parameters.Speed} m/s"
    );


    Console.WriteLine(
        $"Maximum Item Weight: " +
        $"{parameters.MaximumItemWeight} kg"
    );


    // ========================================================
    // CREATE CONVEYOR
    // ========================================================

    conveyor =
        new Conveyor(
            parameters
        );


    // ========================================================
    // OPEN FLOOR
    // ========================================================

    environmentObjects.Add(
        new Cube
        {
            Position =
                new Vector3(
                    0.0f,
                    -0.14f,
                    0.0f
                ),

            Scale =
                new Vector3(
                    24.0f,
                    0.20f,
                    16.0f
                ),

            Color =
                new Vector3(
                    0.20f,
                    0.21f,
                    0.22f
                ),

            IsFloor = true
        }
    );


    // ========================================================
    // FLOOR SAFETY LINE - NORTH
    // ========================================================

    environmentObjects.Add(
        new Cube
        {
            Position =
                new Vector3(
                    0.0f,
                    -0.025f,
                    -6.0f
                ),

            Scale =
                new Vector3(
                    20.0f,
                    0.025f,
                    0.06f
                ),

            Color =
                new Vector3(
                    0.75f,
                    0.62f,
                    0.12f
                ),

            IsFloor = true
        }
    );


    // ========================================================
    // FLOOR SAFETY LINE - SOUTH
    // ========================================================

    environmentObjects.Add(
        new Cube
        {
            Position =
                new Vector3(
                    0.0f,
                    -0.025f,
                    6.0f
                ),

            Scale =
                new Vector3(
                    20.0f,
                    0.025f,
                    0.06f
                ),

            Color =
                new Vector3(
                    0.75f,
                    0.62f,
                    0.12f
                ),

            IsFloor = true
        }
    );


    // // ========================================================
    // // INFORMATION SIGN 1
    // //
    // // WASTE INCOMING
    // //
    // // Located near the input branch.
    // // ========================================================

    // informationSigns.Add(
    //     new InformationSign(
    //         "WASTE INCOMING",
    //         new[]
    //         {
    //             "MATERIAL ENTRY",
    //             "FLOW: FORWARD"
    //         },
    //         new Vector3(
    //             -4.5f,
    //             2.25f,
    //             2.75f
    //         ),
    //         3.8f,
    //         1.45f,
    //         new Vector3(
    //             0.95f,
    //             0.72f,
    //             0.12f
    //         )
    //     )
    // );


    // // ========================================================
    // // INFORMATION SIGN 2
    // //
    // // AI / SORTING ZONE
    // // ========================================================

    // informationSigns.Add(
    //     new InformationSign(
    //         "AI SORTING ZONE",
    //         new[]
    //         {
    //             "VISION DETECTION",
    //             "ROBOTIC SORTING"
    //         },
    //         new Vector3(
    //             2.0f,
    //             2.40f,
    //             2.75f
    //         ),
    //         4.0f,
    //         1.45f,
    //         new Vector3(
    //             0.10f,
    //             0.72f,
    //             0.95f
    //         )
    //     )
    // );


    // // ========================================================
    // // INFORMATION SIGN 3
    // //
    // // MATERIAL OUTPUT
    // // ========================================================

    // informationSigns.Add(
    //     new InformationSign(
    //         "MATERIAL OUTPUT",
    //         new[]
    //         {
    //             "NEXT PROCESS",
    //             "FLOW: FORWARD"
    //         },
    //         new Vector3(
    //             -4.5f,
    //             2.25f,
    //             -2.75f
    //         ),
    //         3.8f,
    //         1.45f,
    //         new Vector3(
    //             0.15f,
    //             0.85f,
    //             0.38f
    //         )
    //     )
    // );


    // // ========================================================
    // // INFORMATION SIGN 4
    // //
    // // CONVEYOR STATUS
    // // ========================================================

    // informationSigns.Add(
    //     new InformationSign(
    //         "CONVEYOR STATUS",
    //         new[]
    //         {
    //             "STATUS: RUNNING",
    //             "SPEED: 0.70 M/S",
    //             "LENGTH: 10.0 M",
    //             "WIDTH: 1.40 M"
    //         },
    //         new Vector3(
    //             6.0f,
    //             2.20f,
    //             2.80f
    //         ),
    //         3.8f,
    //         2.0f,
    //         new Vector3(
    //             0.15f,
    //             0.80f,
    //             0.35f
    //         )
    //     )
    // );


    // ========================================================
    // INITIALIZATION COMPLETE
    // ========================================================

    Console.WriteLine(
        "OpenGL initialization completed."
    );

    Console.WriteLine(
        "Open industrial environment created."
    );

    Console.WriteLine(
        "Digital Twin information signs created."
    );

    Console.WriteLine(
        "Conveyor created successfully."
    );


    conveyor.PrintState();


    Console.WriteLine();

    Console.WriteLine(
        "LEFT MOUSE + DRAG = Orbit 360 degrees"
    );

    Console.WriteLine(
        "RIGHT MOUSE + DRAG = Pan"
    );

    Console.WriteLine(
        "MOUSE WHEEL = Zoom"
    );

    Console.WriteLine(
        "SPACE = Start / Stop conveyor"
    );

    Console.WriteLine(
        "R = Reverse conveyor"
    );
};


// ============================================================
// 12. FRAMEBUFFER RESIZE
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
// 13. UPDATE
// ============================================================

window.UpdateFrame += args =>
{
    if (
        conveyor == null
    )
    {
        return;
    }


    // ========================================================
    // SPACE = START / STOP
    // ========================================================

    if (
        window.KeyboardState.IsKeyPressed(
            Keys.Space
        )
    )
    {
        conveyor.ToggleRunning();
    }


    // ========================================================
    // R = REVERSE
    // ========================================================

    if (
        window.KeyboardState.IsKeyPressed(
            Keys.R
        )
    )
    {
        conveyor.ReverseDirection();
    }


    // ========================================================
    // MOUSE POSITION
    // ========================================================

    Vector2 currentMousePosition =
        window.MouseState.Position;


    // ========================================================
    // LEFT MOUSE = ORBIT
    // ========================================================

    if (
        window.MouseState.IsButtonDown(
            MouseButton.Left
        )
    )
    {
        if (
            !leftMouseDragging
        )
        {
            leftMouseDragging = true;

            lastMousePosition =
                currentMousePosition;
        }


        Vector2 delta =
            currentMousePosition -
            lastMousePosition;


        camera.Orbit(
            delta.X,
            delta.Y
        );


        lastMousePosition =
            currentMousePosition;
    }
    else
    {
        leftMouseDragging =
            false;
    }


    // ========================================================
    // RIGHT MOUSE = PAN
    // ========================================================

    if (
        window.MouseState.IsButtonDown(
            MouseButton.Right
        )
    )
    {
        if (
            !rightMouseDragging
        )
        {
            rightMouseDragging = true;

            lastMousePosition =
                currentMousePosition;
        }


        Vector2 delta =
            currentMousePosition -
            lastMousePosition;


        camera.Pan(
            delta.X,
            delta.Y
        );


        lastMousePosition =
            currentMousePosition;
    }
    else
    {
        rightMouseDragging =
            false;
    }


    // ========================================================
    // MOUSE WHEEL = ZOOM
    // ========================================================

    float scroll =
        window.MouseState.ScrollDelta.Y;


    if (
        MathF.Abs(scroll) >
        0.001f
    )
    {
        camera.Zoom(
            scroll
        );
    }


    // ========================================================
    // CONVEYOR
    // ========================================================

    conveyor.Update(
        args.Time
    );
};


// ============================================================
// 14. RENDER
// ============================================================

window.RenderFrame += args =>
{
    // ========================================================
    // CLEAR
    // ========================================================

    GL.Clear(
        ClearBufferMask.ColorBufferBit |
        ClearBufferMask.DepthBufferBit
    );


    // ========================================================
    // BACKGROUND
    // ========================================================

    background?.Draw();


    // ========================================================
    // 3D SCENE
    // ========================================================

    if (
        shader != null &&
        conveyor != null
    )
    {
        shader.Use();


        // ====================================================
        // ASPECT RATIO
        // ====================================================

        float aspectRatio =
            window.Size.X /
            (float)window.Size.Y;


        // ====================================================
        // CAMERA
        // ====================================================

        Matrix4 view =
            camera.GetViewMatrix();


        Matrix4 projection =
            camera.GetProjectionMatrix(
                aspectRatio
            );


        shader.SetMatrix4(
            "view",
            view
        );


        shader.SetMatrix4(
            "projection",
            projection
        );


        // ====================================================
        // LIGHTING
        // ====================================================

        shader.SetVector3(
            "lightDirection",
            new Vector3(
                -0.45f,
                0.85f,
                0.35f
            )
        );


        shader.SetVector3(
            "fillLightDirection",
            new Vector3(
                0.55f,
                0.45f,
                -0.65f
            )
        );


        shader.SetVector3(
            "viewPosition",
            camera.Position
        );


        shader.SetFloat(
            "ambientStrength",
            0.18f
        );


        shader.SetFloat(
            "specularStrength",
            0.25f
        );


        shader.SetFloat(
            "fillLightStrength",
            0.16f
        );


        // ====================================================
        // POINT LIGHT 1
        // ====================================================

        shader.SetVector3(
            "pointLightPositions[0]",
            new Vector3(
                -5.0f,
                5.0f,
                4.0f
            )
        );


        shader.SetVector3(
            "pointLightColors[0]",
            new Vector3(
                1.0f,
                0.88f,
                0.70f
            )
        );


        shader.SetFloat(
            "pointLightIntensities[0]",
            0.85f
        );


        // ====================================================
        // POINT LIGHT 2
        // ====================================================

        shader.SetVector3(
            "pointLightPositions[1]",
            new Vector3(
                5.0f,
                4.5f,
                -2.0f
            )
        );


        shader.SetVector3(
            "pointLightColors[1]",
            new Vector3(
                0.72f,
                0.84f,
                1.0f
            )
        );


        shader.SetFloat(
            "pointLightIntensities[1]",
            0.65f
        );


        // ====================================================
        // POINT LIGHT 3
        // ====================================================

        shader.SetVector3(
            "pointLightPositions[2]",
            new Vector3(
                0.0f,
                6.0f,
                5.0f
            )
        );


        shader.SetVector3(
            "pointLightColors[2]",
            new Vector3(
                1.0f,
                0.95f,
                0.82f
            )
        );


        shader.SetFloat(
            "pointLightIntensities[2]",
            0.55f
        );


        // ====================================================
        // ATTENUATION
        // ====================================================

        shader.SetFloat(
            "pointLightLinear",
            0.055f
        );


        shader.SetFloat(
            "pointLightQuadratic",
            0.018f
        );


        // ====================================================
        // ENVIRONMENT
        // ====================================================

        foreach (
            Cube environmentObject
            in environmentObjects
        )
        {
            environmentObject.Draw(
                shader
            );
        }


        // ====================================================
        // CONVEYOR
        // ====================================================

        conveyor.Draw(
            shader
        );


        // // ====================================================
        // // INFORMATION SIGNS
        // // ========================================================

        // foreach (
        //     InformationSign sign
        //     in informationSigns
        // )
        // {
        //     sign.Draw(
        //         shader
        //     );
        // }
    }


    // ========================================================
    // OPENGL ERROR CHECK
    // ========================================================

    ErrorCode error =
        GL.GetError();


    if (
        error !=
        ErrorCode.NoError
    )
    {
        Console.WriteLine(
            "OpenGL Error: " +
            error
        );
    }


    // ========================================================
    // DISPLAY
    // ========================================================

    window.SwapBuffers();
};


// ============================================================
// 15. START
// ============================================================

Console.WriteLine(
    "Starting render loop..."
);


window.Run();


Console.WriteLine(
    "Application closed."
);