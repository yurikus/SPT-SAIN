using SAIN.Models.Enums;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class BodyPartHitEffectClass : BotMedicalBase, IBotClass
    {
        public EInjurySeverity LeftArmInjury { get; private set; }
        public EInjurySeverity RightArmInjury { get; private set; }
        public EHitReaction HitReaction { get; private set; }

        public BodyPartHitEffectClass(SAINBotMedicalClass medical) : base(medical)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            if (_updateHealthTime < Time.time)
            {
                checkArmInjuries();
            }
        }

        private void checkArmInjuries()
        {
            _updateHealthTime = Time.time + 1f;
            LeftArmInjury = Medical.HitReaction.BodyParts[EBodyPart.LeftArm].InjurySeverity;
            RightArmInjury = Medical.HitReaction.BodyParts[EBodyPart.RightArm].InjurySeverity;
        }

        public void Dispose()
        {

        }

        public void GetHit(DamageInfoStruct DamageInfoStruct, EBodyPart bodyPart, float floatVal)
        {
            switch (bodyPart)
            {
                case EBodyPart.Head:
                    GetHitInHead(DamageInfoStruct);
                    break;

                case EBodyPart.Chest:
                case EBodyPart.Stomach:
                    GetHitInCenter(DamageInfoStruct);
                    break;

                case EBodyPart.LeftLeg:
                case EBodyPart.RightLeg:
                    GetHitInLegs(DamageInfoStruct);
                    break;

                default:
                    GetHitInArms(DamageInfoStruct);
                    break;
            }
        }

        private void GetHitInLegs(DamageInfoStruct DamageInfoStruct)
        {
            HitReaction = EHitReaction.Legs;
        }

        private void GetHitInArms(DamageInfoStruct DamageInfoStruct)
        {
            HitReaction = EHitReaction.Arms;
            checkArmInjuries();
        }

        private void GetHitInCenter(DamageInfoStruct DamageInfoStruct)
        {
            HitReaction = EHitReaction.Center;
        }

        private void GetHitInHead(DamageInfoStruct DamageInfoStruct)
        {
            HitReaction = EHitReaction.Head;
        }

        private float _updateHealthTime;
    }
}