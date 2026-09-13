using OpenTK.Mathematics;

using AISEG.DigitalTwin.Core;
using AISEG.DigitalTwin.Conveyor;

namespace AISEG.DigitalTwin.Environment;

public enum WasteType
{
    PlasticBottle,
    PlasticContainer,
    MetalCan,
    MetalScrap,
    GlassBottle,
    Paper,
    Cardboard,
    ClothBag,
    Wire,
    Stone,
    Tire,
    MixedWaste
}

public sealed class WasteObject
{
    private readonly UConveyorPath _path;

    private readonly List<Cube> _cubeParts = new();

    private readonly List<
        (Cube Part, Vector3 LocalPosition, float LocalYaw)
    > _cubeTransforms = new();

    private readonly List<Cylinder> _cylinderParts = new();

    private readonly List<
        (Cylinder Part, Vector3 LocalPosition, float LocalYaw)
    > _cylinderTransforms = new();

    private float _distance;
    private WasteType wasteType;
    private float startDistance;
    private float yaw;
    private readonly float _lateralOffset;

    private readonly float _baseYaw;

    private readonly float _scale;

    private const float BeltTopHeight = 1.20f;

    public enum EvaluationState
    {
        Pending,
        Keep,
        Remove
    }

    public WasteType Type { get; }
    
    public bool HasHiddenHazard { get; }
    

    public EvaluationState EvalState { get; set; } = EvaluationState.Pending;

    public float Distance =>
        _distance;

    public WasteObject(
        UConveyorPath path,
        WasteType type,
        float startDistance,
        float lateralOffset,
        float scale,
        float baseYaw,
        bool hasHiddenHazard = false)
    {
        _path =
            path;

        Type =
            type;

        HasHiddenHazard = 
            hasHiddenHazard;

        _distance =
            startDistance;

        _lateralOffset =
            lateralOffset;

        _scale =
            scale;

        _baseYaw =
            baseYaw;

        CreateGeometry();

        UpdateTransform();
    }

    public WasteObject(UConveyorPath path, WasteType wasteType, float startDistance, float yaw, bool hasHiddenHazard = false)
    {
        _path = path;
        Type = wasteType;
        HasHiddenHazard = hasHiddenHazard;
        this.startDistance = startDistance;
        this.yaw = yaw;
    }

    private void CreateGeometry()
    {
        switch (Type)
        {
            case WasteType.PlasticBottle:
                CreatePlasticBottle();
                break;

            case WasteType.PlasticContainer:
                CreatePlasticContainer();
                break;

            case WasteType.MetalCan:
                CreateMetalCan();
                break;

            case WasteType.MetalScrap:
                CreateMetalScrap();
                break;

            case WasteType.GlassBottle:
                CreateGlassBottle();
                break;

            case WasteType.Paper:
                CreatePaper();
                break;

            case WasteType.Cardboard:
                CreateCardboard();
                break;

            case WasteType.ClothBag:
                CreateClothBag();
                break;

            case WasteType.Wire:
                CreateWire();
                break;

            case WasteType.Stone:
                CreateStone();
                break;

            case WasteType.Tire:
                CreateTire();
                break;

            case WasteType.MixedWaste:
                CreateMixedWaste();
                break;
        }
    }

    private void CreatePlasticBottle()
    {
        Vector3 bodyColor =
            new Vector3(
                0.16f,
                0.38f,
                0.30f
            );

        Vector3 capColor =
            new Vector3(
                0.08f,
                0.16f,
                0.13f
            );

        AddCube(
            new Vector3(
                0.0f,
                0.12f,
                0.0f
            ),
            new Vector3(
                0.22f,
                0.28f,
                0.22f
            ),
            bodyColor,
            0.0f
        );

        AddCube(
            new Vector3(
                0.0f,
                0.30f,
                0.0f
            ),
            new Vector3(
                0.16f,
                0.10f,
                0.16f
            ),
            bodyColor,
            0.0f
        );

        AddCube(
            new Vector3(
                0.0f,
                0.39f,
                0.0f
            ),
            new Vector3(
                0.10f,
                0.11f,
                0.10f
            ),
            bodyColor,
            0.0f
        );

        AddCube(
            new Vector3(
                0.0f,
                0.46f,
                0.0f
            ),
            new Vector3(
                0.12f,
                0.05f,
                0.12f
            ),
            capColor,
            0.0f
        );
    }

