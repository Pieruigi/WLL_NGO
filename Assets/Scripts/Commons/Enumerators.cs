using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO
{
    public enum GameMode { Powered, GoldenGoal, Classic }

    /// <summary>
    /// Normal: normal state, you can move, shoot, etc
    /// ReceivingPassage: you can't move it but you can still pass or shot
    /// Busy: you are busy in some way, for example on kick off
    /// Stunned: you are stunned, means you neither can't move nor shoot
    /// </summary>
    public enum PlayerState : byte { Normal, Receiver, Busy, Stunned, Tackling, Shooting }

    public enum TackleType : byte { Slide, Kick, Slap, Elbow }

    public enum StunType : byte { ByContrast, BySlide, ByKick, BySlap, ByElbow, Electrified }

    public enum StunDetail : byte { Front, Back }

    public enum ButtonState: byte { None, Pressed, Held, Released }

    public enum MatchState : byte { NotReady, StartingMatch }

    public enum  ShotTiming : byte { Bad, Normal, Good, Perfect }
    

}
