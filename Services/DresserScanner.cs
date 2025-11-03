using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Collections.Generic;

namespace Dispeller.Services;

public class DresserScanner : IDisposable
{
    private static readonly object LockObject = new();
    private static List<PrismBoxItem> _cachedDresserItems = [];
    private static int _dresserItemSlotsUsed = 0;

    private bool _disposed = false;

    public DresserScanner()
    {
        Plugin.Framework.Update += OnFrameworkUpdate;
    }

    private unsafe void OnFrameworkUpdate(IFramework framework)
    {
        var agent = AgentMiragePrismPrismBox.Instance();
        if (agent == null)
            return;

        if (!agent->IsAddonReady() || agent->Data == null)
            return;

        // Get used slots offset
        ushort* usedSlots = (ushort*)((nint)agent->Data + 0x10B460);
        if (*usedSlots == _dresserItemSlotsUsed)
            return;

        lock (LockObject)
        {
            _cachedDresserItems.Clear();
            
            foreach (var item in agent->Data->PrismBoxItems)
            {
                if (item.ItemId == 0)
                    continue;

                _cachedDresserItems.Add(new PrismBoxItem
                {
                    Name = item.Name.ToString(),
                    Slot = item.Slot,
                    ItemId = item.ItemId,
                    IconId = item.IconId,
                    Stain1 = item.Stains[0],
                    Stain2 = item.Stains[1],
                });
            }

            _dresserItemSlotsUsed = *usedSlots;
        }
    }

    public static unsafe List<PrismBoxItem> GetDresserItems()
    {
        lock (LockObject)
        {
            return _cachedDresserItems;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Plugin.Framework.Update -= OnFrameworkUpdate;
        _disposed = true;
    }
}

public class PrismBoxItem
{
    public string Name { get; set; } = string.Empty;
    public uint Slot { get; set; }
    public uint ItemId { get; set; }
    public uint IconId { get; set; }
    public byte Stain1 { get; set; }
    public byte Stain2 { get; set; }
}
