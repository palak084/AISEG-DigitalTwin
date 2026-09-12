using OpenTK.Mathematics;

namespace AISEG.DigitalTwin.Conveyor;

public readonly struct UConveyorPoint
{
    public Vector2 Position { get; }

    public Vector2 Tangent { get; }

    public Vector2 Normal { get; }

    public UConveyorPoint(Vector2 position, Vector2 tangent, Vector2 normal)
    {
        Position = position;
        Tangent = tangent;
        Normal = normal;
    }
}

public sealed class UConveyorPath
{
    private readonly List<UConveyorPoint> _points = new();
    private readonly float[] _segmentLengths;

    public IReadOnlyList<UConveyorPoint> Points => _points;

    public float BeltWidth { get; }

    public float CurveRadius { get; }

    public float StraightLength { get; }

    public int StraightSegmentCount { get; }

    public int CurveSegmentCount { get; }

    public float HalfStraightLength => StraightLength / 2.0f;

    public float TotalLength { get; }

    public UConveyorPath(
        float overallLength,
        float beltWidth,
        float curveRadius,
        int straightSegments = 28,
        int curveSegments = 32)
    {
        BeltWidth = beltWidth;
        CurveRadius = curveRadius;
        StraightSegmentCount = straightSegments;
        CurveSegmentCount = curveSegments;

        float outerRadius = curveRadius + beltWidth / 2.0f;
        StraightLength = MathF.Max(
            0.5f,
            overallLength - outerRadius);

        float halfStraight = HalfStraightLength;

        // Open input branch. This endpoint is intentionally not connected.
        for (int i = 0; i <= straightSegments; i++)
        {
            float t = i / (float)straightSegments;
            _points.Add(new UConveyorPoint(
                new Vector2(-halfStraight + t * StraightLength, curveRadius),
                Vector2.UnitX,
                Vector2.UnitY));
        }

        // The only turnaround: one 180-degree return at the right side.
        for (int i = 1; i <= curveSegments; i++)
        {
            float angle = MathF.PI / 2.0f -
                i / (float)curveSegments * MathF.PI;
            AddCurvePoint(halfStraight, angle);
        }

        // Open output branch. This endpoint is intentionally not connected
        // back to the input branch.
        for (int i = 1; i <= straightSegments; i++)
        {
            float t = i / (float)straightSegments;
            _points.Add(new UConveyorPoint(
                new Vector2(halfStraight - t * StraightLength, -curveRadius),
                -Vector2.UnitX,
                -Vector2.UnitY));
        }

        _segmentLengths = new float[_points.Count - 1];
        float totalLength = 0.0f;

        for (int i = 0; i < _segmentLengths.Length; i++)
        {
            float length = (
                _points[i + 1].Position -
                _points[i].Position).Length;
            _segmentLengths[i] = length;
            totalLength += length;
        }

        TotalLength = totalLength;
    }

    private void AddCurvePoint(float centerX, float angle)
    {
        Vector2 normal = new Vector2(
            MathF.Cos(angle),
            MathF.Sin(angle));
        Vector2 tangent = new Vector2(
            MathF.Sin(angle),
            -MathF.Cos(angle));

        _points.Add(new UConveyorPoint(
            new Vector2(
                centerX + CurveRadius * normal.X,
                CurveRadius * normal.Y),
            tangent,
            normal));
    }

    public UConveyorPoint GetPointAtDistance(float distance)
    {
        if (TotalLength <= 0.0f)
        {
            return _points[0];
        }

        distance %= TotalLength;

        if (distance < 0.0f)
        {
            distance += TotalLength;
        }

        float segmentStart = 0.0f;

        for (int i = 0; i < _segmentLengths.Length; i++)
        {
            float segmentLength = _segmentLengths[i];

            if (distance <= segmentStart + segmentLength)
            {
                UConveyorPoint first = _points[i];
                UConveyorPoint second = _points[i + 1];
                float t = segmentLength <= 0.0f
                    ? 0.0f
                    : (distance - segmentStart) / segmentLength;

                return new UConveyorPoint(
                    Vector2.Lerp(first.Position, second.Position, t),
                    Vector2.Normalize(Vector2.Lerp(
                        first.Tangent,
                        second.Tangent,
                        t)),
                    Vector2.Normalize(Vector2.Lerp(
                        first.Normal,
                        second.Normal,
                        t)));
            }

            segmentStart += segmentLength;
        }

        return _points[^1];
    }
}