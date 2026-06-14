using UnityEngine;

namespace ColorPool
{
    /// <summary>The 10 numbered colors, ported from the prototype PALETTE/NAMES.</summary>
    public static class Palette
    {
        public static readonly Color32[] Colors =
        {
            new Color32(232, 77, 77, 255),   // 0 Red
            new Color32(242, 120, 60, 255),  // 1 Orange
            new Color32(247, 179, 43, 255),  // 2 Yellow
            new Color32(242, 92, 155, 255),  // 3 Pink
            new Color32(201, 79, 124, 255),  // 4 Rose
            new Color32(63, 142, 252, 255),  // 5 Blue
            new Color32(55, 194, 181, 255),  // 6 Teal
            new Color32(122, 92, 255, 255),  // 7 Purple
            new Color32(73, 182, 117, 255),  // 8 Green
            new Color32(47, 109, 181, 255),  // 9 Navy
        };

        public static Color ColorOf(int i) => Colors[Mathf.Clamp(i, 0, 9)];
    }

    /// <summary>
    /// A picture maps a normalized table point to a palette color index. u runs 0..1 across
    /// the table width (X), v runs 0..1 up the table length (Z); v=0 is the player's end.
    /// cols/rows set the mosaic granularity (how many paintable cells).
    /// </summary>
    public abstract class Picture
    {
        public string name;
        public int cols;
        public int rows;
        public abstract int ColorAt(float u, float v);
    }

    /// <summary>Green cactus + pink flower on blue sky over a yellow desert (4 colors).</summary>
    public class CactusPicture : Picture
    {
        public CactusPicture() { name = "Cactus"; cols = 7; rows = 12; }

        public override int ColorAt(float u, float v)
        {
            float du = u - 0.5f;
            // pink flower atop the cactus
            float fx = u - 0.5f, fy = v - 0.78f;
            if (fx * fx + fy * fy < 0.085f * 0.085f) return 3;
            // green cactus: trunk + two arms
            bool trunk = Mathf.Abs(du) < 0.085f && v > 0.16f && v < 0.74f;
            bool lArmH = u > 0.30f && u < 0.50f && v > 0.40f && v < 0.49f;
            bool lArmV = u > 0.30f && u < 0.385f && v > 0.40f && v < 0.60f;
            bool rArmH = u > 0.50f && u < 0.70f && v > 0.50f && v < 0.59f;
            bool rArmV = u > 0.615f && u < 0.70f && v > 0.50f && v < 0.68f;
            if (trunk || lArmH || lArmV || rArmH || rArmV) return 8;
            // yellow desert band at the bottom
            if (v < 0.20f) return 2;
            // blue sky
            return 5;
        }
    }

    /// <summary>A heart: red upper / pink lower on a teal background (3 colors).</summary>
    public class HeartPicture : Picture
    {
        public HeartPicture() { name = "Heart"; cols = 7; rows = 11; }

        public override int ColorAt(float u, float v)
        {
            float hx = (u - 0.5f) / 0.40f;
            float hy = (v - 0.50f) / 0.40f;
            float a = hx * hx + hy * hy - 1f;
            bool inHeart = a * a * a - hx * hx * hy * hy * hy <= 0f;
            if (inHeart) return v > 0.52f ? 0 : 3;
            return 6;
        }
    }

    public static class PictureLibrary
    {
        public static readonly Picture[] All = { new CactusPicture(), new HeartPicture() };
        public static Picture Get(int index) => All[((index % All.Length) + All.Length) % All.Length];
    }
}
