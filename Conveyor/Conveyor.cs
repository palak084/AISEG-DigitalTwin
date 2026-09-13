using OpenTK.Mathematics;

using AISEG.DigitalTwin.Core;
using AISEG.DigitalTwin.Environment;

namespace AISEG.DigitalTwin.Conveyor;

public sealed class Conveyor
{
    private readonly ConveyorParameters _parameters;
    private readonly UConveyorPath _path;
    private readonly ConveyorBeltMesh _belt;

    private readonly List<Cube> _frames = new();
    private readonly List<Cube> _legs = new();
    private readonly List<Cube> _motorParts = new();

    private readonly List<Cylinder> _rollers = new();
    private readonly List<Cylinder> _motorCylinders = new();

    // =========================================================
    // MOVING BELT SLATS
    // =========================================================

    private readonly List<Cube> _beltSlats = new();

    // =========================================================
    // BELT ANIMATION POSITION
    // =========================================================

    private float _beltDistance = 0.0f;

    // =========================================================
    // CONVEYOR DIMENSIONS
    // =========================================================

    private const float BeltHeight = 1.20f;

    private const float BeltThickness = 0.18f;

    private const float PathCurveRadius = 1.40f;

    private const float FrameThickness = 0.10f;

    private const float FrameHeight = 0.22f;

    // =========================================================
    // BELT SLAT SETTINGS
    // =========================================================

    private const int BeltSlatCount = 60;

    private const float BeltSlatThickness = 0.035f;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public Conveyor(
        ConveyorParameters parameters)
    {
        _parameters = parameters;

        // -----------------------------------------------------
        // CREATE U-SHAPED OPEN CONVEYOR PATH
        // -----------------------------------------------------

        _path = new UConveyorPath(
            _parameters.Length,
            _parameters.Width,
            PathCurveRadius
        );

        // -----------------------------------------------------
        // CREATE MAIN BELT MESH
        // -----------------------------------------------------

        _belt = new ConveyorBeltMesh(
            _path,
            BeltHeight,
            BeltThickness
        );

        // -----------------------------------------------------
        // CREATE STATIC STRUCTURE
        // -----------------------------------------------------

        CreatePathFrames();

        CreateSupportStructure();

        CreateMechanicalDetails();

        CreateDriveAssembly();

        // -----------------------------------------------------
        // CREATE MOVING BELT SLATS
        // -----------------------------------------------------

        CreateBeltSlats();

        Console.WriteLine(
            $"Belt path length: {_path.TotalLength:0.00} m"
        );
    }

    // =========================================================
    // CREATE MOVING BELT SLATS
    // =========================================================

    private void CreateBeltSlats()
    {
        float spacing =
            _path.TotalLength /
            BeltSlatCount;

        for (
            int i = 0;
            i < BeltSlatCount;
            i++)
        {
            Cube slat = new Cube
            {
                Color =
                    new Vector3(
                        0.035f,
                        0.17f,
                        0.24f
                    ),

                // X = direction of travel thickness
                //
                // Y = vertical thickness
                //
                // Z = belt width
                //
                // Therefore the slat initially spans
                // across the belt width.
                Scale =
                    new Vector3(
                        BeltSlatThickness,
                        0.035f,
                        _path.BeltWidth - 0.08f
                    )
            };

            _beltSlats.Add(slat);
        }

        // Put all slats at their initial positions.
        UpdateBeltSlats();
    }

    // =========================================================
    // UPDATE BELT SLATS
    // =========================================================

