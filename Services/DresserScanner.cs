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
        try
        {
            var agent = AgentMiragePrismPrismBox.Instance();
            if (agent == null)
                return;

            if (!agent->IsAddonReady() || agent->Data == null)
                return;

            // Get used slots offset
            ushort* usedSlots = (ushort*)((nint)agent->Data + 0x10B460);
            
            // Always cache if cache is empty, or if the slot count has changed
            bool shouldUpdate = false;
            lock (LockObject)
            {
                shouldUpdate = _cachedDresserItems.Count == 0 || *usedSlots != _dresserItemSlotsUsed;
            }
            
            if (!shouldUpdate)
                return;

            lock (LockObject)
            {
                var wasEmpty = _cachedDresserItems.Count == 0;
                _cachedDresserItems.Clear();
                
                var itemCount = 0;
                foreach (var item in agent->Data->PrismBoxItems)
                {
                    if (item.ItemId == 0)
                        continue;

                    _cachedDresserItems.Add(new PrismBoxItem
                    {
                        // Don't store name from dresser data - it can be incorrect/outdated
                        // Name will be retrieved from Lumina in MainWindow for accuracy
                        Name = string.Empty,
                        Slot = item.Slot,
                        ItemId = item.ItemId,
                        IconId = item.IconId,
                        Stain1 = item.Stains[0],
                        Stain2 = item.Stains[1],
                    });
                    itemCount++;
                }

                _dresserItemSlotsUsed = *usedSlots;
                
                if (itemCount > 0)
                {
                    Plugin.Log.Information($"OnFrameworkUpdate: Cached {itemCount} items from dresser (cache was empty: {wasEmpty})");
                }
            }
        }
        catch
        {
            // Silently handle exceptions in framework update to avoid spam
            // Errors will be logged if they occur during manual refresh
        }
    }

    public static unsafe List<PrismBoxItem> GetDresserItems()
    {
        lock (LockObject)
        {
            // Return a snapshot copy to prevent race conditions if cache updates during scan
            return new List<PrismBoxItem>(_cachedDresserItems);
        }
    }

    public static unsafe bool TryRefresh()
    {
        try
        {
            var agent = AgentMiragePrismPrismBox.Instance();
            if (agent == null)
            {
                Plugin.Log.Debug("TryRefresh: AgentMiragePrismPrismBox.Instance() returned null - dresser not open");
                return false;
            }

            if (!agent->IsAddonReady())
            {
                Plugin.Log.Debug("TryRefresh: Agent is not ready (IsAddonReady = false) - dresser not open");
                return false;
            }

            if (agent->Data == null)
            {
                Plugin.Log.Debug("TryRefresh: Agent data is null - dresser not initialized");
                return false;
            }

            lock (LockObject)
            {
                _cachedDresserItems.Clear();
                
                var itemCount = 0;
                foreach (var item in agent->Data->PrismBoxItems)
                {
                    if (item.ItemId == 0)
                        continue;

                    _cachedDresserItems.Add(new PrismBoxItem
                    {
                        Name = string.Empty,
                        Slot = item.Slot,
                        ItemId = item.ItemId,
                        IconId = item.IconId,
                        Stain1 = item.Stains[0],
                        Stain2 = item.Stains[1],
                    });
                    itemCount++;
                }

                // Update the used slots counter to prevent immediate re-trigger
                ushort* usedSlots = (ushort*)((nint)agent->Data + 0x10B460);
                _dresserItemSlotsUsed = *usedSlots;
                
                Plugin.Log.Information($"TryRefresh: Loaded {itemCount} items from dresser");
                return true;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Error in TryRefresh");
            return false;
        }
    }

    public static bool HasCachedData()
    {
        lock (LockObject)
        {
            var hasData = _cachedDresserItems.Count > 0;
            if (hasData)
            {
                Plugin.Log.Debug($"HasCachedData: Cache contains {_cachedDresserItems.Count} items");
            }
            return hasData;
        }
    }

    public static int GetCachedItemCount()
    {
        lock (LockObject)
        {
            return _cachedDresserItems.Count;
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
