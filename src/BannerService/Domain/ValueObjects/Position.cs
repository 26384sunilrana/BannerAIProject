namespace BannerService.Domain.ValueObjects;

public class Position : IEquatable<Position>
{
    public int X { get; }
    public int Y { get; }

    public Position(int x, int y)
    {
        if (x < 0 || y < 0)
            throw new ArgumentException("Position coordinates must be non-negative");

        X = x;
        Y = y;
    }

    public bool Equals(Position? other)
    {
        return other != null && X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Position);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static bool operator ==(Position? left, Position? right)
    {
        return left?.Equals(right) ?? right == null;
    }

    public static bool operator !=(Position? left, Position? right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}
