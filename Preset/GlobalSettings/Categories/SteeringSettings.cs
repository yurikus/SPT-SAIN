using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class SteeringSettings : SAINSettingsBase<SteeringSettings>, ISAINSettings
    {
        [MinMax(50f, 1000f, 100f)]
        [Advanced]
        public float SMOOTHTURN_MAXTURNSPEED_DEGREES = 500f;

        [Advanced]
        [MinMax(0f, 1f, 100f)]
        public float SMOOTHTURN_SMOOTHING = 0.3f;

        [Advanced]
        [MinMax(0f, 1f, 100f)]
        public float SMOOTHTURN_SMOOTHING_AIM = 0.1f;

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