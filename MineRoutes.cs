using System;
using System.Drawing;

namespace NeonRift {
public static partial class Levels {
    static void AuthorMineRoutes(Level l) {
        // Author against final puzzle/theme geometry, replacing legacy mines
        // which could be left floating when a room was rebuilt.
        l.Hazards.RemoveAll(delegate(Hazard h){return h.Type==7;});
        if(l.Index==0) {
            Mine(l,1532,280);Mine(l,2410,300);Mine(l,5908,300);Mine(l,9012,280);Mine(l,12904,300);
        } else if(l.Index==1) {
            Mine(l,1392,260);Mine(l,1938,300);Mine(l,2710,270);Mine(l,5032,280);Mine(l,9618,300);
        } else {
            Mine(l,1460,300);Mine(l,2090,260);Mine(l,6184,300);Mine(l,7116,300);Mine(l,8728,300);Mine(l,10720,300);
        }
        // Each mine gets one readable decision, rather than being hidden
        // inside an older spike bed or a tall laser occupying the same jump.
        foreach(Hazard mine in l.Hazards.FindAll(delegate(Hazard h){return h.Type==7;})){
            float floor=mine.Y+mine.H;
            l.Hazards.RemoveAll(delegate(Hazard h){return h.Type!=7&&h.X+h.W>mine.X-80&&h.X<mine.X+mine.W+80&&h.Y+h.H>floor-40&&h.Y<floor+10;});
        }
        SeparateMineSupplies(l);
        // One shared rule, remembered for the chapter. A lit red pressure
        // cap is visible before commitment; upper routes remain optional.
        float first=l.Index==0?1460:l.Index==1?1320:1385;
        Sign(l,first,174,"红灯地雷 · 触碰立即爆炸一次；提前跳过，或用无敌冲刺和电磁脉冲通过","mine-v54");
    }
    static void SeparateMineSupplies(Level l){
        foreach(Pickup item in l.Pickups){
            if(item.Type<2)continue;
            bool crowded=false;foreach(Hazard h in l.Hazards)if(h.Type==7&&Math.Abs(item.X-(h.X+12))<78&&Math.Abs(item.Y-(h.Y+h.H-27))<55){crowded=true;break;}
            if(!crowded)continue;
            float best=float.MaxValue,bestX=item.X,bestY=item.Y;
            foreach(Platform deck in l.Platforms){
                if(deck.Kind!=0||deck.Axis!=0||deck.W<80||Math.Abs(deck.Y-27-item.Y)>110)continue;
                for(float x=deck.X+24;x<=deck.X+deck.W-24;x+=2){
                    if(Math.Abs(x-item.X)>300||!SupplySpot(l,x,deck.Y,item.Type==2))continue;
                    bool overlaps=false;foreach(Pickup other in l.Pickups)if(other!=item&&other.Type>=2&&Math.Abs(other.X-x)<58&&Math.Abs(other.Y-(deck.Y-27))<40){overlaps=true;break;}
                    foreach(Hazard h in l.Hazards)if(h.Type==7&&Math.Abs(x-h.X-12)<88&&Math.Abs(deck.Y-h.Y-h.H)<65){overlaps=true;break;}
                    if(overlaps)continue;
                    float score=Math.Abs(x-item.X)*1.3f+Math.Abs(deck.Y-27-item.Y);
                    if(score<best){best=score;bestX=x;bestY=deck.Y-27;}
                }
            }
            item.X=bestX;item.Y=bestY;
        }
    }
}
}
