using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class HearingBulletAnalysisClass : BotSubClass<SAINHearingSensorClass>, IBotClass
    {
        private const float BULLET_FEEL_DOT_THRESHOLD = 0.75f;
        private const float BULLET_FEEL_MAX_DIST = 600f;

        public HearingBulletAnalysisClass(SAINHearingSensorClass hearing) : base(hearing)
        {
        }

        public bool DoIFeelBullet(AISoundData sound)
        {
            if (sound.PlayerDistance > BULLET_FEEL_MAX_DIST)
            {
                return false;
            }
            Vector3 directionToBot = (Bot.Position - sound.Position).normalized;
            float dot = Vector3.Dot(sound.HeardPlayer.LookDirection, directionToBot);
            return dot >= BULLET_FEEL_DOT_THRESHOLD;
        }

        public bool DidShotFlyByMe(AISoundData sound, out Vector3 ProjectionPoint, out float ProjectionPointDistance)
        {
            ProjectionPointDistance = 0;
            ProjectionPoint = Vector3.zero;
            if (BaseClass.SoundInput.IgnoreUnderFire)
            {
                return false;
            }

            ProjectionPoint = CalcProjectionPoint(sound.HeardPlayerComponent, sound.PlayerDistance);
            float pointDistanceSqr = (ProjectionPoint - Bot.Position).sqrMagnitude;

            float maxDist = SAINPlugin.LoadedPreset.GlobalSettings.Mind.SUPP_DISTANCE_SCALE_END;
            float maxDistSqr = maxDist * maxDist;
            if (pointDistanceSqr > maxDistSqr)
            {
                return false;
            }

            // if the direction the player shot hits a wall, and the point that they hit is further than our input max distance, the shot did not fly by the bot.
            if (!sound.Enemy.InLineOfSight)
            {
                Vector3 firePort = sound.HeardPlayerComponent.Transform.WeaponFirePort;
                Vector3 direction = ProjectionPoint - firePort;
                if (Physics.Raycast(firePort, direction, out var hit, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask) &&
                    (hit.point - Bot.Position).sqrMagnitude > maxDistSqr)
                {
                    return false;
                }
            }

            if (SAINPlugin.DebugSettings.Logs.DebugHearing)
            {
                DebugGizmos.Sphere(ProjectionPoint, 0.25f, Color.red, true, 60f);
                DebugGizmos.Line(ProjectionPoint, sound.HeardPlayerComponent.Transform.WeaponFirePort, Color.red, 0.1f, true, 60f, true);
            }

            ProjectionPointDistance = Mathf.Sqrt(pointDistanceSqr);
            return true;
        }

        private static Vector3 CalcProjectionPoint(PlayerComponent playerComponent, float realDistance)
        {
            Vector3 weaponPointDir = playerComponent.Transform.WeaponPointDirection;
            Vector3 shotPos = playerComponent.Transform.WeaponFirePort;
            Vector3 projectionPoint = shotPos + (weaponPointDir * realDistance);
            return projectionPoint;
        }
    }
}