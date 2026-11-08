using System;
using System.Collections.Generic;
using System.Drawing;

namespace NeonRift
{
    /// <summary>Stage 3 arena boss.  The state machine is deliberately data-only so drawing can stay in BossArt.</summary>
    public sealed class RiftBoss
    {
        public float X, Y, W = 112, H = 160;
        public int HP = 120, MaxHP = 120;
        public bool Dead, Active;
        public int Skill, CastCount;
        public float SkillTimer, SkillCooldown, Telegraph, Flash, Phase, DashHitCooldown;
        public float TargetX, TargetY;
        public int Direction = -1;
        public float MoveTargetX;
        public float DefeatTime;
        public bool ShieldRollUsed;
        public int ShieldHP, LastHitDamage;
        public int ReleasedSkills;
        public bool FirstShieldUsed,PermanentShield,CavalrySummoned;
        public float ShieldWindup, ShieldTime, ShieldCheckTimer, StaggerTime, ShieldFlash, CritFlash;
        public bool ShieldPending, BloodSequenceActive, BloodSequenceClean;
        public int BloodCasts, BloodStartHealth;
        public float BloodRecoveryTime, BloodRecoveryDuration;
        public bool Awakened, AwakeningPending;
        public float AwakeningTime;
        public bool ShieldActive { get { return (PermanentShield||ShieldTime>0) && ShieldHP>0 && !Dead; } }
        public RectangleF Bounds { get { return new RectangleF(X, Y, W, H); } }
        public float CenterX { get { return X + W * .5f; } }
        public float CenterY { get { return Y + H * .43f; } }
    }

    public sealed class BossAttack
    {
        // 0: dark magic orb, 1: skeleton arrow, 2: delayed blood eruption, 3: reflected friendly orb.
        public int Kind;
        public float X, Y, VX, VY, Life, Width = 12, Age, Delay, Duration;
        public int Seed;
        // A later wave keeps its own full warning meter and two-rune identity.
        public int BloodWave;
        public float WarningDuration = 2f;
        public bool Hit;
        public Color Color = Color.FromArgb(255, 75, 180);
    }

    public sealed class SkeletonMinion
    {
        public float X, Y, VX, VY, Flash, Stun, KnockbackVX, HitTint;
        public int HP = 2, MaxHP=2, Direction = 1;
        public int Role;
        public float Emerge, AttackWindup, AttackCooldown, AttackTime, TargetX, TargetY, WalkTime, ChargeLockX;
        public bool Dead;
        public int CombatState;
        public float StateTime;
        public bool DashHitLatched,AttackLanded,FirstChargePending;
        public RectangleF Bounds { get { return Role==2?new RectangleF(X+4,Y+4,64,58):new RectangleF(X + 3, Y + 3, 18, 27); } }
        public RectangleF DashTargetBounds
        {
            get {
                if (Emerge <= 0) return Bounds;
                if(Role==2)return Emerge>.65f?RectangleF.Empty:new RectangleF(X+5,Y+24,62,38);
                // Match the clipped skull/body pixels in DrawSkeleton; a bare grave is not a target.
                if (1 - Emerge / 1.6f < .27f) return RectangleF.Empty;
                float offset = (float)Math.Floor(Emerge / 1.6f * 41);
                return new RectangleF(X + 5, Y + offset, 16, Math.Max(0, 30 - offset));
            }
        }
    }

    public sealed partial class GameWorld
    {
        public RiftBoss Boss;
        public readonly List<BossAttack> BossAttacks = new List<BossAttack>();
        public readonly List<SkeletonMinion> Skeletons = new List<SkeletonMinion>();
        public readonly List<PointF> BossOrbOrigins = new List<PointF>();
        public const float BossIntroDuration = 3.2f;
        public const float BossOutroDuration = 4.8f;
        public const int BossShieldMaxHP = 12;
        public const float BossShieldDuration = 10f, BossShieldWindupDuration = .8f, BossStaggerDuration = 4f;
        public const float BossShieldCheckInterval = 20f, BossShieldChance = .60f;
        public const float BossBloodRecoveryDuration = 2.4f;
        public const float BossAwakeningDuration = QueenAwakeningSequence.Duration;
        public static readonly float[] BossBloodWaveStarts={2f,3.6f,5.04f,6.34f,7.54f};
        public bool BossAwakeningActive { get { return Boss!=null&&Boss.Active&&!Boss.Dead&&Boss.AwakeningTime>0; } }
        public float BossIntroRemaining;
        public bool BossIntroActive { get { return BossIntroRemaining > 0 && BossArenaLocked; } }
        public bool BossOutroActive { get { return Boss != null && Boss.Dead && Boss.Active; } }
        public float BossOutroProgress { get { return Boss == null ? 0 : Math.Min(1, Boss.DefeatTime / BossOutroDuration); } }
        public bool BossCinematicActive { get { return CastleEntryActive || QueenArrivalActive || BossIntroActive || BossFinalStrikeActive || BossOutroActive || BossAwakeningActive || EndingActive; } }
        public bool BossArenaLocked { get { return Boss != null && Boss.Active; } }
        public bool BossDefeated { get { return Boss != null && Boss.Dead && !Boss.Active; } }
        public float BossArenaLeft = 6540, BossArenaRight = 7180;
        public float BossTriggerX { get { return Level == null ? 0 : Math.Max(0, Level.ExitX - 560); } }

        public void TickBossIntro(float dt)
        {
            BossIntroRemaining = Math.Max(0, BossIntroRemaining - dt);
        }

