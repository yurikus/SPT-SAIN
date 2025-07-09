using SAIN.Attributes;
using SAIN.Components.RotationController;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class SteeringSettings : SAINSettingsBase<SteeringSettings>, ISAINSettings
    {
        [Name("Smooth Turn Settings")]
        [Hidden]
        public Dictionary<EBotLookMode, TurnSettings> SMOOTHTURN_SETTINGS_BY_STATE = new() { 
            { EBotLookMode.Peace, new TurnSettings(0.5f, 300f) },
            { EBotLookMode.Combat, new TurnSettings(0.4f, 360f) },
            { EBotLookMode.CombatSprint, new TurnSettings(0.3f, 420f) },
            { EBotLookMode.CombatVisibleEnemy, new TurnSettings(0.35f, 420f) },
            { EBotLookMode.Aiming, new TurnSettings(0.25f, 360f ) },
            { EBotLookMode.RandomLook, new TurnSettings(0.8f, 200f ) },
        };
        
        [Name("Random Bot Aim Sway")]
        public bool RANDOMSWAY_TOGGLE = true;

        [MinMax(0f, 1f, 1000f)]
        [Advanced]
        public float RANDOMSWAY_RADIUS = 0.15f;
        
        [MinMax(0f, 10f, 10f)]
        [Advanced]
        public float RANDOMSWAY_LOOPDURATION = 3.5f;
        
        [MinMax(0f, 1f, 1000f)]
        [Advanced]
        public float RANDOMSWAY_SCALE = 0.05f; 
        
        [MinMax(45f, 90f, 100f)]
        [Advanced]
        public float TURN_PITCH_MAX = 65f;

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