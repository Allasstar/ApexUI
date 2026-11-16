namespace ApexUI.Extensions;

public static class SKColorExtensions
{
    extension(SKColor color)
    {
        // Static factory: SKColor.FromHex("#RRGGBB") or SKColor.FromHex("#RRGGBBAA")
        public static SKColor FromHex(string hex)
        {
            hex = hex.TrimStart('#');
            return hex.Length switch
            {
                6 => new SKColor(
                    Convert.ToByte(hex[0..2], 16),
                    Convert.ToByte(hex[2..4], 16),
                    Convert.ToByte(hex[4..6], 16)),
                8 => new SKColor(
                    Convert.ToByte(hex[0..2], 16),
                    Convert.ToByte(hex[2..4], 16),
                    Convert.ToByte(hex[4..6], 16),
                    Convert.ToByte(hex[6..8], 16)),
                _ => throw new ArgumentException($"Invalid hex color: #{hex}")
            };
        }

        // Instance property: color.IsTransparent
        public bool IsTransparent => color.Alpha == 0;

        // Instance method: color.WithAlpha(0.5f)
        public SKColor WithAlpha(float alpha)
            => color.WithAlpha((byte)(Math.Clamp(alpha, 0f, 1f) * 255));

        // Lighten/darken by a ratio (0..1)
        public SKColor Lighten(float ratio)
        {
            float r = ratio;
            return new SKColor(
                (byte)Math.Min(255, color.Red   + (255 - color.Red)   * r),
                (byte)Math.Min(255, color.Green + (255 - color.Green) * r),
                (byte)Math.Min(255, color.Blue  + (255 - color.Blue)  * r),
                color.Alpha);
        }

        public SKColor Darken(float ratio)
            => new SKColor(
                (byte)(color.Red   * (1f - ratio)),
                (byte)(color.Green * (1f - ratio)),
                (byte)(color.Blue  * (1f - ratio)),
                color.Alpha);
    }
}