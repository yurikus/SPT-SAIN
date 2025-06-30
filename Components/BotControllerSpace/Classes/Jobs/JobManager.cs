using SAIN.Types.Jobs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components
{
    public class JobManager : IDisposable
    {
        public JobManager(MonoBehaviour Owner)
        {
            Jobs.Add(new FlashlightRaycastJob(Owner));
            Jobs.Add(new EnemyPathVisibilityRaycastJob(Owner));
            //Jobs.Add(new RandomVisiblePointGeneratorJob(Owner));
        }

        public readonly List<SainJobTemplate> Jobs = [];

        public void Dispose()
        {
            foreach (var job in Jobs)
                job?.Stop();
        }
    }
}