        public bool TickBossAwakening(float dt)
        {
            if(!BossAwakeningActive)return false;
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return true;
            float before=QueenAwakeningSequence.Elapsed(Boss);
            Boss.AwakeningTime=Math.Max(0,Boss.AwakeningTime-dt);
            float after=QueenAwakeningSequence.Elapsed(Boss);
            if(before<QueenAwakeningSequence.RoarAt&&after>=QueenAwakeningSequence.RoarAt)Sound.Play("queen_moon_roar");
            if(before<QueenAwakeningSequence.MoonAt&&after>=QueenAwakeningSequence.MoonAt)Sound.Play("queen_moonlight");
            if(before<QueenAwakeningSequence.RelightAt&&after>=QueenAwakeningSequence.RelightAt)Sound.Play("queen_candles_return");
            Camera=BossArenaLeft;Hero.VX=0;Hero.VY=0;Hero.DashTime=0;
            if(Boss.AwakeningTime<=0) {
                Boss.PermanentShield=true;Boss.ShieldPending=true;
                Boss.SkillCooldown=1.0f;Boss.MoveTargetX=Boss.X>BossArenaLeft+270?BossArenaLeft+105:BossArenaRight-Boss.W-85;
                Hero.Invincible=Math.Max(Hero.Invincible,.6f);Sound.Play("queen_awakened");
                Show("血月真身 · 十二魔弹，五重血潮，永续血盾",2.5f);
            }
            return true;
        }

        private void BeginBossAwakening()
        {
            Boss.Awakened=true;Boss.AwakeningPending=false;Boss.AwakeningTime=BossAwakeningDuration;
            Boss.Skill=0;Boss.SkillTimer=Boss.Telegraph=0;Boss.BloodSequenceActive=false;
            Boss.BloodRecoveryTime=Boss.BloodRecoveryDuration=0;Boss.CastCount=0;
            Boss.Flash=Boss.ShieldFlash=Boss.CritFlash=0;Boss.LastHitDamage=0;
            Boss.ShieldPending=false;Boss.ShieldWindup=Boss.ShieldTime=0;Boss.ShieldHP=0;
            BossAttacks.Clear();BossOrbOrigins.Clear();Shots.Clear();Bullets.Clear();
            foreach(SkeletonMinion s in Skeletons){s.AttackTime=s.AttackWindup=s.VX=0;s.AttackCooldown=Math.Max(s.AttackCooldown,1.2f);}
            Hero.VX=Hero.VY=Hero.DashTime=0;Hero.JumpBuffer=0;CancelWeaponAction();Camera=BossArenaLeft;
            Particles.Clear();ClearDashEffects();PulseVisual=0;CombatHitStop=0;Shake=0;PopupTimer=0;
            Sound.Play("queen_candles_out");
        }

        private void BeginBossOutro()
        {
            Boss.Dead = true; Boss.HP = 0; Boss.DefeatTime = 0;
            Boss.Skill = 0; Boss.Telegraph = 0; Boss.SkillTimer = 0; Boss.Flash = 0;
            Boss.ShieldHP = 0; Boss.ShieldTime = 0; Boss.ShieldWindup = 0; Boss.ShieldCheckTimer = 0; Boss.StaggerTime = 0; Boss.CritFlash = 0;
            Boss.ShieldPending = false; Boss.BloodSequenceActive = false; Boss.BloodRecoveryTime = 0;
            Boss.AwakeningPending=false;Boss.AwakeningTime=0;
            BossIntroRemaining = 0; BossAttacks.Clear(); Skeletons.Clear(); BossOrbOrigins.Clear();
            Bullets.Clear(); Particles.Clear(); PopupTimer = 0; PulseVisual = 0; Shake = 0;
            Hero.VX = 0; Hero.VY = 0; Hero.DashTime = 0; Hero.AttackTime = 0;
            Hero.JumpBuffer = 0; Camera = BossArenaLeft;
            Sound.Play("queen_defeat");
            BeginChapterEnding();
        }

        public void TickBossOutro(float dt)
        {
            if(EndingActive){TickChapterEnding(dt);return;}
            if (!BossOutroActive) return;
            // Only this cinematic clock advances; gameplay remains completely still.
            Shots.Clear(); Camera = BossArenaLeft;
            float before = Boss.DefeatTime;
            Boss.DefeatTime = Math.Min(BossOutroDuration, Boss.DefeatTime + Math.Max(0, dt));
            if (before < 1.6f && Boss.DefeatTime >= 1.6f) Sound.Play("seal_break");
            if (Boss.DefeatTime >= BossOutroDuration)
            {
                Boss.Active = false;
                Hero.Invincible = Math.Max(Hero.Invincible, 1f);
                Show("女皇已消散 · 血之封印解除，前往出口", 3.5f);
                Sound.Play("queen_release");
            }
        }

