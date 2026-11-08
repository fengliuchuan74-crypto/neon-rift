using System;
using System.Collections.Generic;
using System.Drawing;

namespace NeonRift
{
    public class Level
    {
        public string Name, Subtitle, Briefing;
        public int Width, Index;
        public float StartX = 60, StartY = 260, ExitX, ExitY;
        public List<Platform> Platforms = new List<Platform>();
        public List<EnemySpawn> Enemies = new List<EnemySpawn>();
        public List<Hazard> Hazards = new List<Hazard>();
        public List<Pickup> Pickups = new List<Pickup>();
        public List<Checkpoint> Checkpoints = new List<Checkpoint>();
        public List<Signpost> Signs = new List<Signpost>();
        public List<Terminal> Terminals = new List<Terminal>();
        public List<CorePuzzle> CorePuzzles = new List<CorePuzzle>();
    }

    public class Platform
    {
        public float X, Y, W, H, BaseX, BaseY, Range, Speed, Phase, PrevX, PrevY;
        public int Axis;
        public bool OneWay;
        public bool Enabled = true;
        // Disguised crumble tiles render as ordinary flooring until touched.
        public bool Disguised;
        // 0 normal; 1 crumble; 2 belt; 3 spring; 4 terminal gate; 5 lift;
        // 6 pulse relay gate; 7 timed pressure gate; 8 pushable crate.
        public int Kind, Link, GateControl;
        public float Timer, Belt;
        public float GateDuration,GateFloorY;
        public float[] LiftStops;
        public int LiftStop, LiftTarget, LiftDirection = 1;
        public bool LiftMoving;
        public RectangleF Bounds { get { return new RectangleF(X, Y, W, H); } }

        public void Update(float time)
        {
            PrevX = X;
            PrevY = Y;
            // Lift and crate coordinates are controlled by their own physics.
            // A crate's BaseX/BaseY remain its authored respawn position.
            if (Kind == 5 || Kind == 8 || Kind == 10) return;
            float offset = (float)Math.Sin(time * Speed + Phase) * Range;
            X = BaseX + (Axis == 1 ? offset : 0);
            Y = BaseY + (Axis == 2 ? offset : 0);
        }

        public void RequestLift(float floorY, bool riding)
        {
            if (Kind != 5 || LiftMoving || LiftStops == null || LiftStops.Length < 2) return;
            int target = LiftStop;
            if (riding) {
                if (LiftStop == LiftStops.Length - 1) LiftDirection = -1;
                else if (LiftStop == 0) LiftDirection = 1;
                target = LiftStop + LiftDirection;
            } else {
                float nearest = float.MaxValue;
                for (int i = 0; i < LiftStops.Length; i++) {
                    float d = Math.Abs(LiftStops[i] - floorY);
                    if (d < nearest) { nearest = d; target = i; }
                }
            }
            LiftTarget = target; LiftMoving = Math.Abs(Y - LiftStops[target]) > .1f;
        }

        public void TickLift(float dt)
        {
            if (!LiftMoving || LiftStops == null) return;
            float target = LiftStops[LiftTarget], step = 96 * dt;
            if (Math.Abs(target - Y) <= step) {
                Y = target; LiftStop = LiftTarget; LiftMoving = false;
            } else Y += target > Y ? step : -step;
        }
    }

    public class EnemySpawn
    {
        // 0: patrol robot; 1: flying drone; 2: stationary turret; 3: bomber aircraft.
        public float X, Y, MinX, MaxX;
        public int Type;
        public bool RoomGuard,CoreAmbush;
    }

    public class Hazard
    {
        // 5 telegraphed electric floor; 6 pressure-triggered electric floor;
        // 7 proximity mine (armed after the player steps into its trigger).
        public float X, Y, W, H, Period, Phase, Disabled;
        public float BaseX, BaseY, Range, Speed;
        public int Axis;
        public int Type;
        public float TriggerX, TriggerY, TriggerW, Timer;
        public bool PlateOccupied;
        public RectangleF Bounds { get { return new RectangleF(X, Y, W, H); } }

        public void Update(float time)
        {
            if (Type < 2 || Range == 0) return;
            float offset = (float)Math.Sin(time * Speed + Phase) * Range;
            X = BaseX + (Axis == 1 ? offset : 0);
            Y = BaseY + (Axis == 2 ? offset : 0);
        }

        public bool Active(float time)
        {
            if (Disabled > 0) return false;
            if (Type == 7) return false; // Instant circular blast is resolved once by TickMinesAfterMovement.
            if (Type == 6) return Timer > .6f && Timer <= 1.7f;
            if (Type == 5) {
                if (Period <= 0) return false;
                float phase = (time + Phase) % Period;
                if (phase < 0) phase += Period;
                return phase >= .9f && phase < 1.85f;
            }
            if (Type == 0 || Type == 2 || Type == 4) return true;
            if (Period <= 0) return true;
            float cycle = (time + Phase) % Period;
            if (cycle < 0) cycle += Period;
            return cycle < Period * (Type == 3 ? 0.42f : 0.47f);
        }
    }

    public class Pickup
    {
        // 0 optional shard; 1 required core; 2 health; 3 weapon cache;
        // 4 finite ammunition crate (AmmoWeaponId/AmmoAmount).
        public float X, Y;
        public int Type;
        public int WeaponId;
        public int AmmoWeaponId, AmmoAmount;
        public bool Collected;
        public int PuzzleId = -1;
    }

    public class Checkpoint
    {
        public float X, Y;
        public bool Activated;
    }

    public class Signpost
    {
        public float X, Y;
        public string Text,HintKey;
    }

    public class Terminal
    {
        public float X, Y;
        public int Link;
        public bool Activated;
        // 0 permanent F console; 1 pulse coil; 2 raised pressure switch.
        public int Mode;
        public float Timer,Duration,TargetX,TargetY,Width=52;
        public bool Pressed;
        // Puzzle terminals: mode 3 is a push-box socket, mode 4 is a
        // moving target that must be hit by a projectile.  BaseX/MoveRange
        // keep the target moving without introducing another world entity.
        public float BaseX,BaseY,MoveRange,MoveSpeed,MovePhase;
    }

    public static partial class Levels
    {
        public static Level Create(int index)
        {
            if (index < 0 || index > 2) throw new ArgumentOutOfRangeException("index");
            Level level=index==0?Street():index==1?Cargo():Reactor();
            RefineTraversal(level);
            AddBomberPatrols(level);
            ApplyChapterThemes(level);
            LevelInteractions.Author(level);
            AuthorCorePuzzles(level);
            AuthorMineRoutes(level);
            foreach(Platform gate in level.Platforms) {
                if(gate.Kind!=4&&gate.Kind!=6&&gate.Kind!=7)continue;
                bool first=true;
                foreach(Terminal terminal in level.Terminals) {
                    if(terminal.Link!=gate.Link)continue;
                    if(first){gate.GateControl=terminal.Mode;first=false;}
                    terminal.TargetX=gate.X+gate.W*.5f;
                    terminal.TargetY=gate.Kind==4?gate.Y+gate.H:gate.GateFloorY;
                }
            }
            return level;
        }

        private static void AddBomberPatrols(Level level)
        {
            // Wide permanent decks give a full walking escape from a 43px
            // impact radius. Bombers stay well before all story arenas and
            // complex crate/target puzzles; existing turrets are replaced so
            // the new encounter does not stack two ranged enemies at once.
            if(level.Index==0) {
                Enemy(level,2825,181,2760,3090,3);
                Sign(level,2730,137,"轰炸机巡逻区", "bomber");
            }
            else if(level.Index==1) {
                level.Enemies.RemoveAll(delegate(EnemySpawn e){return e.Type==2&&e.X>3020&&e.X<3440;});
                Enemy(level,3200,175,3080,3350,3);
                Sign(level,3070,130,"轰炸机巡逻区", "bomber");
            } else {
                level.Enemies.RemoveAll(delegate(EnemySpawn e){return e.Type==0&&e.X>3260&&e.X<3440;});
                Enemy(level,3315,156,3260,3410,3);
                Sign(level,3250,117,"轰炸机巡逻区", "bomber");
            }
        }

