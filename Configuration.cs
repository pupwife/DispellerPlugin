using Dalamud.Configuration;
using System;

namespace Dispeller;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool ShowOnlyWeapons { get; set; } = false;
    public bool ShowOnlyClothing { get; set; } = false;

    // Save configuration
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
