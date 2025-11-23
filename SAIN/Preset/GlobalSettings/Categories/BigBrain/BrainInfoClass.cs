using System.Collections.Generic;
using EFT;

namespace SAIN.Preset.GlobalSettings.Categories;

public sealed class BrainInfoClass
{
    public string Name;
    public string Description;
    public Dictionary<Layer, int> Layers = new();
    public WildSpawnType[] UsedByWildSpawns;
}
