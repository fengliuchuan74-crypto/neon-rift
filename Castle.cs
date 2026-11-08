using System;

namespace NeonRift
{
    public sealed partial class GameWorld
    {
        public const float CastleTransitionDuration = 1.2f;
        public const float CastleRitualDuration = 3.8f;
        public const float CastleEntryDuration = CastleTransitionDuration + CastleRitualDuration;
        public const float QueenArrivalDuration = 3.6f;
        public bool CastleInside, CastleEntryActive;
        public float CastleEntryTime;
        public bool QueenArrivalActive;
        public float QueenArrivalTime;
        public float CastleDoorX { get { return BossTriggerX + 30; } }
        public float CastleRitualTime { get { return Math.Max(0, CastleEntryTime - CastleTransitionDuration); } }
        public bool AtCastleDoor
        {
            get { return Level.Index == 2 && !CastleInside && !CastleEntryActive && DeathTimer <= 0 && Hero.Grounded
                && Math.Abs(Hero.X + 12 - (CastleDoorX + 44)) < 66; }
        }
        public bool CanEnterCastle { get { return AtCastleDoor && Cores >= 3; } }

        public float CastleCandleRedness(int index)
        {
            if (index < 0 || index >= 6 || !CastleInside) return 0;
            if (BossOutroActive) return 1 - Math.Min(1, Math.Max(0, (Boss.DefeatTime - 1.6f) / 2.8f));
            if (BossDefeated) return 0;
            if (!CastleEntryActive) return 1;
            int pair = Math.Min(index, 5 - index);
            float t = Math.Max(0, Math.Min(1, (CastleRitualTime - (.8f + pair * .85f)) / .35f));
            return t * t * (3 - 2 * t);
        }

        private bool TryEnterCastle(Controls input)
        {
            if (!input.Interact || !AtCastleDoor) return false;
            if (Cores < 3)
            {
                Show("无法进入古堡 · 核心 " + Cores + "/3，还差 " + (3 - Cores) + " 枚，请返回探索", 4f);
                Sound.Play("castle_denied");
                return false;
            }
            CastleEntryActive = true; CastleEntryTime = 0;
            QueenArrivalActive = false; QueenArrivalTime = 0;
            Hero.VX = 0; Hero.VY = 0; Hero.DashTime = 0; Hero.AttackTime = 0; Hero.JumpBuffer = 0;
            BossAttacks.Clear(); BossOrbOrigins.Clear(); Skeletons.Clear(); Bullets.Clear(); Shots.Clear();
            Particles.Clear(); Shake = 0; PulseVisual = 0; PopupTimer = 0;
            Sound.Play("castle_door");
            return true;
        }

        public void TickCastleEntry(float dt)
        {
            if (!CastleEntryActive) return;
            float before = CastleEntryTime;
            CastleEntryTime = Math.Min(CastleEntryDuration, CastleEntryTime + Math.Max(0, dt));
            if (!CastleInside && CastleEntryTime >= CastleTransitionDuration * .5f)
            {
                CastleInside = true;
                StartBossArena();
                Hero.X = BossArenaLeft + 62; Hero.Y = 268; Hero.Grounded = true;
                Hero.Riding = null; Hero.Jumps = 0; Hero.Facing = 1;
                Camera = BossArenaLeft;
                Sound.Play("castle_hall");
            }
            for (int pair = 0; pair < 3; pair++)
            {
                float ignition = CastleTransitionDuration + .8f + pair * .85f;
                if (before < ignition && CastleEntryTime >= ignition) Sound.Play("castle_flame");
            }
            if (CastleEntryTime >= CastleEntryDuration)
            {
                CastleEntryActive = false;
                QueenArrivalActive = true; QueenArrivalTime = 0;
                Sound.Play("queen_bats");
            }
        }

        public void TickQueenArrival(float dt)
        {
            if (!QueenArrivalActive) return;
            float before = QueenArrivalTime;
            QueenArrivalTime = Math.Min(QueenArrivalDuration, QueenArrivalTime + Math.Max(0, dt));
            Camera = BossArenaLeft;
            if (before < 2.25f && QueenArrivalTime >= 2.25f) Sound.Play("queen_materialize");
            if (QueenArrivalTime >= QueenArrivalDuration)
            {
                QueenArrivalActive = false;
                BossIntroRemaining = BossIntroDuration;
                PopupTimer=0;
                Sound.Play("boss_intro");
            }
        }
    }
}
