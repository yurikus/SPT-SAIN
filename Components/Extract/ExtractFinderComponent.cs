using Comfort.Common;
using EFT;
using EFT.Interactive;
using SAIN.Helpers;
using SAIN.SAINComponent;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SAIN.Components.Extract
{
    public class ExtractFinderComponent : MonoBehaviour
    {
        public bool IsFindingExtracts { get; private set; } = false;

        private ExfiltrationPoint[] AllExfils;
        private ExfiltrationPoint[] AllScavExfils;
        private Dictionary<ExfiltrationPoint, Vector3> ValidExfils = new();
        private Dictionary<ExfiltrationPoint, Vector3> ValidScavExfils = new();
        private Dictionary<ExfiltrationPoint, ExtractPositionFinder> extractPositionFinders = new();

        private float CheckExtractDelay = 10f;
        private float NextCheckExtractTime = 0f;
        private bool hasExfilControl = false;

        public void Update()
        {
            if (!hasExfilControl && !GetExfilControl())
            {
                // This is important! Need to wait a couple frames for Waypoints to add NavMeshObstacles to locked doors.
                NextCheckExtractTime = Time.time + 0.1f;

                return;
            }

            hasExfilControl = true;

            if (NextCheckExtractTime > Time.time)
            {
                return;
            }

            NextCheckExtractTime = Time.time + CheckExtractDelay;

            if (!IsFindingExtracts)
            {
                StartCoroutine(FindAllExfils());
            }
        }

        public void OnDisable()
        {
            StopAllCoroutines();
        }

        public void OnGUI()
        {
            DebugGizmos.OnGUIGame();
            DebugGizmos.OnGUIDebug();

            if (!DebugMode || !SAINPlugin.DebugSettings.Logs.DrawDebugLabels)
            {
                return;
            }

            GUIStyle guiStyle = new(GUI.skin.label);
            guiStyle.alignment = TextAnchor.MiddleLeft;
            guiStyle.fontSize = 14;
            guiStyle.margin = new RectOffset(3, 3, 3, 3);

            foreach (ExfiltrationPoint ex in extractPositionFinders.Keys)
            {
                Vector3[] pathEndpoints = extractPositionFinders[ex].PathEndpoints.ToArray();
                for (int i = 0; i < pathEndpoints.Length; i++)
                {
                    Vector3 worldPos = pathEndpoints[i] + new Vector3(0, 1, 0);
                    DebugGizmos.OnGUIDrawLabel(worldPos, "Path Endpoint " + (i + 1) + ": " + ex.Settings.Name, guiStyle);
                }

                if (extractPositionFinders[ex].ExtractPosition.HasValue)
                {
                    Vector3 worldPos = extractPositionFinders[ex].ExtractPosition.Value + new Vector3(0, 1, 0);
                    DebugGizmos.OnGUIDrawLabel(worldPos, "Extract point: " + ex.Settings.Name, guiStyle);
                }
            }
        }

        private void DrawGizmoSpheres(ExtractPositionFinder finder)
        {
            if (!DebugGizmos.DrawGizmos || !DebugMode)
            {
                return;
            }

            foreach (Vector3 pathEndPoint in finder.PathEndpoints)
            {
                DebugGizmos.Sphere(pathEndPoint, 1f, Color.blue, true, CheckExtractDelay);
            }

            if (finder.ExtractPosition.HasValue)
            {
                Color color = finder.ValidPathFound ? Color.green : Color.red;
                DebugGizmos.Sphere(finder.ExtractPosition.Value, 1f, color, true, CheckExtractDelay);
            }
        }

        public int CountValidExfilsForBot(BotComponent bot)
        {
            return GetValidExfilsForBot(bot).Count;
        }

        public IDictionary<ExfiltrationPoint, Vector3> GetValidExfilsForBot(BotComponent bot)
        {
            return bot.Info.Profile.IsScav ? ValidScavExfils : ValidExfils;
        }

        private bool GetExfilControl()
        {
            if (Singleton<AbstractGame>.Instance?.GameTimer == null)
            {
                return false;
            }

            ExfiltrationControllerClass ExfilController = Singleton<GameWorld>.Instance.ExfiltrationController;
            if (ExfilController == null)
            {
                return false;
            }

            AllExfils = ExfilController.ExfiltrationPoints;
            if (DebugMode && AllExfils != null)
            {
                Logger.LogInfo($"Found {AllExfils?.Length} possible Exfil Points in this map.");
            }

            AllScavExfils = ExfilController.ScavExfiltrationPoints;
            if (DebugMode && AllScavExfils != null)
            {
                Logger.LogInfo($"Found {AllScavExfils?.Length} possible Scav Exfil Points in this map.");
            }

            return (AllExfils != null) && (AllScavExfils != null);
        }

        private IEnumerator FindAllExfils()
        {
            bool completedCoroutine = false;
            try
            {
                IsFindingExtracts = true;

                yield return UpdateValidExfils(ValidExfils, AllExfils);
                yield return UpdateValidExfils(ValidScavExfils, AllScavExfils);

                completedCoroutine = true;
            }
            finally
            {
                IsFindingExtracts = false;

                if (!completedCoroutine)
                {
                    Logger.LogError("An error occurred when searching for extracts.");
                }
            }
        }

        private IEnumerator UpdateValidExfils(IDictionary<ExfiltrationPoint, Vector3> validExfils, ExfiltrationPoint[] allExfils)
        {
            if (allExfils == null)
            {
                yield break;
            }

            foreach (var ex in allExfils)
            {
                ExtractPositionFinder finder = GetExtractPositionSearchJob(ex);

                if (validExfils.ContainsKey(ex))
                {
                    DrawGizmoSpheres(finder);

                    continue;
                }

                yield return finder.SearchForExfilPosition();

                DrawGizmoSpheres(finder);

                if (finder.ValidPathFound)
                {
                    validExfils.Add(ex, finder.ExtractPosition.Value);
                    continue;
                }
            }
        }

        private ExtractPositionFinder GetExtractPositionSearchJob(ExfiltrationPoint ex)
        {
            if (extractPositionFinders.ContainsKey(ex))
            {
                return extractPositionFinders[ex];
            }

            ExtractPositionFinder job = new(ex);
            extractPositionFinders.Add(ex, job);

            return job;
        }

        public static bool DebugMode => SAINPlugin.DebugSettings.Logs.DebugExtract;
    }
}