        private static Level Street()
        {
            Level l = NewLevel(0, 6520, "霓虹街区", "01 / NEON OUTSKIRTS",
                "探索上下两条街道，回收 3 枚核心，启动终端开启城区闸门。", 4580, 300);

            Ground(l, 0, 560, 300);
            Ammo(l, 110, 266, 1, 12);
            Ground(l, 680, 510, 300);
            Ground(l, 1320, 560, 280);
            Ground(l, 2040, 510, 300);
            Ground(l, 2700, 500, 300);

            Ledge(l, 175, 252, 112);
            Ledge(l, 335, 212, 112);
            Moving(l, 590, 296, 80, 1, 40, 1.10f, 0);
            Ledge(l, 925, 252, 104);
            Moving(l, 1210, 286, 90, 2, 24, 1.00f, 0);
            Ledge(l, 1590, 234, 100);
            Ledge(l, 1740, 192, 108);
            Moving(l, 1930, 286, 96, 1, 60, 0.95f, 0);
            Ledge(l, 2245, 252, 112);
            Moving(l, 2580, 294, 96, 1, 38, 1.10f, 0);
            Ledge(l, 2905, 248, 108);

            Spikes(l, 365, 300, 52);
            Spikes(l, 1460, 280, 60);
            Spikes(l, 2360, 300, 56);
            Laser(l, 1110, 146, 300, 3.8f, 1.1f);
            Laser(l, 2490, 146, 300, 3.6f, 2.0f);

            Enemy(l, 800, 272, 730, 1040, 0);
            Enemy(l, 1690, 252, 1690, 1690, 2);
            Enemy(l, 1950, 186, 1900, 2050, 1);
            Enemy(l, 2295, 272, 2110, 2330, 0);
            Enemy(l, 2950, 272, 2870, 3050, 0);

            Core(l, 855, 266);
            Core(l, 1780, 246);
            // The old third core was beside the first exit.  Move it into the
            // extended district so players must read the new routes before
            // they can finish the stage.
            Core(l, 6300, 266);
            Shards(l, 202, 225, 3, 27);
            Shards(l, 355, 184, 3, 28);
            Shards(l, 586, 245, 3, 30);
            Shards(l, 1390, 246, 2, 30);
            Shards(l, 1756, 164, 3, 28);
            Shards(l, 1940, 236, 2, 28);
            Shards(l, 2918, 218, 3, 27);
            Heal(l, 1015, 266);
            Heal(l, 2165, 266);
            Check(l, 1020, 300);
            Check(l, 2150, 300);

            Sign(l, 82, 208, "A / D 移动 · 空格跳跃 · 空中可再跳一次","move");
            Sign(l, 450, 205, "1 相位冲刺：越过缺口，短暂无敌","dash");
            Sign(l, 955, 186, "2 电磁脉冲：震晕敌人，关闭附近激光","pulse");
            Sign(l, 2158, 202, "蓝色信标保存进度 · 收集 3 枚金色核心","checkpoint");
            Sign(l, 3005, 170, "街区深处还有新机关 · 第三枚核心位于出口前");

            // The lower service street stays traversable; roofs offer optional weapons.
            Weapon(l, 390, 184, 2);
            Ammo(l, 438, 184, 2, 6);
            Ammo(l, 735, 266, 1, 8);
            Ammo(l, 1395, 246, 1, 10);
            Ledge(l, 3275, 225, 110);
            Ledge(l, 3440, 176, 118);
            Moving(l, 3200, 292, 86, 1, 28, 1.1f, 0);
            Platform belt = Ground(l, 3310, 440, 300);
            belt.Kind = 2; belt.Belt = 62;
            Spring(l, 3420, 292, 56);
            Crumble(l, 3770, 294, 110);
            Ground(l, 3900, 400, 280);
            Ledge(l, 3930, 223, 100);
            Ledge(l, 4070, 174, 118);
            Weapon(l, 4128, 146, 3);
            Moving(l, 4320, 287, 88, 1, 26, .85f, 0);
            Ground(l, 4430, 250, 300);
            Gate(l, 3665, 300, 1, 3570);
            Saw(l, 3505, 252, 26, 1, 44, 1.4f, 0);
            Flame(l, 4198, 280, 4.2f, 2.2f);
            Crusher(l, 4515, 300, 3.6f, 0.8f);
            Enemy(l, 4010, 252, 3935, 4100, 0);
            Enemy(l, 4340, 182, 4290, 4410, 1);
            Ammo(l, 3350, 190, 2, 6);
            Ammo(l, 4020, 195, 2, 6);
            Shards(l, 3290, 196, 3, 27);
            Shards(l, 3460, 146, 3, 27);
            Shards(l, 3944, 195, 3, 27);
            Heal(l, 3945, 246);
            Check(l, 3910, 280);
            Sign(l, 230, 152, "屋顶藏有武器 · B 打开背包 · Q / E 切换武器","weapons");
            Sign(l, 3318, 197, "箭头地板会传送 · 弹射板助你跃上屋顶","spring");
            Sign(l, 3560, 188, "靠近终端按 F 开门 · 脉冲可暂时关闭机械陷阱","console");
            Sign(l, 3908, 145, "橙色平台踩踏后坍塌，稍后重建 · 留意喷火节奏","crumble");
            Sign(l, 4450, 175, "避开落锤进入维护街区 · 最后一枚核心藏在城区深处","trap-cycle");

            // Extended district: a safe-looking maintenance route hides a
            // sequence of collapsing plates.  The raised neon roofs are a
            // forgiving alternate route for players who react to the warning.
            Ground(l, 4700, 245, 300);
            Ledge(l, 4760, 226, 100);
            FalseFloor(l, 4945, 300, 112);
            FalseFloor(l, 5057, 300, 112);
            FalseFloor(l, 5169, 300, 112);
            FalseFloor(l, 5281, 300, 69);
            Ledge(l, 4900, 191, 100);
            Ledge(l, 5050, 214, 102);
            Moving(l, 5200, 198, 104, 1, 25, .8f, .7f);
            Ledge(l, 5050, 354, 160);
            Ground(l, 5350, 230, 300);
            Ledge(l, 5380, 224, 108);
            Ledge(l, 5535, 182, 112);
            FalseFloor(l, 5660, 300, 170);
            Ground(l, 5830, 270, 300);
            Moving(l, 6120, 286, 92, 1, 35, 1.05f, 1.2f);
            Ground(l, 6250, 260, 300);
            Saw(l, 4870, 254, 28, 1, 50, 1.45f, .5f);
            Flame(l, 5450, 300, 3.8f, 1.1f);
            Crusher(l, 5630, 300, 3.4f, .6f);
            Laser(l, 5950, 136, 300, 3.9f, 2.5f);
            Enemy(l, 4760, 272, 4720, 4900, 0);
            Enemy(l, 5415, 252, 5370, 5520, 0);
            Enemy(l, 5580, 156, 5500, 5670, 1);
            Enemy(l, 6020, 272, 5940, 6160, 0);
            Shards(l, 4780, 192, 3, 28);
            Shards(l, 5400, 194, 3, 28);
            Shards(l, 5548, 151, 3, 27);
            Shards(l, 5720, 231, 3, 27);
            Ammo(l, 4800, 192, 1, 12);
            Ammo(l, 5080, 182, 2, 6);
            Ammo(l, 5580, 151, 1, 10);
            Ammo(l, 6340, 266, 2, 6);
            Heal(l, 5360, 266);
            Check(l, 5370, 300);
            Sign(l, 4720, 174, "维护区表面完整的板块会在踩踏后坍塌 · 及时转向上层","crumble");
            Sign(l, 5330, 163, "两条路线都能前进 · 观察锯轮和喷火的节奏","trap-cycle");
            Sign(l, 5790, 177, "最后的核心在终端平台之后 · 不要被假地板拖入深坑","crumble");
            l.ExitX = 6430;
            l.ExitY = 236;
            ExtendStreet(l);
            return l;
        }

