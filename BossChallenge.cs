using System;
using System.Collections.Generic;

namespace NeonRift {
// Boss practice borrows the global combat configuration, but never the
// campaign's progress or inventory. Restoring is entirely in memory.
public static class BossChallengeSession {
    public static bool Active {get;private set;}
    public static int BossIndex {get;private set;}
    internal static int AttemptId {get;private set;}
    public static bool Unlocked {get{return Progress.Won;}}
    public const string ConfigurationText="标准配置 · 3 颗心 · 四把武器 · 无晶片升级";
    public const string AmmunitionText="手枪 48 发 / 霰弹 24 发 · 有限弹药，场景内可拾取补给";
    private static string[] chapterSnapshot;
    private static int savedUnlocked,savedWeaponMask,savedEquipped;
    private static bool savedWon;
    static BossChallengeSession(){BossIndex=-1;}

    public static bool Begin(int bossIndex) {
        if(Active||!Unlocked||bossIndex<0||bossIndex>1)return false;
        chapterSnapshot=ChapterProgress.Serialize();
        savedUnlocked=Progress.Unlocked;savedWon=Progress.Won;
        savedWeaponMask=Progress.WeaponMask;savedEquipped=Progress.EquippedWeapon;
        Active=true;BossIndex=bossIndex;
        ConfigureAttempt();
        return true;
    }

    internal static void ConfigureAttempt() {
        AttemptId++;
        ChapterProgress.Reset();ChapterProgress.Active=true;
        ChapterProgress.Stage=BossIndex==0?1:2;
        ChapterProgress.WeaponMask=15;ChapterProgress.EquippedWeapon=BossIndex==0?1:0;
    }

    public static void End() {
        if(!Active)return;
        // Keep the write guard raised through restoration, even if later
        // versions add incidental Save calls to the chapter parser.
        try {
            ChapterProgress.ReadLines(chapterSnapshot);
            Progress.Unlocked=savedUnlocked;Progress.Won=savedWon;
            Progress.WeaponMask=savedWeaponMask;Progress.EquippedWeapon=savedEquipped;
        } finally {chapterSnapshot=null;BossIndex=-1;Active=false;}
    }
}

public sealed partial class GameWorld {
    public bool IsBossChallenge {get;private set;}
    public int ChallengeBossIndex {get;private set;}
    private int challengeAttemptId;
    private bool CurrentBossChallenge {get{return BossChallengeSession.Active&&BossChallengeSession.BossIndex==ChallengeBossIndex&&BossChallengeSession.AttemptId==challengeAttemptId;}}
    private readonly List<Pickup> challengeSupplies=new List<Pickup>();

    public static GameWorld CreateBossChallenge(int bossIndex) {
        if(!BossChallengeSession.Active||BossChallengeSession.BossIndex!=bossIndex||bossIndex<0||bossIndex>1)
            throw new InvalidOperationException("请先解锁并开始对应的独立首领挑战。");
        BossChallengeSession.ConfigureAttempt();
        GameWorld world=new GameWorld(bossIndex==0?1:2);
        world.InitBossChallenge(bossIndex);
        return world;
    }

    private void InitBossChallenge(int bossIndex) {
        IsBossChallenge=true;ChallengeBossIndex=bossIndex;challengeAttemptId=BossChallengeSession.AttemptId;
        Cores=3;Shards=Kills=Deaths=0;
        Foes.Clear();Level.Enemies.Clear();Level.Hazards.Clear();Level.Pickups.Clear();
        Level.Checkpoints.Clear();Level.Terminals.Clear();Level.Signs.Clear();Level.CorePuzzles.Clear();
        Bag=new Inventory(15,bossIndex==0?1:0);Bag.AddAmmo(1,48);Bag.AddAmmo(2,24);
        Hero.Health=3;Hero.Invincible=0;
        if(bossIndex==0) {
            SpawnX=ChaseMeetingX-12;SpawnY=268;Hero.X=SpawnX;Hero.Y=SpawnY;Hero.Grounded=true;
            Camera=Math.Max(0,Level.Width-640);
            if(!TryStartChase(new Controls{Interact=true}))throw new InvalidOperationException("追逐挑战入口未能开启。");
            // Preserve the actual surprise, boarding and launch animation,
            // while skipping the campaign's preceding six-line conversation.
            ChasePhase=2;ChasePhaseTime=0;ChaseDialogue=ChaseLines.Length;
            ChaseMonsterHP=ChaseMonsterMaxHP;Sound.Play("chase_arrival");
        } else {
            float left=Level.ExitX-600;
            challengeSupplies.Add(new Pickup{Type=4,X=left+146,Y=278,AmmoWeaponId=1,AmmoAmount=24});
            challengeSupplies.Add(new Pickup{Type=4,X=left+490,Y=278,AmmoWeaponId=2,AmmoAmount=12});
            Level.Pickups.AddRange(challengeSupplies);
            StartChallengeCastleEntry();
        }
        PopupTimer=0;TutorialRemaining=0;
    }

    private void StartChallengeCastleEntry() {
        SpawnX=CastleDoorX+32;SpawnY=268;
        Hero.X=SpawnX;Hero.Y=SpawnY;Hero.Grounded=true;Hero.Riding=null;
        Hero.Facing=1;Hero.Jumps=0;Hero.VX=Hero.VY=0;
        Camera=Math.Max(0,CastleDoorX-390);
        if(!TryEnterCastle(new Controls{Interact=true}))throw new InvalidOperationException("女皇挑战入口未能开启。");
    }

    private void RespawnBossChallenge() {
        if(!IsBossChallenge||!CurrentBossChallenge)return;
        Complete=false;Cores=3;
        if(ChallengeBossIndex==1) {
            foreach(Pickup supply in challengeSupplies)supply.Collected=false;
            StartChallengeCastleEntry();
        }
    }
}
}
