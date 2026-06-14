using UnityEngine;

namespace ColorPool
{
    /// <summary>
    /// The paintable table surface (approach A: texture-mask). At level build it rasterizes a
    /// Picture into a jittered-grid Voronoi mosaic of regions, bakes a display texture (white
    /// cells, dark outlines, faint numbers) and remembers each region's required color. In
    /// Regular paint mode, a colored ball rolling over a matching region fills that whole region.
    /// Drives the painted-% and the win check.
    /// </summary>
    public class PaintSurface : MonoBehaviour
    {
        public static PaintSurface Instance { get; private set; }

        const int TexW = 240;
        const int TexH = 384;

        static readonly Color32 ColUnpainted = new Color32(246, 244, 250, 255);
        static readonly Color32 ColBorder = new Color32(96, 90, 124, 255);
        static readonly Color32 ColNumber = new Color32(150, 146, 168, 255);

        Texture2D tex;
        Color32[] buf;
        int[] regionId;          // per texel
        bool[] isBorder;         // per texel
        int[] regionColor;       // per region
        int[] regionPix;         // per region (paintable texels)
        bool[] regionDone;       // per region
        int regionCount;
        int totalPaintable;
        int coveredPaintable;
        int completedCount;
        bool dirty;

        public bool IsComplete => regionCount > 0 && completedCount >= regionCount;
        public float Progress => totalPaintable > 0 ? (float)coveredPaintable / totalPaintable : 0f;

        void Awake() { Instance = this; }

        public bool[] PresentColors()
        {
            var present = new bool[10];
            if (regionColor != null)
                foreach (int c in regionColor) present[c] = true;
            return present;
        }

        public void Build(Picture pic, int seed)
        {
            var rng = new System.Random(seed);
            int cols = pic.cols, rows = pic.rows;
            regionCount = cols * rows;
            float cw = TexW / (float)cols, ch = TexH / (float)rows;

            var sx = new float[regionCount];
            var sy = new float[regionCount];
            for (int gy = 0; gy < rows; gy++)
                for (int gx = 0; gx < cols; gx++)
                {
                    int i = gy * cols + gx;
                    sx[i] = (gx + 0.2f + (float)rng.NextDouble() * 0.6f) * cw;
                    sy[i] = (gy + 0.2f + (float)rng.NextDouble() * 0.6f) * ch;
                }

            regionId = new int[TexW * TexH];
            isBorder = new bool[TexW * TexH];
            regionColor = new int[regionCount];
            regionPix = new int[regionCount];
            regionDone = new bool[regionCount];
            var votes = new int[regionCount * 10];

            for (int y = 0; y < TexH; y++)
            {
                int gy0 = Mathf.Clamp((int)(y / ch), 0, rows - 1);
                for (int x = 0; x < TexW; x++)
                {
                    int gx0 = Mathf.Clamp((int)(x / cw), 0, cols - 1);
                    int best = 0; float bd = float.MaxValue;
                    for (int gy2 = Mathf.Max(0, gy0 - 2); gy2 <= Mathf.Min(rows - 1, gy0 + 2); gy2++)
                        for (int gx2 = Mathf.Max(0, gx0 - 2); gx2 <= Mathf.Min(cols - 1, gx0 + 2); gx2++)
                        {
                            int i = gy2 * cols + gx2;
                            float dx = x - sx[i], dy = y - sy[i], d = dx * dx + dy * dy;
                            if (d < bd) { bd = d; best = i; }
                        }
                    int p = y * TexW + x;
                    regionId[p] = best;
                    float u = (x + 0.5f) / TexW, v = (y + 0.5f) / TexH;
                    votes[best * 10 + pic.ColorAt(u, v)]++;
                }
            }

            for (int i = 0; i < regionCount; i++)
            {
                int bc = 0, bv = -1;
                for (int c = 0; c < 10; c++) if (votes[i * 10 + c] > bv) { bv = votes[i * 10 + c]; bc = c; }
                regionColor[i] = bc;
            }

            for (int y = 0; y < TexH; y++)
                for (int x = 0; x < TexW; x++)
                {
                    int p = y * TexW + x, id = regionId[p];
                    isBorder[p] = x == 0 || y == 0 || x == TexW - 1 || y == TexH - 1 ||
                                  regionId[p - 1] != id || regionId[p - TexW] != id;
                }

            buf = new Color32[TexW * TexH];
            totalPaintable = 0; coveredPaintable = 0; completedCount = 0;
            var cxs = new float[regionCount];
            var cys = new float[regionCount];
            var cnt = new int[regionCount];
            for (int y = 0; y < TexH; y++)
                for (int x = 0; x < TexW; x++)
                {
                    int p = y * TexW + x, id = regionId[p];
                    if (isBorder[p]) { buf[p] = ColBorder; }
                    else { buf[p] = ColUnpainted; regionPix[id]++; totalPaintable++; cxs[id] += x; cys[id] += y; cnt[id]++; }
                }

            for (int i = 0; i < regionCount; i++)
                if (cnt[i] > 0)
                    DrawNumber(regionColor[i] + 1, Mathf.RoundToInt(cxs[i] / cnt[i]), Mathf.RoundToInt(cys[i] / cnt[i]));

            if (tex == null)
            {
                tex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var mr = GetComponent<MeshRenderer>();
                mr.material.mainTexture = tex;
                if (mr.material.HasProperty("_BaseMap")) mr.material.SetTexture("_BaseMap", tex);
            }
            tex.SetPixels32(buf);
            tex.Apply();
            dirty = false;
        }