        private static Level Cargo()
        {
            Level l = NewLevel(1, 6440, "悬空货运线", "02 / SKYLINE FREIGHT",
                "跨越升降货台和运输带，在上下货运线路回收 3 枚核心。", 5020, 280);

            Ground(l, 0, 410, 300);
            Ammo(l, 110, 266, 1, 12);
            Ground(l, 550, 410, 280);
            Ground(l, 1130, 400, 260);
            Ground(l, 1730, 500, 300);
            Ground(l, 2390, 480, 270);
            Ground(l, 3020, 420, 300);

            Ledge(l, 165, 248, 116);
            // The first cargo gap used to read as a bottomless void: after
            // the first ground bank the ferry could drift out of jump range.
            // Add two explicit low stepping decks so the route is readable
            // and can be crossed with a normal jump plus the moving platform.
            Ledge(l, 350, 276, 82);
            Ledge(l, 566, 270, 74);
            Moving(l, 440, 286, 90, 1, 40, 0.95f, 0);
            Ledge(l, 610, 224, 108);
            Ledge(l, 770, 180, 96);
            Moving(l, 975, 280, 110, 2, 45, 0.90f, 0);
            Ledge(l, 1250, 210, 104);
            Moving(l, 1580, 275, 100, 1, 55, 0.85f, 0);
            Ledge(l, 1810, 246, 108);
            Ledge(l, 1960, 204, 100);
            Moving(l, 2260, 282, 96, 1, 38, 1.00f, 0);
            Ledge(l, 2530, 216, 106);
            Ledge(l, 2690, 172, 100);
            Moving(l, 2890, 285, 100, 2, 35, 1.00f, 0);
            Ledge(l, 3180, 248, 106);

            Spikes(l, 710, 280, 58);
            Spikes(l, 2035, 300, 62);
            Spikes(l, 2640, 270, 54);
            Laser(l, 1420, 110, 260, 3.5f, 0.6f);
            Laser(l, 2160, 142, 300, 4.0f, 2.1f);
            Laser(l, 2800, 110, 270, 3.6f, 0.8f);

            Enemy(l, 620, 252, 580, 700, 0);
            Enemy(l, 1030, 150, 925, 1105, 1);
            Enemy(l, 1340, 232, 1280, 1490, 0);
            Enemy(l, 1810, 272, 1810, 1810, 2);
            Enemy(l, 2260, 175, 2160, 2360, 1);
            Enemy(l, 2520, 242, 2460, 2600, 0);
            Enemy(l, 3295, 272, 3295, 3295, 2);

            Core(l, 840, 246);
            Core(l, 1940, 266);
            Core(l, 6260, 266);
            Shards(l, 180, 218, 3, 28);
            Shards(l, 785, 152, 3, 27);
            Shards(l, 991, 198, 3, 26);
            Shards(l, 1265, 182, 3, 27);
            Shards(l, 1570, 212, 3, 29);
            Shards(l, 1975, 174, 3, 27);
            Shards(l, 2702, 144, 3, 27);
            Shards(l, 2897, 210, 3, 27);
            Shards(l, 3195, 218, 3, 27);
            Heal(l, 1190, 226);
            Heal(l, 2418, 236);
            Check(l, 1180, 260);
            Check(l, 2410, 270);

            Sign(l, 72, 206, "货运线启动 · 平台会带着你移动","moving");
            Sign(l, 330, 182, "空格 → 空格 → 1，可以越过更远的缺口","dash");
            Sign(l, 345, 232, "货台缺口 · 先跳上低台，再等吊板靠岸","moving");
            Sign(l, 1185, 151, "落地会恢复空中跳跃 · 留意技能冷却","move");
            Sign(l, 1850, 151, "升降平台到达高处时，起跳更轻松","moving");
            Sign(l, 3035, 181, "前方是运输中枢 · 连续坍塌货台考验你的节奏","crumble");

            Weapon(l, 817, 150, 2);
            Ammo(l, 350, 218, 1, 8);
            Ammo(l, 882, 150, 2, 6);
            Ammo(l, 1200, 226, 1, 10);
            Moving(l, 3465, 285, 88, 2, 31, .95f, 0);
            Ground(l, 3570, 410, 280);
            Spring(l, 3630, 272, 54);
            Ledge(l, 3655, 205, 110);
            Ledge(l, 3810, 161, 118);
            Weapon(l, 3868, 133, 3);
            Crumble(l, 4000, 279, 78);
            Crumble(l, 4098, 279, 78);
            Platform belt = Ground(l, 4200, 430, 300);
            belt.Kind = 2; belt.Belt = -72;
            Ledge(l, 4360, 225, 118);
            Moving(l, 4518, 193, 92, 2, 25, 1.05f, 0);
            Moving(l, 4660, 286, 104, 1, 34, 1.2f, 0);
            Ground(l, 4790, 330, 280);
            Gate(l, 4325, 300, 2, 4215);
            Saw(l, 3860, 236, 30, 1, 60, 1.5f, .8f);
            Flame(l, 4460, 300, 3.7f, 1.5f);
            Crusher(l, 4875, 280, 3.1f, .4f);
            Flame(l, 4970, 280, 4.1f, 2.9f);
            Saw(l, 4623, 182, 28, 2, 48, 1.3f, 1);
            Enemy(l, 3740, 252, 3700, 3800, 0);
            Enemy(l, 4130, 168, 4060, 4210, 1);
            Enemy(l, 4550, 272, 4488, 4590, 0);
            Ammo(l, 3690, 175, 2, 6);
            Ammo(l, 4385, 195, 1, 10);
            Shards(l, 3669, 175, 3, 27);
            Shards(l, 4010, 243, 6, 28);
            Shards(l, 4374, 195, 3, 27);
            Heal(l, 3600, 246);
            Heal(l, 4788, 246);
            Check(l, 3585, 280);
            Check(l, 4800, 280);
            Sign(l, 3590, 151, "弹簧通往稀有武器 · 下层是核心主线","spring");
            Sign(l, 3915, 166, "坍塌桥每块只停留片刻 · 持续移动，再接二段跳","crumble");
            Sign(l, 4190, 170, "F 启动运输闸门 · 逆向传送带会拖慢脚步","conveyor");
            Sign(l, 4810, 160, "先观察落锤与喷火节奏 · 1 冲刺可安全穿越","trap-cycle");

            // Extended loading yard.  The straight bridge is intentionally a
            // decoy: its plates look like ordinary cargo flooring but crumble
            // in sequence.  The moving gantry and upper ledges form a safer
            // alternate route that still asks for jumps and timing.
            Ground(l, 5120, 170, 300);
            FalseFloor(l, 5290, 300, 100);
            FalseFloor(l, 5390, 300, 100);
            FalseFloor(l, 5490, 300, 100);
            Ledge(l, 5230, 224, 106);
            Ledge(l, 5380, 179, 104);
            Ledge(l, 5490, 351, 118);
            Moving(l, 5535, 249, 92, 2, 31, 1.0f, .4f);
            Ledge(l, 5610, 208, 110);
            Ground(l, 5710, 260, 300);
            Ledge(l, 5750, 222, 108);
            Moving(l, 5990, 278, 96, 1, 42, 1.05f, 1.4f);
            FalseFloor(l, 6130, 294, 92);
            Ground(l, 6250, 190, 300);
            Laser(l, 5180, 132, 320, 3.7f, 1.7f);
            Saw(l, 5660, 228, 30, 1, 52, 1.4f, .9f);
            Flame(l, 5860, 300, 3.5f, .8f);
            Mine(l, 5940, 300);
            Crusher(l, 6090, 300, 3.2f, 1.1f);
            Enemy(l, 5120, 272, 5070, 5230, 0);
            Enemy(l, 5780, 252, 5740, 5900, 0);
            Enemy(l, 5980, 145, 5900, 6090, 1);
            Enemy(l, 6330, 272, 6270, 6400, 0);
            Shards(l, 5090, 238, 3, 27);
            Shards(l, 5625, 178, 3, 27);
            Shards(l, 5765, 191, 3, 27);
            Shards(l, 6145, 238, 3, 27);
            Ammo(l, 5250, 194, 1, 12);
            Ammo(l, 5400, 148, 2, 6);
            Ammo(l, 5770, 191, 1, 10);
            Ammo(l, 6330, 266, 2, 6);
            Heal(l, 5730, 266);
            Check(l, 5740, 300);
            Sign(l, 5080, 188, "废弃货台看似完整 · 踩上去会依次塌陷，向上层货架转移","crumble");
            Sign(l, 5700, 166, "升降臂是安全捷径 · 等待它到达高处再跳","moving");
            Sign(l, 6200, 174, "终点前还有移动锯轮和落锤 · 留出冲刺空间","trap-cycle");
            l.ExitX = 6360;
            l.ExitY = 236;
            ExtendCargo(l);
            return l;
        }

        private static Level Reactor()
        {
            Level l = NewLevel(2, 7200, "核心反应炉", "03 / THE NEON REACTOR",
                "穿越双闸门、压机和火焰防线，携带 3 枚核心逃离霓虹城。", 5380, 300);

            Ground(l, 0, 440, 300);
            Ammo(l, 110, 266, 1, 12);
            Ground(l, 580, 450, 280);
            Ground(l, 1190, 470, 300);
            Ground(l, 1840, 450, 260);
            Ground(l, 2450, 470, 300);
            Ground(l, 3100, 580, 280);

            Ledge(l, 158, 248, 110);
            Moving(l, 470, 294, 80, 1, 35, 1.05f, 0);
            Ledge(l, 780, 226, 106);
            Moving(l, 1055, 279, 90, 2, 35, 1.10f, 0);
            Ledge(l, 1340, 248, 108);
            Ledge(l, 1490, 206, 104);
            Moving(l, 1700, 281, 100, 1, 55, 1.00f, 0);
            Ledge(l, 1910, 210, 102);
            Ledge(l, 2070, 166, 106);
            Moving(l, 2320, 270, 90, 2, 35, 0.95f, 0);
            Ledge(l, 2650, 248, 108);
            Moving(l, 2960, 289, 104, 1, 55, 0.95f, 0);
            Ledge(l, 3290, 228, 108);

            Spikes(l, 940, 280, 54);
            Spikes(l, 1980, 260, 60);
            Spikes(l, 2690, 300, 62);
            Spikes(l, 3200, 280, 60);
            Laser(l, 720, 130, 280, 3.2f, 1.0f);
            Laser(l, 1530, 126, 300, 3.6f, 2.0f);
            Laser(l, 2170, 104, 260, 3.2f, 0.3f);
            Laser(l, 2810, 122, 300, 3.5f, 1.5f);
            Laser(l, 3490, 122, 280, 3.4f, 2.3f);

            Enemy(l, 305, 272, 280, 395, 0);
            Enemy(l, 795, 252, 770, 920, 0);
            Enemy(l, 1110, 153, 990, 1170, 1);
            Enemy(l, 1570, 272, 1570, 1570, 2);
            Enemy(l, 2100, 232, 2060, 2220, 0);
            Enemy(l, 2350, 154, 2240, 2420, 1);
            Enemy(l, 2860, 272, 2860, 2860, 2);
            Enemy(l, 3370, 252, 3290, 3430, 0);

            Core(l, 870, 246);
            Core(l, 2100, 226);
            // Keep the final core immediately before the boss approach so the
            // arena cannot be skipped by touching the old exit location.
            Core(l, 6120, 266);
            Shards(l, 171, 218, 3, 27);
            Shards(l, 793, 196, 3, 27);
            Shards(l, 1059, 205, 3, 26);
            Shards(l, 1505, 176, 3, 27);
            Shards(l, 1697, 210, 3, 27);
            Shards(l, 2086, 136, 3, 27);
            Shards(l, 2328, 196, 3, 25);
            Shards(l, 2665, 218, 3, 27);
            Shards(l, 2968, 225, 3, 27);
            Shards(l, 3307, 198, 3, 27);
            Heal(l, 1240, 266);
            Heal(l, 2540, 266);
            Heal(l, 3120, 246);
            Check(l, 1240, 300);
            Check(l, 2530, 300);

            Sign(l, 65, 207, "最终区域 · 冲刺与脉冲可以连续使用","pulse");
            Sign(l, 592, 177, "激光会周期关闭 · 也可按 2 主动关闭","pulse");
            Sign(l, 1247, 173, "检查点已就绪 · 空中二段跳接冲刺","checkpoint");
            Sign(l, 2540, 167, "反应炉还有两道闸门 · 脉冲可关闭机械机关","pulse");
            Sign(l, 3530, 170, "进入熔炉内部 · 激活终端打通安全通路","console");

            Weapon(l, 1535, 176, 2);
            Ammo(l, 340, 238, 1, 8);
            Ammo(l, 1570, 145, 2, 6);
            Ammo(l, 2210, 132, 1, 10);
            Weapon(l, 2112, 136, 3);
            Moving(l, 3710, 286, 98, 1, 34, 1.2f, 0);
            Ground(l, 3830, 460, 300);
            Gate(l, 3950, 300, 3, 3850);
            Spring(l, 4000, 292, 58);
            Ledge(l, 4040, 218, 100);
            Ledge(l, 4180, 161, 104);
            Crumble(l, 4310, 285, 73);
            Crumble(l, 4400, 274, 60);
            Platform belt = Ground(l, 4480, 380, 280);
            belt.Kind = 2; belt.Belt = 82;
            Gate(l, 4695, 280, 4, 4590);
            Moving(l, 4890, 282, 110, 2, 35, 1.1f, 0);
            Ground(l, 5040, 440, 300);
            Ledge(l, 5080, 230, 100);
            Ledge(l, 5230, 176, 102);
            Saw(l, 4120, 250, 30, 1, 70, 1.7f, 0);
            Flame(l, 4535, 280, 3.5f, 1.5f);
            Crusher(l, 4777, 280, 2.9f, 0.5f);
            Saw(l, 4970, 193, 30, 2, 60, 1.45f, 0);
            Flame(l, 5140, 300, 3.2f, 2.2f);
            Crusher(l, 5250, 300, 3.4f, 1.5f);
            Enemy(l, 4210, 272, 4170, 4250, 0);
            Enemy(l, 4470, 151, 4370, 4530, 1);
            Enemy(l, 5080, 272, 5080, 5080, 2);
            Enemy(l, 5340, 272, 5300, 5390, 0);
            Ammo(l, 3870, 220, 2, 6);
            Ammo(l, 5090, 190, 1, 10);
            Shards(l, 4054, 188, 3, 27);
            Shards(l, 4190, 132, 3, 27);
            Shards(l, 4330, 245, 4, 29);
            Shards(l, 5094, 200, 3, 27);
            Shards(l, 5243, 146, 3, 27);
            Heal(l, 3860, 266);
            Heal(l, 4620, 246);
            Heal(l, 5064, 266);
            Check(l, 3840, 300);
            Check(l, 5050, 300);
            Sign(l, 3845, 171, "F 启动第一道闸门 · 上方路线可以避开地面锯轮","console");
            Sign(l, 4465, 156, "传送带将你推向机关 · 脉冲后再靠近终端按 F","conveyor");
            Sign(l, 5060, 151, "最后防线 · 观察喷火与落锤，再冲刺穿越","trap-cycle");

            // Reactor approach extension.  Several identical plates are a
            // deliberate visual decoy: only the first contact arms their
            // crumble timers, while the roof route remains stable.
            Ground(l, 5340, 220, 300);
            FalseFloor(l, 5560, 300, 100);
            FalseFloor(l, 5660, 300, 100);
            Ledge(l, 5480, 220, 106);
            Ledge(l, 5630, 172, 108);
            Moving(l, 5805, 278, 94, 1, 35, 1.1f, .6f);
            Ledge(l, 5890, 218, 112);
            Ground(l, 6000, 250, 300);
            Crumble(l, 6270, 294, 96);
            Moving(l, 6380, 284, 90, 1, 20, .9f, .2f);
            // No hazards, patrols or moving geometry inside the locked arena.
            // The camera uses the 6540..7180 view while combat runs here.
            Ground(l, 6470, 730, 300);
            Saw(l, 5480, 252, 30, 1, 48, 1.5f, .4f);
            Flame(l, 5740, 300, 3.6f, 1.3f);
            Laser(l, 6040, 122, 300, 3.2f, 2.1f);
            Crusher(l, 6215, 300, 3.4f, .7f);
            Enemy(l, 5420, 272, 5370, 5510, 0);
            Enemy(l, 5660, 180, 5580, 5790, 1);
            Enemy(l, 6050, 272, 6000, 6170, 0);
            Shards(l, 5360, 244, 3, 27);
            Shards(l, 5900, 188, 3, 27);
            Shards(l, 6305, 244, 3, 27);
            Ammo(l, 5500, 190, 1, 12);
            Ammo(l, 5650, 142, 2, 6);
            Ammo(l, 5920, 187, 1, 10);
            Ammo(l, 6490, 266, 1, 20);
            Ammo(l, 6530, 266, 2, 10);
            Heal(l, 5410, 266);
            Heal(l, 6020, 266);
            Check(l, 5400, 300);
            Check(l, 6010, 300);
            Check(l, 6500, 300);
            Sign(l, 5350, 165, "反应炉外壁正在失压 · 亮面地板会坍塌，沿上层检修道前进","crumble");
            Sign(l, 5980, 177, "核心就在前方 · 反应炉尽头通向一座古堡");
            Sign(l, 6360, 176, "前方血月古堡 · 靠近大门按 F 进入");
            // The boss agent uses this marker as a stable trigger/arena start;
            // keep it on a plain platform so normal play can reach it.
            l.ExitX = 7140;
            l.ExitY = 236;
            ExtendReactor(l);
            return l;
        }

