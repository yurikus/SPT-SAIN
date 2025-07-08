using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
using SAIN.Classes;
using SAIN.Components.BotController;
using SAIN.Components.BotControllerSpace.Classes.Raycasts;
using SAIN.Components.PlayerComponentSpace.Classes;
using SAIN.Components.PlayerComponentSpace.Classes.Equipment;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Info;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Components.PlayerComponentSpace
{
    public class PlayerComponent : MonoBehaviour, IDisposable
    {
        private const int MaxCachedSounds = 4;

        public event Action<WeaponInfo, Vector3> OnShoot;

        public event Action<PlayerComponent, EftBulletClass> OnBulletFlyBy;

        public string ProfileId { get; private set; }
        public FlashLightClass Flashlight { get; private set; }
        public PersonClass Person { get; private set; }
        public SAINAIData AIData { get; private set; }
        public SAINEquipmentClass Equipment { get; private set; }

        public bool IsActive => Person.Active;
        public Vector3 Position => Person.Transform.Position;
        public Vector3 LookDirection => Person.Transform.LookDirection;
        public Vector3 LookSensorPosition => Transform.EyePosition;

        public PersonTransformClass Transform => Person.Transform;
        public Player Player => Person.Player;
        public IPlayer IPlayer => Person.IPlayer;
        public string Name => Person.Name;
        public BotOwner BotOwner => Person.AIInfo.BotOwner;
        public BotComponent BotComponent => Person.AIInfo.BotComponent;
        public bool IsAI => Person.AIInfo.IsAI;
        public bool IsSAINBot => Person.AIInfo.IsSAINBot;

        public event Action<string> OnComponentDestroyed;

        public PlayerMovementSmoothingClass SmoothController { get; private set; } = new();

        public PlayerTickData PlayerTickData { get; private set; }
        public OtherPlayersData OtherPlayersData { get; private set; }
        public BodyPartsClass BodyParts { get; private set; }

        public event Action<Weapon, Weapon> OnWeaponEquipped;

        public Weapon CurrentWeapon {
            get
            {
                return _currentWeapon;
            }
            private set
            {
                if (_currentWeapon != value)
                {
                    Weapon LastWeapon = _currentWeapon;
#if DEBUG
                    Logger.LogDebug($"[{Player?.Profile.Nickname}] Equipped Weapon [{value?.ShortName}] Last Weapon [{LastWeapon?.ShortName}]");
#endif
                    _currentWeapon = value;
                    OnWeaponEquipped?.Invoke(value, LastWeapon);
                }
            }
        }

        public event Action<Item, Item> OnItemEquipped;

        public Item ItemInHands {
            get
            {
                return _currentItem;
            }
            private set
            {
                if (_currentItem != value)
                {
                    Item LastItem = _currentItem;
#if DEBUG
                    Logger.LogDebug($"[{Player?.Profile.Nickname}] Equipped Item [{value?.ShortName}] Last Item [{LastItem?.ShortName}]");
#endif
                    _currentItem = value;
                    OnItemEquipped?.Invoke(value, LastItem);
                }
            }
        }

        public List<SoundEvent> AISoundCachedEvents { get; private set; } = [];

        public void SetItemEquippedInHands(Item Item)
        {
            ItemInHands = Item;
            CurrentWeapon = Item as Weapon;
        }

        public void PlayAISound(SAINSoundType InSoundType, Vector3 InPosition, float InRange, float InVolume, EPhraseTrigger Phrase = EPhraseTrigger.None, ETagStatus TagStatus = ETagStatus.Unaware)
        {
            if (IsActive && AIData.AISoundPlayer.ShallPlayAISound())
            {
#if DEBUG
                if (Player.IsYourPlayer)
                {
                    Logger.LogDebug($"Sound Cached: [{InSoundType}, {InRange}, {InVolume}]");
                }
#endif
                AddCachedAISoundEvent(InSoundType, InPosition, InRange, InVolume, Phrase, TagStatus);
            }
        }

        protected void AddCachedAISoundEvent(SAINSoundType InSoundType, Vector3 InPosition, float InRange, float InVolume, EPhraseTrigger Phrase = EPhraseTrigger.None, ETagStatus TagStatus = ETagStatus.Unaware)
        {
            float SoundSpeed = 343;
            if (InSoundType.IsGunShot())
            {
                WeaponInfo Weapon = Equipment?.CurrentWeaponInfo;
                if (Weapon != null)
                {
                    SoundSpeed = Weapon.BulletSpeed;
                }
                InVolume *= SAINPlugin.LoadedPreset.GlobalSettings.Hearing.GunshotAudioMultiplier;
            }
            else
            {
                InVolume *= SAINPlugin.LoadedPreset.GlobalSettings.Hearing.FootstepAudioMultiplier;
            }
            if (!AIData.PlayerLocation.InBunker)
            {
                var weather = SAINWeatherClass.Instance;
                if (weather != null)
                {
                    if (Player.AIData.EnvironmentId == 0)
                    {
                        InVolume *= weather.RainSoundModifierOutdoor;
                    }
                    else
                    {
                        InVolume *= weather.RainSoundModifierIndoor;
                    }
                }
            }
            int Count = AISoundCachedEvents.Count;
            if (Count >= MaxCachedSounds)
            {
                bool ShallInsert = false;
                float BaseRange = InRange * InVolume;
                for (int i = 0; i < Count; i++)
                {
                    if (BaseRange < AISoundCachedEvents[i].BaseRangeWithVolume)
                    {
                        continue;
                    }
                    ShallInsert = true;
                    break;
                }
                if (!ShallInsert)
                {
                    return;
                }
                AISoundCachedEvents.Add(new(InSoundType, InPosition, this, InRange, InVolume, SoundSpeed, Phrase, TagStatus));
                AISoundCachedEvents.Sort((a, b) => b.BaseRangeWithVolume.CompareTo(a.BaseRangeWithVolume));
                AISoundCachedEvents.RemoveAt(AISoundCachedEvents.Count - 1);
                return;
            }
            AISoundCachedEvents.Add(new(InSoundType, InPosition, this, InRange, InVolume, SoundSpeed, Phrase, TagStatus));
        }

        public float GetDistanceToPlayer(string ProfileId)
        {
            if (OtherPlayersData.DataDictionary.TryGetValue(ProfileId, out var Data))
            {
                return Data.DistanceData.Distance;
            }
            return float.MaxValue;
        }


        public void ManualUpdate(float currentTime, float deltaTime)
        {
            Person.Update();

            if (!Person.ActivationClass.PlayerActive)
            {
                return;
            }

            if (IsAI)
            {
                BotOwner botOwner = Person.AIInfo.BotOwner;
                if (botOwner != null)
                {
                    SmoothController.ManualUpdate(currentTime, deltaTime, Player, botOwner, Person.AIInfo.BotComponent);
                }
            }

            if (!IsAI || Person.ActivationClass.BotActive)
            {
                drawTransformGizmos();
                Flashlight.Update();
                Equipment.Update();
            }
        }

        public void ManualLateUpdate()
        {
            Person?.LateUpdate();
        }

        public PlayerTickData GetPreparedTickData()
        {
            var data = PlayerTickData;
            data.Prepare(this);
            PlayerTickData = data;
            return data;
        }

        public void SetTickData(PlayerTickData data)
        {
            PlayerTickData = data;
            //SteeringData = data.SteeringData;
        }

        public void RegisterFlyBy(PlayerComponent Source, EftBulletClass Bullet)
        {
            OnBulletFlyBy?.Invoke(Source, Bullet);
        }

        public void OnMakingShot(IWeapon Weapon, Vector3 Force)
        {
            if (Player.IsYourPlayer)
            {
                //Logger.LogDebug($"Shoot");
            }
            if (IsActive)
            {
                WeaponInfo WeaponInfo = Equipment.CurrentWeaponInfo;
                if (WeaponInfo != null)
                {
                    OnShoot?.Invoke(WeaponInfo, Force);
                    PlayAISound(WeaponInfo.SoundType, Transform.WeaponFirePort, WeaponInfo.CalculatedAudibleRange, 1);
                    if (Player.IsYourPlayer)
                    {
                        //Logger.LogDebug($"Shoot");
                    }
                }
                else
                {
                    //Logger.LogError("WeaponInfo Null");
                }
            }
            else
            {
                //Logger.LogDebug("Player Not Active");
            }
        }

        private void StartCoroutines()
        {
            _gearCoroutine ??= StartCoroutine(Equipment.GearInfo.GearUpdateLoop());
        }

        private void StopCoroutines()
        {
            if (_gearCoroutine != null)
            {
                StopCoroutine(_gearCoroutine);
                _gearCoroutine = null;
            }
            StopAllCoroutines();
        }

        public bool Init(IPlayer iPlayer)
        {
            ProfileId = iPlayer.ProfileId;

            try
            {
                PlayerData playerData = new(this, iPlayer as Player, iPlayer);
                Person = new PersonClass(playerData);

                OtherPlayersData = new OtherPlayersData(this);
                PlayerTickData = new PlayerTickData(this);
                BodyParts = new BodyPartsClass(this);
                Flashlight = new FlashLightClass(this);
                Equipment = new SAINEquipmentClass(this);
                AIData = new SAINAIData(Equipment.GearInfo, this);
                Person.ActivationClass.OnPlayerActiveChanged += handleCoroutines;
                handleCoroutines(true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return false;
            }
            //Logger.LogDebug($"{Person.Nickname} Player Component Created");
            StartCoroutine(DelayInit());
            //StartCoroutine(voiceTest());
            return true;
        }

        private void handleCoroutines(bool active)
        {
            if (active)
                StartCoroutines();
            else
                StopCoroutines();
        }

        private IEnumerator DelayInit()
        {
            yield return null;
            Equipment.Init();
            if (Player.HandsController?.Item != null)
            {
                SetItemEquippedInHands(Player.HandsController.Item);
            }
        }

        public void InitBotOwner(BotOwner botOwner)
        {
            Person.ActivationClass.OnPlayerActiveChanged -= handleCoroutines;
            Person.ActivationClass.OnBotActiveChanged += handleCoroutines;
            Person.InitBot(botOwner);
        }

        public void InitBotComponent(BotComponent bot)
        {
            Person.InitBot(bot);
        }

        private void OnDisable()
        {
            Person.ActivationClass.Disable();
            StopCoroutines();
        }

        public void Dispose()
        {
            Logger.LogDebug($"Destroying Playing Component for [Name: {Person?.Name} : Nickname: {Person?.Nickname}, ProfileID: {Person?.ProfileId}, at time: {Time.time}]");
            OnComponentDestroyed?.Invoke(ProfileId);
            StopCoroutines();
            Person.ActivationClass.OnBotActiveChanged -= handleCoroutines;
            Person.ActivationClass.OnPlayerActiveChanged -= handleCoroutines;
            Equipment?.Dispose();
            OtherPlayersData?.Dispose();
            Destroy(this);
        }

        private void navRayCastAllDir()
        {
            if (!SAINPlugin.DebugMode ||
                !SAINPlugin.DrawDebugGizmos ||
                !Player.IsYourPlayer)
            {
                return;
            }

            Vector3 origin = Position;
            if (NavMesh.SamplePosition(origin, out var hit, 1f, -1))
            {
                origin = hit.position;
            }

            Vector3 direction;
            int max = 5;
            for (int i = 0; i < max; i++)
            {
                direction = UnityEngine.Random.onUnitSphere;
                direction.y = 0;
                direction = direction.normalized * 30f;
                Vector3 target = origin + direction;
                if (NavMesh.Raycast(origin, target, out var hit2, -1))
                {
                    target = hit2.position;
                }
                DebugGizmos.Line(origin, target, 0.05f, 0.25f, true);
            }
        }

        private void testObjectInFront()
        {
            if (!Player.IsYourPlayer)
            {
                return;
            }
            if (_hitLabel == null)
            {
                _hitLabel = DebugGizmos.CreateLabel(Vector3.zero, string.Empty);
            }
            if (_hitLabel != null)
            {
                _hitLabel.StringBuilder.Clear();
                if (Physics.Raycast(Transform.EyePosition, Transform.LookDirection, out var hit, 100f, LayerMaskClass.DoorLayer))
                {
                    _hitLabel.Enabled = true;
                    _hitLabel.WorldPos = hit.point;
                    _hitLabel.StringBuilder.AppendLine($"{hit.collider.gameObject.name}");
                    _hitLabel.StringBuilder.AppendLine($"{LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                    _hitLabel.StringBuilder.AppendLine($"{hit.distance}");
                    Door door = hit.collider.gameObject.GetComponent<Door>();
                    if (door != null)
                    {
                        _hitLabel.StringBuilder.AppendLine($"Found Door: [{door.Id}]");
                    }
                    NavMeshDoorLink link = hit.collider.gameObject.GetComponent<NavMeshDoorLink>();
                    if (link != null)
                    {
                        _hitLabel.StringBuilder.AppendLine($"Found Link: [{link.Id}]");
                    }
                }
                if (Physics.Raycast(Transform.EyePosition, Transform.LookDirection, out hit, 100f, LayerMaskClass.PlayerStaticDoorMask))
                {
                    _hitLabel.Enabled = true;
                    _hitLabel.WorldPos = hit.point;
                    _hitLabel.StringBuilder.AppendLine($"{hit.collider.gameObject.name}");
                    _hitLabel.StringBuilder.AppendLine($"{LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                    _hitLabel.StringBuilder.AppendLine($"{hit.distance}");
                    Door door = hit.collider.gameObject.GetComponent<Door>();
                    if (door != null)
                    {
                        _hitLabel.StringBuilder.AppendLine($"Found Door: [{door.Id}]");
                    }
                    NavMeshDoorLink link = hit.collider.gameObject.GetComponent<NavMeshDoorLink>();
                    if (link != null)
                    {
                        _hitLabel.StringBuilder.AppendLine($"Found Link: [{link.Id}]");
                    }
                }
                if (Physics.Raycast(Transform.EyePosition, Transform.LookDirection, out hit, 100f, LayerMaskClass.InteractiveMask))
                {
                    _hitLabel.Enabled = true;
                    _hitLabel.WorldPos = hit.point;
                    _hitLabel.StringBuilder.AppendLine($"{hit.collider.gameObject.name}");
                    _hitLabel.StringBuilder.AppendLine($"{LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                    _hitLabel.StringBuilder.AppendLine($"{hit.distance}");
                    Door door = hit.collider.gameObject.GetComponent<Door>();
                    if (door != null)
                    {
                        _hitLabel.StringBuilder.AppendLine($"Found Door: [{door.Id}]");
                    }
                    NavMeshDoorLink link = hit.collider.gameObject.GetComponent<NavMeshDoorLink>();
                    if (link != null)
                    {
                        _hitLabel.StringBuilder.AppendLine($"Found Link: [{link.Id}]");
                    }
                }
                else if (Physics.Raycast(Transform.EyePosition, Transform.LookDirection, out hit, 100f, LayerMaskClass.HighPolyWithTerrainMaskAI))
                {
                    _hitLabel.Enabled = true;
                    _hitLabel.WorldPos = hit.point;
                    _hitLabel.StringBuilder.AppendLine($"{hit.collider.gameObject.name}");
                    _hitLabel.StringBuilder.AppendLine($"{LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                    _hitLabel.StringBuilder.AppendLine($"{hit.distance}");
                    var ballistic = hit.collider.gameObject.GetComponent<BallisticCollider>();
                    if (ballistic != null)
                    {
                        _hitLabel.StringBuilder.AppendLine($"Found Ballistic: [{ballistic.name}, {ballistic.PenetrationChance}, {ballistic.PenetrationLevel}]");
                    }
                    var components = hit.collider.gameObject.GetComponentsInChildren(typeof(Component));
                    foreach (var component in components)
                    {
                        _hitLabel.StringBuilder.AppendLine($"Found [{component.name}] : Type [{component.GetType()}]");
                    }
                }
                else
                {
                    _hitLabel.Enabled = false;
                }

                if (_hitLabel.Enabled)
                {
                    DebugGizmos.Sphere(_hitLabel.WorldPos, 0.025f, 0.05f);
                }
            }
        }

        private void testNavMeshNodes()
        {
            List<Vector3> visibleNodes = new();
            Vector3 origin = Transform.EyePosition;
            Vector3[] vertices = NavMesh.CalculateTriangulation().vertices;
            foreach (Vector3 vert in vertices)
            {
                Vector3 direction = (vert - origin);
                float sqrMag = direction.sqrMagnitude;
                if (sqrMag > 100f * 100f)
                {
                    continue;
                }
                float distance = Mathf.Sqrt(sqrMag);
                if (!Physics.Raycast(origin, direction, distance, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    visibleNodes.Add(vert);
                    continue;
                }
                direction.y += 0.5f;
                if (!Physics.Raycast(origin, direction, distance, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    visibleNodes.Add(vert);
                    continue;
                }
                direction.y += 0.5f;
                if (!Physics.Raycast(origin, direction, distance, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    visibleNodes.Add(vert);
                    continue;
                }
                direction.y += 0.5f;
                if (!Physics.Raycast(origin, direction, distance, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    visibleNodes.Add(vert);
                    continue;
                }
            }
            foreach (var visibleVert in visibleNodes)
            {
                DebugGizmos.Ray(visibleVert, Vector3.up, Color.green, 1.5f, 0.025f, true, 0.25f);
            }
        }

        private IEnumerator voiceTest()
        {
            while (true)
            {
                yield return playPhrases(EPhraseTrigger.Warning);
                yield return playPhrases(EPhraseTrigger.Mooing);
                yield return playPhrases(EPhraseTrigger.NeedHelp);
                yield return playPhrases(EPhraseTrigger.Greetings);
                yield return playPhrases(EPhraseTrigger.Toxic);
                yield return playPhrases(EPhraseTrigger.StartHeal);
                yield return null;
            }
        }

        public bool PlayVoiceLine(EPhraseTrigger phrase, ETagStatus mask, bool aggressive)
        {
            var speaker = Player.Speaker;
            if (speaker.Speaking || speaker.Busy)
            {
                return false;
            }

            //if (aggressive &&
            //    speaker.PhrasesBanks.TryGetValue(phrase, out var phrasesBank)) {
            //    _aggroIndexes.Clear();
            //    int count = phrasesBank.Clips.Length;
            //    for (int i = 0; i < count; i++) {
            //        if (phrasesBank.Clips[i].Clip.name.EndsWith("_bl")) {
            //            _aggroIndexes.Add(i);
            //        }
            //    }
            //
            //    if (_aggroIndexes.Count > 0) {
            //        int index = _aggroIndexes.PickRandom();
            //        speaker.PlayDirect(phrase, index);
            //        //Logger.LogInfo($"{phrase} :: {phrasesBank.Clips[index].Clip.name} :: {index}");
            //        return true;
            //    }
            //}

            return speaker.Play(phrase, mask, true, null) != null;
        }

        private IEnumerator playPhrases(EPhraseTrigger trigger)
        {
            var speaker = Player.Speaker;
            if (speaker.PhrasesBanks.TryGetValue(trigger, out var phrasesBank))
            {
                int count = phrasesBank.Clips.Length;
                Logger.LogDebug($" Playing {trigger} {count}");
                for (int i = 0; i < count; i++)
                {
                    bool said = false;
                    while (!said)
                    {
                        if (!speaker.Speaking && !speaker.Busy)
                        {
                            speaker.PlayDirect(trigger, i);
                            Logger.LogDebug($"{trigger} :: {phrasesBank.Clips[i].Clip.name} :: {i}");
                            said = true;
                        }
                        yield return null;
                    }
                }
            }
            else
            {
                Logger.LogDebug($"{trigger} no phrases");
            }
        }

        private void drawTransformGizmos()
        {
            if (SAINPlugin.DebugMode &&
                SAINPlugin.DrawDebugGizmos &&
                SAINPlugin.DebugSettings.Gizmos.DrawTransformGizmos)
            {
                DebugGizmos.Sphere(Transform.EyePosition, 0.05f, Color.white, true, 0.1f);
                DebugGizmos.Ray(Transform.EyePosition, Transform.HeadLookDirection, Color.white, Transform.HeadLookDirection.magnitude, 0.025f, true, 0.1f);

                DebugGizmos.Sphere(Transform.HeadPosition, 0.075f, Color.yellow, true, 0.1f);
                DebugGizmos.Ray(Transform.HeadPosition, Transform.LookDirection, Color.yellow, Transform.LookDirection.magnitude, 0.025f, true, 0.1f);

                DebugGizmos.Sphere(Transform.WeaponFirePort, 0.075f, Color.green, true, 0.1f);
                DebugGizmos.Ray(Transform.WeaponFirePort, Transform.WeaponPointDirection, Color.green, Transform.WeaponPointDirection.magnitude, 0.05f, true, 0.1f);

                DebugGizmos.Sphere(Transform.BodyPosition, 0.1f, Color.blue, true, 0.1f);
                DebugGizmos.Ray(Transform.BodyPosition, Transform.LookDirection, Color.blue, Transform.LookDirection.magnitude, 0.05f, true, 0.1f);
            }
        }

        private readonly List<int> _aggroIndexes = new();
        private GUIObject _hitLabel;
        private Coroutine _gearCoroutine;
        private Item _currentItem = null;
        private Weapon _currentWeapon = null;
    }
}