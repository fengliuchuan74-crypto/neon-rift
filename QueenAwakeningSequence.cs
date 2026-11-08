using System;

namespace NeonRift {
// One timeline drives lighting, character animation, sound cues and the
// gameplay lock, so a paused or retried encounter cannot drift out of sync.
public static class QueenAwakeningSequence {
    public const float Duration=8.2f,RoarAt=1.05f,MoonAt=1.9f;
    public const float TransformStart=3f,TransformEnd=6.2f,RelightAt=6.55f;
    static float Smooth(float from,float to,float value){
        float u=Math.Max(0,Math.Min(1,(value-from)/(to-from)));return u*u*(3-2*u);
    }
    public static float Elapsed(RiftBoss boss){return boss==null?0:Math.Max(0,Math.Min(Duration,Duration-boss.AwakeningTime));}
    public static float CandleLight(RiftBoss boss,int index){
        if(index<0||index>=6)return 0;
        if(boss==null||boss.AwakeningTime<=0)return 1;
        float t=Elapsed(boss);int pair=Math.Min(index,5-index);
        return (1-Smooth(pair*.1f,pair*.1f+.5f,t))+Smooth(RelightAt+pair*.18f,RelightAt+pair*.18f+.45f,t);
    }
    public static float Darkness(float t){return Smooth(0,.85f,t)*(1-Smooth(RelightAt,7.85f,t));}
    public static float Moonlight(float t){return Smooth(MoonAt,2.95f,t)*(1-Smooth(7.2f,Duration,t));}
    public static float Transformation(float t){return Smooth(TransformStart,TransformEnd,t);}
    public static float Roar(float t){return Smooth(RoarAt,1.22f,t)*(1-Smooth(1.7f,2.35f,t));}
}
}
