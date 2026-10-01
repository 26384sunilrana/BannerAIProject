namespace BannerService.Domain.ValueObjects;

public class Size : IEquatable<Size>
{
    public int Width { get; }
    public int Height { get; }

    public Size(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and height must be positive");

        if (width > 5000 || height > 5000)
            throw new ArgumentException("Width and height cannot exceed 5000px");

        Width = width;
        Height = height;
    }

    public bool Equals(Size? other)
    {
        return other is not null && Width == other.Width && Height == other.Height;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Size);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    public static bool operator ==(Size? left, Size? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(Size? left, Size? right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"{Width}x{Height}";
    }
}
