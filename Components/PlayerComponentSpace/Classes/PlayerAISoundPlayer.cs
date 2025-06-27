using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.Classes
{
    public class PlayerAISoundPlayer : AIDataBase
    {
        public bool SoundMakerStarted { get; private set; }

        public PlayerAISoundPlayer(SAINAIData aidata) : base(aidata)
        {
            SoundMakerStarted = false;
            _startPlaySoundsTime = Time.time + 0.5f;
        }

        public void InitAI()
        {
            SoundMakerStarted = false;
            _startPlaySoundsTime = Time.time + 1f;
        }

        private float _startPlaySoundsTime;

        private float _soundFrequency => (IsAI ? SOUND_FREQUENCY_AI : SOUND_FREQUENCY_HUMAN);
        private float _lastSoundPower;
        private float _nextPlaySoundTime;

        private const float SOUND_FREQUENCY_HUMAN = 0.2f;
        private const float SOUND_FREQUENCY_AI = 0.75f;
        private const float SOUND_POWER_THRESH = 1.25f;

        public bool ShallPlayAISound(float power)
        {
            if (!SoundMakerStarted)
            {

                if (_startPlaySoundsTime > Time.time)
                {
                    return false;
                }
                SoundMakerStarted = true;
            }

            if (_nextPlaySoundTime < Time.time ||
                _lastSoundPower > power * SOUND_POWER_THRESH)
            {
                _nextPlaySoundTime = Time.time + _soundFrequency;
                _lastSoundPower = power;
                return true;
            }

            return false;
        }
    }
}