        /// <summary>Called from GameWorld.Tick after player movement. It is safe to call every frame.</summary>
        public void TickBoss(float dt)
        {
            if (Level == null || Level.Index != 2) return;
            if (Boss == null || !Boss.Active) return;
            if (BossCinematicActive) return;

            Boss.Flash = Math.Max(0, Boss.Flash - dt);
            Boss.DashHitCooldown = Math.Max(0, Boss.DashHitCooldown - dt);
            if (!Boss.Dead)
            {
                // Her body is harmless except while the visible blood shield is active.
                Hero.X = Math.Max(BossArenaLeft + 12, Math.Min(BossArenaRight - 44, Hero.X));
                if (DashIntersects(Boss.Bounds) && Boss.DashHitCooldown <= 0)
                { HitBoss(4); Boss.DashHitCooldown = .3f; }
                if (BossCinematicActive) return;
                if (Boss.StaggerTime > 0) { TickSkeletons(dt); return; }
                if (Boss.ShieldWindup > 0) { TickSkeletons(dt); return; }
                if (Boss.ShieldActive && BossShieldTouches(Hero.Bounds)) Damage(1, Boss.CenterX);
                if (Boss.BloodSequenceActive)
                {
                    TickBossAttacks(dt); TickSkeletons(dt);
                    if (!BossAttacks.Exists(delegate(BossAttack a) { return a.Kind == 2; })) EndBloodSequence();
                    return;
                }
                Boss.Phase += dt;
                Boss.Direction = Hero.X + 12 >= Boss.CenterX ? 1 : -1;
                if (Boss.Skill == 0&&!Boss.ShieldActive)
                {
                    if (Math.Abs(Boss.X - Boss.MoveTargetX) < 2)
                        Boss.MoveTargetX = Boss.X > BossArenaLeft + 265 ? BossArenaLeft + 92 : BossArenaRight - Boss.W - 74;
                    Boss.X = Approach(Boss.X, Boss.MoveTargetX, (Boss.Awakened?70:52) * dt);
                }
                Boss.Y = 137 + (float)Math.Sin(Boss.Phase * 1.9f) * 3;
                Boss.SkillCooldown = Math.Max(0, Boss.SkillCooldown - dt);
                Boss.SkillTimer = Math.Max(0, Boss.SkillTimer - dt);
                Boss.Telegraph = Math.Max(0, Boss.Telegraph - dt);
                if (Boss.Skill != 0 && Boss.SkillTimer <= 0 && Boss.Telegraph <= 0)
                {
                    ReleaseBossSkill();
                    // Blood marks finish their two-second warning before another cast begins.
                    Boss.SkillCooldown = Boss.Skill == 3 ? 3.7f : (Boss.Awakened ? 1.3f : 2.0f);
                    Boss.Skill = 0;
                }
                if (Boss.Skill == 0 && Boss.SkillCooldown <= 0)
                    BeginBossSkill();
            }
            if(!Boss.Dead) { TickBossAttacks(dt); TickSkeletons(dt); }
        }

        private void StartBossArena()
        {
            // Place the seal relative to the current stage 3 exit so extended maps can move it.
            BossArenaRight = Level.ExitX + 40;
            BossArenaLeft = Math.Max(0, Level.ExitX - 600);
            Boss = new RiftBoss { X = BossArenaLeft + 440, Y = 137, Active = true, SkillCooldown = 1.45f, ShieldCheckTimer = BossShieldCheckInterval,
                MoveTargetX = BossArenaLeft + 92 };
            BossIntroRemaining = 0;
            Hero.X = Math.Max(BossArenaLeft + 26, Math.Min(BossArenaRight - 32, Hero.X));
            // Existing roaming foes become scenery outside the sealed room.
            foreach (Foe foe in Foes) if (foe.X > BossArenaLeft - 80) foe.Dead = true;
            BossAttacks.Clear(); Skeletons.Clear(); BossOrbOrigins.Clear();
        }

        private void BeginBossSkill()
        {
            if (Boss == null || Boss.Dead) return;
            // An explicit rotation guarantees all three attacks appear in every cycle.
            int kind = Boss.Awakened?new int[]{1,2,3,1,3}[Boss.CastCount%5]:1+Boss.CastCount%3;
            if(Boss.Awakened&&kind==2&&Boss.CavalrySummoned)kind=1;
            // Three per ritual, at most two live trios. Extra pressure comes
            // from timing and positioning, never an unlimited body pile.
            if(!Boss.Awakened&&kind==2&&Skeletons.FindAll(delegate(SkeletonMinion s){return !s.Dead;}).Count>3)kind=1;
            // Do not let a contact-damage bubble occupy the marked safe lanes.
            if (kind == 3 && Boss.ShieldActive&&!Boss.PermanentShield) { Boss.SkillCooldown = .2f; return; }
            Boss.CastCount++;
            Boss.Skill = kind;
            Boss.SkillTimer = kind == 1 ? (Boss.Awakened?1.25f:1.05f) : kind == 2 ? (Boss.Awakened?1.25f:.95f) : 2.0f;
            Boss.Telegraph = Boss.SkillTimer;
            Boss.TargetX = Math.Max(BossArenaLeft+26,Math.Min(BossArenaRight-26,Hero.X+12+(Boss.Awakened?Hero.VX*.22f:0)));
            Boss.TargetY = Hero.Y + 16;
            BossOrbOrigins.Clear();
            if (kind == 1)
            {
                if(Boss.Awakened) {
                    for(int n=0;n<12;n++) {
                        float angle=(float)Math.PI*(.06f+n*.88f/11);
                        BossOrbOrigins.Add(new PointF(BossArenaLeft+320-(float)Math.Cos(angle)*290,224-(float)Math.Sin(angle)*122));
                    }
                } else {
                BossOrbOrigins.Add(new PointF(BossArenaLeft + 26, Boss.Awakened?224:145));
                BossOrbOrigins.Add(new PointF(BossArenaLeft + 162, 122));
                BossOrbOrigins.Add(new PointF(BossArenaLeft + 320, 116));
                BossOrbOrigins.Add(new PointF(BossArenaRight - 162, 122));
                BossOrbOrigins.Add(new PointF(BossArenaRight - 26, Boss.Awakened?224:145));
                }
                Show(Boss.Awakened?"十二魔弹 · 预瞄印已锁定，离开交汇点":"暗黑魔弹 · 五方锁定，离开瞄准位置", 1.4f);
            }
            else if (kind == 2) Show(Boss.Awakened?"亡骸骑兵 · 警惕长枪冲锋与旋转横扫":"亡骸召唤 · 墓碑中将出现两名剑士与一名弓手", 2.8f);
            else
            {
                // A successful pulse keeps its projectile until it reaches the shield.
                BossAttacks.RemoveAll(delegate(BossAttack a) { return a.Kind != 3; });
                foreach (SkeletonMinion s in Skeletons) if(s.Role==2) { s.AttackWindup = 0; s.AttackTime = 0; s.VX = 0; }
                Show(Boss.Awakened && Boss.BloodCasts > 0
                    ? "鲜血咏唱 · 血潮将交错喷发，观察地面血印" : "鲜血咏唱 · 咏唱结束后，避开地面血印", 4.1f);
            }
            Sound.Play(kind == 1 ? "boss_aim" : kind == 2 ? "boss_summon" : "boss_chant");
        }