        // Every new district has its own movement question. Stable floors and
        // checkpoints introduce a mechanic before gaps and enemies combine it.
        private static void ExtendStreet(Level l)
        {
            l.Width=13040; l.ExitX=12950;
            l.Subtitle="第一章 · 01 / NEON OUTSKIRTS";
            l.Briefing="穿过霓虹旧城与六片新区，探索电梯支路，回收 3 枚核心前往晶片商店。";
            MoveLastCore(l,12770,266);

            // A residential lift teaches calling and deliberate floor choice.
            Ground(l,6510,230,300); Lift(l,6740,112,300,218,138);
            Ground(l,6852,648,300); Ledge(l,6852,138,202); Ledge(l,6610,218,130);
            Ledge(l,7100,206,112); Ledge(l,7265,242,115);
            Electric(l,7030,300,90,4.5f,2.2f); Enemy(l,7350,272,7300,7450,0);
            Shards(l,6868,110,5,30); Ammo(l,6985,110,2,6); Heal(l,7410,266);
            Check(l,6560,300); Check(l,7440,300);
            Sign(l,6560,116,"居住塔 · 轿厢内按 F 换层，楼层边按 F 呼叫电梯","elevator");
            Sign(l,7170,143,"电网先亮黄色预警 · 熄灭时通过，也可以二段跳越过","electric");

            // Advertisement roofs: a decoy floor falls onto a recovery shelf.
            Ground(l,7500,200,300); FalseFloor(l,7700,300,92); FalseFloor(l,7792,300,92);
            FalseFloor(l,7884,300,116); Ledge(l,7740,351,250);
            Ledge(l,7645,226,120); Ledge(l,7820,175,116); Ledge(l,7990,206,115);
            Ground(l,8000,260,300); Moving(l,8330,274,112,1,40,.75f,.4f);
            Ground(l,8470,140,300); Saw(l,8090,260,28,1,46,1.1f,.9f);
            Enemy(l,8100,148,8030,8230,1); Shards(l,7840,147,3,28);
            Shards(l,7755,324,5,38); Ammo(l,7998,174,1,12); Check(l,8510,300);
            Electric(l,8260,300,78,4.2f,1.6f);
            Sign(l,7550,134,"广告桥 · 完整地板也可能塌陷，下方维修架可以救你一次","crumble");

            // Pressure plates teach reading a wired trap before a paired version.
            Ground(l,8610,370,300); Ground(l,9080,630,300);
            Ledge(l,8880,220,116); Moving(l,8970,281,96,2,23,.8f,0);
            Pressure(l,8735,300,55,8840,110,38);
            Pressure(l,9250,300,58,9360,120,48); Pressure(l,9250,300,58,9520,100,48);
            Ledge(l,9200,224,108); Ledge(l,9370,174,116); Ledge(l,9530,215,116);
            Enemy(l,9650,272,9610,9690,0); Shards(l,9388,146,3,29); Ammo(l,9550,183,2,6);
            Heal(l,9140,266); Check(l,9170,300);
            Sign(l,8670,143,"输电渠 · 踩到黄黑压板会给前方电网充能，预警后才会放电","pressure-trap");
            Sign(l,9430,117,"一块压板连接两道电网 · 上层路线有额外晶片","pressure-trap");

            // Tram depot alternates conveyor direction and changes elevation.
            Ground(l,9710,210,300); Belt(l,9920,300,350,80);
            Ground(l,10270,160,300); Belt(l,10430,280,350,-65);
            Ledge(l,9860,229,112); Ledge(l,10060,182,140); Ledge(l,10300,217,112);
            Flame(l,10035,300,4.6f,2.4f); Crusher(l,10530,280,4.6f,2.2f);
            Enemy(l,10340,272,10285,10400,0); Shards(l,10077,153,4,28);
            Ammo(l,10322,186,1,12); Check(l,10295,300);
            Sign(l,9750,135,"停运电车场 · 顺向带借力起跳，逆向带前先寻找落脚点","conveyor");

            // Vertical service block offers a high reward route and a gate below.
            Ground(l,10780,260,300); Lift(l,11040,112,300,220,140);
            Ground(l,11152,568,300); Ground(l,11820,140,300);
            Ledge(l,11152,140,208); Ledge(l,11500,186,130); Ledge(l,11680,224,118);
            Moving(l,11730,284,88,1,24,.85f,1.1f);
            Gate(l,11380,300,11,11240); Electric(l,11485,300,104,4.4f,.1f);
            Enemy(l,11655,272,11600,11700,0); Shards(l,11165,112,6,30);
            Ammo(l,11530,155,2,8); Heal(l,10855,266); Check(l,10840,300); Check(l,11870,300);
            Sign(l,10830,129,"通讯维修塔 · 高层有晶片补给，下层终端可开启防护闸门","console");
            Sign(l,11545,133,"第二座电梯检验你的选层技巧 · 无需反复等待技能冷却","elevator");

            // A final rooftop sequence tests jumps; the last platform is calm.
            Ground(l,11960,180,300); Ledge(l,12240,264,124);
            Ledge(l,12420,220,132); Moving(l,12570,269,116,2,26,.75f,.5f);
            Ledge(l,12100,215,115); Ledge(l,12285,171,110);
            Ground(l,12700,340,300); Pressure(l,11995,300,48,12065,65,33);
            Saw(l,12380,299,24,1,32,1.0f,.4f);
            Enemy(l,12490,144,12430,12620,1); Shards(l,12304,143,3,29);
            Shards(l,12440,192,3,29); Ammo(l,12730,266,1,14); Heal(l,12715,266);
            Check(l,12710,300);
            Sign(l,12025,116,"屋顶快线 · 先站稳再起跳，远处金色核心通向城区出口");
            Sign(l,12790,160,"第一章第一节结束 · 收集的晶片可在补给站升级装备");

            // Physical puzzle before the final roof run: push the service
            // crate into its socket to open the gate.
            Ground(l,11720,200,300);
            PushBox(l,11745,300,401);
            // Keep the socket left of the closed gate so the crate can reach it
            // without colliding with the gate before the puzzle is solved.
            PuzzleGate(l,11815,300,401,3,11745,300,11770,300);
            // Solid landing space beyond the gate lets the player safely
            // clear the docked crate before the following rooftop jump.
            Sign(l,11710,114,"维修箱解锁 · 推箱子压住蓝色插槽，再穿过闸门","crate");
            Mine(l,12330,300);
        }

