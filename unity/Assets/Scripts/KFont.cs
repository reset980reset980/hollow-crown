using System.Collections.Generic;
using UnityEngine;

// Korean pixel font (Galmuri7, SIL OFL 1.1) baked by pipeline/kfont.mjs into Resources/kfont.txt.
// Hangul sits on an 8px grid (7x7 glyphs), so one font pixel is one game pixel, drawn with the same
// row-gradient colours and 1px outline as the original 3x5 font. PixelText switches to this font for
// any string that contains a non-ASCII character; pure ASCII strings keep the original font.
public static class KFont
{
    public class Glyph { public int cp, adv, w, h, xo, yo; public byte[] rows; public int bpr; }
    class Style { public int[] rows; public int ol; public Style(int[] r, int o) { rows = r; ol = o; } }

    // palette indices, mirroring the STY table in pipeline/bake.js
    static readonly Dictionary<string, Style> Styles = new Dictionary<string, Style>
    {
        { "dmg", new Style(new[] { 61, 61, 62, 62, 63 }, 64) },
        { "crit", new Style(new[] { 65, 66, 66, 67, 67 }, 68) },
        { "critBig", new Style(new[] { 65, 66, 66, 67, 67 }, 68) },
        { "gold", new Style(new[] { 59, 39, 39, 40, 40 }, 0) },
        { "white", new Style(new[] { 35, 35, 38, 38, 90 }, 0) },
        { "vio", new Style(new[] { 48, 49, 49, 50, 50 }, 0) },
        { "goldBig", new Style(new[] { 59, 39, 39, 40, 40 }, 0) },
        { "whiteBig", new Style(new[] { 35, 35, 38, 38, 90 }, 0) },
        { "blue", new Style(new[] { 42, 43, 43, 44, 44 }, 0) },
        { "green", new Style(new[] { 59, 18, 18, 17, 17 }, 0) },
        { "dark", new Style(new[] { 3, 3, 4, 4, 4 }, -1) },
    };

    static Dictionary<int, Glyph> glyphs;
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static bool Needs(string s)
    {
        if (s == null) return false;
        for (int i = 0; i < s.Length; i++) if (s[i] > 0x7e) return true;
        return false;
    }

    static void Load()
    {
        if (glyphs != null) return;
        glyphs = new Dictionary<int, Glyph>();
        var ta = Resources.Load<TextAsset>("kfont");
        if (ta == null) { Debug.LogWarning("kfont.txt missing"); return; }
        foreach (var line in ta.text.Split('\n'))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Trim().Split(' ');
            if (p.Length < 7) continue;
            var g = new Glyph { cp = int.Parse(p[0]), adv = int.Parse(p[1]), w = int.Parse(p[2]), h = int.Parse(p[3]), xo = int.Parse(p[4]), yo = int.Parse(p[5]) };
            g.bpr = (g.w + 7) / 8;
            string hex = p[6] == "-" ? "" : p[6];
            g.rows = new byte[hex.Length / 2];
            for (int i = 0; i < g.rows.Length; i++) g.rows[i] = (byte)System.Convert.ToInt32(hex.Substring(i * 2, 2), 16);
            glyphs[g.cp] = g;
        }
    }

    public static Glyph Get(char c)
    {
        Load();
        Glyph g;
        if (glyphs.TryGetValue(c, out g)) return g;
        if (c == ' ' && glyphs.TryGetValue(' ', out g)) return g;
        glyphs.TryGetValue('?', out g);
        return g;
    }

    public static int Width(string s, int scale = 1)
    {
        int w = 0;
        foreach (char c in s) { var g = Get(c); if (g != null) w += g.adv; }
        return Mathf.Max(0, w - 1) * scale;
    }

    static bool Bit(Glyph g, int x, int r)
    {
        if (x < 0 || r < 0 || x >= g.w || r >= g.h) return false;
        return (g.rows[r * g.bpr + (x >> 3)] & (0x80 >> (x & 7))) != 0;
    }

    // Sprite with a 1px outline border; its bottom-left is 1px left of and below the glyph's box.
    public static Sprite Sprite(string style, Glyph g, int s)
    {
        string key = style + ":" + g.cp;
        Sprite sp;
        if (cache.TryGetValue(key, out sp)) return sp;
        Style st;
        if (!Styles.TryGetValue(style, out st)) st = Styles["white"];
        int W = g.w * s + 2, H = g.h * s + 2;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[W * H];
        var fill = new bool[W * H];
        for (int r = 0; r < g.h; r++)
            for (int x = 0; x < g.w; x++)
            {
                if (!Bit(g, x, r)) continue;
                var col = Px.Pal[st.rows[Mathf.Min(4, r * 5 / g.h)]];
                for (int dy = 0; dy < s; dy++) for (int dx = 0; dx < s; dx++)
                {
                    int tx = 1 + x * s + dx, ty = H - 2 - (r * s + dy);
                    px[ty * W + tx] = col; fill[ty * W + tx] = true;
                }
            }
        if (st.ol >= 0)
        {
            var oc = Px.Pal[st.ol];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (fill[y * W + x]) continue;
                    bool near = false;
                    for (int oy = -1; oy <= 1 && !near; oy++) for (int ox = -1; ox <= 1; ox++)
                    {
                        int nx = x + ox, ny = y + oy;
                        if (nx >= 0 && ny >= 0 && nx < W && ny < H && fill[ny * W + nx]) { near = true; break; }
                    }
                    if (near) px[y * W + x] = oc;
                }
        }
        tex.SetPixels32(px); tex.Apply(false, true);
        sp = UnityEngine.Sprite.Create(tex, new Rect(0, 0, W, H), Vector2.zero, 1f, 0, SpriteMeshType.FullRect);
        cache[key] = sp;
        return sp;
    }
}
