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
    private readonly List<Cube> _beltSlats = new();

    private const float BeltHeight = 1.2f;
    private const float BeltThickness = 0.18f;
    private const float PathCurveRadius = 1.4f;
    private const float FrameThickness = 0.10f;
    private const float FrameHeight = 0.22f;
    private const int BeltSlatCount = 72;

    private float _beltDistance;

    public Conveyor(ConveyorParameters parameters)
    {
        _parameters = parameters;
        _path = new UConveyorPath(
            _parameters.Length,
            _parameters.Width,
            PathCurveRadius);
        _belt = new ConveyorBeltMesh(_path, BeltHeight, BeltThickness);

        CreatePathFrames();
        CreateSupportStructure();
        CreateMechanicalDetails();
        CreateDriveAssembly();
        CreateBeltSlats();
    }

    private void CreateBeltSlats()
    {
        float spacing = _path.TotalLength / BeltSlatCount;

        for (int i = 0; i < BeltSlatCount; i++)
        {
            _beltSlats.Add(new Cube
            {
                Scale = new Vector3(spacing * 1.02f, 0.018f, _path.BeltWidth - 0.05f),
                Color = new Vector3(0.045f, 0.23f, 0.34f)
            });
        }

        UpdateBeltSlats();
    }

    private void UpdateBeltSlats()
    {
        float spacing = _path.TotalLength / BeltSlatCount;

        for (int i = 0; i < _beltSlats.Count; i++)
        {
            UConveyorPoint point = _path.GetPointAtDistance(_beltDistance + i * spacing);
            float rotation = MathHelper.RadiansToDegrees(
                MathF.Atan2(point.Tangent.Y, point.Tangent.X));

            _beltSlats[i].Position = new Vector3(
                point.Position.X,
                BeltHeight + 0.014f,
                point.Position.Y);
            _beltSlats[i].Rotation = new Vector3(0.0f, -rotation, 0.0f);
        }
    }

    public void Update(double deltaTime)
    {
        _beltDistance += _parameters.Velocity * (float)deltaTime;

        if (_path.TotalLength > 0.0f)
        {
            _beltDistance %= _path.TotalLength;

            if (_beltDistance < 0.0f)
            {
                _beltDistance += _path.TotalLength;
            }
        }

        UpdateBeltSlats();
    }

    public void ToggleRunning()
    {
        _parameters.IsRunning = !_parameters.IsRunning;
        PrintState();
    }

    public void ReverseDirection()
    {
        _parameters.Direction = _parameters.Direction == 0
            ? 1
            : -_parameters.Direction;
        PrintState();
    }

    public void PrintState()
    {
        string state = _parameters.IsRunning && _parameters.Direction != 0
            ? "RUNNING"
            : "STOPPED";
        string direction = _parameters.Direction < 0 ? "REVERSE" : "FORWARD";

        Console.WriteLine(
            $"Conveyor: {state} | Speed: {MathF.Abs(_parameters.Velocity):0.00} m/s | Direction: {direction}");
    }

    private void CreatePathFrames()
    {
        float halfWidth = _path.BeltWidth / 2.0f;
        float railY = BeltHeight - FrameHeight / 2.0f + 0.03f;
        Vector3 frameColor = new Vector3(0.58f, 0.60f, 0.62f);

        for (int i = 0; i < _path.Points.Count - 1; i++)
        {
            UConveyorPoint first = _path.Points[i];
            UConveyorPoint second = _path.Points[i + 1];
            Vector2 segment = second.Position - first.Position;
            float segmentLength = segment.Length;

            if (segmentLength < 0.001f)
            {
                continue;
            }

            Vector2 direction = segment / segmentLength;
            Vector2 midpoint = (first.Position + second.Position) / 2.0f;
            Vector2 normal = new Vector2(-direction.Y, direction.X);
            float rotation = MathHelper.RadiansToDegrees(
                MathF.Atan2(direction.Y, direction.X));

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 position = midpoint + normal * side * halfWidth;

                _frames.Add(new Cube
                {
                    Position = new Vector3(position.X, railY, position.Y),
                    Scale = new Vector3(segmentLength + 0.05f, FrameHeight, FrameThickness),
                    Rotation = new Vector3(0.0f, -rotation, 0.0f),
                    Color = frameColor
                });
            }
        }

        float beamY = BeltHeight - BeltThickness - 0.08f;
        float beamOffset = halfWidth + 0.08f;

        AddLowerBeam(beamY, PathCurveRadius + beamOffset);
        AddLowerBeam(beamY, PathCurveRadius - beamOffset);
        AddLowerBeam(beamY, -PathCurveRadius + beamOffset);
        AddLowerBeam(beamY, -PathCurveRadius - beamOffset);
    }

    private void AddLowerBeam(float y, float z)
    {
        _frames.Add(new Cube
        {
            Position = new Vector3(0.0f, y, z),
            Scale = new Vector3(_path.StraightLength, 0.15f, 0.15f),
            Color = new Vector3(0.48f, 0.50f, 0.52f)
        });
    }

    private void CreateSupportStructure()
    {
        float halfWidth = _parameters.Width / 2.0f;
        float halfStraight = _path.HalfStraightLength;
        float[] stationPositions =
        {
            -halfStraight + 0.45f,
            -halfStraight * 0.50f,
            0.0f,
            halfStraight * 0.50f,
            halfStraight - 0.45f
        };

        foreach (float x in stationPositions)
        {
            CreateBranchSupport(x, PathCurveRadius, halfWidth);
            CreateBranchSupport(x, -PathCurveRadius, halfWidth);
        }
    }

    private void CreateBranchSupport(float x, float branchZ, float halfWidth)
    {
        CreateLeg(x, branchZ - halfWidth - 0.02f);
        CreateLeg(x, branchZ + halfWidth + 0.02f);

        _legs.Add(new Cube
        {
            Position = new Vector3(x, 0.30f, branchZ),
            Scale = new Vector3(0.18f, 0.18f, _parameters.Width + 0.10f),
            Color = new Vector3(0.42f, 0.44f, 0.46f)
        });
    }

    private void CreateLeg(float x, float z)
    {
        _legs.Add(new Cube
        {
            Position = new Vector3(x, 0.71f, z),
            Scale = new Vector3(0.15f, 1.50f, 0.15f),
            Color = new Vector3(0.42f, 0.44f, 0.46f)
        });
    }

    private void CreateMechanicalDetails()
    {
        float halfStraight = _path.HalfStraightLength;
        float rollerY = 1.00f;
        Vector3 rollerColor = new Vector3(0.20f, 0.22f, 0.24f);

        for (int i = 0; i < 5; i++)
        {
            float x = -halfStraight + (i + 0.5f) / 5.0f * _path.StraightLength;
            AddRoller(new Vector2(x, PathCurveRadius), Vector2.UnitY, rollerY, rollerColor);
            AddRoller(new Vector2(x, -PathCurveRadius), -Vector2.UnitY, rollerY, rollerColor);
        }

        for (int i = 4; i < _path.CurveSegmentCount; i += 6)
        {
            UConveyorPoint point = _path.Points[_path.StraightSegmentCount + i];
            AddRoller(point.Position, point.Normal, rollerY, rollerColor);
        }

        AddPulley(
            new Vector3(_path.HalfStraightLength + PathCurveRadius, 1.06f, 0.0f),
            0.22f,
            new Vector3(0.24f, 0.25f, 0.26f));
        AddPulley(
            new Vector3(-_path.HalfStraightLength, 1.06f, PathCurveRadius),
            0.25f,
            new Vector3(0.18f, 0.19f, 0.20f));
    }

    private void AddRoller(Vector2 position, Vector2 axis, float y, Vector3 color)
    {
        Cylinder roller = new Cylinder
        {
            Position = new Vector3(position.X, y, position.Y),
            Scale = new Vector3(0.12f, 1.25f, 0.12f),
            Rotation = new Vector3(
                90.0f,
                0.0f,
                -MathHelper.RadiansToDegrees(MathF.Atan2(axis.X, axis.Y))),
            Color = color
        };
        _rollers.Add(roller);
    }

    private void AddPulley(Vector3 position, float radius, Vector3 color)
    {
        Cylinder pulley = new Cylinder
        {
            Position = position,
            Scale = new Vector3(radius * 2.0f, 1.25f, radius * 2.0f),
            Rotation = new Vector3(90.0f, 0.0f, 0.0f),
            Color = color
        };
        _rollers.Add(pulley);
    }

    private void CreateDriveAssembly()
    {
        float driveX = -_path.HalfStraightLength;
        float assemblyZ = PathCurveRadius + 0.78f;

        _motorParts.Add(new Cube
        {
            Position = new Vector3(driveX + 0.45f, 0.16f, assemblyZ),
            Scale = new Vector3(0.95f, 0.12f, 0.76f),
            Color = new Vector3(0.50f, 0.52f, 0.54f)
        });
        _motorParts.Add(new Cube
        {
            Position = new Vector3(driveX + 0.45f, 0.56f, assemblyZ),
            Scale = new Vector3(0.58f, 0.42f, 0.54f),
            Color = new Vector3(0.16f, 0.18f, 0.20f)
        });
        _motorParts.Add(new Cube
        {
            Position = new Vector3(driveX + 0.10f, 0.75f, assemblyZ),
            Scale = new Vector3(0.34f, 0.48f, 0.62f),
            Color = new Vector3(0.28f, 0.30f, 0.32f)
        });

        Cylinder motor = new Cylinder
        {
            Position = new Vector3(driveX + 0.45f, 0.56f, assemblyZ),
            Scale = new Vector3(0.34f, 0.58f, 0.34f),
            Rotation = new Vector3(0.0f, 0.0f, 90.0f),
            Color = new Vector3(0.12f, 0.14f, 0.16f)
        };
        _motorCylinders.Add(motor);

        Cylinder coupling = new Cylinder
        {
            Position = new Vector3(driveX, 1.06f, PathCurveRadius + 0.39f),
            Scale = new Vector3(0.07f, 0.78f, 0.07f),
            Rotation = new Vector3(90.0f, 0.0f, 0.0f),
            Color = new Vector3(0.10f, 0.11f, 0.12f)
        };
        _motorCylinders.Add(coupling);
    }

    public void Draw(Shader shader)
    {
        _belt.Draw(
            shader,
            new Vector3(0.035f, 0.20f, 0.28f),
            _path.TotalLength <= 0.0f ? 0.0f : _beltDistance / _path.TotalLength);

        foreach (Cube slat in _beltSlats)
        {
            slat.Draw(shader);
        }
        foreach (Cylinder roller in _rollers)
        {
            roller.Draw(shader);
        }
        foreach (Cylinder motorPart in _motorCylinders)
        {
            motorPart.Draw(shader);
        }
        foreach (Cube frame in _frames)
        {
            frame.Draw(shader);
        }
        foreach (Cube leg in _legs)
        {
            leg.Draw(shader);
        }
        foreach (Cube motorPart in _motorParts)
        {
            motorPart.Draw(shader);
        }
    }
}