        private void ReleaseBossSkill()
        {
            if(Boss.Skill<1||Boss.Skill>3)return;
            Boss.ReleasedSkills++;
            if(!Boss.Awakened&&Boss.ReleasedSkills>=3&&!Boss.FirstShieldUsed)Boss.ShieldPending=true;
            if (Boss.Skill == 1)
            {
                foreach (PointF origin in BossOrbOrigins)
                {
                    float dx = Boss.TargetX - origin.X, dy = Boss.TargetY - origin.Y;
                    float len = Math.Max(1, (float)Math.Sqrt(dx * dx + dy * dy));
                    BossAttacks.Add(new BossAttack { Kind = 0, X = origin.X, Y = origin.Y,
                        VX = dx / len * (Boss.Awakened?238:205), VY = dy / len * (Boss.Awakened?238:205), Life = 4.0f, Width = 12,
                        Duration = 4.0f, Seed = random.Next(10000), Color = Color.FromArgb(206, 81, 255) });
                    Burst(origin.X, origin.Y, Color.FromArgb(209, 115, 255), 12, 65);
                }
                BossOrbOrigins.Clear();
                Sound.Play("boss_orbs");
            }
            else if (Boss.Skill == 2)
            {
                if(Boss.Awakened){SummonCavalry();return;}
                // Rejection sampling keeps every grave separate and away from the player.
                List<float> positions = new List<float>();
                for (int i = 0; i < 3; i++)
                {
                    float x = BossArenaLeft + 24;
                    bool found = false;
                    for (int attempt = 0; attempt < 100; attempt++)
                    {
                        x = BossArenaLeft + 24 + (float)random.NextDouble() * (BossArenaRight - BossArenaLeft - 70);
                        bool safe = Math.Abs(x - Hero.X) >= 66;
                        foreach (float previous in positions) if (Math.Abs(x - previous) < 104) safe = false;
                        if (safe) { found = true; break; }
                    }
                    if (!found)
                    {
                        for (float candidate = BossArenaLeft + 24; candidate < BossArenaRight - 44; candidate += 8)
                        {
                            bool safe = Math.Abs(candidate - Hero.X) >= 66;
                            foreach (float previous in positions) if (Math.Abs(candidate - previous) < 104) safe = false;
                            if (safe) { x = candidate; break; }
                        }
                    }
                    positions.Add(x);
                }
                int archerIndex = random.Next(3);
                SkeletonSpawned+=3;
                for (int i = 0; i < 3; i++)
                    Skeletons.Add(new SkeletonMinion { X = positions[i], Y = 270, Role = i == archerIndex ? 1 : 0,
                        Direction = Hero.X >= positions[i] ? 1 : -1, HP = 2, Emerge = 1.6f, AttackCooldown = .6f });
                Sound.Play("skeleton_rise");
            }
            else if (Boss.Skill == 3)
            {
                BeginBloodSequence();
            }
        }

        private void BeginBloodSequence()
        {
            bool secondHalf = Boss.Awakened;
            Boss.BloodCasts++;
            Boss.BloodSequenceActive = true; Boss.BloodSequenceClean = true; Boss.BloodStartHealth = Hero.Health;
            BossAttacks.RemoveAll(delegate(BossAttack a) { return a.Kind != 3; });
            if (!secondHalf)
            {
                float prediction = Hero.X + 12 + Hero.VX * .65f;
                prediction = Math.Max(BossArenaLeft + 177, Math.Min(BossArenaRight - 177, prediction));
                for (int i = -2; i <= 2; i++) AddBloodMark(prediction + i * 75, 38, 2f, 0);
                Show("血印已锁定 · 两秒后喷发", 2.0f);
            }
            else
            {
                // Five waves accelerate without taking away walking routes:
                // even the last .75-second gap clears an entire 98px lane.
                float lane = (BossArenaRight - BossArenaLeft - 48) / 6f;
                int firstParity = Boss.BloodCasts % 2;
                for (int wave = 0; wave < 5; wave++)for(int i=0;i<6;i++)
                {
                    if(i%2!=(firstParity+wave)%2)continue;
                    AddBloodMark(BossArenaLeft + 24 + lane * (i + .5f), lane - 18, BossBloodWaveStarts[wave], wave);
                }
                Show("五重血潮 · 血印依次加速，喷流熄灭后换位", 4.2f);
            }
        }

