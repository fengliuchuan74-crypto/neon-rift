using System;
using System.Collections.Generic;

namespace NeonRift {
public static partial class Levels {
    // Authored after the legacy spacing pass: these rooms have measured jump
    // arcs and intentional upper/lower routes, rather than a shared template.
    static void ApplyChapterThemes(Level l) {
        if(l.Index==0) RooftopTheme(l);
        else if(l.Index==1) FreightTheme(l);
        else CastlewardTheme(l);
        // Core directions are location guidance, not repeated mechanism rules.
        foreach(Signpost s in l.Signs)
            if((l.Index==0&&s.X==10830)||(l.Index==1&&s.X==8250)||(l.Index==2&&s.X==9480))s.HintKey=null;
    }

    // Preserve exterior geometry when a legacy platform crosses a room edge.
    // Cores are retained and explicitly relocated by each room designer.
    static void ReplaceThemeRoom(Level l,float from,float to) {
        List<Platform> edges=new List<Platform>();
        foreach(Platform p in l.Platforms) {
            if(p.BaseX+p.W<=from||p.BaseX>=to||p.Axis!=0||p.Kind!=0)continue;
            if(p.X<from)edges.Add(new Platform {X=p.X,BaseX=p.X,PrevX=p.X,
                Y=p.Y,BaseY=p.Y,PrevY=p.Y,W=from-p.X,H=p.H,OneWay=p.OneWay});
            if(p.X+p.W>to)edges.Add(new Platform {X=to,BaseX=to,PrevX=to,
                Y=p.Y,BaseY=p.Y,PrevY=p.Y,W=p.X+p.W-to,H=p.H,OneWay=p.OneWay});
        }
        ClearRoom(l,from,to);l.Platforms.AddRange(edges);
    }

    static void RooftopTheme(Level l) {
        l.Briefing="沿霓虹屋顶练习连续跳跃，探索高处补给与下层维修路，集齐三枚核心。";
        // Learn height changes in a quiet residential block, then meet one
        // grounded opponent. No lift/industrial conveyor duplicates here.
        ReplaceThemeRoom(l,6510,7500);
        Ground(l,6510,225,300);Ground(l,6820,200,238);
        Ground(l,7110,185,190);Ground(l,7375,125,300);
        Ledge(l,6725,310,80);Ledge(l,7030,298,75);Ledge(l,7290,269,75);
        Shards(l,6850,208,3,46);Shards(l,7140,160,3,46);
        Ammo(l,7215,163,1,12);Enemy(l,7145,162,7120,7255,0);
        Check(l,6550,300);Check(l,7398,300);
        Sign(l,6570,166,"居民屋顶 · 高处有补给，下方维修架可以重新起跳");

        // Variation -> combination -> respite: no continuous floor, and no
        // mandatory cooldown gate. Higher roofs pay crystals; lower catches
        // permit recovery without erasing the jump sequence.
        ReplaceThemeRoom(l,8610,10780);
        Ground(l,8610,230,300);Ground(l,8950,210,280);
        Ground(l,9280,190,248);Ground(l,9580,160,280);
        Ground(l,9890,240,250);Ground(l,10220,180,218);
        Ground(l,10530,250,300);
        // Keep rescue surfaces above the bottom HUD, so a failed jump has a
        // visible destination instead of another apparent invisible floor.
        Ledge(l,8815,310,120);Ledge(l,9175,310,90);
        Ledge(l,9460,310,105);Ledge(l,9770,310,105);
        Ledge(l,10410,310,105);
        Ledge(l,8760,218,110);Ledge(l,8930,161,120);
        Ledge(l,9120,130,120);Ledge(l,9455,168,108);
        Ledge(l,9690,173,105);Ledge(l,9920,149,120);
        Enemy(l,8990,252,8970,9100,0);Enemy(l,9970,222,9910,10080,0);
        // The isolated mine is visible from a safe roof, never on a landing.
        Mine(l,9650,280);
        Shards(l,8950,131,3,46);Shards(l,9138,100,3,46);
        Shards(l,9470,138,2,46);Ammo(l,9950,122,1,14);
        Heal(l,10075,223);Check(l,8660,300);Check(l,9320,248);Check(l,10565,300);
        Sign(l,8640,153,"屋顶双线 · 上层晶片路线更高，下层维修架留有补救落点");
        Sign(l,9390,106,"越过天线屋顶 · 在宽平台站稳，再接下一次跳跃");
        Sign(l,10565,167,"通讯塔前的安静平台 · 核心在前方高处");

        // Give the physical crate puzzle its own approach and landing. The
        // legacy pulse gate, moving ferry, socket and checkpoint occupied
        // almost the same 170px and could trap the crate behind a timed gate.
        ReplaceThemeRoom(l,11360,11930);
        l.Terminals.RemoveAll(delegate(Terminal t){return t.Link==11||t.Link==101||t.Link==401;});
        Ground(l,11360,570,300);
        PushBox(l,11405,300,401);
        PuzzleGate(l,11570,300,401,3,11405,300,11485,300);
        AbilityGate(l,11810,300,101,1,11670,300,11875,300);
        Check(l,11935,300);
        Sign(l,11380,147,"维修箱解锁 · 将货箱推入插槽，随后越过固定的货箱","crate");
        Sign(l,11655,145,"脉冲继电器 · 沿蓝色电缆找到线圈，按 2 暂停电网","pulse-gate");
    }