        private static void ExtendCargo(Level l)
        {
            l.Width=12880; l.ExitX=12800;
            l.Subtitle="第一章 · 02 / SKYLINE FREIGHT";
            l.Briefing="穿越货运塔、分拣厂与坍塌管廊，集齐核心后到末端空地与同伴会合。";
            MoveLastCore(l,12220,266);

            // Freight lift: a lower walkable yard and three separate landings.
            Ground(l,6440,180,300); Lift(l,6620,116,300,220,140);
            Ground(l,6736,584,300); Ledge(l,6736,140,196); Ledge(l,6485,220,135);
            Ledge(l,7030,201,120); Ground(l,7420,100,300);
            Moving(l,7330,280,90,1,25,.9f,.2f); Electric(l,6970,300,100,4.7f,2.6f);
            Pressure(l,7200,300,44,7275,72,34);
            Enemy(l,7130,272,7080,7260,0); Shards(l,6750,112,6,29);
            Ammo(l,6910,110,2,8); Check(l,6470,300); Check(l,7440,300);
            Sign(l,6460,127,"货运调度塔 · F 呼叫电梯，到站后可继续换层","elevator");

            // Gantry crossing: large moving decks, staggered shelves and rescue ledges.
            Ledge(l,7580,249,130); Moving(l,7770,240,126,1,58,.7f,.5f);
            Ledge(l,7980,219,138); Ledge(l,8170,273,146);
            Lift(l,8380,116,300,220,140); Ground(l,8496,164,300);
            Ledge(l,8110,350,130); Ledge(l,8260,333,120); Ledge(l,8496,140,128);
            Ledge(l,7410,210,110); Shards(l,7602,220,3,30); Shards(l,8000,190,3,30);
            Shards(l,8510,112,4,28); Ammo(l,8195,242,1,14); Heal(l,8520,266);
            Enemy(l,7980,147,7890,8100,1); Saw(l,8265,242,26,2,30,1.05f,.4f);
            Check(l,8525,300); Sign(l,7570,135,"磁轨吊臂 · 先落到宽平台，再借移动方向换乘","moving");
            Sign(l,8250,101,"下方检修架是补救路线 · 右侧电梯可返回上层货架","elevator");

            // Sorting factory: short conveyor belts feed separate crushers.
            Belt(l,8660,300,280,92); Ground(l,8940,170,300);
            Belt(l,9110,300,260,-85); Ground(l,9370,320,300);
            Ledge(l,8740,212,110); Ledge(l,8930,169,128); Ledge(l,9170,218,126);
            Crusher(l,8850,300,4.3f,1.7f); Crusher(l,9250,300,4.8f,.5f);
            Pressure(l,9400,300,48,9500,110,43);
            Enemy(l,9050,272,8960,9080,0); Enemy(l,9550,182,9470,9650,1);
            Shards(l,8945,140,4,28); Ammo(l,9188,188,2,8); Check(l,8980,300);
            Sign(l,8690,135,"分拣厂 · 传送带会把你送向压机，在上层平台等待一轮","conveyor");

            // A wind-exposed power spine varies laser timing and platform height.
            Ground(l,9690,180,280); Moving(l,9920,244,126,2,36,.72f,.7f);
            Pressure(l,9720,280,45,9790,70,36);
            Ledge(l,10135,196,148); Ground(l,10400,250,300);
            Moving(l,10715,261,112,1,36,.8f,.6f); Ground(l,10900,160,300);
            Ledge(l,10225,337,146); Electric(l,10162,196,88,4.9f,2.2f);
            Laser(l,10495,144,300,4.7f,2.8f);
            Shards(l,9950,163,3,28); Shards(l,10247,310,4,29); Ammo(l,10425,266,1,14);
            Enemy(l,10700,152,10600,10820,1); Check(l,10420,300); Check(l,10920,300);
            Sign(l,9730,128,"高空输电脊 · 电场亮黄时先等，带电平台下有维修架","electric");

            // Abandoned tunnel: two ways through a visibly supported ruin.
            Ground(l,11060,190,300); FalseFloor(l,11250,300,110); FalseFloor(l,11360,300,100);
            Ledge(l,11260,349,190); Ground(l,11520,220,300);
            Ledge(l,11150,224,116); Ledge(l,11325,179,122); Ledge(l,11530,211,130);
            Moving(l,11790,278,120,1,31,.8f,.5f); Ground(l,11950,190,300);
            Flame(l,11655,300,4.5f,2.5f); Pressure(l,11975,300,42,12050,60,32);
            Enemy(l,11560,272,11535,11630,0); Shards(l,11345,150,3,30);
            Shards(l,11285,322,4,34); Ammo(l,11545,181,2,8); Heal(l,11965,266);
            Check(l,11080,300); Sign(l,11100,120,"废弃运输管廊 · 下路会塌陷，上路可绕开喷火口","crumble");

            // Keep this whole clearing free of enemies, traps and moving parts.
            // The chase system owns the friend, bike and trigger at ExitX-180.
            Ground(l,12140,740,300); Check(l,12165,300); Heal(l,12290,266);
            Ammo(l,12340,266,1,16); Ammo(l,12390,266,2,8);
            Shards(l,12440,263,5,28);
            Sign(l,12250,151,"末端会合点 · 带齐三枚核心，前往空地与同伴会合");

            // Moving target puzzle: hit the drifting target to release the
            // freight lock before the motorcycle clearing.
            MovingTargetGate(l,12310,268,34,402,4,12425,300);
            Sign(l,12300,183,"移动靶校准 · 开火击中漂移靶，解除货运锁","target");
        }

        private static void ExtendReactor(Level l)
        {
            // Relocate the COMPLETE castle approach, including supplies and
            // checkpoints. The combat camera and architecture remain ExitX based.
            ShiftTail(l,6000,7200);
            l.Width=14400; l.ExitX=14340;
            l.Subtitle="第一章 · 03 / THE BLOODMOON CASTLE";
            l.Briefing="穿过冷却站与失压管廊，携带 3 枚核心进入血月古堡，终结女皇的封印。";
            foreach(Signpost sign in l.Signs)if(sign.X==5980)sign.Text="冷却站已开放 · 穿过六片新区后才能抵达血月古堡";

            // Coolant station starts with room to learn an elevator/electric combo.
            Ground(l,6000,280,300); Lift(l,6280,116,300,220,140);
            Ground(l,6396,654,300); Ledge(l,6396,140,198); Ledge(l,6135,220,145);
            Ledge(l,6730,218,124); Electric(l,6515,300,100,4.7f,2.5f);
            Electric(l,6840,300,90,4.6f,1.4f); Enemy(l,6950,272,6935,7030,0);
            Ammo(l,6495,110,2,8); Shards(l,6410,112,5,31); Heal(l,6050,266);
            Check(l,6040,300); Check(l,6980,300);
            Sign(l,6070,112,"冷却站 · 先从电梯登高，检查电网闪烁的节奏","elevator");

            // Data bridge alternates solid islands with unstable main flooring.
            Ground(l,7050,200,300); FalseFloor(l,7250,300,110); FalseFloor(l,7360,300,110);
            Ground(l,7470,220,300); FalseFloor(l,7690,300,120); FalseFloor(l,7810,300,110);
            Ground(l,7920,330,300); Ledge(l,7280,352,160); Ledge(l,7740,351,160);
            Ledge(l,7170,223,114); Ledge(l,7350,177,122); Ledge(l,7560,224,128);
            Moving(l,7760,182,116,1,35,.73f,.8f); Ledge(l,8010,221,132);
            Pressure(l,7970,300,48,8110,94,42); Enemy(l,7590,152,7500,7720,1);
            Shards(l,7370,147,3,30); Shards(l,7300,324,4,32); Ammo(l,8040,190,1,14);
            Check(l,7950,300); Sign(l,7100,135,"数据桥 · 完整的短岛可以歇脚，发亮的长板下藏有裂缝","crumble");

            // Pressure foundry: high and low routes exchange mechanical hazards.
            Belt(l,8250,300,310,74); Ground(l,8560,190,300); Belt(l,8750,280,300,-64);
            Ground(l,9050,390,300); Ledge(l,8335,219,124); Ledge(l,8520,173,138);
            Ledge(l,8780,206,124); Ledge(l,9120,220,135);
            Crusher(l,8450,300,4.7f,1.8f); Flame(l,8880,280,4.9f,2.8f);
            Crusher(l,9190,300,4.6f,.9f); Electric(l,9300,300,90,5.0f,2.9f);
            Enemy(l,8620,272,8580,8720,0); Shards(l,8540,143,4,29);
            Ammo(l,8810,176,2,8); Heal(l,9060,266); Check(l,9080,300);
            Sign(l,8290,124,"失压铸造厂 · 高层绕过落锤，低层通过前先关闭机关","trap-cycle");

            // Twin lift shaft: high staging decks and a safe lower recovery route.
            Ground(l,9440,170,300); Lift(l,9610,116,300,220,140);
            Ground(l,9726,294,300); Ledge(l,9726,140,208);
            Ledge(l,10075,193,135); Moving(l,10265,236,114,1,35,.74f,.7f);
            Lift(l,10435,116,300,220,140); Ground(l,10551,109,300);
            Ledge(l,10005,352,170); Ledge(l,10200,321,168); Ledge(l,10551,140,130);
            Electric(l,9800,300,90,5.1f,2.2f); Enemy(l,10205,143,10145,10330,1);
            Shards(l,9745,111,5,31); Shards(l,10020,324,4,31); Ammo(l,10574,112,1,18);
            Check(l,9470,300); Check(l,10565,300);
            Sign(l,9480,105,"中央双塔 · 换乘电梯穿过上层，失足后可从检修架返回","elevator");

            // Blackout corridor connects one pressure plate to separated fields.
            Ground(l,10660,420,300); Ground(l,11185,360,300); Ground(l,11645,200,300);
            Moving(l,11075,267,112,2,24,.81f,1.1f); Moving(l,11555,275,100,1,25,.8f,.7f);
            Pressure(l,10720,300,50,10835,96,47); Pressure(l,10720,300,50,10980,90,47);
            Pressure(l,11210,300,50,11320,100,42); Pressure(l,11210,300,50,11460,72,42);
            Ledge(l,10815,208,120); Ledge(l,10970,164,112); Ledge(l,11240,211,130);
            Ledge(l,11640,221,120); Enemy(l,11720,272,11670,11810,0);
            Ammo(l,11255,181,2,8); Shards(l,10988,135,3,27); Shards(l,11655,193,3,29);
            Check(l,11195,300); Sign(l,10690,132,"断电管廊 · 一次踩踏连锁放电，沿电缆辨认危险范围","pressure-trap");

            // Quiet stoneward climb changes cadence before the preserved castle.
            Ground(l,11845,225,300); Ledge(l,12130,259,145); Ledge(l,12345,213,138);
            Ground(l,12580,250,300); Ledge(l,12200,350,154);
            Moving(l,12498,274,110,2,25,.75f,.4f); Ground(l,12930,270,300);
            Ledge(l,12820,240,118); Ledge(l,12620,210,132);
            Electric(l,12675,300,85,4.9f,3.0f); Saw(l,12855,292,25,1,25,.85f,.3f);
            Enemy(l,12375,141,12310,12530,1); Shards(l,12360,183,3,30);
            Shards(l,12634,179,4,29); Ammo(l,12596,266,1,16); Ammo(l,13010,266,2,8);
            Heal(l,11890,266); Heal(l,12970,266); Check(l,11880,300); Check(l,12960,300);
            Sign(l,11905,134,"城郊旧道 · 霓虹渐远，沿石阶寻找最后一枚核心");
            Sign(l,12970,147,"古堡门禁需要三枚核心 · 前方补给可留给女皇战");

            // A final crate socket makes the approach ask for positioning,
            // rather than allowing a straight sprint to the castle.
            Ground(l,13080,260,300);
            PushBox(l,13125,300,403);
            // The closed gate starts at 13220; place the socket on its left
            // side so the box can be pushed fully into the target.
            PuzzleGate(l,13220,300,403,3,13125,300,13175,300);
            // Leave a readable landing after the service door.  The original
            // beam touched the door's far edge and the patrol crossed into
            // the crate area, hiding damage inside the puzzle interaction.
            foreach(Hazard hazard in l.Hazards)if(hazard.Type==1&&Math.Abs(hazard.X-13240)<.01f)
                hazard.X=hazard.BaseX=13325;
            foreach(EnemySpawn enemy in l.Enemies)if(enemy.Type==0&&Math.Abs(enemy.X-13250)<.01f){
                enemy.X=13310;enemy.MinX=13290;enemy.MaxX=13370;
            }
            Mine(l,12795,300);
            Sign(l,13070,116,"检修闸 · 把重箱推入插槽，别被地雷逼回去","crate");
        }

