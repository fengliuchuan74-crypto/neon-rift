using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NeonRift
{
    // All artwork is original, deliberately built on a small pixel grid.
    public static class Art
    {
        private static readonly Dictionary<int, SolidBrush> Ink = new Dictionary<int, SolidBrush>();
        private static readonly Color Night = Color.FromArgb(10, 13, 31);
        private static readonly Color Shadow = Color.FromArgb(6, 8, 22);
        private static readonly Color Cyan = Color.FromArgb(78, 246, 255);
        private static readonly Color Pink = Color.FromArgb(255, 70, 174);
        private static SolidBrush B(Color c)
        {
            SolidBrush b;
            if (!Ink.TryGetValue(c.ToArgb(), out b)) { b = new SolidBrush(c); Ink.Add(c.ToArgb(), b); }
            return b;
        }
        private static Color C(int r, int g, int b) { return Color.FromArgb(r, g, b); }
        private static void P(Graphics g, Color c, float x, float y, float w, float h)
        { if (w > 0 && h > 0) g.FillRectangle(B(c), (int)x, (int)y, (int)w, (int)h); }
        // Small pixel heart used by both HUDs.  Keeping the silhouette in
        // integer rectangles makes it stay crisp at the game's native scale.
        public static void Heart(Graphics g, float x, float y, bool filled)
        {
            Color edge = Color.FromArgb(47, 35, 67);
            Color fill = filled ? Color.FromArgb(255, 82, 164) : Color.FromArgb(63, 47, 77);
            P(g, edge, x + 2, y, 4, 2); P(g, edge, x + 8, y, 4, 2);
            P(g, edge, x, y + 2, 14, 4); P(g, edge, x + 2, y + 6, 10, 4); P(g, edge, x + 4, y + 10, 6, 2);
            P(g, fill, x + 3, y + 2, 3, 3); P(g, fill, x + 8, y + 2, 3, 3);
            P(g, fill, x + 2, y + 4, 10, 3); P(g, fill, x + 4, y + 7, 6, 3); P(g, fill, x + 5, y + 10, 4, 1);
            if (filled) P(g, Color.FromArgb(255, 255, 185), x + 3, y + 3, 2, 1);
        }
        private static int H(int n) { unchecked { n = (n ^ 274763641) *  668265263; n ^= n >> 13; n *= 1274126177; return n & 0x7fffffff; } }
        private static float Wrap(float n, float width) { return (n % width + width) % width; }

        public static void Background(Graphics g, int stage, float camera, float time, int width, int height)
        {
            stage = Math.Max(0, Math.Min(2, stage));
            P(g, Night, 0, 0, width, height);
            // Broad, restrained bands make the skyline glow without obscuring play.
            for (int k = 0; k < 9; k++)
            {
                int blue = stage == 1 ? 31 + k * 2 : 40 + k * 3;
                P(g, C(12 + k, 14 + k, blue), 0, k * 26, width, 26);
            }
            int sunX = (int)(width * .76f - Wrap(camera * .015f, width * .08f));
            for (int k = 0; k < 45; k++)
            {
                int hash = H(k + 53);
                float x = Wrap(hash % 997 - camera * .015f, width);
                int y = 15 + (hash / 997) % 118;
                Color star = k % 4 == 0 ? C(83, 96, 142) : C(41, 53, 90);
                P(g, star, x, y, 1, k % 13 == 0 ? 2 : 1);
            }
            WorldSkyArt.Moon(g,sunX,68,stage);
            // Far buildings: each index produces a stable city section while the camera moves.
            DrawCity(g, stage, camera, width, height, .055f, 58, 220, true, time);
            DrawCity(g, stage, camera, width, height, .14f, 72, 282, false, time);
            if (stage == 1)
            {
                for (int k = 0; k < 8; k++)
                {
                    float x = Wrap(k * 133 - camera * .22f, width + 200) - 100;
                    P(g, C(16, 23, 40), x, 143, 10, height - 143);
                    P(g, C(44, 39, 59), x - 6, 144, 22, 4);
                    P(g, C(30, 31, 48), x + 3, 96, 4, 48);
                    P(g, C(96, 42, 58), x + 3, 96, 4, 3);
                    for (int p = 0; p < 4; p++)
                    {
                        float drift = Wrap(time * 6 + p * 15, 60);
                        P(g, C(30, 28, 46), x - 1 + drift * .22f, 93 - drift, 10 + drift * .3f, 3);
                    }
                }
                P(g, C(24, 29, 43), 0, 215, width, 7);
                P(g, C(49, 33, 51), 0, 215, width, 1);
            }
            else if (stage == 2)
            {
                float towerX = width * .52f - camera * .025f;
                P(g, C(17, 23, 45), towerX - 36, 75, 72, height);
                P(g, C(22, 31, 54), towerX - 26, 54, 52, height);
                P(g, C(28, 40, 67), towerX - 15, 38, 30, height);
                P(g, C(53, 109, 127), towerX - 1, 25, 2, 117);
                for (int y = 86; y < height; y += 17)
                {
                    P(g, C(35, 75, 99), towerX - 23, y, 46, 2);
                    P(g, C(40, 113, 129), towerX - 20, y, 7, 2);
                }
                if ((int)(time * 2) % 2 == 0) P(g, C(216, 68, 149), towerX - 2, 21, 4, 3);
            }
            // Air traffic is scenery, entirely separate from enemy silhouettes.
            for (int k = 0; k < 4; k++)
            {
                bool right=k%2==0,distant=k!=2;
                float x = Wrap(42+k*173+time*(right?1:-1)*(8+k*2)-camera*(distant?.045f:.085f),width+110)-55;
                float y=92+k*32+(float)Math.Sin(time*.8f+k)*.7f;
                CityAircraftArt.Draw(g,x,y,right,k%2,distant,time+k);
            }
            // Quiet pixel rain and low horizon haze preserve platform contrast.
            for (int k = 0; k < 70; k++)
            {
                int hash = H(k * 71 + 991);
                float y = Wrap((hash % 379) + time * (34 + k % 17), height);
                float x = Wrap(hash % 647 - camera * .3f - y * .25f, width);
                Color rain = k % 6 == 0 ? C(46, 70, 97) : C(26, 39, 65);
                P(g, rain, x, y, 1, 4);
                P(g, rain, x - 1, y + 4, 1, 3);
            }
            P(g, C(11, 17, 30), 0, height - 10, width, 10);
        }

        private static void DrawCity(Graphics g, int stage, float camera, int width, int height,
                                     float speed, int stride, int baseline, bool far, float time)
        {
            int first = (int)Math.Floor(camera * speed / stride) - 1;
            for (int i = first; i < first + width / stride + 4; i++)
            {
                int seed = H(i + 10000 + (far ? 77 : 1));
                int x = (int)(i * stride - camera * speed);
                int bw = stride - 5 - seed % 16;
                int bh = (far ? 46 : 59) + seed % (far ? 95 : 122);
                int y = baseline - bh;
                Color body = far ? C(21, 24, 49) : C(14, 20, 37);
                Color side = far ? C(29, 28, 57) : C(20, 28, 47);
                P(g, body, x, y, bw, height - y);
                P(g, side, x + bw - 7, y + 3, 7, height - y - 3);
                P(g, far ? C(42, 33, 66) : C(42, 40, 63), x, y, bw, 2);
                if (seed % 3 == 0)
                {
                    P(g, side, x + bw / 2, y - 16, 2, 16);
                    P(g, C(108, 43, 92), x + bw / 2, y - 17, 2, 2);
                }
                for (int wy = y + 10; wy < baseline; wy += far ? 12 : 13)
                    for (int wx = x + 5; wx < x + bw - 10; wx += 9)
                    {
                        int wh = H(seed + wy * 137 + (wx - x) * 41);
                        if (wh % 4 == 0) continue;
                        Color window = far ? C(40, 42, 75) : (wh % 5 == 0 ? C(96, 44, 93) : C(30, 64, 82));
                        if (!far && stage == 1 && wh % 3 == 0) window = C(107, 68, 57);
                        P(g, window, wx, wy, far ? 2 : 3, far ? 3 : 4);
                    }
                if (!far && seed % 3 == 1)
                {
                    Color sign = stage == 1 ? C(174, 79, 93) : stage == 2 ? C(64, 138, 166) : C(171, 55, 134);
                    P(g, C(39, 26, 58), x + bw - 12, y + 12, 10, 35);
                    P(g, sign, x + bw - 12, y + 12, 1, 35);
                    for (int n = 0; n < 4; n++)
                    {
                        P(g, sign, x + bw - 9, y + 16 + n * 7, 5, 1);
                        P(g, sign, x + bw - 7, y + 14 + n * 7, 1, 5);
                        P(g, sign, x + bw - 9, y + 19 + n * 7, n % 2 == 0 ? 3 : 5, 1);
                    }
                }
            }
        }

        public static void Hero(Graphics g, float x, float y, int facing, float walk, bool airborne, bool dash, bool hurt, int meleeId=-1, float attackTime=0)
        {
            GraphicsState save = g.Save();
            g.TranslateTransform((int)x + (facing < 0 ? 24 : 0), (int)y);
            if (facing < 0) g.ScaleTransform(-1, 1);
            Color outline = C(6, 11, 24), hair = hurt ? Color.White : Pink;
            Color jacket = hurt ? C(222, 255, 255) : C(29, 177, 197);
            Color skin = hurt ? Color.White : C(240, 178, 160);
            bool armed=meleeId==0||meleeId==3;
            MeleePose pose=armed?MeleeMotion.Pose(meleeId,attackTime,Weapons.Effective(meleeId).Range):new MeleePose();
            int stride = airborne ? 2 : (int)(Math.Sin(walk * 10) * 2);
            // Scarf and coat trails are drawn first to give a clear forward silhouette.
            P(g, C(23, 75, 106), 0, 13, 10, 3);
            P(g, Cyan, 1, 13, 8, 1);
            P(g, C(23, 75, 106), dash ? 0 : 2, 15, 5, 2);
            P(g, outline, 7, 20, 12, 10);
            P(g, C(52, 48, 82), 8, 22, 4, 7 - stride);
            P(g, C(93, 49, 111), 8, 22, 2, 3);
            P(g, C(36, 37, 64), 14, 22, 4, 7 + stride);
            P(g, outline, 6, 28 - Math.Max(0, stride), 7, 4);
            P(g, outline, 14, 28 + Math.Min(0, stride), 7, 4);
            P(g, Cyan, 7, 30 - Math.Max(0, stride), 6, 1);
            P(g, Cyan, 15, 30 + Math.Min(0, stride), 6, 1);
            if(armed&&attackTime>0&&!airborne){P(g,outline,18+(int)Math.Max(0,pose.Lean),28,6,4);P(g,Cyan,19+(int)Math.Max(0,pose.Lean),30,5,1);}
            if(armed)g.TranslateTransform((int)Math.Round(pose.Lean),(int)Math.Round(pose.Dip));
            P(g, outline, 5, 11, 16, 13);
            P(g, jacket, 7, 13, 11, 9);
            P(g, C(19, 91, 122), 7, 18, 5, 5);
            P(g, C(60, 227, 236), 8, 13, 3, 5);
            P(g, C(14, 39, 66), 12, 13, 2, 10);
            P(g, C(196, 245, 245), 12, 14, 1, 7);
            P(g, Pink, 8, 21, 4, 2);
            if(!armed){
            P(g, C(24, 46, 72), 4, 15 + stride / 2, 4, 8);
            P(g, jacket, 5, 15 + stride / 2, 3, 5);
            P(g, skin, 5, 21 + stride / 2, 3, 2);
            P(g, C(20, 50, 76), 17, 14 - stride / 2, 4, 9);
            P(g, jacket, 17, 14 - stride / 2, 3, 5);
            P(g, Cyan, 18, 19 - stride / 2, 3, 2);
            P(g, skin, 18, 21 - stride / 2, 3, 2);
            }
            P(g, outline, 7, 2, 13, 11);
            P(g, outline, 9, 0, 9, 3);
            P(g, hair, 8, 2, 11, 5);
            P(g, hair, 6, 4, 4, 4);
            P(g, C(164, 40, 117), 7, 7, 3, 5);
            P(g, C(255, 143, 203), 10, 1, 6, 2);
            P(g, hair, 17, 0, 3, 3);
            P(g, skin, 10, 6, 9, 6);
            P(g, C(188, 112, 127), 9, 9, 3, 3);
            P(g, C(16, 34, 57), 11, 6, 10, 3);
            P(g, Cyan, 13, 6, 8, 2);
            P(g, C(228, 255, 251), 17, 6, 3, 1);
            P(g, C(34, 65, 86), 8, 6, 3, 4);
            P(g, Cyan, 8, 7, 2, 2);
            P(g, C(97, 56, 87), 16, 10, 3, 1);
            P(g, Cyan, 10, 12, 9, 2);
            if (dash) { P(g, Color.White, 19, 15, 4, 2); P(g, Cyan, 22, 13, 2, 6); }
            g.Restore(save);
        }

        public static void Enemy(Graphics g, float x, float y, int type, int facing, float time, bool stunned)
        { DrawEnemy(g,x,y,type,facing,time,stunned,0,0); }

        public static void Enemy(Graphics g,Foe foe,float time)
        {
            if(foe.Type>=4){EnemyArt.DrawEnemy(g,foe,time);return;}
            if(foe.Type==3){BomberArt.DrawAircraft(g,foe,time);return;}
            float shake=foe.Type==2&&foe.HitTint>0?(float)Math.Sin(time*105)*Math.Min(2.5f,foe.HitTint*10):0;
            DrawEnemy(g,foe.X+shake,foe.Y,foe.Type,foe.Direction,time,foe.Stun>0,foe.Flash,foe.HitTint);
        }
        private static Color HitColor(Color color,float flash,float tint)
        {
            if(flash>.10f)return Color.FromArgb(248,252,255);
            float a=Math.Min(.75f,Math.Max(0,tint/.35f*.75f));
            return Color.FromArgb((int)(color.R+(247-color.R)*a),(int)(color.G+(66-color.G)*a),(int)(color.B+(106-color.B)*a));
        }
        private static void DrawEnemy(Graphics g,float x,float y,int type,int facing,float time,bool stunned,float flash,float tint)
        {
            GraphicsState save = g.Save();
            g.TranslateTransform((int)x + (facing < 0 ? 26 : 0), (int)y);
            if (facing < 0) g.ScaleTransform(-1, 1);
            Color armor = HitColor(C(69,54,86),flash,tint), rim = HitColor(C(126,76,117),flash,tint);
            Color eye = HitColor(stunned?Cyan:C(255,115,98),flash,tint);
            if (type == 1)
            {
                P(g, Shadow, 5, 6, 17, 14); P(g, armor, 7, 7, 13, 11);
                P(g, rim, 9, 6, 10, 2); P(g, C(38, 31, 60), 9, 14, 9, 5);
                P(g, Shadow, 0, 9, 6, 5); P(g, Shadow, 21, 9, 5, 5);
                P(g, eye, 10, 10, 10, 3); P(g, Color.FromArgb(255, 220, 174), 17, 10, 2, 2);
                P(g, rim, 1, 8, 4, 3); P(g, rim, 22, 8, 4, 3);
                P(g, C(45, 56, 83), 8, 19, 4, 3); P(g, C(45, 56, 83), 17, 19, 4, 3);
                int jet = 2 + (int)(time * 15) % 3;
                P(g, C(31, 159, 186), 9, 22, 2, jet); P(g, Cyan, 18, 22, 2, jet);
            }
            else if (type == 2)
            {
                P(g, Shadow, 3, 22, 22, 6); P(g, armor, 6, 21, 16, 6);
                P(g, rim, 7, 22, 14, 2); P(g, C(33, 30, 52), 10, 14, 8, 8);
                P(g, Shadow, 4, 5, 18, 13); P(g, armor, 6, 6, 14, 10);
                P(g, rim, 7, 5, 10, 2); P(g, eye, 13, 8, 7, 3);
                P(g, C(28, 31, 49), 19, 12, 7, 4); P(g, rim, 21, 12, 5, 1);
                P(g, C(250, 184, 92), 6, 24, 3, 2); P(g, C(250, 184, 92), 18, 24, 3, 2);
            }
            else
            {
                int step = (int)(Math.Sin(time * 8) * 2);
                P(g, Shadow, 6, 18, 6, 10); P(g, Shadow, 16, 18, 6, 10);
                P(g, armor, 7, 19, 4, 7 - step); P(g, armor, 17, 19, 4, 7 + step);
                P(g, rim, 5, 25 - Math.Max(0, step), 8, 3); P(g, rim, 16, 25 + Math.Min(0, step), 8, 3);
                P(g, Shadow, 3, 9, 21, 13); P(g, armor, 5, 10, 16, 10);
                P(g, C(47, 38, 65), 7, 17, 14, 4); P(g, rim, 5, 10, 3, 7);
                P(g, Shadow, 6, 1, 17, 11); P(g, armor, 8, 2, 13, 8);
                P(g, rim, 9, 2, 10, 2); P(g, eye, 13, 5, 9, 3);
                P(g, C(255, 223, 174), 19, 5, 2, 1); P(g, C(28, 26, 42), 10, 13, 7, 4);
                P(g, eye, 11, 14, 5, 1); P(g, C(29, 31, 47), 20, 12, 6, 5);
                P(g, rim, 21, 12, 5, 2);
            }
            if (stunned)
            {
                P(g, Cyan, 4, 0, 2, 3); P(g, Cyan, 2, 1, 6, 1);
                P(g, Cyan, 23, 1, 2, 3);
            }
            g.Restore(save);
        }

        public static void Portrait(Graphics g, Rectangle bounds, float time)
        {
            GraphicsState save = g.Save();
            g.TranslateTransform(bounds.X, bounds.Y);
            g.ScaleTransform(bounds.Width / 40f, bounds.Height / 40f);
            P(g, C(8, 13, 31), 0, 0, 40, 40);
            P(g, C(22, 29, 57), 2, 2, 36, 36);
            P(g, C(12, 19, 39), 3, 3, 34, 34);
            for (int y = 4; y < 37; y += 4) P(g, C(20, 33, 56), 4, y, 32, 1);
            P(g, C(60, 22, 74), 4, 4, 14, 29); P(g, C(28, 58, 81), 28, 8, 8, 26);
            P(g, Pink, 2, 3, 1, 10); P(g, Cyan, 37, 26, 1, 11);
            P(g, C(6, 9, 22), 8, 9, 25, 28);
            P(g, C(23, 86, 109), 9, 31, 24, 9); P(g, C(42, 193, 204), 10, 33, 21, 7);
            P(g, C(15, 45, 69), 19, 34, 5, 6); P(g, Cyan, 11, 34, 5, 2);
            P(g, C(162, 81, 111), 17, 26, 10, 8); P(g, C(245, 176, 158), 20, 27, 8, 5);
            P(g, C(230, 155, 149), 14, 13, 17, 16); P(g, C(250, 190, 165), 19, 16, 12, 11);
            P(g, C(162, 82, 110), 13, 18, 5, 9); P(g, C(255, 201, 170), 30, 19, 3, 6);
            P(g, C(12, 14, 34), 8, 9, 24, 7); P(g, C(12, 14, 34), 12, 5, 19, 7);
            P(g, C(174, 38, 126), 8, 12, 7, 15); P(g, Pink, 9, 10, 10, 9);
            P(g, C(249, 55, 157), 13, 7, 18, 7); P(g, C(255, 104, 184), 16, 5, 13, 5);
            P(g, C(255, 152, 204), 17, 5, 9, 2); P(g, Pink, 29, 3, 4, 6);
            P(g, Pink, 9, 8, 7, 7); P(g, C(255, 104, 184), 11, 9, 7, 3);
            P(g, C(248, 55, 155), 26, 10, 7, 4); P(g, Pink, 7, 15, 3, 5);
            P(g, C(24, 31, 54), 16, 15, 18, 7); P(g, C(36, 107, 135), 18, 16, 16, 4);
            P(g, Cyan, 19, 16, 15, 3); P(g, C(222, 255, 246), 26, 16, 7, 1);
            P(g, C(30, 50, 74), 11, 15, 7, 9); P(g, C(83, 147, 169), 12, 16, 5, 6);
            P(g, Cyan, 13, 17, 3, 4); P(g, C(232, 255, 255), 13, 17, 2, 1);
            P(g, C(169, 89, 113), 29, 22, 2, 2); P(g, C(111, 49, 88), 25, 26, 5, 1);
            P(g, C(255, 214, 182), 22, 23, 2, 2); P(g, C(22, 36, 62), 10, 29, 9, 5);
            P(g, Cyan, 13, 31, 17, 3); P(g, C(59, 159, 188), 11, 34, 5, 6);
            P(g, C(134, 249, 248), 18, 31, 10, 1); P(g, Pink, 29, 35, 4, 2);
            P(g, Cyan, 2, 36, 9, 1); P(g, Pink, 30, 2, 7, 1);
            g.Restore(save);
        }

        public static void DisposeResources()
        {
            foreach (SolidBrush brush in Ink.Values) brush.Dispose();
            Ink.Clear();
            EnemyArt.DisposeResources();
        }
    }
}
