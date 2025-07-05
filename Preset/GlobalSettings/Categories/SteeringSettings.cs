using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class SteeringSettings : SAINSettingsBase<SteeringSettings>, ISAINSettings
    {
        [Name("Smooth Bot Turn")]
        [Description("When this is enabled, bots will not turn at a constant speed, scale the speed based on the angle amount they are turning.")]
        [Category("Character Turning")]
        public bool SMOOTH_TURN_TOGGLE = true;

        [MinMax(0.01f, 5.0f, 100f)]
        [Category("Character Turning")]
        [Advanced]
        public float SmoothTurn_MaxTurnSpeed = 1.75f;
        [Advanced]
        [MinMax(0f, 3f, 100f)]
        [Category("Character Turning")]
        public float SmoothTurn_Smoothing = 0.35f;
        [Advanced]
        [MinMax(0f, 3f, 100f)]
        [Category("Character Turning")]
        public float SmoothTurn_X_Coef = 1.0f;
        [Advanced]
        [MinMax(0f, 3f, 100f)]
        [Category("Character Turning")]
        public float SmoothTurn_Y_Coef = 1.0f;
        [Advanced]
        [MinMax(0f, 3f, 100f)]
        [Category("Character Turning")]
        public float SmoothTurn_Z_Coef = 1.0f;

        [Name("Last Seen to Last Known Position Distance Threshold")]
        [Description("If the last known position of an enemy (something heard or reported by their squad) is within X distance (meters) to the place they last saw an enemy, focus on the place they were last seen.")]
        [Category("Look Point Decision")]
        [Advanced]
        [MinMax(0f, 50f, 1000f)]
        public float STEER_LASTSEEN_TO_LASTKNOWN_DISTANCE = 2.5f;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}