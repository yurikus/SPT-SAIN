using EFT;
using EFT.Visual;
using HarmonyLib;
using SAIN.Components.PlayerComponentSpace;
using SAIN.SAINComponent;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.Components
{
    public class FlashLightClass : PlayerComponentBase
    {
        public event Action<bool> OnLightToggle;

        public event Action<bool> OnLaserToggle;

        public bool UsingLight { get; private set; }
        public bool UsingLaser { get; private set; }

        public bool IRLaser => ActiveModes.Contains(DeviceMode.IRLaser);
        public bool IRLight => ActiveModes.Contains(DeviceMode.IRLight);
        public bool Laser => ActiveModes.Contains(DeviceMode.VisibleLaser);
        public bool WhiteLight => ActiveModes.Contains(DeviceMode.WhiteLight);
        public LightDetectionClass LightDetection { get; }

        public readonly List<DeviceMode> ActiveModes = new();

        public FlashLightClass(PlayerComponent component) : base(component)
        {
            LightDetection = new LightDetectionClass(component);
        }

        public void Update()
        {
            ClearPoints();
            CreatePoints();
            DetectPoints();
        }

        private void ClearPoints()
        {
            var points = LightDetection.LightPoints;
            if (points.Count > 0)
            {
                points.RemoveAll(x => x.ShallExpire);
            }
        }

        private void CreatePoints()
        {
            if (!PlayerComponent.IsAI &&
                _nextPointCreateTime < Time.time &&
                ActiveModes.Count > 0)
            {
                _nextPointCreateTime = Time.time + 0.15f;
                bool onlyLaser = !WhiteLight && !IRLight && (Laser || IRLaser);
                LightDetection.CreateDetectionPoints(WhiteLight || Laser, onlyLaser);
                //Logger.LogDebug("Creating flashlight points");
            }
        }

        private void DetectPoints()
        {
            if (PlayerComponent.IsAI &&
                _nextPointCheckTime < Time.time)
            {
                _nextPointCheckTime = Time.time + 0.05f;
                LightDetection.DetectAndInvestigateFlashlight();
            }
        }

        public void CheckDevice()
        {
            ActiveModes.Clear();
            CheckUsingLightModes();

            bool wasUsingLight = UsingLight;
            UsingLight = ActiveModes.Contains(DeviceMode.WhiteLight) || ActiveModes.Contains(DeviceMode.IRLight);
            if (wasUsingLight != UsingLight)
            {
                OnLightToggle?.Invoke(UsingLight);
            }

            bool wasUsingLaser = UsingLaser;
            UsingLaser = ActiveModes.Contains(DeviceMode.VisibleLaser) || ActiveModes.Contains(DeviceMode.IRLaser);
            if (wasUsingLaser != UsingLaser)
            {
                OnLaserToggle?.Invoke(UsingLaser);
            }
        }

        private void CheckUsingLightModes()
        {
            Player player = Player;
            if (player == null) return;

            if (_tacticalModesField == null)
            {
                Logger.LogError("Could find not find _tacticalModesField");
                return;
            }

            // Get the firearmsController for the player, this will be their IsCurrentEnemy weapon
            Player.FirearmController firearmController = player.HandsController as Player.FirearmController;
            if (firearmController == null)
            {
                Logger.LogError("Could find not find firearmController");
                return;
            }

            // Get the list of tacticalComboVisualControllers for the current weapon (One should exist for every flashlight, laser, or combo device)
            Transform weaponRoot = firearmController.WeaponRoot;
            List<TacticalComboVisualController> tacticalComboVisualControllers = weaponRoot.GetComponentsInChildrenActiveIgnoreFirstLevel<TacticalComboVisualController>();
            if (tacticalComboVisualControllers == null)
            {
                Logger.LogError("Could find not find tacticalComboVisualControllers");
                return;
            }

            bool foundWhiteLight = false;
            bool foundVisibleLaser = false;
            bool foundIRLight = false;
            bool FoundIRLaser = false;

            // Loop through all of the tacticalComboVisualControllers, then its modes, then that modes children, and look for a light
            foreach (TacticalComboVisualController tacticalComboVisualController in tacticalComboVisualControllers)
            {
                List<Transform> tacticalModes = _tacticalModesField.GetValue(tacticalComboVisualController) as List<Transform>;
                foreach (var mode in tacticalModes)
                {
                    // Skip disabled modes
                    if (!mode.gameObject.activeInHierarchy)
                        continue;

                    foreach (var child in mode.GetChildren())
                    {
                        // Try to find a "VolumetricLight", hopefully only visible flashlights have these
                        if (!foundWhiteLight &&
                            child.GetComponent<VolumetricLight>() != null)
                        {
                            foundWhiteLight = true;
                            if (_debugMode) Logger.LogDebug("Found Light!");
                            ActiveModes.Add(DeviceMode.WhiteLight);
                        }
                        if (!foundVisibleLaser &&
                            child.name.StartsWith("VIS_"))
                        {
                            foundVisibleLaser = true;
                            if (_debugMode) Logger.LogDebug("Found Visible Laser!");
                            ActiveModes.Add(DeviceMode.VisibleLaser);
                        }
                        if (!foundIRLight &&
                            child.GetComponent<IkLight>() != null)
                        {
                            foundIRLight = true;
                            if (_debugMode) Logger.LogDebug("Found IR Light!");
                            ActiveModes.Add(DeviceMode.IRLight);
                        }
                        if (!FoundIRLaser &&
                            child.name.StartsWith("IR_"))
                        {
                            if (_debugMode) Logger.LogDebug("Found IR Laser!");
                            FoundIRLaser = true;
                            ActiveModes.Add(DeviceMode.IRLaser);
                        }
                    }
                }
            }
        }

        private float _nextPointCheckTime;
        private float _nextPointCreateTime;
        static bool _debugMode => SAINPlugin.LoadedPreset.GlobalSettings.General.Flashlight.DebugFlash;

        static FlashLightClass()
        {
            _tacticalModesField = AccessTools.Field(typeof(TacticalComboVisualController), "list_0");
        }

        private static readonly FieldInfo _tacticalModesField;
    }
}