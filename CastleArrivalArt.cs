using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace NeonRift
{
    public static partial class BossArt
    {
        private static RiftBoss ArrivalOwner;
        private static Bitmap ArrivalSource, ArrivalFrame;
        private static int[] ArrivalPixels, ArrivalOutput;

        public static void DisposeQueenArrivalResources()
        {
            if (ArrivalSource != null) ArrivalSource.Dispose();
            if (ArrivalFrame != null) ArrivalFrame.Dispose();
            ArrivalSource = ArrivalFrame = null;
            ArrivalPixels = ArrivalOutput = null;
            ArrivalOwner = null;
        }

        // This is a pure rendering sequence: no random state, gameplay updates, or frame counters.
        public static void DrawQueenArrival(Graphics g, GameWorld world, float time)
        {
            if (world == null || world.Boss == null || !world.QueenArrivalActive) return;
            RiftBoss boss = world.Boss;
            float t = Math.Max(0, Math.Min(GameWorld.QueenArrivalDuration, world.QueenArrivalTime));
            if (t >= 3.42f) { DrawBoss(g, boss, time); return; }
            float cx = boss.CenterX, cy = boss.Y + 68;
            GraphicsState saved = g.Save();
            g.SmoothingMode = SmoothingMode.None;
            float gathering = Smooth(.12f, 1.0f, t) * (1 - Smooth(2.7f, 3.4f, t));
            float seal = Smooth(.8f, 1.5f, t) * (1 - Smooth(2.8f, 3.4f, t));

            // A restrained moving shadow hints at something gathering, without showing a body.
            P(g, Color.FromArgb((int)(gathering * 85), 6, 4, 14), cx - 52, 295, 104, 5);
            if (seal > 0)
            {
                Ring(g, Color.FromArgb((int)(seal * 120), 160, 36, 74), cx, 295, 63, 10, -t * .4f, 56);
                Ring(g, Color.FromArgb((int)(seal * 80), 228, 105, 136), cx, 295, 53, 7, t * .65f, 42);
                for (int n = 0; n < 8; n++)
                {
                    double angle = n * Math.PI / 4 + t * .25;
                    P(g, Color.FromArgb((int)(seal * 150), 204, 71, 108),
                        cx + (float)Math.Cos(angle) * 63, 295 + (float)Math.Sin(angle) * 10, 2, 2);
                }
            }

            DrawArrivalSwarm(g, world, t, false);
            if (t > 1.88f) DrawMaterializingQueen(g, boss, t, time);
            DrawArrivalSwarm(g, world, t, true);

            // Loose crimson motes complete the same inward motion as the bats, then fall quiet.
            for (int n = 0; n < 74; n++)
            {
                float birth = 1.24f + Noise(n * 17 + 2) * 1.3f;
                float u = (t - birth) / (.58f + Noise(n * 11 + 7) * .27f);
                if (u <= 0 || u >= 1) continue;
                double angle = n * 2.399963 + u * 1.9;
                float radius = (1 - u) * (45 + Noise(n * 23) * 75);
                float x = cx + (float)Math.Cos(angle) * radius;
                float y = cy + (float)Math.Sin(angle) * radius * .72f + (Noise(n * 31) - .5f) * 50;
                int a = (int)(Math.Sin(u * Math.PI) * 190);
                P(g, Color.FromArgb(a, n % 5 == 0 ? 251 : 196, n % 5 == 0 ? 163 : 51, n % 5 == 0 ? 183 : 94), x, y, n % 7 == 0 ? 3 : 2, 2);
            }

            float flourish = Smooth(2.76f, 2.9f, t) * (1 - Smooth(2.93f, 3.4f, t));
            if (flourish > 0)
            {
                float radius = 40 + (t - 2.76f) * 65;
                Ring(g, Color.FromArgb((int)(flourish * 100), 219, 79, 116), cx, cy, radius, radius * .92f, t, 75);
                float gy = boss.Y + 14 + (int)(Math.Sin(time * 2.6) * 3);
                P(g, Color.FromArgb((int)(flourish * 230), 255, 214, 218), cx - 6, gy, 13, 1);
                P(g, Color.FromArgb((int)(flourish * 230), 255, 214, 218), cx, gy - 6, 1, 13);
                Diamond(g, Color.FromArgb((int)(flourish * 120), 242, 73, 115), cx, gy, 4);
            }
            g.Restore(saved);
        }

        private static void DrawArrivalSwarm(Graphics g, GameWorld world, float t, bool front)
        {
            RiftBoss boss = world.Boss;
            float cx = boss.CenterX, cy = boss.Y + 66;
            for (int n = 0; n < 48; n++)
            {
                float delay = Noise(n * 37 + 11) * .46f;
                float age = t - delay;
                if (age <= 0) continue;
                float travel = Smooth(0, .88f + Noise(n * 7) * .25f, age);
                float merge = Smooth(1.76f + delay * .8f, 2.65f + delay * .5f, t);
                if (merge >= 1) continue;
                double angle = n * 2.399963 + age * (n % 2 == 0 ? 3.3 : -3.0);
                float depth = (float)Math.Sin(angle);
                if ((depth > 0) != front) continue;
                float radius = (61 + Noise(n * 19 + 4) * 48) * (1 - merge);
                float destinationX = cx + (float)Math.Cos(angle) * radius;
                float destinationY = cy + depth * radius * .69f + (Noise(n * 29) - .5f) * 70 * (1 - merge);
                float startX, startY;
                if (n % 4 == 0) { startX = world.BossArenaLeft - 32; startY = 61 + Noise(n * 71) * 134; }
                else if (n % 4 == 1) { startX = world.BossArenaRight + 32; startY = 49 + Noise(n * 71) * 150; }
                else { startX = world.BossArenaLeft + 116 + (n % 3) * 204 + (Noise(n * 43) - .5f) * 58; startY = 39 + Noise(n * 53) * 30; }
                float x = startX + (destinationX - startX) * travel;
                float y = startY + (destinationY - startY) * travel + (float)Math.Sin(age * 7 + n) * 12 * (1 - travel);
                float visibility = Smooth(0, .11f, age) * (1 - Smooth(.72f, 1, merge));
                if (visibility < .02f) continue;
                float size = (n % 5 == 0 ? 1.16f : n % 3 == 0 ? .86f : .68f) * (1 - merge * .42f);
                // Sparse broken trails reveal the flight direction without hiding the sprites.
                if (travel < .9f || merge > .22f)
                {
                    float dx = destinationX - startX, dy = destinationY - startY;
                    float length = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (length > 0)
                    {
                        dx /= length; dy /= length;
                        for (int k = 1; k <= 3; k++) P(g, Color.FromArgb((int)(visibility * (55 - k * 11)), 206, 60, 112),
                            x - dx * (k * 6 + 6), y - dy * (k * 6 + 6), 2, 1);
                    }
                }
                DrawArrivalBat(g, x, y, size, age * 20 + n * 2.7f, visibility, front);
            }
        }

        private static void DrawArrivalBat(Graphics g, float x, float y, float size, float flap, float opacity, bool front)
        {
            int a = Math.Min(255, Math.Max(0, (int)(opacity * 16) * 16));
            if (a == 0) return;
            GraphicsState saved = g.Save();
            g.TranslateTransform((int)x, (int)y); g.ScaleTransform(size, size);
            bool raised = Math.Sin(flap) >= -.15;
            Color ink = Color.FromArgb(a, 10, 6, 19), rim = Color.FromArgb(a, front ? 138 : 104, 55, 96);
            Color membrane = Color.FromArgb(a, front ? 91 : 67, 22, 51);
            // Scalloped, fingered wings and two pointed ears keep even the small silhouettes legible.
            if (raised)
            {
                Poly(g, rim, -2, 0, -7, -7, -16, -11, -13, -3, -16, 3, -10, 1, -10, 7, -5, 4, 0, 7, 5, 4, 10, 7, 10, 1, 16, 3, 13, -3, 16, -11, 7, -7, 2, 0);
                Poly(g, ink, -2, 1, -7, -5, -14, -9, -11, -3, -14, 1, -8, 0, -8, 5, -4, 2, 0, 6, 4, 2, 8, 5, 8, 0, 14, 1, 11, -3, 14, -9, 7, -5, 2, 1);
                Poly(g, membrane, -3, 0, -7, -4, -12, -7, -9, -1, -7, -1, -7, 3, -3, 1, 0, 5, 3, 1, 7, 3, 7, -1, 9, -1, 12, -7, 7, -4, 3, 0);
            }
            else
            {
                Poly(g, rim, -2, -1, -7, -2, -15, 0, -10, 3, -12, 9, -6, 7, -4, 11, 0, 6, 4, 11, 6, 7, 12, 9, 10, 3, 15, 0, 7, -2, 2, -1);
                Poly(g, ink, -2, 0, -7, -1, -12, 0, -8, 3, -10, 7, -5, 5, -3, 8, 0, 5, 3, 8, 5, 5, 10, 7, 8, 3, 12, 0, 7, -1, 2, 0);
                Poly(g, membrane, -2, 1, -6, 0, -10, 0, -6, 3, -7, 5, -3, 4, 0, 5, 3, 4, 7, 5, 6, 3, 10, 0, 6, 0, 2, 1);
            }
            P(g, rim, -3, -4, 2, 4); P(g, rim, 2, -4, 2, 4);
            P(g, ink, -2, -2, 5, 8); P(g, Color.FromArgb(a, 84, 45, 74), -1, -1, 3, 5);
            P(g, Color.FromArgb(a, 252, 94, 114), -1, -1, 1, 1); P(g, Color.FromArgb(a, 252, 94, 114), 2, -1, 1, 1);
            P(g, ink, 0, 6, 1, 2);
            g.Restore(saved);
        }

        private static void DrawMaterializingQueen(Graphics g, RiftBoss boss, float t, float time)
        {
            if (!Object.ReferenceEquals(ArrivalOwner, boss))
            {
                DisposeQueenArrivalResources(); ArrivalOwner = boss;
            }
            if (ArrivalSource == null)
            {
                ArrivalSource = new Bitmap(QueenCanvasWidth, QueenCanvasHeight, PixelFormat.Format32bppArgb);
                ArrivalFrame = new Bitmap(QueenCanvasWidth, QueenCanvasHeight, PixelFormat.Format32bppArgb);
                ArrivalPixels = new int[QueenCanvasWidth * QueenCanvasHeight];
                ArrivalOutput = new int[ArrivalPixels.Length];
            }
            // Re-render the living pose into reusable buffers, so the last frame matches DrawBoss.
            using (Graphics sprite = Graphics.FromImage(ArrivalSource))
            {
                sprite.Clear(Color.Transparent); sprite.SmoothingMode = SmoothingMode.None;
                sprite.TranslateTransform(9, 8); DrawQueen(sprite, time, boss.Skill, false, boss.Direction);
            }
            BitmapData pixels = ArrivalSource.LockBits(new Rectangle(0, 0, QueenCanvasWidth, QueenCanvasHeight), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try { Marshal.Copy(pixels.Scan0, ArrivalPixels, 0, ArrivalPixels.Length); }
            finally { ArrivalSource.UnlockBits(pixels); }
            for (int y = 0; y < QueenCanvasHeight; y++) for (int x = 0; x < QueenCanvasWidth; x++)
            {
                int index = y * QueenCanvasWidth + x, pixel = ArrivalPixels[index];
                if (((uint)pixel >> 24) == 0) { ArrivalOutput[index] = 0; continue; }
                float birth = 1.94f + Math.Abs(x - 65) / 65f * .27f + (1 - y / (float)QueenCanvasHeight) * .48f + Noise((x / 3) * 97 + y / 3) * .29f;
                float opacity = Smooth(birth, birth + .25f, t);
                if (t >= birth && t < birth + .18f)
                {
                    float glow = 1 - (t - birth) / .18f;
                    int r = Math.Min(255, ((pixel >> 16) & 255) + (int)(glow * 99));
                    int green = Math.Min(255, ((pixel >> 8) & 255) + (int)(glow * 21));
                    int b = Math.Min(255, (pixel & 255) + (int)(glow * 55));
                    pixel = (pixel & unchecked((int)0xff000000)) | (r << 16) | (green << 8) | b;
                }
                ArrivalOutput[index] = FadePixel(pixel, opacity);
            }
            WritePixels(ArrivalFrame, ArrivalOutput);
            float shadow = Smooth(2.05f, 2.9f, t);
            P(g, Color.FromArgb((int)(shadow * 125), 3, 4, 13), boss.X + 12, boss.Y + 157, 89, 5);
            P(g, Color.FromArgb((int)(shadow * 65), 98, 24, 56), boss.X + 26, boss.Y + 156, 64, 3);
            int bob = (int)(Math.Sin(time * 2.6) * 3);
            GraphicsState pixelsState=g.Save();g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            g.DrawImageUnscaled(ArrivalFrame, (int)boss.X - 9, (int)boss.Y + bob - 11);g.Restore(pixelsState);
        }
    }
}