    private void UpdateBeltSlats()
    {
        float spacing =
            _path.TotalLength /
            BeltSlatCount;

        for (
            int i = 0;
            i < _beltSlats.Count;
            i++)
        {
            // -------------------------------------------------
            // Calculate position along conveyor path.
            // -------------------------------------------------

            float distance =
                _beltDistance +
                i * spacing;

            UConveyorPoint point =
                _path.GetPointAtDistance(
                    distance
                );

            // -------------------------------------------------
            // Calculate instantaneous direction of travel.
            // -------------------------------------------------

            float rotation =
                MathF.Atan2(
                    point.Tangent.Y,
                    point.Tangent.X
                );

            // -------------------------------------------------
            // POSITION
            // -------------------------------------------------

            _beltSlats[i].Position =
                new Vector3(
                    point.Position.X,
                    BeltHeight + 0.035f,
                    point.Position.Y
                );

            // -------------------------------------------------
            // ROTATION
            // -------------------------------------------------
            //
            // IMPORTANT:
            //
            // The Cube's long axis is Z.
            //
            // The belt slat must remain perpendicular to
            // the direction of travel.
            //
            // Therefore there is NO +90 degree offset here.
            //
            // -------------------------------------------------

            _beltSlats[i].Rotation =
                new Vector3(
                    0.0f,
                    -MathHelper.RadiansToDegrees(
                        rotation
                    ),
                    0.0f
                );
        }
    }

    // =========================================================
    // UPDATE CONVEYOR
    // =========================================================

    public void Update(
        double deltaTime)
    {
        if (deltaTime <= 0.0)
        {
            return;
        }

        // -----------------------------------------------------
        // REAL CONVEYOR MOTION
        // -----------------------------------------------------
        //
        // Current project specification:
        //
        // Conveyor speed = 0.7 m/s
        //
        // Distance travelled:
        //
        // distance = speed × time
        //
        // At approximately 60 FPS:
        //
        // 0.7 × 0.0167
        // ≈ 0.0117 m/frame
        //
        // -----------------------------------------------------

        float movement =
            _parameters.Velocity *
            (float)deltaTime;

        _beltDistance += movement;

        // -----------------------------------------------------
        // LOOP AROUND THE COMPLETE BELT PATH
        // -----------------------------------------------------

        if (_path.TotalLength > 0.0f)
        {
            _beltDistance %=
                _path.TotalLength;

            if (_beltDistance < 0.0f)
            {
                _beltDistance +=
                    _path.TotalLength;
            }
        }

        // -----------------------------------------------------
        // UPDATE VISIBLE MOVING SLATS
        // -----------------------------------------------------

        UpdateBeltSlats();
    }

    // =========================================================
    // TOGGLE CONVEYOR RUNNING
    // =========================================================

    public void ToggleRunning()
    {
        _parameters.IsRunning =
            !_parameters.IsRunning;

        PrintState();
    }

    // =========================================================
    // REVERSE CONVEYOR
    // =========================================================

    public void ReverseDirection()
    {
        _parameters.Direction =
            _parameters.Direction == 0
                ? 1
                : -_parameters.Direction;

        PrintState();
    }

    // =========================================================
    // PRINT CONVEYOR STATE
    // =========================================================

    public void PrintState()
    {
        string state =
            _parameters.IsRunning &&
            _parameters.Direction != 0
                ? "RUNNING"
                : "STOPPED";

        string direction =
            _parameters.Direction < 0
                ? "REVERSE"
                : "FORWARD";

        Console.WriteLine(
            $"Conveyor: {state} | " +
            $"Speed: {MathF.Abs(_parameters.Velocity):0.00} m/s | " +
            $"Direction: {direction}"
        );
    }

    // =========================================================
    // CREATE SIDE FRAMES
    // =========================================================

