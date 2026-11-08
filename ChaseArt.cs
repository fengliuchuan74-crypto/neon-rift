using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace NeonRift {
public static partial class ChaseArt {
    static readonly Color Cyan=Color.FromArgb(86,243,247),Pink=Color.FromArgb(255,95,161),White=Color.FromArgb(232,242,252);
    static Color C(int r,int g,int b){return Color.FromArgb(r,g,b);}
    static void P(Graphics g,Color c,float x,float y,float w,float h){if(w<=0||h<=0)return;using(SolidBrush b=new SolidBrush(c))g.FillRectangle(b,(int)x,(int)y,(int)w,(int)h);}
    static float Wrap(float x,float n){return (x%n+n)%n;}
    static float Clamp(float n){return Math.Max(0,Math.Min(1,n));}
    static float Ease(float n){n=Clamp(n);return n*n*(3-2*n);}
    public static void DrawWorldEncounter(Graphics g,GameWorld w){
        if(w.Level.Index!=1||w.ChaseDefeated)return;
        float x=w.ChaseMeetingX,y=300;
        P(g,C(21,31,43),x-98,y-2,236,3);
        for(int i=0;i<7;i++)P(g,C(35,110,115),x-90+i*32,y-1,16,2);
        P(g,C(31,40,58),x-102,y-106,4,108);P(g,C(77,109,134),x-105,y-108,44,5);
        P(g,Cyan,x-100,y-102,30,2);P(g,Color.FromArgb(25,80,235,240),x-92,y-98,34,96);
        P(g,C(26,39,56),x+106,y-43,11,45);P(g,Cyan,x+108,y-41,7,5);
        ChaseCharacterArt.Bike(g,x+8,y,w.Time,false,w.ChaseWeaponId,0,false);
        ChaseCharacterArt.Friend(g,x-34,y-36,w.Time,0,0);
        for(int k=0;k<3;k++){P(g,C(52,96,116),x+53+k*7,y-45,4,7);if(w.Cores>k)P(g,Cyan,x+53+k*7,y-45,4,5);}
    }
    public static void Draw(Graphics g,GameWorld w,Action<string,float,float,Color> text){
        if(w.ChasePhase==1||w.ChasePhase==2){Meeting(g,w,text);return;}
        if(w.ChasePhase==4){Victory(g,w,text);return;}
        float road=w.ChaseDistance;
        Art.Background(g,1,road*.75f,w.Time,640,360);P(g,Color.FromArgb(85,4,12,21),0,0,640,236);
        for(int i=0;i<7;i++){
            float x=Wrap(i*122-road*.36f,854)-110;
            P(g,C(15,22,37),x,109,7,141);P(g,C(45,52,68),x,109,49,4);P(g,C(145,62,105),x+5,114,29,2);P(g,C(20,38,55),x-10,243,27,7);
        }
        RoadCollapse(g,w,false);
        P(g,C(27,45,59),0,239,640,5);P(g,C(63,97,108),0,239,640,1);
        for(int i=0;i<23;i++){float x=Wrap(i*32-road*.65f,736)-32;P(g,C(35,67,82),x,243,3,26);}
        P(g,C(12,20,30),0,270,640,90);P(g,C(55,78,87),0,270,640,4);P(g,C(17,34,44),0,278,640,8);P(g,C(18,25,38),0,300,640,60);
        for(int i=0;i<12;i++){float x=Wrap(i*79-road,790)-79;P(g,C(84,112,113),x,282,26,2);P(g,C(36,72,80),x+17,300,45,3);P(g,C(30,42,59),x-29,330,40,2);}
        RoadCollapse(g,w,true);
        foreach(ChaseObject o in w.ChaseObjects)RoadObject(g,o,w.Time);
        float dissolve=w.ChasePhase==4?Clamp(w.ChasePhaseTime/3.2f):0,charge=w.ChaseAttackWarning>0?Clamp(1-w.ChaseAttackWarning/1.4f):0;
        ChaseMonsterArt.Draw(g,w.ChaseMonsterX,42,w.Time,1,dissolve,w.ChaseFlash>0,charge);ChaseMonsterArt.DrawRecovery(g,w);ChaseMonsterArt.DrawCast(g,w);ChaseMonsterArt.DrawAttacks(g,w);
        foreach(ChaseBolt b in w.ChaseBolts){Color c=b.Color;int s=Math.Max(2,b.Size);P(g,Color.FromArgb(40,c),b.X-9,b.Y-s,18,s*2+1);P(g,Color.FromArgb(95,c),b.X,b.Y-1,14,3);P(g,c,b.X-s,b.Y-1,s+3,3);P(g,White,b.X-s,b.Y,3,1);}
        float shot=w.ChaseMuzzleTime;
        if(w.Hero.Invincible<=0||(int)(w.Time*18)%3!=0)ChaseCharacterArt.Bike(g,w.ChaseBikeX,w.ChaseBikeY,w.Time,true,w.ChaseWeaponId,shot,w.ChasePhase==5);
        for(int k=0;k<9+w.ChaseCombatStage*3;k++){float x=190+Wrap(937*k-road*(1.1f+k%3*.1f),510),y=99+k%11*17;P(g,C(31,53,67),x,y,16+k%4*7+w.ChaseCombatStage*5,1);}
        // Exhaust and wheel spray stretch with actual simulated road speed.
        float rush=Clamp((w.ChaseRoadSpeed-265)/85);if(w.ChasePhase==3&&w.ChaseBikeY>263){
            for(int k=0;k<6;k++){float age=Wrap(w.Time*4+k*.17f,1);P(g,C(85,115,128),w.ChaseBikeX-47-age*(17+rush*27),269-age*(3+k%2*2),3+rush*3,1);}
        }
        P(g,C(6,10,21),0,0,640,35);P(g,C(44,55,80),0,34,640,1);
        text(w.IsBossChallenge?"挑战 / 深渊快线":"02 / 深渊快线",13,10,Cyan);text("渊喉 · 阿刻隆",202,10,C(173,230,154));
        P(g,C(25,42,45),309,12,227,10);P(g,C(114,63,98),311,14,223,6);P(g,C(177,224,133),311,14,223*Math.Max(0,w.ChaseMonsterHP)/(float)GameWorld.ChaseMonsterMaxHP,6);
        text(w.ChaseMonsterHP+" / "+GameWorld.ChaseMonsterMaxHP,548,10,White);
        P(g,Color.FromArgb(235,6,12,24),406,43,225,48);P(g,w.ChaseWeapon.Color,406,43,2,48);WeaponArt.Icon(g,w.ChaseWeaponId,new RectangleF(414,48,33,27),false);
        text(w.ChaseWeapon.Name+"  "+w.ChaseAmmo+" / "+w.ChaseWeapon.MaxAmmo,454,48,w.ChaseAmmo>0?w.ChaseWeapon.Color:Pink);text("Q / E 换枪 · B 背包 · 地图拾弹",416,72,C(156,186,204));
        if(w.ChaseAttackWarning>0||w.ChaseAttackTime>0){
            P(g,Color.FromArgb(235,6,12,24),174,43,223,48);P(g,w.ChaseAttackKind==2?C(154,246,136):Pink,174,43,2,48);
            string skill=w.ChaseAttackKind==0?(w.ChaseCombatStage>0?"错拍 · 三线冥焰":"三线冥焰"):w.ChaseAttackKind==1?(w.ChaseRetaliation?"眼核回缩 · 触手反扑":w.ChaseCombatStage>0?"二连触手 · "+w.ChaseTentacleVolley+" / 2":"锁定触手"):"腐蚀毒沼";
            string hint=w.ChaseAttackWarning>0?(w.ChaseAttackKind==0?(w.ChaseHighFlame?"高位冥焰 · 保持贴地，别跳":"低位冥焰 · 准备起跳"):w.ChaseAttackKind==1?(w.ChaseRetaliation?"左侧即将扫荡 · 向右撤离":"位置已锁定 · 移出紫框"):(w.ChasePoisonSafeSide<0?"毒印锁定 · 左侧留有通路":"毒印锁定 · 右侧留有通路")):(w.ChaseAttackKind==0?"冥焰喷发":w.ChaseAttackKind==1?"触手突袭":"毒区持续中 · 避开绿沼");
            text(skill,182,47,w.ChaseAttackKind==2?C(154,246,136):Pink);text(hint,182,68,White);
            if(w.ChaseAttackWarning>0){P(g,C(57,37,62),180,87,211,2);P(g,Pink,180,87,211*(1-w.ChaseAttackWarning/w.ChaseWarningDuration),2);}
        }
        else if(w.ChaseEyeOpen){
            Color eye=C(255,211,119);P(g,Color.FromArgb(235,6,12,24),174,43,223,48);P(g,eye,174,43,2,48);
            text("眼核暴露 · "+w.ChaseWeakpointTime.ToString("0.0")+" 秒",182,47,eye);
            text(w.ChaseBikeX<=w.ChaseCloseEyeMaxX?(w.ChaseWeaponId==2?"近射 3 倍 · 闭眼前准备撤离":"命中 2 倍 · 闭眼前准备撤离"):"命中 2 倍 · 靠左霰弹 3 倍",182,68,White);
            P(g,C(73,44,60),179,87,213,2);P(g,eye,179,87,213*w.ChaseWeakpointTime/GameWorld.ChaseWeakpointDuration,2);
        }
        text(w.ChaseCombatStage==0?"运输线逃逸":w.ChaseCombatStage==1?"外壳破裂 · 加速":"断桥极速 · 狂追",13,42,w.ChaseCombatStage>0?C(255,155,115):C(113,173,188));
        text("车速 "+((int)(w.ChaseRoadSpeed*.38f))+" km/h",13,59,C(138,177,191));
        P(g,Color.FromArgb(241,5,11,23),0,312,640,48);P(g,C(44,71,90),0,312,640,1);
        text("岚驾驶 · A/D 走位 · 空格跳跃",13,320,White);text("后座：J 开枪 · Q/E 换枪 · ESC 暂停",307,320,White);
        text("生命",13,341,White);for(int h=0;h<w.MaxHealth;h++)Art.Heart(g,48+h*15,339,h<w.Hero.Health);text(w.IsBossChallenge?"独立挑战 · 标准装备 · 无升级":"弹药与背包共用 · 晶片升级有效",305,341,C(162,184,204));
        if(w.PopupTimer>0&&w.ChasePhase==3){P(g,Color.FromArgb(225,7,17,31),182,290,449,18);text(w.Popup,190,292,Cyan);}
        if(w.ChasePhase==5){P(g,Color.FromArgb(155,6,9,22),0,35,640,278);P(g,C(13,22,39),159,129,358,76);P(g,Pink,159,129,358,2);text("追逐失败 · 返回快线检查点",195,145,Pink);text("F / 空格重试 · 已消耗弹药不会恢复",188,176,White);}
    }
    static void RoadCollapse(Graphics g,GameWorld w,bool foreground){
        float ruin=w.ChaseRoadRuin;if(ruin<=0)return;
        // Scenery breaks below/behind the playable road, so cosmetic rubble
        // never looks like an unmarked collision hazard in the bike lane.
        if(foreground){
            for(int n=0;n<12;n++){
                if((n%5)*.18f>ruin)continue;
                float x=Wrap(n*83-w.ChaseDistance,996)-83;
                P(g,C(5,12,22),x,286,3,12);P(g,C(5,12,22),x+3,296,8,3);P(g,C(5,12,22),x+9,298,3,13);
                P(g,C(110,66,66),x+3,288,1,8);P(g,C(83,64,76),x+12,300,17,2);
                if(ruin>.7f){P(g,C(7,13,22),x+27,295,22,7);P(g,C(66,69,79),x+30,295,15,2);}
            }
            return;
        }
        for(int n=0;n<6;n++){
            if(n*.14f>ruin)continue;
            float x=Wrap(n*149-w.ChaseDistance*.42f,894)-95,y=178+n%3*17;
            GraphicsState saved=g.Save();g.TranslateTransform(x,y);g.RotateTransform(-12-n*4);
            P(g,C(25,31,42),-3,-47,8,96);P(g,C(79,71,75),-3,-47,47,5);P(g,C(169,70,79),1,-41,33,2);g.Restore(saved);
            P(g,C(27,27,39),x-20,y+24,70,16);P(g,C(72,46,56),x-16,y+21,20,5);
            for(int k=0;k<7;k++){
                float flicker=(float)Math.Sin(w.Time*(4+k%3)+n*2+k)*3;
                float xx=x-12+k*7,top=y+17-(k%3+1)*4+flicker;
                P(g,C(109,39,61),xx,top-4,8,y+34-top);P(g,C(204,75,50),xx+1,top,6,y+31-top);P(g,C(248,152,73),xx+2,top+5,3,Math.Max(2,y+26-top));
                float age=(w.Time*.36f+n*.19f+k*.12f)%1;
                P(g,Color.FromArgb((int)(90*(1-age)),65,47,66),xx+age*29,top-12-age*48,9+age*12,8+age*11);
                if(k%2==0)P(g,C(219,121,73),xx+age*24,top-age*37,2,2);
            }
        }
    }
    static void SceneActor(Graphics g,bool friend,float x,float floor,float time,int expression,int pose,float scale){
        GraphicsState s=g.Save();g.TranslateTransform((int)x,(int)(floor-36*scale));g.ScaleTransform(scale,scale);
        if((friend&&pose!=3)||(!friend&&expression>=3&&pose!=3)){g.TranslateTransform(24,0);g.ScaleTransform(-1,1);}
        if(friend)ChaseCharacterArt.Friend(g,0,0,time,expression,pose);else ChaseCharacterArt.Hero(g,0,0,time,expression,pose);g.Restore(s);
    }
    static void SceneBike(Graphics g,float x,float y,float scale,GameWorld w,bool riders){GraphicsState s=g.Save();g.TranslateTransform((int)x,(int)y);g.ScaleTransform(scale,scale);ChaseCharacterArt.Bike(g,0,0,w.Time,riders,w.ChaseWeaponId,0,false);g.Restore(s);}
    static void Meeting(Graphics g,GameWorld w,Action<string,float,float,Color> text){
        float t=w.ChasePhaseTime;bool arrival=w.ChasePhase==2;float pull=arrival?Ease((t-5.2f)/1.8f):0;
        float road=arrival?Math.Max(0,t-4.6f)*Math.Max(0,t-4.6f)*75:0;
        Art.Background(g,1,w.Level.Width-640+road,w.Time,640,360);P(g,Color.FromArgb(88,5,9,21),0,0,640,360);
        float floor=238+pull*32;P(g,C(20,33,46),0,floor,640,125);P(g,C(91,117,128),0,floor,640,3);
        for(int i=0;i<20;i++){float x=Wrap(i*38-road,760);P(g,C(44,112,118),x,floor+6,18,2);}
        P(g,C(27,43,62),549,92,5,floor-92);P(g,C(87,113,132),499,88,57,5);P(g,Cyan,504,95,42,3);P(g,Color.FromArgb(18,103,254,243),471,100,96,floor-100);
        for(int i=0;i<4;i++){P(g,C(31,43,61),46+i*149,floor-27,7,27);P(g,Pink,47+i*149,floor-28,5,2);}
        if(arrival){float summon=Ease(t/1.3f);ChaseMonsterArt.Draw(g,-35*(1-summon),42,w.Time,summon,0,false,0);for(int i=0;i<14;i++)P(g,C(99,151,125),151-i*9,floor-((int)(w.Time*50)+i*17)%120,2,3);}
        float bikeX=450+(350-450)*pull,scale=2-pull;bool mounting=arrival&&t>=2.6f&&t<4.6f,boarded=arrival&&t>=4.6f;
        if(mounting){GraphicsState mount=g.Save();g.TranslateTransform((int)bikeX,(int)floor);g.ScaleTransform(scale,scale);ChaseCharacterArt.Boarding(g,0,0,w.Time,(t-2.6f)/2f,w.ChaseWeaponId);g.Restore(mount);}
        else SceneBike(g,bikeX,floor,scale,w,boarded);
        int line=Math.Max(0,Math.Min(GameWorld.ChaseLines.Length-1,w.ChaseDialogue));
        if(!boarded&&!mounting){
            float heroX=arrival?300:250+Ease(t/1.0f)*50;if(line>0)heroX=300;
            float friendX=389,heroFloor=floor,friendFloor=floor;int heroExpression=0,friendExpression=line==1||line==3?1:0,heroPose=1,friendPose=line%2==1?1:0;
            if(line==2){heroExpression=2;heroFloor-=Math.Abs((float)Math.Sin(Math.Min(1,t/.5f)*Math.PI))*5;}if(line==3)friendPose=2;if(line>=4){heroExpression=3;friendExpression=line==5?3:0;}
            if(arrival){
                heroExpression=t<2.6f?3:4;friendExpression=t<1.3f?3:4;heroFloor-=Math.Max(0,(float)Math.Sin(Math.Min(1,t/.7f)*Math.PI))*12;friendFloor-=Math.Max(0,(float)Math.Sin(Math.Min(1,t/.65f)*Math.PI))*10;friendPose=t>1.3f?2:1;
            }
            SceneActor(g,false,heroX,heroFloor,w.Time,heroExpression,heroPose,2);SceneActor(g,true,friendX,friendFloor,w.Time,friendExpression,friendPose,2);
            if(arrival&&t<2.6f){float jump=(float)Math.Sin(w.Time*16)*2;Exclamation(g,heroX+23,floor-94+jump,Cyan);Exclamation(g,friendX+23,floor-94-jump,Pink);}
        }
        P(g,C(4,7,17),0,0,640,32);text("第二关 · 运输线接应点",15,10,Cyan);
        if(!arrival){bool friend=line%2==1;int expression=line==1||line==3?1:line>=4?3:line==2?2:0;Dialogue(g,w,text,friend,expression,GameWorld.ChaseSpeakers[line],GameWorld.ChaseLines[line],"",true);}
        else{string words=t<1.3f?"你：……你说的顺风车，包含这个吗？！":t<2.6f?"快上车！我开车，你负责把它打回海里！":t<4.6f?"你：等等，安全带呢？   岚：你的求生欲就是！":"坐稳后座！拿好你的枪——出发！";Dialogue(g,w,text,t>=1.3f,t<1.3f?3:4,t<1.3f?"你 / 后座枪手":"岚 / 驾驶员",words,"",false);}
    }
    static void Exclamation(Graphics g,float x,float y,Color color){P(g,C(7,13,25),x-5,y-3,11,23);P(g,color,x-2,y,5,12);P(g,White,x-1,y,2,8);P(g,color,x-2,y+16,5,4);}
    static void Dialogue(Graphics g,GameWorld w,Action<string,float,float,Color> text,bool friend,int expression,string speaker,string words,string extra,bool advance){
        Color edge=friend?Pink:Cyan;P(g,Color.FromArgb(248,6,12,26),8,253,624,103);P(g,C(48,65,85),9,254,622,1);P(g,edge,8,253,4,103);P(g,edge,12,253,68,2);P(g,edge,573,354,59,2);P(g,C(21,31,49),17,261,70,85);
        ChaseCharacterArt.Portrait(g,new Rectangle(20,263,64,77),friend,expression,w.Time);text(speaker,99,261,edge);
        string first=words,second=extra;if(words.Length>42){int cut=words.LastIndexOf('，',Math.Min(40,words.Length-1));if(cut<20)cut=38;first=words.Substring(0,cut+1);second=words.Substring(cut+1);}
        text(first,99,283,White);if(second.Length>0)text(second,99,303,White);
        text(advance?"F / ENTER 继续 · 也会自动推进":"演出进行中 · ESC 暂停",advance?360:434,336,C(152,179,199));if(advance)text(w.ChasePhase==4?"暂别 0"+(w.ChaseFarewellDialogue+1)+" / 04":"接应记录 0"+(w.ChaseDialogue+1)+" / 0"+GameWorld.ChaseLines.Length,99,336,C(106,136,160));
    }
    static void RoadObject(Graphics g,ChaseObject o,float time){
        if(o.Used&&o.Kind!=1)return;float x=o.X,y=o.Y;
        if(o.Kind==1){P(g,C(3,7,16),x,270,o.W,76);P(g,C(89,54,77),x,270,6,11);P(g,C(89,54,77),x+o.W-6,270,6,11);P(g,Pink,x,270,5,3);P(g,Pink,x+o.W-5,270,5,3);for(int k=0;k<5;k++)P(g,C(34,24,48),x+10+k*15,282+k%3*8,9,6);}
        else if(o.Kind==0){P(g,C(12,19,30),x-3,y-3,o.W+6,o.H+4);P(g,C(99,79,76),x,y,o.W,o.H);P(g,C(243,159,70),x,y,o.W,4);P(g,C(46,46,55),x+3,y+8,o.W-6,o.H-11);for(int i=0;i<o.W-9;i+=11)for(int j=0;j<15;j++)P(g,C(219,116,61),x+4+i+j/3,y+8+j,3,1);P(g,Pink,x+4,y-5,5,3);P(g,Pink,x+o.W-9,y-5,5,3);}
        else if(o.Kind==4){P(g,C(44,53,65),x+7,89,3,y-89);P(g,C(44,53,65),x+o.W-10,89,3,y-89);P(g,C(25,39,57),x,y,o.W,o.H);P(g,C(102,82,87),x,y,o.W,4);P(g,C(42,65,79),x+4,y+7,o.W-8,o.H-12);for(int k=0;k<5;k++)P(g,C(21,36,52),x+8+k*12,y+8,3,31);P(g,Pink,x,y+o.H-3,o.W,3);}
        else{Color c=o.Kind==2?Weapons.All[o.AmmoWeaponId].Color:C(120,255,152);float bob=(float)Math.Sin(time*6)*2;P(g,Color.FromArgb(40,c),x-4,y-4+bob,o.W+8,o.H+8);P(g,C(10,22,35),x,y+bob,o.W,o.H);P(g,c,x+2,y+2+bob,o.W-4,2);P(g,c,x+2,y+o.H-4+bob,o.W-4,2);if(o.Kind==2){for(int i=0;i<(o.AmmoWeaponId==2?3:2);i++){P(g,c,x+6+i*5,y+7+bob,3,10);P(g,White,x+6+i*5,y+6+bob,2,2);}}else{P(g,c,x+9,y+6+bob,6,14);P(g,c,x+5,y+10+bob,14,6);}}
    }
}
}
