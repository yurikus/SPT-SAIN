using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public class NavMeshChecker : PersonSubClass
    {
        private const float NAVMESH_CHECK_FREQUENCY = 0.33f;
        private const float NAVMESH_CHECK_FREQUENCY_AI = 0.66f;

        public Vector3 LastNavmeshPosition { get; private set; }

        public void Update()
        {
            checkOnNavMesh();
        }

        public bool IsOnNavMesh(out NavMeshHit hit, float range = 0.5f)
        {
            return NavMesh.SamplePosition(Person.Transform.Position, out hit, range, -1);
        }

        private void checkOnNavMesh()
        {
            if (_nextCheckNavmeshTime < Time.time)
            {
                float delay = Person.AIInfo.IsAI ? NAVMESH_CHECK_FREQUENCY_AI : NAVMESH_CHECK_FREQUENCY;
                _nextCheckNavmeshTime = Time.time + delay;
                if (IsOnNavMesh(out var hit))
                {
                    LastNavmeshPosition = hit.position;
                }
            }
        }

        public NavMeshChecker(PersonClass person, PlayerData playerData) : base(person, playerData)
        {
        }

        private float _nextCheckNavmeshTime;
    }
}