        private void AddBloodMark(float x, float width, float warning, int wave)
        {
            BossAttacks.Add(new BossAttack { Kind = 2, X = x, Y = 300, Delay = warning,
                WarningDuration = warning, BloodWave = wave, Duration = Boss.Awakened?.45f:.70f, Life = Boss.Awakened?.45f:.70f,
                Width = width, Seed = random.Next(10000), Color = Color.FromArgb(225, 28, 79) });
        }

        private void EndBloodSequence()
        {
            Boss.BloodSequenceActive = false;
            Boss.BloodRecoveryTime = Boss.BloodRecoveryDuration = 0;
            Boss.SkillCooldown = Boss.Awakened?.7f:1.1f;
            Boss.Skill = 0; Boss.SkillTimer = 0; Boss.Telegraph = 0;
            foreach (SkeletonMinion s in Skeletons) if(s.Role==2) { s.AttackWindup = 0; s.AttackTime = 0; s.VX = 0; }
        }

        private void TickBossAttacks(float dt)
        {
            bool bloodSoundPlayed = false;
            for (int i = BossAttacks.Count - 1; i >= 0; i--)
            {
                BossAttack a = BossAttacks[i];
                a.Age += dt;
                if(a.Kind==3) {
                    if(Boss==null||Boss.Dead){BossAttacks.RemoveAt(i);continue;}
                    float dx=Boss.CenterX-a.X,dy=Boss.CenterY-a.Y,len=Math.Max(1,(float)Math.Sqrt(dx*dx+dy*dy));
                    a.VX=dx/len*300;a.VY=dy/len*300;
                    float nx=a.X+a.VX*dt,ny=a.Y+a.VY*dt,impact;
                    if(BossSegmentHit(a.X,a.Y,nx,ny,out impact)) {
                        BossAttacks.RemoveAt(i);HitBoss(1,true);return;
                    }
                    a.X=nx;a.Y=ny;a.Life-=dt;if(a.Life<=0)BossAttacks.RemoveAt(i);
                    continue;
                }
                if (a.Kind == 2)
                {
                    if (a.Delay > 0)
                    {
                        a.Delay = Math.Max(0, a.Delay - dt);
                        if (a.Delay <= 0)
                        {
                            Burst(a.X, a.Y - 5, Color.FromArgb(238, 45, 84), 18, 100);
                            Shake = Math.Max(Shake, 3);
                            if (!bloodSoundPlayed) { Sound.Play("boss_blood"); bloodSoundPlayed = true; }
                        }
                        continue;
                    }
                    a.Life -= dt;
                    if (!a.Hit && Hero.Bounds.IntersectsWith(BloodAttackBounds(a)))
                    { a.Hit = true; Damage(1, a.X); }
                }
                else
                {
                    float endX = a.X + a.VX * dt, endY = a.Y + a.VY * dt, hit;
                    RectangleF target = Hero.Bounds;
                    target.Inflate(a.Kind == 0 ? a.Width * .5f : 2, a.Kind == 0 ? a.Width * .5f : 2);
                    if (!a.Hit && SegmentHitForBoss(a.X, a.Y, endX, endY, target, out hit))
                    { a.Hit = true; Damage(1, a.X); }
                    a.X = endX; a.Y = endY; a.Life -= dt;
                }
                if (a.Life <= 0 || (a.Hit && a.Kind != 2) || a.X < BossArenaLeft - 50 || a.X > BossArenaRight + 50 || a.Y > 330 || a.Y < -90) BossAttacks.RemoveAt(i);
            }
        }

        public static RectangleF BloodAttackBounds(BossAttack a)
        { return new RectangleF(a.X-a.Width*.5f,a.Y-104,a.Width,104); }