        private static void MoveLastCore(Level l,float x,float y)
        {
            Pickup last=null;
            foreach(Pickup p in l.Pickups)if(p.Type==1&&(last==null||p.X>last.X))last=p;
            if(last!=null){last.X=x;last.Y=y;}
        }

        private static void ShiftTail(Level l,float start,float delta)
        {
            foreach(Platform p in l.Platforms)if(p.BaseX>=start){p.X+=delta;p.BaseX+=delta;p.PrevX+=delta;}
            foreach(Hazard h in l.Hazards)if(h.X>=start){h.X+=delta;h.BaseX+=delta;}
            foreach(EnemySpawn e in l.Enemies)if(e.X>=start){e.X+=delta;e.MinX+=delta;e.MaxX+=delta;}
            foreach(Pickup p in l.Pickups)if(p.X>=start)p.X+=delta;
            foreach(Checkpoint c in l.Checkpoints)if(c.X>=start)c.X+=delta;
            foreach(Signpost s in l.Signs)if(s.X>=start)s.X+=delta;
            foreach(Terminal t in l.Terminals)if(t.X>=start)t.X+=delta;
        }

        private static Platform Lift(Level l,float x,float width,params float[] stops)
        {
            Platform p=new Platform{X=x,BaseX=x,PrevX=x,Y=stops[0],BaseY=stops[0],PrevY=stops[0],
                W=width,H=16,OneWay=true,Kind=5,LiftStops=stops};
            l.Platforms.Add(p);return p;
        }

        private static void Belt(Level l,float x,float y,float width,float speed)
        { Platform p=Ground(l,x,width,y);p.Kind=2;p.Belt=speed; }

        private static void Electric(Level l,float x,float floorY,float width,float period,float phase)
        {
            l.Hazards.Add(new Hazard{Type=5,X=x,Y=floorY-46,BaseX=x,BaseY=floorY-46,
                W=width,H=46,Period=period,Phase=phase});
        }

        private static void Pressure(Level l,float plateX,float floorY,float plateWidth,float x,float width,float height)
        {
            l.Hazards.Add(new Hazard{Type=6,X=x,Y=floorY-height,BaseX=x,BaseY=floorY-height,
                W=width,H=height,TriggerX=plateX,TriggerY=floorY,TriggerW=plateWidth});
        }

        private static Platform At(Level l,float x)
        {return l.Platforms.Find(delegate(Platform p){return Math.Abs(p.BaseX-x)<.01f;});}
        private static void Place(Platform p,float x,float y,float width)
        {p.X=p.BaseX=p.PrevX=x;p.Y=p.BaseY=p.PrevY=y;p.W=width;if(!p.OneWay)p.H=420-y;}
        private static void CoreAt(Level l,int index,float x,float y)
        {int count=0;foreach(Pickup p in l.Pickups)if(p.Type==1){if(count==index){p.X=x;p.Y=y;return;}count++;}}
        private static void ClearRoom(Level l,float from,float to)
        {
            l.Platforms.RemoveAll(delegate(Platform p){return p.BaseX+p.W+(p.Axis==1?p.Range:0)>from&&p.BaseX-(p.Axis==1?p.Range:0)<to;});
            l.Hazards.RemoveAll(delegate(Hazard h){return h.X+h.W>from&&h.X<to;});
            l.Enemies.RemoveAll(delegate(EnemySpawn e){return e.MaxX+28>from&&e.MinX<to;});
            l.Pickups.RemoveAll(delegate(Pickup p){return p.Type!=1&&p.X>=from&&p.X<to;});
            l.Checkpoints.RemoveAll(delegate(Checkpoint c){return c.X>=from&&c.X<to;});
            l.Terminals.RemoveAll(delegate(Terminal t){return t.X>=from&&t.X<to;});
            l.Signs.RemoveAll(delegate(Signpost s){return s.X>=from&&s.X<to;});
        }
        private static void AbilityGate(Level l,float gateX,float floor,int link,int mode,
            float switchX,float switchFloor,float returnX,float returnFloor)
        {
            float seconds=mode==1?4f:2.6f;
            l.Platforms.Add(new Platform{X=gateX,BaseX=gateX,PrevX=gateX,Y=-180,BaseY=-180,PrevY=-180,
                W=20,H=600,Kind=mode==1?6:7,Link=link,GateDuration=seconds,GateFloorY=floor});
            l.Terminals.Add(new Terminal{X=switchX,Y=switchFloor-32,Link=link,Mode=mode,Duration=seconds,TargetX=gateX+10,TargetY=floor});
            l.Terminals.Add(new Terminal{X=returnX,Y=returnFloor-32,Link=link,Mode=mode,Duration=seconds,TargetX=gateX+10,TargetY=floor});
        }
        private static void SpringRoom(Level l,float from,float to,float floor,float padX,float wallX,float top,float wallWidth,int coreIndex)
        {
            ClearRoom(l,from,to);
            Ground(l,from,wallX-from,floor);Spring(l,padX,floor-8,56);
            Ground(l,wallX,wallWidth,top);Ground(l,wallX+wallWidth,to-wallX-wallWidth,floor);
            Check(l,from+28,floor);
            CoreAt(l,coreIndex,wallX+wallWidth*.55f,top-27);
            // Three separated guide points describe the extra airborne jump.
            Shards(l,padX+55,floor-62,1,0);Shards(l,padX+97,floor-119,1,0);
            Shards(l,wallX+6,top-36,1,0);
            Sign(l,from+45,Math.Max(57,top-26),"弹射井 · 升至高处再按住空格二段跳","spring");
        }
        private static void RefineTraversal(Level l)
        {
            // Measured at 8ms with the real controller: ordinary double jump
            // rises 118.30px; spring + late airborne jump rises 171.23px.
            // The 154-156px launch rises below therefore require the spring.
            if(l.Index==0) {
                Place(At(l,0),0,300,480);Place(At(l,680),770,300,420);
                Platform ferry=At(l,590);Place(ferry,600,270,72);ferry.Range=24;ferry.Speed=.85f;
                Ledge(l,574,307,112);
                foreach(EnemySpawn e in l.Enemies)if(e.X==800)e.MinX=785;
                SpringRoom(l,3220,3900,300,3405,3575,138,170,1);
                Weapon(l,3800,272,3);

                // The last core requires riding to the high left landing, then
                // reversing direction to rejoin the main street below.
                Platform lift=At(l,11040);lift.LiftStops=new float[]{300,220,100};
                l.Platforms.RemoveAll(delegate(Platform p){return p.OneWay&&p.Kind!=5&&p.BaseX>=10800&&p.BaseX<11720;});
                Ledge(l,10855,100,185);CoreAt(l,2,10910,73);
                l.Pickups.RemoveAll(delegate(Pickup p){return p.Type!=1&&p.X>=10800&&p.X<11720;});
                Ammo(l,10990,73,2,8);
                l.Hazards.RemoveAll(delegate(Hazard h){return h.X>=11450&&h.X<11720;});
                AbilityGate(l,11700,300,101,1,11535,300,11835,300);

                // The forward pressure pad is upstairs, so simply holding right
                // cannot open the timed route. The return pad is reachable behind.
                Ledge(l,8790,205,135);
                AbilityGate(l,9120,300,102,2,8830,205,9160,300);
                l.Hazards.RemoveAll(delegate(Hazard h){return h.X>=8700&&h.X<9130;});
                Sign(l,8750,132,"踩亮上层压板 · 在光条耗尽前穿过金色闸门","pressure-gate");
                Sign(l,11495,146,"靠近线圈按 2 · 蓝色通路开启四秒","pulse-gate");
            } else if(l.Index==1) {
                Place(At(l,0),0,300,330);Place(At(l,550),650,280,310);
                Platform ferry=At(l,440);Place(ferry,465,262,80);ferry.Range=25;ferry.Speed=.78f;
                // Keep the low stepping decks from Cargo() as the visual
                // route.  The old 120px ledge sat below the ferry and made
                // the gap look like an unreachable void floor.
                l.Platforms.RemoveAll(delegate(Platform p){return p.OneWay&&Math.Abs(p.BaseX-437)<.01f;});
                foreach(EnemySpawn e in l.Enemies)if(e.X==620){e.X=680;e.MinX=665;e.MaxX=745;}
                foreach(Pickup p in l.Pickups)if(p.Type==4&&p.X==350){p.X=285;p.Y=273;}
                SpringRoom(l,3530,4200,280,3660,3830,118,170,1);
                Weapon(l,4100,252,3);

                Platform lift=At(l,8380);lift.LiftStops=new float[]{300,218,96};
                Platform shelf=At(l,7980);Place(shelf,7980,236,shelf.W);
                l.Platforms.RemoveAll(delegate(Platform p){return p.OneWay&&p.BaseX==8496;});
                Ledge(l,8190,96,190);Ground(l,8316,64,300);CoreAt(l,2,8245,69);
                l.Pickups.RemoveAll(delegate(Pickup p){return p.Type!=1&&p.X>=8180&&p.X<8640;});
                Ammo(l,8330,69,1,14);
                AbilityGate(l,9440,300,201,2,9210,218,9485,300);
                l.Hazards.RemoveAll(delegate(Hazard h){return h.X>=9380&&h.X<9680;});
                AbilityGate(l,10615,300,202,1,10455,300,10920,300);
                Sign(l,9160,142,"踩板与闸门同屏可见 · 放开踏板后倒计时开始","pressure-gate");
                Sign(l,10405,157,"脉冲线圈 · 按 2 断电，再起跳换乘吊臂","pulse-gate");
            } else {
                Place(At(l,0),0,300,380);Place(At(l,580),700,280,330);
                Platform ferry=At(l,470);Place(ferry,510,264,80);ferry.Range=30;ferry.Speed=.78f;
                Ledge(l,484,307,130);
                foreach(EnemySpawn e in l.Enemies)if(e.X==305)e.MaxX=338;
                SpringRoom(l,3790,4480,300,3980,4150,136,165,0);
                Platform lift=At(l,9610);lift.LiftStops=new float[]{300,218,100};
                l.Platforms.RemoveAll(delegate(Platform p){return p.OneWay&&p.BaseX==9726;});
                Ledge(l,9726,100,208);CoreAt(l,1,9790,73);
                Platform shelf=At(l,10075);Place(shelf,10075,238,shelf.W);
                l.Pickups.RemoveAll(delegate(Pickup p){return p.Type!=1&&p.X>=9700&&p.X<9934;});
                Ammo(l,9870,73,1,16);
                AbilityGate(l,8690,300,301,2,8380,219,8720,300);
                AbilityGate(l,11465,300,302,1,11290,300,11660,300);
                l.Hazards.RemoveAll(delegate(Hazard h){return h.Type==6&&h.X>=11190&&h.X<11545;});
                Sign(l,8310,141,"检修压板在上层 · 落地后抢在金门合拢前通过","pressure-gate");
                Sign(l,11235,140,"脉冲继电器 · 通路关闭后可再次充能重试","pulse-gate");
            }
            l.Signs.RemoveAll(delegate(Signpost s){return l.Index==0&&(s.X==8670||s.X==11545);});
            foreach(Signpost s in l.Signs){
                if(l.Index==0&&s.X==3005)s.Text="前方弹射井 · 回收高台上的核心";
                if(l.Index==0&&s.X==5790)s.Text="继续深入城区 · 核心保管在高架电梯塔";
                if(l.Index==0&&s.X==10830)s.Text="电梯到顶后向左 · 高台存放必需核心";
                if(l.Index==1&&s.X==6200)s.Text="货运新区 · 核心保管在高层货架";
                if(l.Index==1&&s.X==8250)s.Text="电梯顶层向左 · 核心藏在上层检修台";
                if(l.Index==2&&s.X==9480)s.Text="中央双塔 · 搭乘电梯到顶，向右回收核心";
            }
            NormalizeMovingRoutes(l);
            AnchorSuppliesAndPatrols(l);
            SpaceSupplies(l);
            NormalizeWeaponCaches(l);
        }