    private void CreatePlasticContainer()
    {
        AddCube(
            new Vector3(
                0.0f,
                0.13f,
                0.0f
            ),
            new Vector3(
                0.32f,
                0.24f,
                0.27f
            ),
            new Vector3(
                0.48f,
                0.36f,
                0.15f
            ),
            -7.0f
        );

        AddCube(
            new Vector3(
                -0.02f,
                0.28f,
                0.01f
            ),
            new Vector3(
                0.23f,
                0.08f,
                0.20f
            ),
            new Vector3(
                0.42f,
                0.31f,
                0.13f
            ),
            4.0f
        );
    }

    private void CreateMetalCan()
    {
        Cylinder can =
            new Cylinder
            {
                Scale =
                    new Vector3(
                        0.13f,
                        0.22f,
                        0.13f
                    ),

                Color =
                    new Vector3(
                        0.43f,
                        0.45f,
                        0.46f
                    )
            };

        _cylinderParts.Add(can);

        _cylinderTransforms.Add(
            (
                can,
                new Vector3(
                    0.0f,
                    0.22f,
                    0.0f
                ),
                0.0f
            )
        );

        AddCube(
            new Vector3(
                0.0f,
                0.45f,
                0.0f
            ),
            new Vector3(
                0.18f,
                0.025f,
                0.18f
            ),
            new Vector3(
                0.65f,
                0.66f,
                0.67f
            ),
            0.0f
        );
    }

    private void CreateMetalScrap()
    {
        Vector3 metal =
            new Vector3(
                0.29f,
                0.31f,
                0.32f
            );

        AddCube(
            new Vector3(
                0.0f,
                0.08f,
                0.0f
            ),
            new Vector3(
                0.34f,
                0.10f,
                0.12f
            ),
            metal,
            17.0f
        );

        AddCube(
            new Vector3(
                0.08f,
                0.17f,
                -0.02f
            ),
            new Vector3(
                0.16f,
                0.08f,
                0.10f
            ),
            new Vector3(
                0.37f,
                0.40f,
                0.41f
            ),
            -23.0f
        );

        AddCube(
            new Vector3(
                -0.11f,
                0.13f,
                0.05f
            ),
            new Vector3(
                0.18f,
                0.06f,
                0.08f
            ),
            new Vector3(
                0.50f,
                0.51f,
                0.52f
            ),
            31.0f
        );
    }

    private void CreateGlassBottle()
    {
        Vector3 glass =
            new Vector3(
                0.25f,
                0.43f,
                0.38f
            );

        AddCube(
            new Vector3(
                0.0f,
                0.14f,
                0.0f
            ),
            new Vector3(
                0.22f,
                0.28f,
                0.22f
            ),
            glass,
            0.0f
        );

        AddCube(
            new Vector3(
                0.0f,
                0.31f,
                0.0f
            ),
            new Vector3(
                0.15f,
                0.08f,
                0.15f
            ),
            glass,
            0.0f
        );

        AddCube(
            new Vector3(
                0.0f,
                0.39f,
                0.0f
            ),
            new Vector3(
                0.09f,
                0.10f,
                0.09f
            ),
            glass,
            0.0f
        );
    }

    private void CreatePaper()
    {
        AddCube(
            new Vector3(
                0.0f,
                0.035f,
                0.0f
            ),
            new Vector3(
                0.34f,
                0.07f,
                0.25f
            ),
            new Vector3(
                0.70f,
                0.65f,
                0.49f
            ),
            14.0f
        );

        AddCube(
            new Vector3(
                0.04f,
                0.08f,
                0.02f
            ),
            new Vector3(
                0.20f,
                0.035f,
                0.18f
            ),
            new Vector3(
                0.56f,
                0.52f,
                0.38f
            ),
            -8.0f
        );
    }

    private void CreateCardboard()
    {
        AddCube(
            new Vector3(
                0.0f,
                0.07f,
                0.0f
            ),
            new Vector3(
                0.38f,
                0.14f,
                0.28f
            ),
            new Vector3(
                0.48f,
                0.31f,
                0.16f
            ),
            -9.0f
        );

        AddCube(
            new Vector3(
                0.06f,
                0.15f,
                -0.01f
            ),
            new Vector3(
                0.24f,
                0.06f,
                0.20f
            ),
            new Vector3(
                0.58f,
                0.40f,
                0.22f
            ),
            12.0f
        );
    }