        private void TickSkeletons(float dt)
        {
            for (int i = Skeletons.Count - 1; i >= 0; i--)
            {
                SkeletonMinion s = Skeletons[i];
                if (s.Dead) { Skeletons.RemoveAt(i); continue; }
                s.Flash = Math.Max(0, s.Flash - dt);
                s.HitTint = Math.Max(0, s.HitTint - dt);
                if(Hero.DashTime<=0)s.DashHitLatched=false;
                if(s.Role!=2)s.Stun=0;
                if(s.Emerge<=0&&Math.Abs(s.KnockbackVX)>.1f){
                    ApplyEnemyKnockback(ref s.X,s.Y,s.Role==2?72:24,s.Role==2?62:32,ref s.KnockbackVX,dt,true);
                    s.X=Math.Max(BossArenaLeft+12,Math.Min(BossArenaRight-28,s.X));
                }
                if (HitSkeletonWithDash(s)) continue;
                if (s.Emerge > 0)
                {
                    s.Emerge = Math.Max(0, s.Emerge - dt);
                    s.VX = 0;
                    if(s.Role==2)TickCavalryEmergence(s,dt);
                    if (s.Emerge <= 0) Burst(s.X + 12, 295, Color.FromArgb(165, 115, 181), 12, 65);
                    HitSkeletonWithDash(s);
                    continue;
                }
                if(s.Role==2){TickCavalry(s,dt);continue;}
                s.WalkTime += dt * Math.Abs(s.VX) * .08f;
                s.AttackCooldown = Math.Max(0, s.AttackCooldown - dt);
                s.AttackTime = Math.Max(0, s.AttackTime - dt);
                if (s.Stun > 0)
                { s.Stun = Math.Max(0, s.Stun - dt); s.AttackWindup = 0; s.AttackTime = 0; s.VX = 0; continue; }
                float dx = Hero.X - s.X;
                if (s.AttackWindup <= 0 && s.AttackTime <= 0) s.Direction = dx >= 0 ? 1 : -1;
                if (s.AttackWindup > 0)
                {
                    s.AttackWindup = Math.Max(0, s.AttackWindup - dt);
                    s.VX = 0;
                    if (s.AttackWindup <= 0)
                    {
                        s.AttackTime = s.Role == 0 ? .18f : .22f;
                        s.AttackCooldown = s.Role == 0 ? (Boss.Awakened?1.1f:1.3f) : (Boss.Awakened?2f:2.3f);
                        if (s.Role == 0) Sound.Play("skeleton_sword");
                        else
                        {
                            float arrowX = s.X + 12, arrowY = s.Y + 12;
                            float ax = s.TargetX - arrowX, ay = s.TargetY - arrowY;
                            float length = Math.Max(1, (float)Math.Sqrt(ax * ax + ay * ay));
                            BossAttacks.Add(new BossAttack { Kind = 1, X = arrowX, Y = arrowY,
                                VX = ax / length * 205, VY = ay / length * 205, Life = 3.4f, Duration = 3.4f,
                                Width = 8, Seed = random.Next(10000), Color = Color.FromArgb(222, 202, 154) });
                            Sound.Play("skeleton_arrow");
                        }
                    }
                }
                else if (s.AttackTime <= 0)
                {
                    float targetVelocity = s.Direction * (Boss.Awakened?74:62);
                    if (s.Role == 1)
                        targetVelocity = Math.Abs(dx) < 125 ? -s.Direction * 48 : Math.Abs(dx) > 240 ? s.Direction * 56 : 0;
                    else if (Math.Abs(dx) < 34) targetVelocity = 0;
                    s.VX = Approach(s.VX, targetVelocity, 390 * dt);
                    if (s.AttackCooldown <= 0 && (s.Role == 1 ? Math.Abs(dx) < 480 : Math.Abs(dx) < 45))
                    {
                        s.TargetX = Hero.X + 12; s.TargetY = Hero.Y + 16;
                        s.AttackWindup = s.Role == 0 ? (Boss.Awakened?.44f:.50f) : (Boss.Awakened?.78f:.90f);
                        s.VX = 0;
                    }
                }
                else s.VX = 0;
                s.X += s.VX * dt;
                s.X = Math.Max(BossArenaLeft + 12, Math.Min(BossArenaRight - 28, s.X));
                if (HitSkeletonWithDash(s)) continue;
                if (s.Bounds.IntersectsWith(Hero.Bounds))
                {
                    if (Hero.VY > 85 && Hero.Bounds.Bottom < s.Y + 18)
                    { s.HP = 0; s.Dead = true; Kills++; SkeletonKilled++; AchievementProgress.RecordMinionKill(); AwardEnemyShard(s.X+11,s.Y); Hero.VY = -245; Burst(s.X + 11, s.Y + 12, Color.FromArgb(180, 196, 210), 8, 55); }
                }
                if (!s.Dead && s.Role == 0 && s.AttackTime > 0)
                {
                    RectangleF slash = new RectangleF(s.Direction > 0 ? s.X + 11 : s.X - 29, s.Y + 5, 42, 24);
                    if (slash.IntersectsWith(Hero.Bounds)) Damage(1, s.X + 12);
                }
            }
        }

        private bool HitSkeletonWithDash(SkeletonMinion s)
        {
            if (s.Dead || !DashIntersects(s.DashTargetBounds)) return false;
            if(s.Role==2) {
                if(s.DashHitLatched)return false;
                s.DashHitLatched=true;HitSkeleton(s,4,Hero.X+12,true);
                DashImpactTime=.45f;DashImpactX=s.X+36;DashImpactY=s.Y+15;Sound.Play("dash_hit");
                return s.Dead;
            }
            s.HP = 0; s.Dead = true; s.AttackTime = 0; s.AttackWindup = 0; Kills++; SkeletonKilled++; AchievementProgress.RecordMinionKill();
            AwardEnemyShard(s.X+11,s.Y);
            DashImpactTime = .45f; DashImpactX = s.X + 12; DashImpactY = s.Y - 8;
            Burst(s.X + 11, s.Y + 12, Color.FromArgb(216, 224, 201), 12, 78);
            Burst(s.X + 12, s.Y + 17, Color.FromArgb(91, 244, 255), 10, 92);
            Sound.Play("dash_hit");
            return true;
        }

        public void HitBoss(int damage)
        { HitBoss(damage, false); }

        public void HitBoss(int damage, bool ranged)
        {
            if (Boss == null || !Boss.Active || Boss.Dead || BossCinematicActive || damage <= 0) return;
            // Phase one ends at the half-health seal. Extra pellets and a
            // prolonged shield-break burst cannot skip the transformation.
            // Its earned stagger clock still finishes normally before entry.
            if(!Boss.Awakened&&Boss.AwakeningPending) {
                Boss.Flash=.08f;Boss.LastHitDamage=0;return;
            }
            if (Boss.ShieldActive)
            {
                Boss.ShieldFlash = .18f;
                if (!ranged) {
                    if (PopupTimer <= 0) Show("血色结界 · 枪械或 2 技能反弹魔弹破盾", 1.8f);
                    return;
                }
                Boss.ShieldHP = Math.Max(0, Boss.ShieldHP - damage);
                Burst(Boss.CenterX, Boss.Y + 100, Color.FromArgb(255, 67, 114), 7, 85);
                if (Boss.ShieldHP == 0) BreakBossShield();
                return;
            }
            if (Boss.StaggerTime > 0) {
                damage *= 2; Boss.CritFlash = .30f; Sound.Play("queen_critical");
                RequestCombatHitStop(.022f); Shake = Math.Max(Shake, 2.1f);
                Burst(Boss.CenterX, Boss.Y + 60, Color.FromArgb(255, 224, 153), 6, 115);
            }
            int before=Boss.HP;
            Boss.HP=Boss.Awakened?Boss.HP-damage:Math.Max(Math.Max(1,Boss.MaxHP/2),Boss.HP-damage);
            Boss.LastHitDamage=Math.Max(0,before-Boss.HP);
            Boss.Flash = .12f; Shake = Math.Max(Shake, 1.4f);
            Burst(Boss.CenterX, Boss.Y + 60 + (float)(random.NextDouble() * 55), Color.FromArgb(255, 150, 240), 10, 75);
            if (Boss.HP <= 0) { Boss.AwakeningPending=false; Kills++; BeginBossFinalStrike(); }
            else if(!Boss.Awakened&&Boss.HP<=Math.Max(1,Boss.MaxHP/2))Boss.AwakeningPending=true;
        }