    private void CreatePathFrames()
    {
        float halfWidth =
            _path.BeltWidth / 2.0f;

        float railY =
            BeltHeight -
            FrameHeight / 2.0f +
            0.03f;

        Vector3 frameColor =
            new Vector3(
                0.58f,
                0.60f,
                0.62f
            );

        // -----------------------------------------------------
        // FOLLOW THE COMPLETE U-SHAPED PATH
        // -----------------------------------------------------

        for (
            int i = 0;
            i < _path.Points.Count - 1;
            i++)
        {
            UConveyorPoint first =
                _path.Points[i];

            UConveyorPoint second =
                _path.Points[i + 1];

            Vector2 segment =
                second.Position -
                first.Position;

            float segmentLength =
                segment.Length;

            if (segmentLength < 0.001f)
            {
                continue;
            }

            Vector2 direction =
                segment /
                segmentLength;

            Vector2 midpoint =
                (first.Position +
                 second.Position) /
                2.0f;

            Vector2 normal =
                new Vector2(
                    -direction.Y,
                    direction.X
                );

            float rotation =
                MathHelper.RadiansToDegrees(
                    MathF.Atan2(
                        direction.Y,
                        direction.X
                    )
                );

            // -------------------------------------------------
            // LEFT + RIGHT SIDE RAIL
            // -------------------------------------------------

            for (
                int side = -1;
                side <= 1;
                side += 2)
            {
                Vector2 position =
                    midpoint +
                    normal *
                    side *
                    halfWidth;

                _frames.Add(
                    new Cube
                    {
                        Position =
                            new Vector3(
                                position.X,
                                railY,
                                position.Y
                            ),

                        Scale =
                            new Vector3(
                                segmentLength + 0.05f,
                                FrameHeight,
                                FrameThickness
                            ),

                        Rotation =
                            new Vector3(
                                0.0f,
                                -rotation,
                                0.0f
                            ),

                        Color =
                            frameColor
                    }
                );
            }
        }

        // -----------------------------------------------------
        // LOWER SUPPORT BEAMS
        // -----------------------------------------------------

        float beamY =
            BeltHeight -
            BeltThickness -
            0.08f;

        float beamOffset =
            halfWidth + 0.08f;

        AddLowerBeam(
            beamY,
            PathCurveRadius +
            beamOffset
        );

        AddLowerBeam(
            beamY,
            PathCurveRadius -
            beamOffset
        );

        AddLowerBeam(
            beamY,
            -PathCurveRadius +
            beamOffset
        );

        AddLowerBeam(
            beamY,
            -PathCurveRadius -
            beamOffset
        );
    }

    // =========================================================
    // LOWER BEAM
    // =========================================================

    private void AddLowerBeam(
        float y,
        float z)
    {
        _frames.Add(
            new Cube
            {
                Position =
                    new Vector3(
                        0.0f,
                        y,
                        z
                    ),

                Scale =
                    new Vector3(
                        _path.StraightLength,
                        0.15f,
                        0.15f
                    ),

                Color =
                    new Vector3(
                        0.48f,
                        0.50f,
                        0.52f
                    )
            }
        );
    }

    // =========================================================
    // SUPPORT STRUCTURE
    // =========================================================

    private void CreateSupportStructure()
    {
        float halfWidth =
            _parameters.Width / 2.0f;

        float halfStraight =
            _path.HalfStraightLength;

        float[] stationPositions =
        {
            -halfStraight + 0.45f,
            -halfStraight * 0.50f,
            0.0f,
            halfStraight * 0.50f,
            halfStraight - 0.45f
        };

        foreach (
            float x in stationPositions)
        {
            CreateBranchSupport(
                x,
                PathCurveRadius,
                halfWidth
            );

            CreateBranchSupport(
                x,
                -PathCurveRadius,
                halfWidth
            );
        }
    }

    // =========================================================
    // BRANCH SUPPORT
    // =========================================================

    private void CreateBranchSupport(
        float x,
        float branchZ,
        float halfWidth)
    {
        CreateLeg(
            x,
            branchZ -
            halfWidth -
            0.02f
        );

        CreateLeg(
            x,
            branchZ +
            halfWidth +
            0.02f
        );

        _legs.Add(
            new Cube
            {
                Position =
                    new Vector3(
                        x,
                        0.30f,
                        branchZ
                    ),

                Scale =
                    new Vector3(
                        0.18f,
                        0.18f,
                        _parameters.Width +
                        0.10f
                    ),

                Color =
                    new Vector3(
                        0.42f,
                        0.44f,
                        0.46f
                    )
            }
        );
    }

    // =========================================================
    // SUPPORT LEG
    // =========================================================

    private void CreateLeg(
        float x,
        float z)
    {
        _legs.Add(
            new Cube
            {
                Position =
                    new Vector3(
                        x,
                        0.71f,
                        z
                    ),

                Scale =
                    new Vector3(
                        0.15f,
                        1.50f,
                        0.15f
                    ),

                Color =
                    new Vector3(
                        0.42f,
                        0.44f,
                        0.46f
                    )
            }
        );
    }