    private void CreateClothBag()
    {
        AddCube(
            new Vector3(
                0.0f,
                0.12f,
                0.0f
            ),
            new Vector3(
                0.38f,
                0.22f,
                0.30f
            ),
            new Vector3(
                0.29f,
                0.22f,
                0.15f
            ),
            -8.0f
        );

        AddCube(
            new Vector3(
                -0.02f,
                0.27f,
                0.0f
            ),
            new Vector3(
                0.29f,
                0.09f,
                0.22f
            ),
            new Vector3(
                0.37f,
                0.28f,
                0.17f
            ),
            13.0f
        );
    }

    private void CreateWire()
    {
        Vector3 wireColor =
            new Vector3(
                0.18f,
                0.20f,
                0.21f
            );

        AddCube(
            new Vector3(
                0.0f,
                0.035f,
                0.0f
            ),
            new Vector3(
                0.48f,
                0.045f,
                0.045f
            ),
            wireColor,
            18.0f
        );

        AddCube(
            new Vector3(
                0.05f,
                0.06f,
                0.08f
            ),
            new Vector3(
                0.32f,
                0.035f,
                0.035f
            ),
            wireColor,
            -34.0f
        );

        AddCube(
            new Vector3(
                -0.10f,
                0.08f,
                -0.07f
            ),
            new Vector3(
                0.23f,
                0.03f,
                0.03f
            ),
            wireColor,
            51.0f
        );
    }

    private void CreateStone()
    {
        AddCube(
            new Vector3(
                0.0f,
                0.08f,
                0.0f
            ),
            new Vector3(
                0.25f,
                0.16f,
                0.21f
            ),
            new Vector3(
                0.35f,
                0.34f,
                0.31f
            ),
            19.0f
        );

        AddCube(
            new Vector3(
                0.07f,
                0.16f,
                -0.03f
            ),
            new Vector3(
                0.14f,
                0.09f,
                0.13f
            ),
            new Vector3(
                0.42f,
                0.39f,
                0.35f
            ),
            -15.0f
        );
    }

    private void CreateTire()
    {
        Cylinder outer =
            new Cylinder
            {
                Scale =
                    new Vector3(
                        0.25f,
                        0.08f,
                        0.25f
                    ),

                Color =
                    new Vector3(
                        0.045f,
                        0.048f,
                        0.052f
                    )
            };

        _cylinderParts.Add(outer);

        _cylinderTransforms.Add(
            (
                outer,
                new Vector3(
                    0.0f,
                    0.09f,
                    0.0f
                ),
                0.0f
            )
        );

        Cylinder hub =
            new Cylinder
            {
                Scale =
                    new Vector3(
                        0.09f,
                        0.095f,
                        0.09f
                    ),

                Color =
                    new Vector3(
                        0.22f,
                        0.23f,
                        0.24f
                    )
            };

        _cylinderParts.Add(hub);

        _cylinderTransforms.Add(
            (
                hub,
                new Vector3(
                    0.0f,
                    0.095f,
                    0.0f
                ),
                0.0f
            )
        );
    }

    private void CreateMixedWaste()
    {
        AddCube(
            new Vector3(
                -0.08f,
                0.09f,
                0.0f
            ),
            new Vector3(
                0.25f,
                0.14f,
                0.20f
            ),
            new Vector3(
                0.44f,
                0.30f,
                0.16f
            ),
            -14.0f
        );

        AddCube(
            new Vector3(
                0.10f,
                0.15f,
                0.03f
            ),
            new Vector3(
                0.18f,
                0.20f,
                0.15f
            ),
            new Vector3(
                0.16f,
                0.36f,
                0.27f
            ),
            22.0f
        );

        AddCube(
            new Vector3(
                0.0f,
                0.23f,
                -0.09f
            ),
            new Vector3(
                0.22f,
                0.05f,
                0.10f
            ),
            new Vector3(
                0.24f,
                0.25f,
                0.26f
            ),
            -31.0f
        );
    }

