using System;
using System.IO;
using System.Collections.Generic;

namespace NeonRift {
public sealed class UpgradeOffer {
    public string Name;
    public int Price,Limit,Weapon=-1;
    public UpgradeOffer(string name,int price,int limit,int weapon) {Name=name;Price=price;Limit=limit;Weapon=weapon;}
}
public static class ChapterProgress {
    public static bool Active,ShopPending,ArrivalBriefingDone;
    public static int Stage,Chips;
    // The saved ShopPending bit represents the entire between-stage preparation
    // period, independent of which menu is visible. Leaving a menu never spends it.
    public static bool Intermission {get{return Active&&ShopPending&&Stage>0&&Stage<3;}}
    // Weapon ownership belongs to the current chapter run.  The protagonist
    // starts with the pulse blade; each other weapon is obtained once from
    // its map cache and is kept while progressing through this chapter.
    public static int WeaponMask=1,EquippedWeapon=0;
    public static int[] Upgrades=new int[7];
    public static readonly HashSet<string> SeenHints=new HashSet<string>(StringComparer.Ordinal);
    public static bool RememberHint(string key){
        if(BossChallengeSession.Active||string.IsNullOrEmpty(key)||!SeenHints.Add(key))return false;
        Save();return true;
    }
    public static readonly UpgradeOffer[] Offers={
        new UpgradeOffer("生命扩容",18,2,-1),new UpgradeOffer("脉冲刃强化",26,1,0),
        new UpgradeOffer("离子手枪强化",30,1,1),new UpgradeOffer("霰弹枪强化",40,1,2),
        new UpgradeOffer("电弧长枪强化",30,1,3),new UpgradeOffer("霰弹分流",28,2,2),
        new UpgradeOffer("近战延展",22,2,-1)
    };
    public static int MaxHealth {get{return 3+Upgrades[0];}}
    public static int ShotgunPellets {get{return 3+Upgrades[5];}}
    public static int ReachBonus {get{return 10*Upgrades[6];}}
    public static int DamageBonus(int weapon){return Upgrades[1+Math.Max(0,Math.Min(3,weapon))];}
    public static int Cost(int id){return Offers[id].Price+Upgrades[id]*15;}
    public static bool HasWeapon(int weapon){
        int mask=Active?WeaponMask:Progress.WeaponMask;
        return (mask&(1<<weapon))!=0;
    }
    public static bool Owned(int id){return Offers[id].Weapon<0||HasWeapon(Offers[id].Weapon);}
    public static string Effect(int id) {
        if(id==0)return "生命上限 "+MaxHealth+" → "+Math.Min(5,MaxHealth+1)+"  · 最多 +2 心";
        if(id==5)return "每次弹丸 "+ShotgunPellets+" → "+Math.Min(5,ShotgunPellets+1)+" · 仍耗 1 弹壳";
        if(id==6)return "两种近战距离 +"+ReachBonus+" → +"+Math.Min(20,ReachBonus+10);
        int weapon=id-1,damage=Weapons.All[weapon].Damage+DamageBonus(weapon);
        return "单次伤害 "+damage+" → "+(Weapons.All[weapon].Damage+1)+(weapon==2?" / 每颗弹丸":"");
    }
    public static bool Buy(int id,out string message) {
        if(BossChallengeSession.Active){message="独立首领挑战采用固定配置。";return false;}
        if(id<0||id>=Offers.Length||!Active||!ShopPending){message="请在关间晶片商店升级。";return false;}
        if(!Owned(id)){message="先在地图中获得这把武器。";return false;}
        if(Upgrades[id]>=Offers[id].Limit){message="该项目已达到本章上限。";return false;}
        int price=Cost(id);
        if(Chips<price){message="晶片不足，还需要 "+(price-Chips)+" 枚。";return false;}
        Chips-=price;Upgrades[id]++;Save();message=Offers[id].Name+"已升级 · 本章有效";return true;
    }
    // Tutorial history belongs to the player's familiarity, not chapter
    // upgrades. A fresh run keeps the existing saved rules already taught.
    public static void Reset(){Active=false;ShopPending=false;ArrivalBriefingDone=false;Stage=0;Chips=0;WeaponMask=1;EquippedWeapon=0;Upgrades=new int[7];AchievementProgress.ResetRun();}
    public static void Begin(){Reset();Active=true;AchievementProgress.BeginRun();Save();}
    public static bool ClearStage(int stage,int earned) {
        if(BossChallengeSession.Active)return false;
        // One settlement per stage. Reloading a result/shop cannot award it twice.
        if(!Active||ShopPending||stage!=Stage)return false;
        Chips=Math.Min(99999,Chips+Math.Max(0,Math.Min(9999,earned)));
        if(stage<2){Stage++;ShopPending=true;}else{Active=false;Stage=2;}
        Save();return true;
    }
    public static bool BeginNextStage(){if(!Intermission)return false;ShopPending=false;Save();return true;}
    // Legacy callers use this only when actually launching the following region.
    public static void LeaveShop(){BeginNextStage();}
    public static string[] Serialize(){
        string[] data=new string[20];data[0]="NEON_CHAPTER_1";data[1]=Active?"1":"0";data[2]=Stage.ToString();data[3]=ShopPending?"1":"0";data[4]=Chips.ToString();
        for(int i=0;i<7;i++)data[5+i]=Upgrades[i].ToString();
        data[12]=WeaponMask.ToString();data[13]=EquippedWeapon.ToString();
        string[] hints=new string[SeenHints.Count];SeenHints.CopyTo(hints);Array.Sort(hints,StringComparer.Ordinal);
        data[14]=string.Join(",",hints);data[15]=AchievementProgress.RunCatMask.ToString();
        data[16]=AchievementProgress.RunKills.ToString();data[17]=AchievementProgress.StageFullMask.ToString();data[18]=AchievementProgress.RunTracked?"1":"0";
        data[19]=ArrivalBriefingDone?"1":"0";return data;
    }
    static int Number(string[] data,int index,int limit){int n;return index<data.Length&&int.TryParse(data[index],out n)?Math.Max(0,Math.Min(limit,n)):0;}
    public static void ReadLines(string[] data){
        Reset();SeenHints.Clear();if(data==null||data.Length<5||data[0]!="NEON_CHAPTER_1")return;
        Active=data[1]=="1";Stage=Number(data,2,2);ShopPending=Active&&Stage>0&&data[3]=="1";Chips=Number(data,4,99999);
        for(int i=0;i<7;i++)Upgrades[i]=Number(data,5+i,Offers[i].Limit);
        // v4.2 chapter saves had only 12 fields. Treat those as a fresh
        // sword-only run instead of accidentally restoring every weapon from
        // the numeric fallback used by the legacy parser.
        WeaponMask=(data.Length>=14?Number(data,12,15):1)|1;
        int equipped=data.Length>=14?Number(data,13,3):0;
        EquippedWeapon=(WeaponMask&(1<<equipped))!=0?equipped:0;
        ArrivalBriefingDone=data.Length>19?data[19]=="1":Stage>0;
        // Optional trailing field keeps pre-4.4 saves compatible. Only accept
        // bounded identifier tokens, never text or executable save content.
        if(data.Length>14&&data[14]!=null&&data[14].Length<16384){
            foreach(string key in data[14].Split(',')){
                if(key.Length==0||key.Length>80||SeenHints.Count>=256)continue;
                bool valid=true;foreach(char c in key)if(!(c>='a'&&c<='z')&&!(c>='0'&&c<='9')&&c!='-'&&c!=':'){valid=false;break;}
                if(valid)SeenHints.Add(key);
            }
        }
        AchievementProgress.ReadRun(data);
    }
    // Kept separate from the legacy file, so an already-open older EXE cannot erase chapter growth.
    public static string PathName {get{return Path.Combine(Path.GetDirectoryName(Progress.PathName),"chapter-1.txt");}}
    public static void Load(){Reset();try{ReadLines(File.ReadAllLines(PathName));}catch{}}
    public static void Save(){SaveTo(PathName);}
    internal static void SaveTo(string path){
        if(!Progress.SavingEnabled||BossChallengeSession.Active)return;
        try{
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";
            File.WriteAllLines(temp,Serialize());
            if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        }catch{}
    }
}
}
