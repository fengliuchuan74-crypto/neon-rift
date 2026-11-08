using System;
using System.Drawing;

namespace NeonRift {
public sealed class WeaponDef {
    public int Id,Damage;
    public string Name,Description;
    public float Cooldown,Range;
    public Color Color;
    public bool Ranged;
    // Ranged weapons consume one round/shell per trigger pull. Melee weapons
    // keep the value at zero and never consult the ammo pool.
    public int MaxAmmo;
}
public static class Weapons {
    public static float Windup(int id){return id==3?.15f:id==0?.065f:0;}
    public static float ActiveTime(int id){return id==3?.16f:id==0?.12f:0;}
    public static float ActionDuration(int id){return id==3?.58f:id==0?.30f:id==2?.18f:.14f;}
    // Grip, tip and body all come from one rigid weapon pose.
    public static PointF MeleeTip(int id,float remaining,float reach) {
        return MeleeMotion.Pose(id,remaining,reach).Tip;
    }
    public static WeaponDef Effective(int id) {
        WeaponDef w=All[id];
        return new WeaponDef{Id=w.Id,Name=w.Name,Description=id==2?"近中程散射 · 每次发射 "+ChapterProgress.ShotgunPellets+" 枚弹丸，每次消耗 1 弹壳。":id==3?"双手长枪 · 回拉蓄势后挺枪直刺，枪尖命中可贯穿多个敌人。":w.Description,
            Damage=w.Damage+ChapterProgress.DamageBonus(id),Cooldown=w.Cooldown,Range=w.Range+(w.Ranged?0:ChapterProgress.ReachBonus),Color=w.Color,Ranged=w.Ranged,MaxAmmo=w.MaxAmmo};
    }
    public static readonly WeaponDef[] All = new WeaponDef[] {
        new WeaponDef {Id=0,Name="脉冲刃",Description="轻型近战 · 提剑蓄势后向前挥斩，可同时击中刀弧内多个敌人。",Damage=1,Cooldown=.32f,Range=51,Color=Color.FromArgb(101,243,255)},
        new WeaponDef {Id=1,Name="离子手枪",Description="远程连射 · 精准离子弹，适合应对无人机与炮台。",Damage=1,Cooldown=.24f,Range=410,Color=Color.FromArgb(195,135,255),Ranged=true,MaxAmmo=48},
        new WeaponDef {Id=2,Name="磁轨霰弹枪",Description="近中程散射 · 每次发射 3 枚弹丸，贴近时威力更强。",Damage=1,Cooldown=.65f,Range=275,Color=Color.FromArgb(255,174,81),Ranged=true,MaxAmmo=24},
        new WeaponDef {Id=3,Name="电弧长枪",Description="双手长枪 · 回拉蓄势后挺枪直刺，枪尖命中造成 3 点伤害。",Damage=3,Cooldown=.72f,Range=86,Color=Color.FromArgb(114,249,169)}
    };
}
public sealed class Inventory {
    public bool[] Owned = new bool[4];
    // Ammo is deliberately held by the run inventory. All ranged ammunition
    // starts at zero and can only increase through map pickups.
    public int[] Ammo = new int[4];
    public int Equipped;
    public Inventory() : this(1,0) {}
    public Inventory(int mask,int equipped) {
        // Bit 0 is the starter pulse blade.  Ranged weapons are map finds;
        // never silently grant them when a run starts.
        mask|=1;
        for(int i=0;i<Owned.Length;i++) Owned[i]=(mask&(1<<i))!=0;
        Equipped=0;Equip(equipped);
    }
    public WeaponDef Current {get {return Weapons.Effective(Equipped);}}
    public bool Equip(int id) {
        if(id<0 || id>=Owned.Length || !Owned[id]) return false;
        Equipped=id;return true;
    }
    public void Cycle(int delta) {
        if(delta==0) return;
        int direction=delta>0?1:-1;
        for(int i=1;i<=Owned.Length;i++) {
            int next=(Equipped+i*direction+Owned.Length*2)%Owned.Length;
            if(Owned[next]) {Equipped=next;return;}
        }
    }
    public bool Acquire(int id) {
        if(id<0 || id>=Owned.Length || Owned[id]) return false;
        Owned[id]=true;Equipped=id;return true;
    }
    public int AmmoFor(int id) {
        if(id<0 || id>=Ammo.Length) return 0;
        return Ammo[id];
    }
    public int AmmoForCurrent { get { return AmmoFor(Equipped); } }
    public bool ConsumeAmmo(int id,int amount) {
        if(id<0 || id>=Ammo.Length || amount<1 || Ammo[id]<amount) return false;
        Ammo[id]-=amount;return true;
    }
    public int AddAmmo(int id,int amount) {
        if(id<0 || id>=Ammo.Length || amount<=0) return 0;
        int cap=Weapons.All[id].MaxAmmo;
        if(cap<=0) return 0;
        int before=Ammo[id];Ammo[id]=Math.Min(cap,Ammo[id]+amount);return Ammo[id]-before;
    }
    public int Mask {get {int mask=0;for(int i=0;i<Owned.Length;i++) if(Owned[i]) mask|=1<<i;return mask;}}
}
public sealed class Shot {
    public float X,Y,VX,VY,Life;
    public Color Color;
    public int Size,Damage;
    public bool Pierce;
    public Platform OriginPlatform;
}
}
