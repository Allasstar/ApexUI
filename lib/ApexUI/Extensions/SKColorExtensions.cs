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
    }
}