        // Map a world position on the table to a texel; false if off-surface.
        bool WorldToTexel(Vector3 world, out int tx, out int ty)
        {
            float hw = Tuning.TableWidth * 0.5f, hd = Tuning.TableDepth * 0.5f;
            float u = (world.x + hw) / (2f * hw);
            float v = (world.z + hd) / (2f * hd);
            tx = (int)(u * TexW); ty = (int)(v * TexH);
            if (tx < 0 || ty < 0 || tx >= TexW || ty >= TexH) { tx = ty = 0; return false; }
            return true;
        }

        /// <summary>Regular mode: fill the whole matching region under the ball. Returns true if it painted.</summary>
        public bool TryPaintAt(int colorIndex, Vector3 world)
        {
            if (regionId == null) return false;
            if (!WorldToTexel(world, out int tx, out int ty)) return false;
            int id = regionId[ty * TexW + tx];
            if (regionDone[id] || regionColor[id] != colorIndex) return false;
            CompleteRegion(id);
            return true;
        }

        void CompleteRegion(int id)
        {
            regionDone[id] = true;
            completedCount++;
            Color32 col = Palette.Colors[regionColor[id]];
            for (int p = 0; p < buf.Length; p++)
                if (regionId[p] == id && !isBorder[p]) { buf[p] = col; coveredPaintable++; }
            dirty = true;
        }

        void LateUpdate()
        {
            if (dirty && tex != null) { tex.SetPixels32(buf); tex.Apply(); dirty = false; }
        }

        // --- tiny 3x5 bitmap font for the faint region numbers ---
        static readonly byte[][] Font =
        {
            new byte[]{0x7,0x5,0x5,0x5,0x7}, // 0
            new byte[]{0x2,0x6,0x2,0x2,0x7}, // 1
            new byte[]{0x7,0x1,0x7,0x4,0x7}, // 2
            new byte[]{0x7,0x1,0x7,0x1,0x7}, // 3
            new byte[]{0x5,0x5,0x7,0x1,0x1}, // 4
            new byte[]{0x7,0x4,0x7,0x1,0x7}, // 5
            new byte[]{0x7,0x4,0x7,0x5,0x7}, // 6
            new byte[]{0x7,0x1,0x2,0x2,0x2}, // 7
            new byte[]{0x7,0x5,0x7,0x5,0x7}, // 8
            new byte[]{0x7,0x5,0x7,0x1,0x7}, // 9
        };

        void DrawNumber(int value, int cx, int cy)
        {
            string s = value.ToString();
            int scale = 2;
            int glyphW = 3 * scale, glyphH = 5 * scale, gap = scale;
            int totalW = s.Length * glyphW + (s.Length - 1) * gap;
            int ox = cx - totalW / 2, oy = cy - glyphH / 2;
            for (int ci = 0; ci < s.Length; ci++)
            {
                int d = s[ci] - '0';
                if (d < 0 || d > 9) continue;
                byte[] g = Font[d];
                int gx = ox + ci * (glyphW + gap);
                for (int row = 0; row < 5; row++)
                    for (int col = 0; col < 3; col++)
                        if ((g[row] & (1 << (2 - col))) != 0)
                            FillBlock(gx + col * scale, oy + (4 - row) * scale, scale);
            }
        }

        void FillBlock(int x0, int y0, int s)
        {
            for (int y = y0; y < y0 + s; y++)
                for (int x = x0; x < x0 + s; x++)
                {
                    if (x < 0 || y < 0 || x >= TexW || y >= TexH) continue;
                    int p = y * TexW + x;
                    if (!isBorder[p]) buf[p] = ColNumber;
                }
        }
    }
}
