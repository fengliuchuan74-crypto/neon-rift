using System;
using System.Collections.Generic;
using System.IO;

namespace NeonRift {
// Permanent badges and chapter-run conditions use separate saves. Beginning
// another chapter only clears the latter; boss practice cannot award either.
public static class AchievementProgress {
    public static readonly string[] Names={"赛博破晓","至尊猫皇","一个不留","潇洒赛博"};
    public static readonly string[] Descriptions={
        "依次通过第一章的三个关卡。",
        "在同一轮第一章中，摸摸三个关卡里的猫。",
        "同一轮清除三关的全部固定敌人，决战不留下召唤骷髅。",
        "第一章通关时，未击杀任何普通敌人或骷髅兵。"
    };
    public static int Mask {get;private set;}
    public static int RunCatMask {get;private set;}
    public static int RunKills {get;private set;}
    public static int StageFullMask {get;private set;}
    public static bool RunTracked {get;private set;}
    public const float ToastDuration=3.8f;
    public static float ToastTime {get;private set;}
    private static int toastId=-1;
    private static readonly Queue<int> toasts=new Queue<int>();
    public static int ActiveToastId {get{return BossChallengeSession.Active?-1:toastId;}}
    public static string ActiveToast {get{return ActiveToastId<0?"":"成就解锁 · "+Names[toastId];}}
    public static string PathName {get{return Path.Combine(Path.GetDirectoryName(Progress.PathName),"achievements.txt");}}
    private static bool CampaignPlaying {get{return ChapterProgress.Active&&!ChapterProgress.ShopPending&&!BossChallengeSession.Active;}}

    public static bool IsUnlocked(int id){return id>=0&&id<4&&(Mask&(1<<id))!=0;}
    public static void PetCat(int stage) {
        if(!CampaignPlaying||stage<0||stage>2||stage!=ChapterProgress.Stage)return;
        int before=RunCatMask;RunCatMask|=1<<stage;
        if(before!=RunCatMask)ChapterProgress.Save();
        if(RunCatMask==7)Unlock(1);
    }
    public static void RecordMinionKill() {
        if(!CampaignPlaying)return;
        RunKills=Math.Min(999999,RunKills+1);ChapterProgress.Save();
    }
    public static void OnStageClear(int stage,bool allFoesDead,bool allSkeletonsCleared) {
        if(!CampaignPlaying||stage!=ChapterProgress.Stage||stage<0||stage>2)return;
        if(allFoesDead&&allSkeletonsCleared)StageFullMask|=1<<stage;
        ChapterProgress.Save();
    }
    public static void OnChapterWin() {
        // Main calls this after committing the final campaign result, so the
        // chapter may already be inactive. Practice still cannot enter here.
        if(BossChallengeSession.Active||ChapterProgress.Stage!=2)return;
        Unlock(0);
        if(RunCatMask==7)Unlock(1);
        if(StageFullMask==7)Unlock(2);
        if(RunTracked&&RunKills==0)Unlock(3);
    }
    private static void Unlock(int id) {
        if(BossChallengeSession.Active||IsUnlocked(id))return;
        Mask|=1<<id;toasts.Enqueue(id);StartNextToast();Save();
    }
    private static void StartNextToast() {
        if(toastId>=0||toasts.Count==0)return;
        toastId=toasts.Dequeue();ToastTime=ToastDuration;
    }
    public static void TickToast(float dt) {
        if(BossChallengeSession.Active||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
        StartNextToast();if(toastId<0)return;
        // Do not discard queued notices after a suspended window resumes:
        // each newly displayed achievement receives its own full duration.
        ToastTime=Math.Max(0,ToastTime-dt);
        if(ToastTime<=0){toastId=-1;StartNextToast();}
    }
    internal static void ResetRun(){RunCatMask=RunKills=StageFullMask=0;RunTracked=false;}
    internal static void BeginRun(){ResetRun();RunTracked=true;}
    internal static void ReadRun(string[] data) {
        RunCatMask=Number(data,15,7);RunKills=Number(data,16,999999);StageFullMask=Number(data,17,7);
        // Older chapter files cannot prove whether earlier enemies were
        // killed. They may still earn every other badge; a fresh chapter
        // supplies a complete record for the zero-kill achievement.
        RunTracked=data.Length>18&&data[18]=="1";
    }
    private static int Number(string[] data,int index,int maximum) {
        int n;return data!=null&&index<data.Length&&int.TryParse(data[index],out n)?Math.Max(0,Math.Min(maximum,n)):0;
    }
    public static string[] Serialize(){return new string[]{"NEON_ACHIEVEMENTS_1",Mask.ToString()};}
    public static void ReadLines(string[] data) {
        if(BossChallengeSession.Active)return;
        Mask=data!=null&&data.Length>=2&&data[0]=="NEON_ACHIEVEMENTS_1"?Number(data,1,15):0;
        toasts.Clear();toastId=-1;ToastTime=0;
    }
    public static void Load(){LoadFrom(PathName);}
    internal static void LoadFrom(string path) {
        if(BossChallengeSession.Active)return;
        try{ReadLines(File.ReadAllLines(path));}catch{ReadLines(null);}
        // A pre-achievement save already proves completion. Migrate it once,
        // silently, instead of asking an existing player to beat the chapter again.
        if(Progress.Won&&!IsUnlocked(0)){Mask|=1;SaveTo(path);}
    }
    public static void Save(){SaveTo(PathName);}
    internal static void SaveTo(string path) {
        if(!Progress.SavingEnabled||BossChallengeSession.Active)return;
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";
            File.WriteAllLines(temp,Serialize());
            if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        }catch{}
    }
}
}