    private void AddCube(
        Vector3 localPosition,
        Vector3 scale,
        Vector3 color,
        float localYaw)
    {
        Cube part =
            new Cube
            {
                Scale =
                    scale,

                Color =
                    color
            };

        _cubeParts.Add(part);

        _cubeTransforms.Add(
            (
                part,
                localPosition,
                localYaw
            )
        );
    }

    public void Update(
        double deltaTime,
        float conveyorVelocity)
    {
        if (
            deltaTime <= 0.0 ||
            _path.TotalLength <= 0.0f
        )
        {
            return;
        }

        _distance +=
            conveyorVelocity *
            (float)deltaTime;

        _distance %=
            _path.TotalLength;

        if (
            _distance < 0.0f
        )
        {
            _distance +=
                _path.TotalLength;
        }

        UpdateTransform();
    }

    private void UpdateTransform()
    {
        UConveyorPoint point =
            _path.GetPointAtDistance(
                _distance
            );

        float pathRotation =
            MathHelper.RadiansToDegrees(
                MathF.Atan2(
                    point.Tangent.Y,
                    point.Tangent.X
                )
            );

        Vector2 lateralPosition =
            point.Normal *
            _lateralOffset;

        foreach (
            var transform
            in _cubeTransforms)
        {
            Cube part =
                transform.Part;

            Vector3 local =
                transform.LocalPosition *
                _scale;

            Vector2 worldOffset =
                point.Tangent *
                local.X +
                point.Normal *
                (
                    local.Z +
                    _lateralOffset
                );

            part.Position =
                new Vector3(
                    point.Position.X +
                    worldOffset.X,
                    BeltTopHeight +
                    local.Y,
                    point.Position.Y +
                    worldOffset.Y
                );

            part.Rotation =
                new Vector3(
                    0.0f,
                    -pathRotation +
                    _baseYaw +
                    transform.LocalYaw,
                    0.0f
                );

            part.Scale =
                transform.Part.Scale *
                _scale;
        }

        foreach (
            var transform
            in _cylinderTransforms)
        {
            Cylinder part =
                transform.Part;

            Vector3 local =
                transform.LocalPosition *
                _scale;

            Vector2 worldOffset =
                point.Tangent *
                local.X +
                point.Normal *
                (
                    local.Z +
                    _lateralOffset
                );

            part.Position =
                new Vector3(
                    point.Position.X +
                    worldOffset.X,
                    BeltTopHeight +
                    local.Y,
                    point.Position.Y +
                    worldOffset.Y
                );

            part.Rotation =
                new Vector3(
                    0.0f,
                    -pathRotation +
                    _baseYaw +
                    transform.LocalYaw,
                    0.0f
                );

            part.Scale =
                transform.Part.Scale *
                _scale;
        }
    }

    public void Draw(
        Shader shader)
    {
        foreach (
            Cube part
            in _cubeParts)
        {
            part.Draw(shader);
        }

        foreach (
            Cylinder part
            in _cylinderParts)
        {
            part.Draw(shader);
        }
        
        // Draw bounding box if evaluated
        if (EvalState != EvaluationState.Pending && _cubeParts.Count > 0)
        {
            // Simple bounding box based on the first part's position and an overarching scale
            Cube bbox = new Cube();
            bbox.Position = _cubeParts[0].Position;
            bbox.Scale = new Vector3(0.5f, 0.5f, 0.5f); // Roughly encompasses most waste items
            
            if (EvalState == EvaluationState.Remove)
                bbox.Color = new Vector3(1.0f, 0.0f, 0.0f); // Red
            else
                bbox.Color = new Vector3(0.0f, 1.0f, 0.0f); // Green
                
            // Use wireframe
            OpenTK.Graphics.OpenGL4.GL.PolygonMode(OpenTK.Graphics.OpenGL4.TriangleFace.FrontAndBack, OpenTK.Graphics.OpenGL4.PolygonMode.Line);
            
            // Draw with a slightly customized color passing
            shader.SetVector3("objectColor", bbox.Color);
            bbox.Draw(shader);
            
            // Restore fill mode
            OpenTK.Graphics.OpenGL4.GL.PolygonMode(OpenTK.Graphics.OpenGL4.TriangleFace.FrontAndBack, OpenTK.Graphics.OpenGL4.PolygonMode.Fill);
        }
    }
}