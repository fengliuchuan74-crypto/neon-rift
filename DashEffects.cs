using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace NeonRift
{
    internal sealed class DashEcho
    {
        public float X, Y, Age;
        public int Facing;
    }

    internal sealed class DashSpark
    {
        public float X, Y, VX, VY, Life, Max;
        public int Kind, Size;
    }

    public sealed partial class GameWorld
    {
        internal readonly List<DashEcho> DashEchoes = new List<DashEcho>(12);
        internal readonly List<DashSpark> DashSparks = new List<DashSpark>(56);
        internal float DashLaunchTime, DashLaunchX, DashLaunchY;
        public float DashImpactTime, DashImpactX, DashImpactY;
        internal int DashLaunchFacing;
        private float dashEchoClock;
        private int dashEchoSerial;

        public void ClearDashEffects()
        {
            DashEchoes.Clear(); DashSparks.Clear(); DashLaunchTime = 0;
            DashImpactTime = 0;
            dashEchoClock = 0; dashEchoSerial = 0;
        }

        public void TickDashEffects(float dt)
        {
            if (DeathTimer > 0 || (BossCinematicActive && !BossFinalStrikeActive)) { ClearDashEffects(); return; }
            DashLaunchTime = Math.Max(0, DashLaunchTime - dt);
            DashImpactTime = Math.Max(0, DashImpactTime - dt);
            for (int i = DashEchoes.Count - 1; i >= 0; i--)
            {
                DashEchoes[i].Age += dt;
                if (DashEchoes[i].Age >= .24f) DashEchoes.RemoveAt(i);
            }
            for (int i = DashSparks.Count - 1; i >= 0; i--)
            {
                DashSpark s = DashSparks[i]; s.Life -= dt;
                if (s.Life <= 0) { DashSparks.RemoveAt(i); continue; }
                s.X += s.VX * dt; s.Y += s.VY * dt; s.VY += 100 * dt;
                s.VX *= Math.Max(0, 1 - dt * 2.4f);
            }
        }

        public void StartDashEffects()
        {
            ClearDashEffects();
            DashLaunchTime = .27f; DashLaunchX = Hero.X + 12; DashLaunchY = Hero.Y + 16;
            DashLaunchFacing = Hero.Facing;
            DashEchoes.Add(new DashEcho { X = Hero.X, Y = Hero.Y, Facing = Hero.Facing });
            // A directional spray, distinct from the round burst used by skill 2.
            for (int i = 0; i < 18; i++)
            {
                float life = .19f + (i % 5) * .035f;
                DashSparks.Add(new DashSpark { X = Hero.X + 12 - Hero.Facing * 8,
                    Y = Hero.Y + 5 + (i * 7 % 26), VX = -Hero.Facing * (35 + i % 6 * 27),
                    VY = ((i * 13 % 19) - 9) * 9, Life = life, Max = life, Kind = i % 3,
                    Size = i % 5 == 0 ? 2 : 1 });
            }
        }

        public void RecordDashEcho(float dt)
        {
            if (Hero.DashTime <= 0 || DeathTimer > 0 || (BossCinematicActive && !BossFinalStrikeActive)) return;
            dashEchoClock += dt;
            if (dashEchoClock < .020f) return;
            dashEchoClock %= .020f;
            // Positions come from the resolved movement. Hitting a wall cannot
            // produce a misleading trail through it or advance the silhouette.
            if (DashEchoes.Count > 0)
            {
                DashEcho last = DashEchoes[DashEchoes.Count - 1];
                if (Math.Abs(Hero.X - last.X) + Math.Abs(Hero.Y - last.Y) < 2) return;
            }
            if (DashEchoes.Count >= 12) DashEchoes.RemoveAt(0);
            DashEchoes.Add(new DashEcho { X = Hero.X, Y = Hero.Y, Facing = Hero.Facing });
            dashEchoSerial++;
            for (int i = 0; i < 3; i++)
            {
                int seed = dashEchoSerial * 7 + i * 11;
                float life = .13f + seed % 5 * .025f;
                DashSparks.Add(new DashSpark { X = Hero.X + 12 - Hero.Facing * 6,
                    Y = Hero.Y + 3 + seed % 28, VX = -Hero.Facing * (42 + seed % 73),
                    VY = (seed % 17 - 8) * 6, Life = life, Max = life,
                    Kind = seed % 3, Size = seed % 8 == 0 ? 2 : 1 });
            }
        }
    }

    public static class DashArt
    {
        private static readonly Color Cyan = Color.FromArgb(64, 237, 255);
        private static readonly Color Magenta = Color.FromArgb(255, 71, 181);
        private static readonly Color Ember = Color.FromArgb(255, 175, 78);
        private static readonly Dictionary<int, SolidBrush> Ink = new Dictionary<int, SolidBrush>();
        private static Bitmap[,,] echoSprites;

        private static void Pixel(Graphics g, Color color, float opacity, float x, float y, float w, float h)
        {
            int alpha = Math.Min(255, Math.Max(0, (int)(opacity * 15) * 17));
            if (alpha <= 0 || w <= 0 || h <= 0) return;
            Color tint = Color.FromArgb(alpha, color);
            SolidBrush brush;
            if (!Ink.TryGetValue(tint.ToArgb(), out brush))
            { brush = new SolidBrush(tint); Ink.Add(tint.ToArgb(), brush); }
            g.FillRectangle(brush, (int)x, (int)y, Math.Max(1, (int)w), Math.Max(1, (int)h));
        }

        private static void BuildEchoSprites()
        {
            if (echoSprites != null) return;
            echoSprites = new Bitmap[2, 2, 8];
            for (int face = 0; face < 2; face++)
            using (Bitmap source = new Bitmap(28, 34, PixelFormat.Format32bppArgb))
            {
                using (Graphics sg = Graphics.FromImage(source))
                { sg.Clear(Color.Transparent); Art.Hero(sg, 2, 0, face == 0 ? -1 : 1, 0, true, true, false); }
                for (int kind = 0; kind < 2; kind++)
                for (int alpha = 0; alpha < 8; alpha++)
                {
                    Color c = kind == 0 ? Cyan : Magenta;
                    Bitmap tinted = new Bitmap(28, 34, PixelFormat.Format32bppArgb);
                    using (Graphics tg = Graphics.FromImage(tinted))
                    using (ImageAttributes attr = new ImageAttributes())
                    {
                        ColorMatrix matrix = new ColorMatrix(new float[][] {
                            new float[] {0,0,0,0,0}, new float[] {0,0,0,0,0}, new float[] {0,0,0,0,0},
                            new float[] {0,0,0,(alpha+1)*.045f,0},
                            new float[] {c.R/255f,c.G/255f,c.B/255f,0,1}});
                        attr.SetColorMatrix(matrix);
                        tg.DrawImage(source, new Rectangle(0,0,28,34), 0,0,28,34, GraphicsUnit.Pixel, attr);
                    }
                    echoSprites[face, kind, alpha] = tinted;
                }
            }
        }

        public static void DrawBehind(Graphics g, GameWorld world)
        {
            if (world.DeathTimer > 0 || (world.BossCinematicActive && !world.BossFinalStrikeActive)) return;
            if (world.DashEchoes.Count > 0)
            {
                BuildEchoSprites();
                for (int i = 0; i < world.DashEchoes.Count; i++)
                {
                    DashEcho e = world.DashEchoes[i]; float fade = (1 - e.Age / .24f) * (world.Hero.DashTime > 0 ? 1 : .42f);
                    int alpha = Math.Max(0, Math.Min(7, (int)(fade * 7)));
                    g.DrawImageUnscaled(echoSprites[e.Facing < 0 ? 0 : 1, i % 2, alpha], (int)e.X - 2, (int)e.Y);
                    if (i + 1 < world.DashEchoes.Count)
                    {
                        DashEcho next = world.DashEchoes[i + 1];
                        float left = Math.Min(e.X, next.X) + 12, width = Math.Abs(next.X - e.X);
                        // Fine separated filaments leave platforms and hazards readable.
                        Pixel(g, Cyan, fade * .48f, left, e.Y + 13, width, 1);
                        Pixel(g, Magenta, fade * .35f, left, e.Y + 25, width, 1);
                    }
                }
            }
            if (world.DashLaunchTime > 0)
            {
                float p = 1 - world.DashLaunchTime / .27f, fade = 1 - p;
                float cx = world.DashLaunchX, cy = world.DashLaunchY;
                float radius = 10 + p * 17;
                // An expanding, broken pixel ring stays at the actual launch point.
                for (int i = 0; i < 12; i++)
                {
                    if (i % 3 == 2) continue;
                    double angle = i * Math.PI / 6;
                    float px = cx + (float)Math.Cos(angle) * radius * .38f;
                    float py = cy + (float)Math.Sin(angle) * radius;
                    Pixel(g, i % 2 == 0 ? Cyan : Magenta, fade * .8f, px - 1, py - 1, 2, 3);
                }
            }
            foreach (DashSpark s in world.DashSparks)
            {
                float fade = Math.Min(1, s.Life / s.Max * 1.6f);
                Color c = s.Kind == 0 ? Ember : s.Kind == 1 ? Cyan : Magenta;
                Pixel(g, c, fade * .30f, s.X - 1, s.Y - 1, s.Size + 2, s.Size + 2);
                Pixel(g, c, fade, s.X, s.Y, s.Size, s.Size);
                if (s.Size == 2) Pixel(g, Color.White, fade * .85f, s.X, s.Y, 1, 1);
            }
        }

        public static void DrawFront(Graphics g, GameWorld world)
        {
            if (world.Hero.DashTime <= 0 || world.DeathTimer > 0 || (world.BossCinematicActive && !world.BossFinalStrikeActive)) return;
            Player a = world.Hero; int facing = a.Facing;
            float x = a.X + 12 + facing * 14, y = a.Y + 16;
            int flicker = (int)(world.Time * 75) % 3;
            // A narrow leading crescent frames, rather than covers, the hero.
            for (int row = -3; row <= 3; row++)
            {
                float px = x + facing * (7 - Math.Abs(row) * 2);
                float py = y + row * 4;
                Pixel(g, Cyan, .18f, px - 3, py - 2, 7, 5);
                Pixel(g, row % 2 == 0 ? Cyan : Magenta, .9f, px - 1, py, 3, 2);
                if (Math.Abs(row) < 2) Pixel(g, Color.White, .90f, px, py, 1, 1);
            }
            // Two short pointed speed flares underline the forward impulse.
            float tip = x + facing * (12 + flicker * 2);
            Pixel(g, Cyan, .90f, Math.Min(x + facing * 7, tip), y - 1, Math.Abs(tip - x) - 7, 1);
            Pixel(g, Ember, .85f, a.X + 10 - facing * 10, a.Y + 30, 3 + flicker, 1);
            Pixel(g, Cyan, .70f, a.X + 12 - facing * 12, a.Y + 3, 6, 1);
        }

        public static void DisposeResources()
        {
            foreach (SolidBrush b in Ink.Values) b.Dispose(); Ink.Clear();
            if (echoSprites == null) return;
            foreach (Bitmap bitmap in echoSprites) if (bitmap != null) bitmap.Dispose();
            echoSprites = null;
        }
    }
}