        public bool BossSegmentHit(float x, float y, float endX, float endY, out float hit)
        {
            hit = 0;
            if (Boss == null || !Boss.Active || Boss.Dead) return false;
            if (Boss.ShieldActive) return SegmentHitsBossShield(x, y, endX, endY, out hit);
            return SegmentHitForBoss(x, y, endX, endY, Boss.Bounds, out hit);
        }

        private void TickBossDefense(float dt)
        {
            if (Boss == null || !Boss.Active || Boss.Dead || BossCinematicActive) return;
            Boss.ShieldFlash = Math.Max(0, Boss.ShieldFlash - dt);
            Boss.CritFlash = Math.Max(0, Boss.CritFlash - dt);
            Boss.BloodRecoveryTime=Boss.BloodRecoveryDuration=0;
            if (Boss.StaggerTime > 0) {
                Boss.StaggerTime = Math.Max(0, Boss.StaggerTime - dt);
                if (Boss.StaggerTime <= 0) { Boss.SkillCooldown = 1.2f; Show("女皇恢复行动", 1.3f); }
                return;
            }
            if(Boss.AwakeningPending&&!Boss.ShieldActive&&Boss.ShieldWindup<=0&&!Boss.BloodSequenceActive&&Boss.Skill!=3)
            { BeginBossAwakening();return; }
            if (Boss.ShieldWindup > 0) {
                Boss.ShieldWindup = Math.Max(0, Boss.ShieldWindup - dt);
                if (Boss.ShieldWindup <= 0) {
                    Boss.ShieldHP = BossShieldMaxHP; Boss.ShieldTime = Boss.PermanentShield?0:BossShieldDuration;
                    if(Boss.Awakened&&Boss.PermanentShield) {
                        // The rider belongs to the opening of the second-phase
                        // shield, not the later summon slot in the spell rotation.
                        SummonCavalry();
                        Boss.SkillCooldown=Math.Max(Boss.SkillCooldown,CavalryQueenOpeningDelay);
                    }
                    Sound.Play("queen_shield");
                    Show(Boss.PermanentShield?"永续血盾 · 枪械 / 脉冲反弹破盾":"血色结界 · 第三次施法后展开，持续十秒", 3.2f);
                }
                return;
            }
            if (Boss.ShieldActive) {
                if(Boss.PermanentShield)return;
                Boss.ShieldTime = Math.Max(0, Boss.ShieldTime - dt);
                if (Boss.ShieldTime <= 0) {
                    Boss.ShieldHP = 0; Boss.ShieldFlash = 0;
                    Show("血色结界已消散", 2f);
                }
                return;
            }
            if(!Boss.ShieldPending)return;
            // Both shield events are authored. A blood ritual finishes before
            // its first-stage bubble can take away one of the safe lanes.
            if (Boss.Skill == 3 || Boss.BloodSequenceActive) return;
            Boss.ShieldPending = false;
            if(!Boss.Awakened)Boss.FirstShieldUsed=true;
            Boss.ShieldRollUsed=true;
            Boss.ShieldWindup = BossShieldWindupDuration;
            Boss.Skill = 0; Boss.SkillTimer = 0; Boss.Telegraph = 0; Boss.SkillCooldown = .8f;
            BossAttacks.Clear(); BossOrbOrigins.Clear();
            Show("血色结界即将升起 · 立刻拉开距离", BossShieldWindupDuration + .2f);
            Sound.Play("queen_shield_warning");
        }

        private void BreakBossShield()
        {
            Boss.PermanentShield=false;
            Boss.ShieldHP = 0; Boss.ShieldTime = 0; Boss.ShieldWindup = 0; Boss.ShieldFlash = 0;
            Boss.ShieldPending = false; Boss.BloodSequenceActive = false; Boss.BloodRecoveryTime = 0;
            Boss.StaggerTime = BossStaggerDuration; Boss.Skill = 0; Boss.SkillTimer = 0;
            Boss.Telegraph = 0; Boss.SkillCooldown = 1.2f;
            BossAttacks.Clear(); BossOrbOrigins.Clear(); Bullets.Clear();
            // Breaking her shield staggers the Queen. The independent rider
            // keeps its visible warning and attack instead of freezing too.
            Burst(Boss.CenterX, Boss.Y + 78, Color.FromArgb(255, 208, 155), 44, 150);
            Shake = 4; Show("破盾！女皇僵直 4 秒 · 全部攻击暴击 ×2", 3.8f);
            RequestCombatHitStop(.04f);
            Sound.Play("queen_shield_break");
        }

