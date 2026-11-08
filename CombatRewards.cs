using System;
using System.Collections.Generic;
using System.Drawing;
namespace NeonRift {
public sealed class CombatReward {
    public float X,Y,Remaining=.85f;
}
public sealed partial class GameWorld {
    public readonly List<CombatReward> CombatRewards=new List<CombatReward>();
    // Credit on the unique alive -> dead transition. Airborne kills and
    // summoned skeletons cannot strand their reward below the playable floor.
    private void AwardEnemyShard(float x,float y) {
        Shards++;
        CombatRewards.Add(new CombatReward{X=x,Y=y-6});
        if(CombatRewards.Count>16)CombatRewards.RemoveAt(0);
        Burst(x,y+12,Color.FromArgb(100,242,235),9,75);
        Sound.Play("coin");
    }
    private void TickCombatRewards(float dt) {
        for(int i=CombatRewards.Count-1;i>=0;i--){
            CombatReward reward=CombatRewards[i];reward.Remaining-=dt;reward.Y-=18*dt;
            if(reward.Remaining<=0)CombatRewards.RemoveAt(i);
        }
    }
}
}
