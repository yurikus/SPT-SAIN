using EFT;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINBotMedicalClass : BotComponentClassBase
    {
        public SAINBotMedicalClass(BotComponent sain) : base(sain)
        {
            TickRequirement = ESAINTickState.OnlyNoSleep;
            Surgery = new BotSurgery(sain);
            HitReaction = new SAINBotHitReaction(sain);
            HitByEnemy = new BotHitByEnemyClass(sain);
        }

        public BotSurgery Surgery { get; private set; }
        public SAINBotHitReaction HitReaction { get; private set; }
        public BotHitByEnemyClass HitByEnemy { get; private set; }

        public void TryCancelHeal()
        {
            if (_nextCancelTime < Time.time)
            {
                _nextCancelTime = Time.time + _cancelFreq;
                BotOwner.Medecine?.FirstAid?.CancelCurrent();
            }
        }

        private float _nextCancelTime;
        private float _cancelFreq = 1f;

        public override void Init()
        {
            Player.BeingHitAction += GetHit;
            Surgery.Init();
            HitReaction.Init();
            HitByEnemy.Init();
            base.Init();
        }

        public override void ManualUpdate()
        {
            Surgery.ManualUpdate();
            HitReaction.ManualUpdate();
            HitByEnemy.ManualUpdate();
            base.ManualUpdate();
        }

        public override void Dispose()
        {
            if (Player != null)
                Player.BeingHitAction -= GetHit;
            Surgery?.Dispose();
            HitReaction?.Dispose();
            HitByEnemy?.Dispose();
            base.Dispose();
        }

        public void GetHit(DamageInfoStruct DamageInfoStruct, EBodyPart bodyPart, float floatVal)
        {
            TimeLastShot = Time.time;
            HitByEnemy.GetHit(DamageInfoStruct, bodyPart, floatVal);
            HitReaction.GetHit(DamageInfoStruct, bodyPart, floatVal);
            Bot.Cover.GetHit(DamageInfoStruct, bodyPart, floatVal);
        }

        public float TimeLastShot { get; private set; }
        public float TimeSinceShot => Time.time - TimeLastShot;
    }
}