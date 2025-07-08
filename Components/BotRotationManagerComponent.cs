using Comfort.Common;
using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.SAINComponent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Bindings;

namespace SAIN.Components.RotationController
{
    public class BotRotationManagerComponent : MonoBehaviour
    {
        public static BotRotationManagerComponent Create(GameObject gameObject, BotSpawner botSpawner, PlayerSpawnTracker playerSpawner)
        {
            return gameObject.AddComponent<BotRotationManagerComponent>();
        }

        protected void Update()
        {
            //HashSet<PlayerComponent> bots = GameWorldComponent.Instance.PlayerTracker.AlivePlayerArray;
            //foreach (PlayerComponent player in bots)
            //{
            //    player?.UpdateControlRotation(Time.deltaTime);
            //}
        }
    }
}