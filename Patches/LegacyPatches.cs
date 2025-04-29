using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using HarmonyLib;
using SAIN.Models.Enums;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.AI;

// TODO: Get the AI working w/o these patches

namespace SAIN.Patches.Legacy

{
	public class CheckLookEnemyPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(EnemyInfo), "CheckLookEnemy");
		}

		[PatchPrefix]
		public static bool Patch(EnemyInfo __instance, GClass589 lookAll)
		{
			if (SAINPlugin.IsBotExluded(__instance.Owner))
			{
				return true;
			}
			Legacy.CheckLookEnemy(__instance, lookAll);
			return false;
		}
	}

	public class SetPlayerToNavMeshPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(BotMover), "SetPlayerToNavMesh");
		}

		[PatchPrefix]
		public static bool Patch(BotMover __instance, Vector3 castPoint)
		{
			if (SAINPlugin.IsBotExluded(__instance.botOwner_0))
			{
				return true;
			}
			Legacy.SetPlayerToNavMesh(__instance, __instance.botOwner_0.Transform.position, castPoint);
			return false;
		}
	}

	public class DefaultDictionary<TKey, TVal> : IDictionary<TKey, TVal>
	{
		private readonly IDictionary<TKey, TVal> _dictionary = new Dictionary<TKey, TVal>();
		private readonly TVal _defaultValue;

		public DefaultDictionary(TVal defaultValue)
		{
			_defaultValue = defaultValue;
		}

		public TVal this[TKey key]
		{
			get
			{
				if (_dictionary.TryGetValue(key, out TVal value))
				{
					return value;
				}
				return _defaultValue;
			}
			set => _dictionary[key] = value;
		}

		public ICollection<TKey> Keys => _dictionary.Keys;

		public ICollection<TVal> Values => _dictionary.Values;

		public int Count => _dictionary.Count;

		public bool IsReadOnly => _dictionary.IsReadOnly;

		public void Add(TKey key, TVal value)
		{
			_dictionary.Add(key, value);
		}

		public void Add(KeyValuePair<TKey, TVal> item)
		{
			_dictionary.Add(item.Key, item.Value);
		}

		public void Clear()
		{
			_dictionary.Clear();
		}

		public bool Contains(KeyValuePair<TKey, TVal> item)
		{
			return _dictionary.Contains(item);
		}

		public bool ContainsKey(TKey key)
		{
			return _dictionary.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<TKey, TVal>[] array, int arrayIndex)
		{
			_dictionary.CopyTo(array, arrayIndex);
		}

		public IEnumerator<KeyValuePair<TKey, TVal>> GetEnumerator()
		{
			return _dictionary.GetEnumerator();
		}

		public bool Remove(TKey key)
		{
			return _dictionary.Remove(key);
		}

		public bool Remove(KeyValuePair<TKey, TVal> item)
		{
			return _dictionary.Remove(item.Key);
		}

		public bool TryGetValue(TKey key, out TVal value)
		{
			if (!_dictionary.TryGetValue(key, out value))
			{
				value = _defaultValue;
			}
			return true;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	internal class Legacy
	{
		public static DefaultDictionary<GClass589, bool> _GClass589_distCheck = new DefaultDictionary<GClass589, bool>(false);
		public static DefaultDictionary<EnemyPartData, bool> _EnemyPartData_LastCheckVisibleAng = new DefaultDictionary<EnemyPartData, bool>(false);
		public static DefaultDictionary<EnemyPartData, bool> _EnemyPartData_VisibleBySense = new DefaultDictionary<EnemyPartData, bool>(false);
		public static DefaultDictionary<EnemyPartData, float> _EnemyPartData_TimeSeen = new DefaultDictionary<EnemyPartData, float>(0f);
		public static DefaultDictionary<EnemyInfo, float> _EnemyInfo_SeenCoef = new DefaultDictionary<EnemyInfo, float>(0f);
		public static DefaultDictionary<EnemyPartData, float> _EnemyPartData__lastSeenCoef = new DefaultDictionary<EnemyPartData, float>(0f);
		public static DefaultDictionary<EnemyPartData, float> _EnemyPartData__firstTimeSpotted = new DefaultDictionary<EnemyPartData, float>(0f);
		public static DefaultDictionary<EnemyPartData, EEnemyPartVisibleType> _EnemyPartData_VisionType = new DefaultDictionary<EnemyPartData, EEnemyPartVisibleType>(EEnemyPartVisibleType.NotVisible);
		public static Dictionary<BotsController, Dictionary<GameObject, GClass574>> _BotsController_ailayerLookObjetcs = new Dictionary<BotsController, Dictionary<GameObject, GClass574>>();
		public static void CheckLookEnemy(EnemyInfo __instance, GClass589 lookAll)
		{
			IPlayer person = __instance.Person;
			BotOwner owner = __instance.Owner;
			if (person == null || person.Transform == null || person.Transform.Original == null)
			{
				return;
			}
			__instance.Direction = __instance.CurrPosition - owner.Transform.position;
			__instance.Distance = __instance.Direction.magnitude;

			// different right here perhaps?
			_GClass589_distCheck[lookAll] = true;
			_EnemyInfo_SeenCoef[__instance] = method_5(__instance, owner.Transform, __instance.Person.Transform, owner.Settings, __instance.Person.AIData, __instance.PersonalLastSeenTime, __instance.PersonalLastPos);


			if (__instance.Distance < lookAll.MinDistance)
			{
				lookAll.MinDistance = __instance.Distance;
			}
			__instance.method_1();
			Dictionary<EnemyPart, EnemyPartData> allActiveParts = __instance.AllActiveParts;
			float addVisibility = __instance.method_0(owner);
			bool onSense = !__instance.IsFullDissapear(owner);
			bool onSenceGreen = !__instance.IsFullDissapearGreen(owner);
			if (owner.FlashGrenade.IsFlashed)
			{
				onSense = false;
				addVisibility = -1f;
			}
			__instance._totalCheck.VisibleType = EEnemyPartVisibleType.NotVisible;
			__instance._totalCheck.CanShoot = false;


			__instance._totalCheck.IsVisible = false;
			GClass588 visionCheck = CheckVisibilityPart(__instance, __instance._bodyPart, onSense, onSenceGreen, addVisibility);
			__instance.method_6(visionCheck, __instance._totalCheck);
			if (__instance._forceHeadCheck)
			{
				GClass588 visionCheck2 = CheckVisibilityPart(__instance, __instance._headPart, onSense, onSenceGreen, addVisibility);
				__instance.method_6(visionCheck2, __instance._totalCheck);
			}
			if (allActiveParts != null)
			{
				foreach (KeyValuePair<EnemyPart, EnemyPartData> enemyPart in allActiveParts)
				{
					visionCheck = CheckVisibilityPart(__instance, enemyPart, onSense, onSenceGreen, addVisibility);
					__instance.method_6(visionCheck, __instance._totalCheck);
				}
			}


			__instance.SetCanShoot(__instance._totalCheck.CanShoot);
			bool isVisible = __instance.IsVisible;
			__instance.SetVisible(__instance._totalCheck.IsVisible);
			__instance.method_2(__instance._totalCheck.VisibleType);
			if (__instance._totalCheck.IsVisible)
			{
				if (__instance._visibleOnlyBySense == EEnemyPartVisibleType.Visible)
				{
					__instance.PrevIsVisible(true);
					__instance.PersonalLastSeenTime = Time.time;
					__instance.PersonalLastPos = __instance.CurrPosition;
				}
				else
				{
					__instance.PrevIsVisible(false);
				}
				GClass564 item = new GClass564(owner, person, person.Transform.position, __instance._visibleOnlyBySense);
				lookAll.ReportsData.Add(item);
				if (!isVisible)
				{
					lookAll.ShallRecalcGoal = true;
					return;
				}
			}
			else
			{
				__instance.PrevIsVisible(false);
			}
		}

		public static float method_5(EnemyInfo __instance, BifacialTransform BotTransform, BifacialTransform enemy, BotDifficultySettingsClass settings, IAIData aiData, float personalLastSeenTime, Vector3 personalLastSeenPos)
		{
			float num = 1f;
			if (Time.time - personalLastSeenTime < __instance.Owner.Settings.FileSettings.Look.SEC_REPEATED_SEEN && (double)(personalLastSeenPos - enemy.position).sqrMagnitude < __instance.Owner.Settings.FileSettings.Look.DIST_SQRT_REPEATED_SEEN)
			{
				num = __instance.Owner.Settings.FileSettings.Look.COEF_REPEATED_SEEN;
			}
			Vector3 from = BotTransform.rotation * Vector3.forward;
			Vector3 to = enemy.position - BotTransform.position;
			float num2;
			if (__instance.Owner.LookSensor.IsFullSectorView)
			{
				num2 = 0.1f;
			}
			else
			{
				num2 = Vector3.Angle(from, to);
			}
			float time = num2 / 90f;
			if (num2 > 90f)
			{
				return 8888f;
			}
			float magnitude = (enemy.position - BotTransform.position).magnitude;
			float num3 = settings.Curv.VisionAngCoef.Evaluate(time);
			float num4 = 1f - num3;
			float num5 = magnitude;
			float max_DIST_CLAMP_TO_SEEN_SPEED = __instance.Owner.Settings.FileSettings.Look.MAX_DIST_CLAMP_TO_SEEN_SPEED;
			if (magnitude > max_DIST_CLAMP_TO_SEEN_SPEED)
			{
				num5 = max_DIST_CLAMP_TO_SEEN_SPEED;
			}
			return num5 * settings.Current.CurrentGainSightCoef * aiData.PoseVisibilityCoef * num4 * num;
		}

		public static bool CheckVisibleAng(EnemyInfo __instance, Vector3 position, EnemyPartData data)
		{
			bool flag = __instance.IsPointInVisibleSector(position);
			_EnemyPartData_LastCheckVisibleAng[data] = flag;
			return flag;
		}

		public static GClass588 CheckVisibilityPart(EnemyInfo __instance, KeyValuePair<EnemyPart, EnemyPartData> enemyPart, bool onSense, bool onSenceGreen, float addVisibility)
		{
			bool isVisible;
			if (CheckVisibleAng(__instance, enemyPart.Key.Position, enemyPart.Value))
			{
				isVisible = CheckVisibility(__instance, enemyPart, _EnemyInfo_SeenCoef[__instance], onSense, onSenceGreen, addVisibility);
				float num = _EnemyPartData_TimeSeen[enemyPart.Value] / _EnemyInfo_SeenCoef[__instance];
			}
			else
			{
				enemyPart.Value.CanShoot = false;
				_EnemyPartData_VisionType[enemyPart.Value] = EEnemyPartVisibleType.NotVisible;
				isVisible = false;
			}
			enemyPart.Key.VisibilityResult.IsVisible = isVisible;
			if (enemyPart.Key.VisibilityResult.IsVisible)
			{
				CheckCanShoot(__instance, enemyPart);
			}
			else
			{
				enemyPart.Value.CanShoot = false;
			}
			enemyPart.Key.VisibilityResult.VisibleType = _EnemyPartData_VisionType[enemyPart.Value];
			enemyPart.Key.VisibilityResult.CanShoot = enemyPart.Value.CanShoot;
			return enemyPart.Key.VisibilityResult;
		}

		public class GClass574
		{
			public GClass574(GameObject gameObject)
			{
				this.GameObject = gameObject;
				if (this.GameObject.GetComponent<Terrain>() != null)
				{
					this.LookObjectTypeAI = LookObjectTypeAI.grass;
					return;
				}
				this.LookObjectTypeAI = LookObjectTypeAI.tree;
			}
			public GameObject GameObject;
			public LookObjectTypeAI LookObjectTypeAI;
		}

		public enum LookObjectTypeAI
		{
			tree,
			grass
		}


		public static float method_6(EnemyInfo __instance, float dist, bool flare, ref bool freeLook)
		{
			float num = flare ? __instance.Owner.Settings.FileSettings.Look.MAX_VISION_GRASS_METERS_FLARE_OPT : __instance.Owner.Settings.FileSettings.Look.MAX_VISION_GRASS_METERS_OPT;
			float num2 = 1f - num * dist;
			float num3 = 1f + dist * 1.5f;
			float num4 = num2 / num3;
			freeLook = true;
			if (num4 <= 0.001f)
			{
				num4 = 0.001f;
				freeLook = false;
			}
			else if (num4 > 1f)
			{
				num4 = 1f;
			}
			return num4;
		}

		public static bool UpdateVision(EnemyPartData __instance, float seenCoef, bool canSee, bool onSense, bool onSenseGreen, BotOwner owner, float visibleCoef = 1f)
		{
			_EnemyPartData__lastSeenCoef[__instance] = seenCoef; // __instance._lastSeenCoef = seenCoef;
			float num = Time.time - (_EnemyPartData__firstTimeSpotted[__instance] + _EnemyPartData_TimeSeen[__instance]); // float num = Time.time - (__instance._firstTimeSpotted + __instance.TimeSeen);
			if (canSee)
			{
				if (!__instance.IsVisible && num >= Time.deltaTime + owner.Settings.FileSettings.Look.POSIBLE_VISION_SPACE)
				{
					_EnemyPartData__firstTimeSpotted[__instance] = Time.time; // __instance._firstTimeSpotted = Time.time;
					_EnemyPartData_TimeSeen[__instance] = 0f; // __instance.TimeSeen = 0f;
				}
				else
				{
					_EnemyPartData_TimeSeen[__instance] += num * visibleCoef; // __instance.TimeSeen += num * visibleCoef;
				}
				_EnemyPartData_VisibleBySense[__instance] = false; // __instance.VisibleBySense = false;
				__instance.IsVisible = (_EnemyPartData_TimeSeen[__instance] > seenCoef || (__instance.IsVisible && (onSense || onSenseGreen))); // __instance.IsVisible = (__instance.TimeSeen > seenCoef || (__instance.IsVisible && (onSense || onSenseGreen)));
				if (__instance.IsVisible)
				{
					__instance._lastVisibleTrueTime = Time.time;
				}
			}
			else
			{
				bool flag;
				if (!(flag = (onSense || onSenseGreen)))
				{
					_EnemyPartData_TimeSeen[__instance] = 0f; // __instance.TimeSeen = 0f;
				}
				__instance.IsVisible = (_EnemyPartData_TimeSeen[__instance] > seenCoef && flag); // __instance.IsVisible = (__instance.TimeSeen > seenCoef && flag);
			}
			if (canSee)
			{
				_EnemyPartData_VisionType[__instance] = EEnemyPartVisibleType.Visible; // __instance.VisionType = EEnemyPartVisibleType.visible;
			}
			else if (onSense)
			{
				_EnemyPartData_VisionType[__instance] = EEnemyPartVisibleType.Sence; // __instance.VisionType = EEnemyPartVisibleType.sence;
			}
			else if (onSenseGreen)
			{
				_EnemyPartData_VisionType[__instance] = EEnemyPartVisibleType.GreenSence; // __instance.VisionType = EEnemyPartVisibleType.greenSence;
			}
			else
			{
				_EnemyPartData_VisionType[__instance] = EEnemyPartVisibleType.NotVisible; // __instance.VisionType = EEnemyPartVisibleType.notVisible;
			}
			return __instance.IsVisible;
		}

		public static bool CheckVisibility(EnemyInfo __instance, KeyValuePair<EnemyPart, EnemyPartData> part, float seenCoef, bool onSense, bool onSenceGreen, float addVisibility)
		{
			Vector3 headPoint = __instance.Owner.LookSensor._headPoint;
			Vector3 vector = part.Key.Position;
			Vector3 vector2 = Vector3.zero;
			if (part.Key.Collider != null)
			{
				if (part.Value.LastVisibilityCastSucceed)
				{
					vector2 = part.Value.LastVisibilityCastOffsetLocal;
				}
				else
				{
					vector2 = part.Key.Collider.GetRandomPointToCastLocal(headPoint);
				}
				Vector3 b = part.Key.Collider.transform.TransformVector(vector2);
				vector += b;
			}
			Vector3 vector3 = vector - headPoint;
			float magnitude = vector3.magnitude;
			if (__instance.Owner.LookSensor.VisibleDist + addVisibility < magnitude)
			{
				return false;
			}
			Ray ray = new Ray(headPoint, vector3);
			Ray ray2 = new Ray(vector, -vector3);
			RaycastHit raycastHit = default(RaycastHit);
			LayerMask mask = __instance.Owner.LookSensor.Mask;
			bool flag = false;
			GClass574 gclass = null;
			GClass574 gclass2 = null;

			// Dictionary<GameObject, GClass574> ailayerLookObjetcs = _BotsController_ailayerLookObjetcs[__instance.Owner.BotsController]; // Dictionary<GameObject, GClass574> ailayerLookObjetcs = __instance.Owner.BotsController.AILayerLookObjetcs;
			Dictionary<GameObject, GClass574> ailayerLookObjetcs;
			if (!_BotsController_ailayerLookObjetcs.TryGetValue(__instance.Owner.BotsController, out ailayerLookObjetcs))
			{
				ailayerLookObjetcs = new Dictionary<GameObject, GClass574>();
				_BotsController_ailayerLookObjetcs.Add(__instance.Owner.BotsController, ailayerLookObjetcs);
			}

			if (!__instance.Owner.Settings.FileSettings.Look.LOOK_THROUGH_GRASS)
			{
				if (magnitude < __instance.Owner.Settings.FileSettings.Look.NO_GREEN_DIST)
				{
					mask = LayerMaskClass.HighPolyWithTerrainMask;
				}
				else if (magnitude < __instance.Owner.Settings.FileSettings.Look.NO_GRASS_DIST)
				{
					mask = LayerMaskClass.HighPolyWithTerrainNoGrassMask;
				}
			}
			RaycastHit raycastHit2;
			bool flag3;
			bool flag2 = !(flag3 = !Physics.Raycast(ray, out raycastHit2, magnitude, mask)) && GClass1403.Contains(LayerMaskClass.AI, raycastHit2.collider.gameObject.layer);
			bool flag4 = false;
			bool flag5;
			bool flag6;
			if (flag3)
			{
				flag4 = (!(flag5 = !Physics.Raycast(ray2, out raycastHit, magnitude, mask)) && GClass1403.Contains(LayerMaskClass.AI, raycastHit.collider.gameObject.layer));
				flag6 = flag5;
			}
			else if (flag2)
			{
				flag4 = (!(flag5 = !Physics.Raycast(ray2, out raycastHit, magnitude, mask)) && GClass1403.Contains(LayerMaskClass.AI, raycastHit.collider.gameObject.layer));
				flag6 = false;
			}
			else
			{
				flag5 = false;
				flag6 = false;
			}
			if (!flag6)
			{
				if (flag2)
				{
					if (!ailayerLookObjetcs.TryGetValue(raycastHit2.collider.gameObject, out gclass))
					{
						gclass = new GClass574(raycastHit2.collider.gameObject);
						ailayerLookObjetcs.Add(raycastHit2.collider.gameObject, gclass);
					}
					flag = true;
				}
				if (flag4)
				{
					if (!ailayerLookObjetcs.TryGetValue(raycastHit.collider.gameObject, out gclass))
					{
						gclass = new GClass574(raycastHit.collider.gameObject);
						ailayerLookObjetcs.Add(raycastHit.collider.gameObject, gclass);
					}
					flag = true;
				}
			}
			bool flag7 = false;
			bool flag8 = false;
			if (flag)
			{
				if (!flag5)
				{
					if (flag4 && !ailayerLookObjetcs.TryGetValue(raycastHit.collider.gameObject, out gclass2))
					{
						gclass2 = new GClass574(raycastHit.collider.gameObject);
						ailayerLookObjetcs.Add(raycastHit.collider.gameObject, gclass2);
					}
				}
				else if (!ailayerLookObjetcs.TryGetValue(raycastHit2.collider.gameObject, out gclass2))
				{
					gclass2 = new GClass574(raycastHit2.collider.gameObject);
					ailayerLookObjetcs.Add(raycastHit2.collider.gameObject, gclass2);
				}
			}
			float num = 0f;
			if (gclass != null)
			{
				if (gclass.LookObjectTypeAI == LookObjectTypeAI.grass)
				{
					flag8 = true;
				}
				else
				{
					flag7 = true;
				}
				if (gclass2 != null)
				{
					if (part.Key.Owner.AIData.IsInTree)
					{
						num = (raycastHit2.point - vector).magnitude;
					}
					else if (!flag5 && !flag3)
					{
						num = (raycastHit2.point - raycastHit.point).magnitude;
					}
					else if (!flag3)
					{
						num = (vector - raycastHit2.point).magnitude;
					}
					else
					{
						num = (raycastHit.point - headPoint).magnitude * __instance.Owner.Settings.FileSettings.Look.INSIDE_BUSH_COEF;
					}
				}
				else
				{
					num = (raycastHit2.point - vector).magnitude;
				}
				num = Mathf.Min(num, magnitude);
			}
			bool flag9;
			if (flag9 = (flag8 || flag7))
			{
				float dist = num;
				float num2 = method_6(__instance, dist, part.Key.Owner.AIData.GetFlare, ref flag3);
				seenCoef /= num2;
			}
			if (part.Key.BodyPartType == BodyPartType.body)
			{
				part.Key.GrassDist = num;
				if (flag9)
				{
					if (flag8)
					{
						part.Key.GreenType = 1;
					}
					else
					{
						part.Key.GreenType = 2;
					}
				}
				else
				{
					part.Key.GreenType = 0;
				}
			}
			bool flag10 = flag3 && flag5;
			bool onSenseGreen = flag9 && onSenceGreen;
			float num3 = part.Key.Owner.AIData.FlarePower;
			if (__instance.Owner.Profile.Info.Settings.Role.IsInfected() && __instance.Owner.BotsController.EventsController.BotHalloweenWithZombies != null)
			{
				num3 *= __instance.Owner.BotsController.EventsController.BotHalloweenWithZombies.ZombieLookCoeff;
			}
			bool result = UpdateVision(part.Value, seenCoef, flag10, onSense, onSenseGreen, __instance.Owner, num3);
			part.Value.LastVisibilityCastSucceed = flag10;
			part.Value.LastVisibilityCastOffsetLocal = vector2;
			return result;
		}

		public static void CheckCanShoot(EnemyInfo __instance, KeyValuePair<EnemyPart, EnemyPartData> part)
		{
			Vector3 shootStartPos = __instance.Owner.LookSensor.ShootStartPos;
			Vector3 vector = part.Key.Position;
			Vector3 vector2 = Vector3.zero;
			if (part.Key.Collider != null)
			{
				if (part.Value.LastShootCastSucceed)
				{
					vector2 = part.Value.LastShootCastOffsetLocal;
				}
				else if (!part.Value.VisCastForShootIsTried && part.Value.LastVisibilityCastSucceed)
				{
					vector2 = part.Value.LastVisibilityCastOffsetLocal;
					part.Value.VisCastForShootIsTried = true;
				}
				else
				{
					vector2 = part.Key.Collider.GetRandomPointToCastLocal(shootStartPos);
				}
				Vector3 b = part.Key.Collider.transform.TransformVector(vector2);
				vector += b;
			}
			Vector3 vector3 = vector - shootStartPos;
			float magnitude = vector3.magnitude;
			Ray ray = new Ray(shootStartPos, vector3);
			Ray ray2 = new Ray(vector, -vector3);
			float num = magnitude;
			RaycastHit raycastHit;
			bool flag;
			RaycastHit raycastHit2;
			if (flag = Physics.Raycast(ray, out raycastHit, magnitude, LayerMaskClass.HighPolyWithTerrainMask))
			{
				num = raycastHit.distance;
			}
			else if (Physics.Raycast(ray2, out raycastHit2, magnitude, LayerMaskClass.HighPolyWithTerrainMask))
			{
				flag = true;
				num = raycastHit2.distance;
			}
			if (flag)
			{
				part.Value.CanShoot = false;
			}
			else
			{
				bool flag2 = false;

				if (_EnemyPartData_VisibleBySense[part.Value])
				{
					flag2 = part.Value.IsFullDisappearForShoot(__instance.Owner);
				}
				part.Value.CanShoot = (!flag2 && __instance.Owner.LookSensor.MaxShootDist > num);
			}
			part.Value.LastShootCastSucceed = part.Value.CanShoot;
			part.Value.LastShootCastOffsetLocal = vector2;
			if (part.Value.CanShoot)
			{
				part.Value.VisCastForShootIsTried = false;
			}
		}

		public static int SetPlayerToNavMesh(BotMover __instance, Vector3 curPos, Vector3 castPoint)
		{
			Vector3? vector = null;
			bool flag;
			if (false) //__instance._onlyVertical) // this doesn't seem to be used so I don't feel like making the Dictionary
			{
				// NavMeshHit navMeshHit;
				// flag = NavMesh.Raycast(castPoint + __instance._offsetCastUp, castPoint + __instance._offsetCastDown, out navMeshHit, -1);
				// vector = new Vector3?(navMeshHit.position);
			}
			else
			{
				NavMeshHit navMeshHit2;
				flag = NavMesh.SamplePosition(castPoint, out navMeshHit2, 2f, -1);
				if (navMeshHit2.position.y - castPoint.y > 0.5f)
				{
					flag = false;
				}
				vector = new Vector3?(navMeshHit2.position);
			}
			int result;
			if (flag && vector != null)
			{
				RaycastHit raycastHit;
				if (Physics.Raycast(new Ray(vector.Value, Vector3.down), out raycastHit, 0.3f, LayerMaskClass.PlayerCollisionsMask))
				{
					result = 1;
					__instance.botOwner_0.Transform.position = raycastHit.point;
				}
				else
				{
					result = 2;
					__instance.botOwner_0.Transform.position = vector.Value;
				}
			}
			else
			{
				result = 3;
				__instance.botOwner_0.Transform.position = curPos;
			}
			__instance._prevLinkPos = curPos;
			return result;
		}
	}
}