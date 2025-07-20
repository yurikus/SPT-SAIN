using SAIN.Attributes;
using SAIN.Components.RotationController;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class SteeringSettings : SAINSettingsBase<SteeringSettings>, ISAINSettings
    {
        [Name("Smooth Turn Settings")]
        [Hidden]
        public Dictionary<EBotLookMode, TurnSettings> SMOOTHTURN_SETTINGS = new() {
            { EBotLookMode.Peace, new TurnSettings(0.5f, 300f) },
            { EBotLookMode.Combat, new TurnSettings(0.4f, 480f) },
            { EBotLookMode.CombatSprint, new TurnSettings(0.3f, 540f) },
            { EBotLookMode.CombatVisibleEnemy, new TurnSettings(0.35f, 540f) },
            { EBotLookMode.Aiming, new TurnSettings(0.15f, 540f ) },
            { EBotLookMode.RandomLook, new TurnSettings(0.8f, 200f ) },
        };
        
        [Name("Max Path Length")]
        [Description("How far along a path to an enemy a bot will check vision to, in meters.")]
        [Category("Enemy Path Visibility System")]
        [MinMax(0f, 500f, 1)]
        [Advanced]
        public float DistToCheckVision = 50.0f;
        
        [Name("Path Nodes Stack Height")]
        [Description("X number of points will be generated above each node in the path")]
        [Category("Enemy Path Visibility System")]
        [MinMax(2f, 20, 1)]
        [Advanced]
        public float GeneratePointStackHeight = 4;

        [Name("Max Path Nodes - Player")]
        [Category("Enemy Path Visibility System")]
        [MinMax(0f, 2048, 1)]
        [Advanced]
        public float MaxPathPoints = 512;
        
        [Name("Max Path Nodes - Bot")]
        [MinMax(0f, 2048, 1)]
        [Advanced]
        public float MaxPathPoints_AI = 128;
        
        [Name("Distance Between Nodes - Player")]
        [Category("Enemy Path Visibility System")]
        [MinMax(0.01f, 1, 1000)]
        [Advanced]
        public float DistanceBetweenPoints = 0.33f;
        
        [Name("Distance Between Nodes - Bot")]
        [Category("Enemy Path Visibility System")]
        [MinMax(0.01f, 1, 1000)]
        [Advanced]
        public float DistanceBetweenPoints_AI = 0.66f;
        
        [Name("Random Bot Aim Sway")]
        [Category("Random Sway")]
        public bool RANDOMSWAY_TOGGLE = true;
        
        [Category("Random Sway")]
        [MinMax(0f, 1f, 1000f)]
        [Advanced]
        public float RANDOMSWAY_CIRCLE_RADIUS = 0.035f;
        
        [Category("Random Sway")]
        [MinMax(0f, 10f, 10f)]
        [Advanced]
        public float RANDOMSWAY_LOOP_DURATION = 3.5f;
        
        [Category("Random Sway")]
        [MinMax(0f, 1f, 1000f)]
        [Advanced]
        public float RANDOMSWAY_CIRCLE_SCALE = 0.015f;
        
        [Category("Random Sway")]
        [MinMax(45f, 90f, 100f)]
        [Advanced]
        public float TURN_PITCH_MAX = 65f;

        [Name("Last Seen to Last Known Position Distance Threshold")]
        [Description("If the last known position of an enemy (something heard or reported by their squad) is within X distance (meters) to the place they last saw an enemy, focus on the place they were last seen.")]
        [Advanced]
        [MinMax(0f, 50f, 1000f)]
        public float STEER_LASTSEEN_TO_LASTKNOWN_DISTANCE = 2.5f;

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
        }
    }
}