        private bool BossShieldTouches(RectangleF bounds)
        {
            float cx = Boss.CenterX, cy = Boss.Y + Boss.H * .5f;
            float dx = (Math.Max(bounds.Left, Math.Min(bounds.Right, cx)) - cx) / (Boss.W * .5f + 24);
            float dy = (Math.Max(bounds.Top, Math.Min(bounds.Bottom, cy)) - cy) / (Boss.H * .5f + 10);
            return dx * dx + dy * dy <= 1;
        }

        private bool SegmentHitsBossShield(float x, float y, float endX, float endY, out float hit)
        {
            float rx = Boss.W * .5f + 24, ry = Boss.H * .5f + 10;
            float ox = (x - Boss.CenterX) / rx, oy = (y - Boss.Y - Boss.H * .5f) / ry;
            float dx = (endX - x) / rx, dy = (endY - y) / ry;
            hit = 0; float c = ox * ox + oy * oy - 1;
            if (c <= 0) return true;
            float a = dx * dx + dy * dy, b = 2 * (ox * dx + oy * dy);
            float discriminant = b * b - 4 * a * c;
            if (a < .0000001f || discriminant < 0) return false;
            hit = (-b - (float)Math.Sqrt(discriminant)) / (2 * a);
            return hit >= 0 && hit <= 1;
        }

        private static bool SegmentHitForBoss(float x, float y, float endX, float endY, RectangleF b, out float hit)
        {
            float enter = 0, leave = 1;
            if (!SegmentAxisForBoss(x, endX - x, b.Left, b.Right, ref enter, ref leave) ||
                !SegmentAxisForBoss(y, endY - y, b.Top, b.Bottom, ref enter, ref leave)) { hit = 0; return false; }
            hit = enter; return true;
        }
        private static bool SegmentAxisForBoss(float origin, float delta, float min, float max, ref float enter, ref float leave)
        {
            if (Math.Abs(delta) < .00001f) return origin >= min && origin <= max;
            float a = (min - origin) / delta, b = (max - origin) / delta;
            if (a > b) { float swap = a; a = b; b = swap; }
            enter = Math.Max(enter, a); leave = Math.Min(leave, b); return enter <= leave;
        }

        public void HitSkeletons(RectangleF hit, int damage)
        {
            foreach (SkeletonMinion s in Skeletons)
            {
                if (s.Dead || s.Emerge > 0 || !hit.IntersectsWith(s.Bounds)) continue;
                HitSkeleton(s,damage,Hero.X+12,Weapon.Id==3);
            }
        }

        public void HitSkeleton(SkeletonMinion s,int damage,float sourceX,bool heavy)
        {
            if(s==null||s.Dead||s.Emerge>0||damage<=0||BossCinematicActive)return;
            s.HP-=damage;s.Flash=.085f;s.HitTint=.32f;
            s.KnockbackVX=(s.X+12>=sourceX?1:-1)*(s.Role==2?0:heavy?75:45);
            if(s.Role!=2)s.Stun=0;
            Burst(s.X+12,s.Y+14,Color.FromArgb(252,230,182),heavy?12:7,heavy?95:65);
            RequestCombatHitStop(heavy?.026f:.016f);Shake=Math.Max(Shake,heavy?1.8f:.8f);
            if(s.HP<=0){s.Dead=true;Kills++;SkeletonKilled++;AchievementProgress.RecordMinionKill();AwardEnemyShard(s.X+11,s.Y);Burst(s.X+11,s.Y+12,Color.FromArgb(180,196,210),10,65);}
            Sound.Play("hit");
        }

        public void PulseBoss(float x, float y)
        {
            if (BossArenaLocked && RectangleDistance(x, y, Boss.Bounds) < 119) HitBoss(1);
            foreach (SkeletonMinion s in Skeletons)
            {
                if (s.Dead || s.Emerge > 0 || RectangleDistance(x, y, s.Bounds) >= 119) continue;
                HitSkeleton(s,1,x,false);
            }
            BossAttack reflected=null;float nearest=125;
            foreach(BossAttack a in BossAttacks)if(a.Kind==0&&Distance(x,y,a.X,a.Y)<nearest){reflected=a;nearest=Distance(x,y,a.X,a.Y);}
            BossAttacks.RemoveAll(delegate(BossAttack a) { return a.Kind != 2&&a.Kind!=3 && Distance(x, y, a.X, a.Y) < 125; });
            if(reflected!=null&&Boss!=null&&!Boss.Dead)BossAttacks.Add(new BossAttack{Kind=3,X=reflected.X,Y=reflected.Y,Width=12,Life=4,Duration=4,Color=Color.Cyan,Seed=random.Next(10000)});
        }

        public void ResetBossAfterDeath()
        {
            ResetChapterEnding();
            BossFinalStrikeActive=false;BossFinalStrikeTime=0;
            if (!BossDefeated)
            {
                // Retrying removes only the unfinished summons from the denominator.
                // Actual kills stay cumulative, and a victory never forgives living summons.
                if (Boss == null || !Boss.Dead)
                    SkeletonSpawned = Math.Max(SkeletonKilled, SkeletonSpawned - Skeletons.FindAll(delegate(SkeletonMinion s) { return !s.Dead; }).Count);
                Boss = null; CastleInside = false; CastleEntryActive = false; CastleEntryTime = 0; QueenArrivalActive = false; QueenArrivalTime = 0;
            }
            BossAttacks.Clear(); Skeletons.Clear(); BossOrbOrigins.Clear(); BossIntroRemaining = 0;
        }
    }
}