    // =========================================================
    // MECHANICAL DETAILS
    // =========================================================

    private void CreateMechanicalDetails()
    {
        float halfStraight =
            _path.HalfStraightLength;

        float rollerY =
            1.00f;

        Vector3 rollerColor =
            new Vector3(
                0.20f,
                0.22f,
                0.24f
            );

        // -----------------------------------------------------
        // STRAIGHT SECTION ROLLERS
        // -----------------------------------------------------

        for (int i = 0; i < 5; i++)
        {
            float x =
                -halfStraight +
                (i + 0.5f) /
                5.0f *
                _path.StraightLength;

            AddRoller(
                new Vector2(
                    x,
                    PathCurveRadius
                ),
                Vector2.UnitY,
                rollerY,
                rollerColor
            );

            AddRoller(
                new Vector2(
                    x,
                    -PathCurveRadius
                ),
                -Vector2.UnitY,
                rollerY,
                rollerColor
            );
        }

        // -----------------------------------------------------
        // CURVED SECTION ROLLERS
        // -----------------------------------------------------

        for (
            int i = 4;
            i < _path.CurveSegmentCount;
            i += 6)
        {
            int pointIndex =
                _path.StraightSegmentCount +
                i;

            if (
                pointIndex <
                0 ||
                pointIndex >=
                _path.Points.Count)
            {
                continue;
            }

            UConveyorPoint point =
                _path.Points[
                    pointIndex
                ];

            AddRoller(
                point.Position,
                point.Normal,
                rollerY,
                rollerColor
            );
        }

        // -----------------------------------------------------
        // DRIVE PULLEY
        // -----------------------------------------------------

        AddPulley(
            new Vector3(
                _path.HalfStraightLength +
                PathCurveRadius,
                1.06f,
                0.0f
            ),
            0.22f,
            new Vector3(
                0.24f,
                0.25f,
                0.26f
            )
        );

        // -----------------------------------------------------
        // RETURN PULLEY
        // -----------------------------------------------------

        AddPulley(
            new Vector3(
                -_path.HalfStraightLength,
                1.06f,
                PathCurveRadius
            ),
            0.25f,
            new Vector3(
                0.18f,
                0.19f,
                0.20f
            )
        );
    }

    // =========================================================
    // ROLLER
    // =========================================================

    private void AddRoller(
        Vector2 position,
        Vector2 axis,
        float y,
        Vector3 color)
    {
        Cylinder roller =
            new Cylinder
            {
                Position =
                    new Vector3(
                        position.X,
                        y,
                        position.Y
                    ),

                Scale =
                    new Vector3(
                        0.12f,
                        1.25f,
                        0.12f
                    ),

                Rotation =
                    new Vector3(
                        90.0f,
                        0.0f,
                        -MathHelper.RadiansToDegrees(
                            MathF.Atan2(
                                axis.X,
                                axis.Y
                            )
                        )
                    ),

                Color =
                    color
            };

        _rollers.Add(
            roller
        );
    }

    // =========================================================
    // PULLEY
    // =========================================================

    private void AddPulley(
        Vector3 position,
        float radius,
        Vector3 color)
    {
        Cylinder pulley =
            new Cylinder
            {
                Position =
                    position,

                Scale =
                    new Vector3(
                        radius * 2.0f,
                        1.25f,
                        radius * 2.0f
                    ),

                Rotation =
                    new Vector3(
                        90.0f,
                        0.0f,
                        0.0f
                    ),

                Color =
                    color
            };

        _rollers.Add(
            pulley
        );
    }

    // =========================================================
    // DRIVE / MOTOR ASSEMBLY
    // =========================================================

