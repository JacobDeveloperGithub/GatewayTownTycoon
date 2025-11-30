using UnityEngine;

public enum Direction {North, East, South, West}

public class Coordinate {
    public readonly int x;
    public readonly int y;

    public Coordinate(int x, int y) {
        this.x = x;
        this.y = y;
    }

    public override string ToString() => $"[{x},{y}]";
    public override bool Equals(object o) => o is Coordinate c && c.x == x && c.y == y;
    public override int GetHashCode() => x ^ (y << 16 | y >> 16);

    public static Coordinate operator +(Coordinate a, Coordinate b) => new (a.x + b.x, a.y + b.y);
    public static Coordinate operator -(Coordinate a, Coordinate b) => new (a.x - b.x, a.y - b.y);
}

public class GridService {
    private readonly float _size;
    
    public GridService(float size) {
        _size = size;        
    }

    public float GetSize() => _size;
    public Vector2 PositionFromCoordinate(Coordinate c) => new (c.x * _size, c.y * _size);
    public Coordinate GetCoordinate(Vector2 position) => new(FloatToCoordinateInt(position.x), FloatToCoordinateInt(position.y));
    public int FloatToCoordinateInt(float f) => Mathf.RoundToInt(f / _size);
    
    public static Vector2 Quantize(Vector3 v) {
        const float scale = 100f;
        return new Vector2(
            Mathf.Round(v.x * scale) / scale,
            Mathf.Round(v.y * scale) / scale
        );
    }
}
