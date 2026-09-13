using OpenTK.Mathematics;
using AISEG.DigitalTwin.Core;
using AISEG.DigitalTwin.Conveyor;

namespace AISEG.DigitalTwin.Environment;

public sealed class SensorGantry
{
    private readonly List<Cube> _structure = new();
    private readonly List<Cube> _sensors = new();
    private readonly List<Cylinder> _lenses = new();
    
    // Placement distance along the conveyor path (from start)
    public float Distance { get; }
    
    public SensorGantry(UConveyorPath path, float distance)
    {
        Distance = distance;
        
        UConveyorPoint point = path.GetPointAtDistance(distance);
        
        Vector3 position = new Vector3(point.Position.X, 0.0f, point.Position.Y);
        
        float rotation = MathF.Atan2(point.Tangent.Y, point.Tangent.X);
        float rotDegrees = -MathHelper.RadiansToDegrees(rotation);
        
        Vector3 rotVec = new Vector3(0.0f, rotDegrees, 0.0f);
        
        float width = path.BeltWidth + 0.4f; // Wider than belt
        float height = 2.5f; // Arch height
        
        Vector3 structureColor = new Vector3(0.75f, 0.76f, 0.78f); // Light industrial grey
        
        // --- Structural Arch ---
        // Left Leg
        _structure.Add(new Cube {
            Position = position + RotateVector(new Vector3(0, height / 2.0f, width / 2.0f), rotDegrees),
            Scale = new Vector3(0.3f, height, 0.3f),
            Rotation = rotVec,
            Color = structureColor
        });
        
        // Right Leg
        _structure.Add(new Cube {
            Position = position + RotateVector(new Vector3(0, height / 2.0f, -width / 2.0f), rotDegrees),
            Scale = new Vector3(0.3f, height, 0.3f),
            Rotation = rotVec,
            Color = structureColor
        });
        
        // Top Beam
        _structure.Add(new Cube {
            Position = position + RotateVector(new Vector3(0, height, 0), rotDegrees),
            Scale = new Vector3(0.4f, 0.3f, width + 0.3f),
            Rotation = rotVec,
            Color = structureColor
        });
        
        // --- Sensors ---
        // We will place 5 sensors across the top beam
        
        // 1. 3D Depth (Navy)
        AddSensor(position, rotDegrees, rotVec, new Vector3(0.0f, height, width * 0.35f), new Vector3(0.016f, 0.106f, 0.298f));
        
        // 2. NIR (Indigo)
        AddSensor(position, rotDegrees, rotVec, new Vector3(0.0f, height, width * 0.15f), new Vector3(0.275f, 0.345f, 0.537f));
        
        // 3. Inductive (Blue-gray)
        AddSensor(position, rotDegrees, rotVec, new Vector3(0.0f, height, -width * 0.05f), new Vector3(0.463f, 0.494f, 0.584f));
        
        // 4. Load Cell (Copper)
        AddSensor(position, rotDegrees, rotVec, new Vector3(0.0f, height, -width * 0.25f), new Vector3(0.718f, 0.514f, 0.412f));
        
        // 5. Capacitive (Green)
        AddSensor(position, rotDegrees, rotVec, new Vector3(0.0f, height, -width * 0.45f), new Vector3(0.122f, 0.353f, 0.227f));
    }
    
    private void AddSensor(Vector3 basePos, float rotDegrees, Vector3 baseRot, Vector3 localPos, Vector3 color)
    {
        // Sensor Box
        _sensors.Add(new Cube {
            Position = basePos + RotateVector(localPos, rotDegrees),
            Scale = new Vector3(0.25f, 0.35f, 0.15f),
            Rotation = baseRot,
            Color = color
        });
        
        // Sensor Lens/Emitter (pointing down)
        _lenses.Add(new Cylinder {
            Position = basePos + RotateVector(localPos - new Vector3(0.0f, 0.2f, 0.0f), rotDegrees),
            Scale = new Vector3(0.08f, 0.1f, 0.08f),
            Rotation = baseRot,
            Color = new Vector3(0.1f, 0.1f, 0.1f) // Dark glass
        });
    }
    
    private Vector3 RotateVector(Vector3 v, float degrees)
    {
        float rad = MathHelper.DegreesToRadians(degrees);
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);
        
        // Rotation around Y axis
        return new Vector3(
            v.X * cos + v.Z * sin,
            v.Y,
            -v.X * sin + v.Z * cos
        );
    }
    
    public void Draw(Shader shader)
    {
        foreach(var part in _structure)
            part.Draw(shader);
            
        foreach(var sensor in _sensors)
            sensor.Draw(shader);
            
        foreach(var lens in _lenses)
            lens.Draw(shader);
    }
}
