using OpenTK.Mathematics;
using AISEG.DigitalTwin.Core;
using AISEG.DigitalTwin.Conveyor;

namespace AISEG.DigitalTwin.Environment;

public sealed class SensorNode
{
    private readonly Cube _stand;
    private readonly Cube _arm;
    private readonly Cube _sensorBox;
    private readonly Cylinder _lens;
    
    public float Distance { get; }
    public string Name { get; }
    
    public SensorNode(UConveyorPath path, float distance, string name, Vector3 color)
    {
        Distance = distance;
        Name = name;
        
        UConveyorPoint point = path.GetPointAtDistance(distance);
        Vector3 position = new Vector3(point.Position.X, 0.0f, point.Position.Y);
        
        float rotation = MathF.Atan2(point.Tangent.Y, point.Tangent.X);
        float rotDegrees = -MathHelper.RadiansToDegrees(rotation);
        
        float width = path.BeltWidth + 0.4f;
        float height = 2.0f;
        
        Matrix4 baseFrame = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotDegrees)) * Matrix4.CreateTranslation(position);
        
        _stand = new Cube { Scale = new Vector3(0.2f, height, 0.2f), Color = new Vector3(0.7f, 0.7f, 0.75f) };
        _stand.TransformOverride = Matrix4.CreateScale(_stand.Scale) * Matrix4.CreateTranslation(0, height / 2.0f, width / 2.0f) * baseFrame;
        
        _arm = new Cube { Scale = new Vector3(0.15f, 0.15f, width / 2.0f + 0.2f), Color = new Vector3(0.7f, 0.7f, 0.75f) };
        _arm.TransformOverride = Matrix4.CreateScale(_arm.Scale) * Matrix4.CreateTranslation(0, height, width / 4.0f) * baseFrame;
        
        // Custom Sensor Head
        if (Name.Contains("3D Depth"))
        {
            _sensorBox = new Cube { Scale = new Vector3(0.4f, 0.1f, 0.1f), Color = color };
            _sensorBox.TransformOverride = Matrix4.CreateScale(_sensorBox.Scale) * Matrix4.CreateTranslation(0, height, 0) * baseFrame;
            
            _lens = new Cylinder { Scale = new Vector3(0.06f, 0.1f, 0.06f), Color = new Vector3(0.1f) };
            _lens.TransformOverride = Matrix4.CreateScale(_lens.Scale) * Matrix4.CreateTranslation(0.12f, height - 0.08f, 0) * baseFrame;
            
            // Reusing a hack for dual lens (using the same ref? No, need a new cylinder)
            // But we only have _lens field. Let's just keep _lens as is and maybe make it wider instead.
            _lens.Scale = new Vector3(0.15f, 0.1f, 0.05f);
            _lens.TransformOverride = Matrix4.CreateScale(_lens.Scale) * Matrix4.CreateTranslation(0, height - 0.08f, 0) * baseFrame;
        }
        else if (Name.Contains("NIR"))
        {
            _sensorBox = new Cube { Scale = new Vector3(0.15f, 0.15f, 0.3f), Color = color };
            _sensorBox.TransformOverride = Matrix4.CreateScale(_sensorBox.Scale) * Matrix4.CreateTranslation(0, height, 0) * baseFrame;
            
            _lens = new Cylinder { Scale = new Vector3(0.1f, 0.15f, 0.1f), Color = new Vector3(0.1f) };
            _lens.TransformOverride = Matrix4.CreateScale(_lens.Scale) * Matrix4.CreateTranslation(0, height - 0.15f, 0) * baseFrame;
        }
        else if (Name.Contains("Inductive"))
        {
            // Donut/probe shape
            _sensorBox = new Cube { Scale = new Vector3(0.2f, 0.4f, 0.2f), Color = color };
            _sensorBox.TransformOverride = Matrix4.CreateScale(_sensorBox.Scale) * Matrix4.CreateTranslation(0, height, 0) * baseFrame;
            
            _lens = new Cylinder { Scale = new Vector3(0.12f, 0.4f, 0.12f), Color = new Vector3(0.8f, 0.6f, 0.1f) }; // Copper probe
            _lens.TransformOverride = Matrix4.CreateScale(_lens.Scale) * Matrix4.CreateTranslation(0, height - 0.3f, 0) * baseFrame;
        }
        else // Capacitive
        {
            // Flat plate
            _sensorBox = new Cube { Scale = new Vector3(0.5f, 0.05f, 0.5f), Color = color };
            _sensorBox.TransformOverride = Matrix4.CreateScale(_sensorBox.Scale) * Matrix4.CreateTranslation(0, height, 0) * baseFrame;
            
            _lens = new Cylinder { Scale = new Vector3(0.4f, 0.06f, 0.4f), Color = new Vector3(0.2f) };
            _lens.TransformOverride = Matrix4.CreateScale(_lens.Scale) * Matrix4.CreateTranslation(0, height - 0.05f, 0) * baseFrame;
        }
    }
    
    public void Draw(Shader shader)
    {
        _stand.Draw(shader);
        _arm.Draw(shader);
        _sensorBox.Draw(shader);
        _lens.Draw(shader);
    }
    
    public Vector3 GetAnnotationPosition(UConveyorPath path)
    {
        var pos = path.GetPointAtDistance(Distance).Position;
        return new Vector3(pos.X, 2.5f, pos.Y);
    }
}
