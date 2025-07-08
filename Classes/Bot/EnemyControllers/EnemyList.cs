using System;
using System.Collections.Generic;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyList : List<Enemy>
    {
        public EnemyList(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public event Action<bool> OnListEmptyOrGetFirst;
        public event Action<bool> OnListEmptyOrGetFirstHuman;

        public void SubOrUnSub(bool value, ref Action<bool, Enemy> action, Enemy enemy)
        {
            if (value)
            {
                action += AddOrRemoveEnemy;
            }
            else
            {
                action -= AddOrRemoveEnemy;
                this.RemoveEnemy(enemy);
            }
        }

        public void AddOrRemoveEnemy(bool value, Enemy enemy)
        {
            if (value)
            {
                //Logger.LogDebug($"EnemyList {Name} added {enemy.EnemyName} IsAI? {enemy.IsAI}");
                this.AddEnemy(enemy);
            }
            else
            {
                //Logger.LogDebug($"EnemyList {Name} removed {enemy.EnemyName} IsAI? {enemy.IsAI}");
                this.RemoveEnemy(enemy);
            }
        }

        private void sortByLastUpdated()
        {
            this.Sort((x, y) => x.KnownPlaces.TimeSinceLastKnownUpdated.CompareTo(y.KnownPlaces.TimeSinceLastKnownUpdated));
        }

        public Enemy First()
        {
            switch (this.Count)
            {
                case 0: return null;
                case 1: break;
                default:
                    sortByLastUpdated();
                    break;
            }
            return this[0];
        }

        public void AddEnemy(Enemy enemy)
        {
            this.Add(enemy);

            if (this.Count == 1)
            {
                OnListEmptyOrGetFirst?.Invoke(true);
            }

            if (!enemy.IsAI)
            {
                Humans++;
                if (Humans == 1)
                {
                    OnListEmptyOrGetFirstHuman?.Invoke(true);
                }
            }
            else
            {
                Bots++;
            }
        }

        public void RemoveEnemy(Enemy enemy)
        {
            if (enemy == null)
                return;

            this.Remove(enemy);

            if (!enemy.IsAI)
            {
                Humans--;
                if (Humans == 0)
                {
                    OnListEmptyOrGetFirstHuman?.Invoke(false);
                }
            }
            else
            {
                Bots--;
            }

            if (Bots < 0)
            {
                Bots = 0;
            }
            if (Humans < 0)
            {
                Humans = 0;
            }

            if (this.Count == 0)
            {
                OnListEmptyOrGetFirst?.Invoke(false);
            }
        }

        public int Humans { get; private set; }
        public int Bots { get; private set; }
    }
}