        private static void NormalizeWeaponCaches(Level l)
        {
            // Each chapter stage has one deliberate weapon discovery.  Older
            // layout revisions placed duplicate caches in optional roofs; a
            // single cache makes the once-per-run progression legible while
            // leaving the separate ammo crates untouched.
            l.Pickups.RemoveAll(delegate(Pickup p) { return p.Type == 3; });
            if (l.Index == 0) Weapon(l, 390, 184, 1);       // ion pistol
            else if (l.Index == 1) Weapon(l, 817, 150, 2);  // rail shotgun
            else Weapon(l, 2112, 136, 3);                   // arc spear
        }

        private static void NormalizeMovingRoutes(Level l)
        {
            if(l.Index==0){
                // This transfer follows the raised pressure switch. Keep the
                // deck above that upper approach, rather than letting gravity
                // carry a running player down across both banks.
                Platform timedFerry=At(l,8970);Place(timedFerry,9048,179,timedFerry.W);
            }
            if(l.Index==2){
                // This raised route begins on the 213px catwalk. Its vertical
                // ferry must remain above that bank throughout the cycle.
                Platform upperFerry=At(l,12498);Place(upperFerry,12498,167,upperFerry.W);
                // The old last crumble tile and ferry occupied the same 220px
                // opening. One broad ferry leaves room for two real jumps.
                l.Platforms.RemoveAll(delegate(Platform p){return p.BaseX==13470&&p.Kind==1;});
                Platform last=At(l,13580);Place(last,13516,274,88);last.Range=24;
            }
            for(int pass=0;pass<3;pass++)foreach(Platform ferry in l.Platforms){
                if(ferry.Axis==0)continue;
                if(ferry.Axis==1)ferry.Range=Math.Min(28,ferry.Range);
                float center=ferry.BaseX+ferry.W/2;
                Platform left=null,right=null;
                foreach(Platform shore in l.Platforms){
                    if(shore==ferry||shore.Axis>0||shore.Kind>=3)continue;
                    if(Math.Abs(shore.Y-ferry.BaseY)>32+(ferry.Axis==2?ferry.Range:0))continue;
                    if(shore.X+shore.W<=center&&(left==null||shore.X+shore.W>left.X+left.W))left=shore;
                    if(shore.X>=center&&(right==null||shore.X<right.X))right=shore;
                }
                if(left==null||right==null)continue;
                float excursion=ferry.Axis==1?ferry.Range:0;
                float leftGap=ferry.BaseX-excursion-(left.X+left.W);
                float rightGap=right.X-(ferry.BaseX+ferry.W+excursion);
                // Only nearby banks participate; remote roofs are separate routes.
                // More than the 42px running/collision-tolerance envelope, so
                // same-height banks cannot auto-land just by holding right.
                const float gap=52;
                if(leftGap<gap){
                    float shift=gap-leftGap;
                    if(left.W-shift>=64)left.W-=shift;
                    else ferry.BaseX+=shift;
                }
                rightGap=right.X-(ferry.BaseX+ferry.W+excursion);
                if(rightGap<gap){
                    float shift=gap-rightGap;
                    // Short landing shelves retain their width; relocate the
                    // whole shelf instead of leaving a precision-only sliver.
                    Place(right,right.BaseX+shift,right.BaseY,right.W-shift>=64?right.W-shift:right.W);
                }
                float lowest=Math.Min(left.Y,right.Y)-18;
                float currentLowest=ferry.BaseY+(ferry.Axis==2?ferry.Range:0);
                // Opening ferries are separately measured against the actual
                // floor jump; nearby optional teaching roofs must not raise them.
                if(pass==0&&ferry.BaseX>800&&currentLowest>lowest){
                    ferry.BaseY=lowest-(ferry.Axis==2?ferry.Range:0);
                    ferry.Y=ferry.PrevY=ferry.BaseY;
                }
                ferry.Update(0);ferry.PrevX=ferry.X;ferry.PrevY=ferry.Y;
            }
        }

        private static Platform StableSupport(Level l,float x,float floor,float radius,out float landingX)
        {
            Platform best=null;float score=float.MaxValue;landingX=x;
            foreach(Platform p in l.Platforms){
                if(p.Axis>0||p.Kind!=0||p.W<70)continue;
                float px=Math.Max(p.X+24,Math.Min(p.X+p.W-24,x));
                float dx=Math.Abs(px-x),dy=Math.Abs(p.Y-floor);
                if(dx>radius||dy>105)continue;
                float cost=dx*1.5f+dy;
                if(cost<score){best=p;landingX=px;score=cost;}
            }
            return best;
        }
        private static void AnchorSuppliesAndPatrols(Level l)
        {
            foreach(Checkpoint check in l.Checkpoints){
                float x;Platform floor=StableSupport(l,check.X+12,check.Y+32,160,out x);
                if(floor!=null){check.X=x-12;check.Y=floor.Y-32;}
            }
            foreach(Pickup item in l.Pickups){
                if(item.Type<2)continue;
                float x;Platform floor=StableSupport(l,item.X,item.Y+27,145,out x);
                if(floor==null)continue;
                // Do not stack a crate on another large pickup. Preserve its
                // ammunition amount while finding a separate place on the shelf.
                foreach(Pickup other in l.Pickups){
                    if(other==item||other.Type<2||Math.Abs(other.Y-(floor.Y-27))>35||Math.Abs(other.X-x)>=62)continue;
                    if(x+68<floor.X+floor.W-24)x+=68;
                    else if(x-68>floor.X+24)x-=68;
                }
                item.X=x;item.Y=floor.Y-27;
            }
            foreach(EnemySpawn enemy in l.Enemies){
                if(enemy.Type==1)continue;
                float x;Platform floor=StableSupport(l,enemy.X+13,enemy.Y+28,180,out x);
                if(floor==null)continue;
                enemy.X=Math.Max(floor.X+8,Math.Min(floor.X+floor.W-35,x-13));enemy.Y=floor.Y-28;
                if(enemy.Type==2)enemy.MinX=enemy.MaxX=enemy.X;
                else{
                    enemy.MinX=Math.Max(floor.X+8,Math.Min(enemy.MinX,enemy.X));
                    enemy.MaxX=Math.Min(floor.X+floor.W-35,Math.Max(enemy.MaxX,enemy.X));
                }
            }
        }