    static void FreightTheme(Level l) {
        l.Briefing="登上运货升降机、换乘吊臂与分拣带，集齐核心后前往摩托会合点。";
        // A real freight elevator replaces the second copy of the spring
        // core puzzle. The high shelf feeds directly back down to the route.
        ReplaceThemeRoom(l,3530,4200);
        Ground(l,3530,180,280);Lift(l,3710,116,280,196,108);
        Ground(l,3826,374,280);Ledge(l,3826,108,214);Ledge(l,3550,196,160);
        CoreAt(l,1,3900,81);Ammo(l,3995,81,2,6);
        Shards(l,3600,166,3,46);Shards(l,4090,250,2,46);
        Check(l,3560,280);
        Sign(l,3565,137,"货运升降井 · 电梯到顶向右回收核心，再落回卸货场","elevator");
        Sign(l,3790,65,"货运核心在顶层右侧 · 收集后可直接落回卸货场");

        // Final transport crescendo: horizontal crane -> guarded unloading
        // shelf -> vertical crane. Rescue shelves return to the same cranes.
        ReplaceThemeRoom(l,11060,12140);
        Ground(l,11060,170,300);Moving(l,11300,239,110,1,20,.72f,0);
        Ground(l,11495,175,230);Moving(l,11755,184,110,2,28,.8f,.6f);
        Ground(l,11955,185,240);
        Ledge(l,11270,310,170);Ledge(l,11720,306,180);
        Ledge(l,11505,154,120);
        Crusher(l,11585,230,4.8f,1.8f);
        Shards(l,11522,125,2,50);Ammo(l,12050,213,2,8);
        Check(l,11085,300);Check(l,11980,240);
        Sign(l,11095,155,"最后装卸线 · 横向吊臂之后换乘升降吊臂，失足可从检修架返回");
        // Remove the earlier instruction to gamble a dash at the first gap.
        l.Signs.RemoveAll(delegate(Signpost s){return s.X==330;});
        foreach(Signpost s in l.Signs)if(s.X==3035)s.Text="轰炸货场 · 留意可移动货箱与开裂的补给库";
    }

    static void CastlewardTheme(Level l) {
        l.Name="血月城郊";
        l.Briefing="穿过废墟与失压工厂，在高低支路规划有限补给，携三枚核心步入血月古堡。";
        // Broken stone terraces replace the third spring-core copy. A short
        // close-range encounter protects the relic; falling has a lower shelf.
        ReplaceThemeRoom(l,3790,4480);
        Ground(l,3790,180,300);Ledge(l,4020,235,112);
        Ground(l,4190,165,178);Ground(l,4380,100,300);
        Ledge(l,3960,310,215);CoreAt(l,0,4290,151);
        Enemy(l,4220,150,4200,4250,0);
        Shards(l,4045,205,2,46);Ammo(l,4415,273,1,10);
        Check(l,3825,300);
        Sign(l,3840,162,"废墟圣龛 · 沿断阶向上，核心由近卫看守");

        // Resource fork: low route has readable mines and healing, upper
        // route offers ammo but a turret. Either route reconnects before a
        // safe checkpoint, so the decision is based on current resources.
        ReplaceThemeRoom(l,8250,9440);
        Ground(l,8250,220,300);Ground(l,8600,270,300);
        Ground(l,8990,200,280);Ground(l,9290,150,300);
        Ledge(l,8370,218,140);Ledge(l,8580,160,140);
        Ledge(l,8790,197,130);Ledge(l,9020,188,150);
        Ledge(l,8460,310,125);Ledge(l,8870,310,105);Ledge(l,9180,310,95);
        Mine(l,8730,300);Mine(l,8820,300);
        Enemy(l,8610,132,8610,8610,2);
        Ammo(l,8845,170,2,8);Ammo(l,9105,161,1,14);
        Heal(l,9040,253);Shards(l,8400,188,2,48);
        Shards(l,8655,130,2,48);Check(l,8320,300);Check(l,9320,300);
        Sign(l,8310,144,"失压分岔 · 上层炮台守着弹药，下层地雷后留有医疗补给");

        // A sustained calm walk before the castle lets the next cinematic
        // breathe. Preserve the final physical box puzzle and core; all
        // bombers, mines, lasers and patrols end before the stone approach.
        ReplaceThemeRoom(l,11845,13080);
        Ground(l,11845,225,300);Ledge(l,12130,259,145);
        Ledge(l,12345,213,138);Ground(l,12565,260,270);
        Ledge(l,12200,310,154);Ledge(l,12895,238,140);
        Ground(l,13035,45,300);
        Ammo(l,12605,243,1,16);Ammo(l,12715,243,2,8);
        Shards(l,12375,183,2,50);Check(l,11880,300);Check(l,12595,270);
        l.Hazards.RemoveAll(delegate(Hazard h){return h.X+h.W>=11845;});
        l.Enemies.RemoveAll(delegate(EnemySpawn e){return e.MaxX+28>=11845;});
        Sign(l,11905,148,"城郊旧道 · 霓虹渐远，沿石阶寻找最后一枚核心");
        Sign(l,12930,139,"古堡门禁需要三枚核心 · 补给可留给女皇战");
        foreach(Signpost s in l.Signs)if(s.X==13070)s.Text="最后一道检修闸 · 推箱入槽，穿过石桥抵达古堡";
        // Replace the final moving industrial ferry with two distinct, quiet
        // stone steps. Neither can be crossed by simply holding right.
        l.Platforms.RemoveAll(delegate(Platform p){return p.Axis>0&&p.BaseX>=13440&&p.BaseX<13670;});
        Ledge(l,13455,259,98);Ledge(l,13600,235,92);
    }
}
}
