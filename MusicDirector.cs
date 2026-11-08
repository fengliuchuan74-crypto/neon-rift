namespace NeonRift
{
    public sealed partial class GameForm
    {
        // Called once at the end of the UI tick. SetMusic is idempotent: repeated calls preserve playback position.
        void UpdateMusicState()
        {
            MusicScene score = MusicScene.Title;
            double intensity = screen == 2 ? 1 : .72;
            if(screen==0||screen==1||screen==6)Sound.PrepareMusic(MusicScene.Prologue,MusicScene.Rendezvous,MusicScene.Pursuit);
            if (world != null && screen == 2)
            {
                if(world.Level.Index==2)Sound.PrepareMusic(MusicScene.CastleQuiet,MusicScene.Queen,MusicScene.QueenAwakened);
                score = world.Level.Index == 0 ? MusicScene.RooftopDrive : MusicScene.Neon;
                if (world.Level.Index == 1)
                {
                    score = MusicScene.Industrial;
                    Sound.PrepareMusic(MusicScene.Rendezvous,MusicScene.Pursuit);
                    if(world.ChasePhase==1){score=MusicScene.Rendezvous;intensity=.84;}
                    else if(world.ChasePhase>=2){
                        score=MusicScene.Pursuit;
                        if(world.ChasePhase==4){intensity=.48;if(world.ChaseFarewellActive){score=MusicScene.Rendezvous;intensity=.82;}}
                        else if(world.ChasePhase==5)intensity=.48;
                        else if(world.ChasePhase>=6){score=MusicScene.Rendezvous;intensity=.78;}
                    }
                }
                else if (world.Level.Index == 2 && (world.CastleInside || world.CastleEntryActive))
                {
                    score = MusicScene.Queen;
                    if(world.CastleEntryActive){
                        for(int i=0;i<6;i++)if(world.CastleCandleRedness(i)<1){score=MusicScene.CastleQuiet;break;}
                    }
                    if(world.Boss!=null&&world.Boss.Awakened&&!world.BossAwakeningActive&&!world.CastleEntryActive&&!world.QueenArrivalActive&&!world.BossIntroActive)
                        score=MusicScene.QueenAwakened;
                    if (world.BossOutroActive || world.BossDefeated) { score = MusicScene.CastleAftermath; intensity = .65; }
                    else if (world.BossFinalStrikeActive) intensity = .52;
                    else if (world.BossAwakeningActive) intensity = QueenAwakeningMusic(QueenAwakeningSequence.Elapsed(world.Boss));
                    else if (world.CastleEntryActive || world.QueenArrivalActive) intensity = .58;
                    else if (world.BossIntroActive) intensity = .84;
                }
            }
            else if ((screen == 3 || screen == 7) && world != null)
            {
                if (world.Level.Index == 2) { score = MusicScene.CastleAftermath; intensity = .64; }
                else if (world.Level.Index == 1) { score = MusicScene.Rendezvous; intensity = .78; }
            }
            if (screen == 5) { score = MusicScene.Prologue; intensity = .90; Sound.PrepareMusic(MusicScene.RooftopDrive); }
            Sound.SetMusic(score, intensity);
            bool scorePaused=paused||inventoryOpen||confirmExit||confirmChapterRestart||(screen==5&&(ChapterIntroSuspended||ChapterIntroManualPaused));
            Sound.SetMusicPaused(scorePaused);
            if(screen==5)Sound.SyncPrologueClock(ChapterIntroElapsed,scorePaused);
            else Sound.ReleasePrologueClock();
        }
        static double QueenAwakeningMusic(float elapsed)
        {
            // Preserve the current Queen score/cursor. Extinguished candles leave
            // room for her voice; the orchestra returns with the relighting.
            if(elapsed<QueenAwakeningSequence.RoarAt)
                return MusicBlend(.68,.14,elapsed/QueenAwakeningSequence.RoarAt);
            if(elapsed<QueenAwakeningSequence.TransformStart)return .14;
            if(elapsed<QueenAwakeningSequence.RelightAt)
                return MusicBlend(.14,.31,(elapsed-QueenAwakeningSequence.TransformStart)/(QueenAwakeningSequence.RelightAt-QueenAwakeningSequence.TransformStart));
            // Allow the mixer's short volume smoothing to settle by combat resume.
            return MusicBlend(.31,1,(elapsed-QueenAwakeningSequence.RelightAt)/(QueenAwakeningSequence.Duration-QueenAwakeningSequence.RelightAt-.2f));
        }
        static double MusicBlend(double from,double to,double amount)
        {
            amount=System.Math.Max(0,System.Math.Min(1,amount));amount=amount*amount*(3-2*amount);
            return from+(to-from)*amount;
        }
    }
}