        private static bool SupplySpot(Level l,float x,float floor,bool healing)
        {
            if(healing)foreach(Checkpoint c in l.Checkpoints)if(Math.Abs(c.X-x)<180)return false;
            foreach(Hazard h in l.Hazards)if(x>h.X-45&&x<h.X+h.W+45&&floor>h.Y-20&&floor<h.Y+h.H+48)return false;
            foreach(Platform p in l.Platforms)if(p.Kind>=4&&p.Kind!=5&&Math.Abs(p.X-x)<48)return false;
            foreach(Pickup p in l.Pickups)if(p.Type==1&&Math.Abs(p.X-x)<64&&Math.Abs(p.Y-(floor-27))<50)return false;
            return true;
        }
        private static void SpaceSupplies(Level l)
        {
            // Checkpoints already restore all health. Separate healing stations
            // from them and place each one on an actual, stable landing.
            for(int i=l.Pickups.Count-1;i>=0;i--) {
                Pickup item=l.Pickups[i];if(item.Type!=2)continue;
                Platform best=null;float bx=0,score=float.MaxValue;
                foreach(Platform p in l.Platforms) {
                    if(p.Kind!=0||p.Axis!=0||p.OneWay||p.W<85||p.Y<220||p.Y>305)continue;
                    for(float x=p.X+32;x<=p.X+p.W-32;x+=26) {
                        float distance=Math.Abs(item.X-x);
                        if(distance>520||distance>=score||!SupplySpot(l,x,p.Y,true))continue;
                        bool crowded=false;
                        foreach(Pickup other in l.Pickups)if(other!=item&&other.Type>=2&&Math.Abs(other.X-x)<62&&Math.Abs(other.Y-(p.Y-27))<48){crowded=true;break;}
                        if(crowded)continue;best=p;bx=x;score=distance;
                    }
                }
                if(best==null)l.Pickups.RemoveAt(i);else{item.X=bx;item.Y=best.Y-27;}
            }
            // Sparse breadcrumbs retain the shape of a jump without turning a
            // landing into a stack of crystal, ammo, health and checkpoint icons.
            List<Pickup> retained=new List<Pickup>();
            for(int i=0;i<l.Pickups.Count;i++) {
                Pickup p=l.Pickups[i];if(p.Type!=0)continue;
                bool crowded=false;
                foreach(Pickup other in l.Pickups)if(other.Type>0&&Math.Abs(other.X-p.X)<43&&Math.Abs(other.Y-p.Y)<38){crowded=true;break;}
                if(!crowded)foreach(Pickup other in retained)if(Math.Abs(other.X-p.X)<45&&Math.Abs(other.Y-p.Y)<34){crowded=true;break;}
                if(crowded){l.Pickups.RemoveAt(i);i--;}else retained.Add(p);
            }
        }

        private static Level NewLevel(int index, int width, string name, string subtitle,
            string briefing, float exitX, float floorY)
        {
            return new Level { Index = index, Width = width, Name = name, Subtitle = subtitle,
                Briefing = briefing, ExitX = exitX, ExitY = floorY - 64 };
        }

        private static Platform Ground(Level l, float x, float width, float y)
        {
            Platform p = new Platform { X = x, BaseX = x, PrevX = x,
                Y = y, BaseY = y, PrevY = y, W = width, H = 420 - y };
            l.Platforms.Add(p);
            return p;
        }

        private static void Ledge(Level l, float x, float y, float width)
        {
            l.Platforms.Add(new Platform { X = x, BaseX = x, PrevX = x,
                Y = y, BaseY = y, PrevY = y, W = width, H = 14, OneWay = true });
        }

        private static void Moving(Level l, float x, float y, float width,
            int axis, float range, float speed, float phase)
        {
            Platform p = new Platform { BaseX = x, BaseY = y, W = width, H = 16,
                Axis = axis, Range = range, Speed = speed, Phase = phase, OneWay = true };
            p.Update(0);
            p.PrevX = p.X;
            p.PrevY = p.Y;
            l.Platforms.Add(p);
        }

        private static void Enemy(Level l, float x, float y, float min, float max, int type)
        {
            l.Enemies.Add(new EnemySpawn { X = x, Y = y, MinX = min, MaxX = max, Type = type });
        }

        private static void Crumble(Level l, float x, float y, float width)
        {
            l.Platforms.Add(new Platform { X = x, BaseX = x, PrevX = x,
                Y = y, BaseY = y, PrevY = y, W = width, H = 16,
                OneWay = true, Kind = 1 });
        }

        private static void FalseFloor(Level l, float x, float y, float width)
        {
            // Solid sides and a ground-height facade make this look like the
            // adjacent street. It cracks only when the player stands on it.
            l.Platforms.Add(new Platform { X = x, BaseX = x, PrevX = x,
                Y = y, BaseY = y, PrevY = y, W = width, H = 420 - y,
                OneWay = false, Kind = 1, Disguised = true });
        }

        private static void Spring(Level l, float x, float y, float width)
        {
            l.Platforms.Add(new Platform { X = x, BaseX = x, PrevX = x,
                Y = y, BaseY = y, PrevY = y, W = width, H = 8,
                OneWay = true, Kind = 3 });
        }

        private static void Gate(Level l, float x, float floorY, int link, float terminalX)
        {
            l.Platforms.Add(new Platform { X = x, BaseX = x, PrevX = x,
                Y = 52, BaseY = 52, PrevY = 52, W = 20, H = floorY - 52,
                Kind = 4, Link = link });
            l.Terminals.Add(new Terminal { X = terminalX, Y = floorY - 32, Link = link });
        }

        private static void PushBox(Level l, float x, float floorY, int link)
        {
            l.Platforms.Add(new Platform { X = x, BaseX = x, PrevX = x,
                Y = floorY - 26, BaseY = floorY - 26, PrevY = floorY - 26,
                W = 30, H = 26, Kind = 8, Link = link });
        }

        private static void PuzzleGate(Level l, float gateX, float floorY, int link,
            int mode, float boxX, float boxFloor, float switchX, float switchFloor)
        {
            l.Platforms.Add(new Platform { X = gateX, BaseX = gateX, PrevX = gateX,
                Y = 52, BaseY = 52, PrevY = 52, W = 20, H = floorY - 52,
                Kind = 4, Link = link });
            l.Terminals.Add(new Terminal { X = switchX, Y = switchFloor - 32,
                Link = link, Mode = mode, TargetX = gateX + 10, TargetY = floorY,
                Width = mode == 3 ? 34 : 22 });
        }

        private static void MovingTargetGate(Level l, float targetX, float targetY,
            float range, int link, int mode, float gateX, float floorY)
        {
            l.Platforms.Add(new Platform { X = gateX, BaseX = gateX, PrevX = gateX,
                Y = 52, BaseY = 52, PrevY = 52, W = 20, H = floorY - 52,
                Kind = 4, Link = link });
            l.Terminals.Add(new Terminal { X = targetX, Y = targetY,
                BaseX = targetX, BaseY = targetY, MoveRange = range, MoveSpeed = 1.7f,
                MovePhase = targetX * .013f, Link = link, Mode = mode,
                TargetX = gateX + 10, TargetY = floorY, Width = 22 });
        }

        private static void Mine(Level l, float x, float floorY)
        {
            l.Hazards.Add(new Hazard { Type = 7, X = x, Y = floorY - 10,
                BaseX = x, BaseY = floorY - 10, W = 24, H = 10, Period = .65f });
        }

        private static void Saw(Level l, float x, float y, float size,
            int axis, float range, float speed, float phase)
        {
            Hazard h = new Hazard { X = x, Y = y, BaseX = x, BaseY = y,
                W = size, H = size, Type = 2, Axis = axis, Range = range,
                Speed = speed, Phase = phase };
            h.Update(0); l.Hazards.Add(h);
        }

        private static void Flame(Level l, float x, float floorY, float period, float phase)
        {
            l.Hazards.Add(new Hazard { X = x, Y = floorY - 76, BaseX = x,
                BaseY = floorY - 76, W = 24, H = 76, Type = 3,
                Period = period, Phase = phase });
        }

        private static void Crusher(Level l, float x, float floorY, float period, float phase)
        {
            Hazard h = new Hazard { X = x, Y = floorY - 105, BaseX = x,
                BaseY = floorY - 105, W = 48, H = 38, Type = 4,
                Axis = 2, Range = 65, Speed = (float)(Math.PI * 2 / period), Phase = phase };
            h.Update(0); l.Hazards.Add(h);
        }

        private static void Spikes(Level l, float x, float floorY, float width)
        {
            l.Hazards.Add(new Hazard { X = x, Y = floorY - 12, W = width, H = 12, Type = 0 });
        }

        private static void Laser(Level l, float x, float y, float floorY, float period, float phase)
        {
            l.Hazards.Add(new Hazard { X = x, Y = y, W = 12, H = floorY - y,
                Type = 1, Period = period, Phase = phase });
        }

        private static void Core(Level l, float x, float y)
        {
            l.Pickups.Add(new Pickup { X = x, Y = y, Type = 1 });
        }

        private static void Heal(Level l, float x, float y)
        {
            l.Pickups.Add(new Pickup { X = x, Y = y, Type = 2 });
        }

        private static void Weapon(Level l, float x, float y, int id)
        {
            l.Pickups.Add(new Pickup { X = x, Y = y, Type = 3, WeaponId = id });
        }

        private static void Ammo(Level l, float x, float y, int weaponId, int amount)
        {
            l.Pickups.Add(new Pickup { X = x, Y = y, Type = 4,
                AmmoWeaponId = weaponId, AmmoAmount = amount });
        }

        private static void Shards(Level l, float x, float y, int count, float spacing)
        {
            for (int i = 0; i < count; i++)
                l.Pickups.Add(new Pickup { X = x + i * spacing, Y = y, Type = 0 });
        }

        private static void Check(Level l, float x, float floorY)
        {
            l.Checkpoints.Add(new Checkpoint { X = x, Y = floorY - 32 });
        }

        private static void Sign(Level l, float x, float y, string text, string hintKey=null)
        {
            l.Signs.Add(new Signpost { X = x, Y = y, Text = text, HintKey=hintKey });
        }
    }
}