    private void CreateDriveAssembly()
    {
        float driveX =
            -_path.HalfStraightLength;

        float assemblyZ =
            PathCurveRadius +
            0.78f;

        // -----------------------------------------------------
        // MOTOR BASE
        // -----------------------------------------------------

        _motorParts.Add(
            new Cube
            {
                Position =
                    new Vector3(
                        driveX + 0.45f,
                        0.16f,
                        assemblyZ
                    ),

                Scale =
                    new Vector3(
                        0.95f,
                        0.12f,
                        0.76f
                    ),

                Color =
                    new Vector3(
                        0.50f,
                        0.52f,
                        0.54f
                    )
            }
        );

        // -----------------------------------------------------
        // MOTOR BODY
        // -----------------------------------------------------

        _motorParts.Add(
            new Cube
            {
                Position =
                    new Vector3(
                        driveX + 0.45f,
                        0.56f,
                        assemblyZ
                    ),

                Scale =
                    new Vector3(
                        0.58f,
                        0.42f,
                        0.54f
                    ),

                Color =
                    new Vector3(
                        0.16f,
                        0.18f,
                        0.20f
                    )
            }
        );

        // -----------------------------------------------------
        // MOTOR FRONT
        // -----------------------------------------------------

        _motorParts.Add(
            new Cube
            {
                Position =
                    new Vector3(
                        driveX + 0.10f,
                        0.75f,
                        assemblyZ
                    ),

                Scale =
                    new Vector3(
                        0.34f,
                        0.48f,
                        0.62f
                    ),

                Color =
                    new Vector3(
                        0.28f,
                        0.30f,
                        0.32f
                    )
            }
        );

        // -----------------------------------------------------
        // MOTOR CYLINDER
        // -----------------------------------------------------

        Cylinder motor =
            new Cylinder
            {
                Position =
                    new Vector3(
                        driveX + 0.45f,
                        0.56f,
                        assemblyZ
                    ),

                Scale =
                    new Vector3(
                        0.34f,
                        0.58f,
                        0.34f
                    ),

                Rotation =
                    new Vector3(
                        0.0f,
                        0.0f,
                        90.0f
                    ),

                Color =
                    new Vector3(
                        0.12f,
                        0.14f,
                        0.16f
                    )
            };

        _motorCylinders.Add(
            motor
        );

        // -----------------------------------------------------
        // COUPLING
        // -----------------------------------------------------

        Cylinder coupling =
            new Cylinder
            {
                Position =
                    new Vector3(
                        driveX,
                        1.06f,
                        PathCurveRadius +
                        0.39f
                    ),

                Scale =
                    new Vector3(
                        0.07f,
                        0.78f,
                        0.07f
                    ),

                Rotation =
                    new Vector3(
                        90.0f,
                        0.0f,
                        0.0f
                    ),

                Color =
                    new Vector3(
                        0.10f,
                        0.11f,
                        0.12f
                    )
            };

        _motorCylinders.Add(
            coupling
        );
    }

    // =========================================================
    // DRAW CONVEYOR
    // =========================================================

    public void Draw(
        Shader shader)
    {
        // -----------------------------------------------------
        // MAIN STATIC BELT BODY
        // -----------------------------------------------------

        _belt.Draw(
            shader,
            new Vector3(
                0.025f,
                0.12f,
                0.18f
            ),
            0.0f
        );

        // -----------------------------------------------------
        // MOVING TRANSVERSE SLATS
        // -----------------------------------------------------

        foreach (
            Cube slat in _beltSlats)
        {
            slat.Draw(
                shader
            );
        }

        // -----------------------------------------------------
        // SUPPORT ROLLERS
        // -----------------------------------------------------

        foreach (
            Cylinder roller in _rollers)
        {
            roller.Draw(
                shader
            );
        }

        // -----------------------------------------------------
        // MOTOR CYLINDERS
        // -----------------------------------------------------

        foreach (
            Cylinder motorPart
            in _motorCylinders)
        {
            motorPart.Draw(
                shader
            );
        }

        // -----------------------------------------------------
        // SIDE FRAMES
        // -----------------------------------------------------

        foreach (
            Cube frame in _frames)
        {
            frame.Draw(
                shader
            );
        }

        // -----------------------------------------------------
        // SUPPORT LEGS
        // -----------------------------------------------------

        foreach (
            Cube leg in _legs)
        {
            leg.Draw(
                shader
            );
        }

        // -----------------------------------------------------
        // MOTOR HOUSING
        // -----------------------------------------------------

        foreach (
            Cube motorPart
            in _motorParts)
        {
            motorPart.Draw(
                shader
            );
        }
    }
}