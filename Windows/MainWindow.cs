using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Textures.TextureWraps;
using Lumina.Excel.Sheets;
using Dispeller.Services;

// yea i used emojis bc i wanted to be cute and funny so what 

namespace Dispeller.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private List<SharedModelGroup>? sharedGroups;
    private bool isScanning = false;
    private string statusMessage = "Ready to scan!";
    
    // Darker purple + soft magenta + text colors
    private static readonly Vector4 DarkerPurple = new(0.28f, 0.20f, 0.45f, 1.00f);  // deep purple
    private static readonly Vector4 SoftMagenta  = new(0.78f, 0.37f, 0.64f, 1.00f);  // soft magenta
    private static readonly Vector4 HeaderEdge   = new(0.20f, 0.15f, 0.35f, 1.00f);  // even darker edge
    private static readonly Vector4 BrightWhite  = new(1.00f, 1.00f, 1.00f, 1.00f);  // white for most text
    private static readonly Vector4 AshBlack  = new(0.10f, 0.10f, 0.10f, 1.00f);  // ash black for dropdown header text only
    
    // Light variants for UI elements
    private static readonly Vector4 LightMagenta = new(0.88f, 0.47f, 0.74f, 1.00f);  // lighter magenta (pink) for main gear
    private static readonly Vector4 LightPurple = new(0.65f, 0.60f, 0.80f, 1.00f);  // pastel purple for accessories (lighter, softer)
    private static readonly Vector4 LightMintGreen = new(0.50f, 0.85f, 0.75f, 1.00f);  // minty green for weapons

    public MainWindow(Plugin plugin)
        : base("Dispeller - Shared Model Analyzer", ImGuiWindowFlags.NoScrollbar)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        // Pink gradient header
        DrawHeader();
        
        ImGui.Spacing();

        // Scan button
        DrawScanButton();

        ImGui.Spacing();

        // Status message
        DrawStatus();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Results display
        DrawResults();

        // Footer
        DrawFooter();
    }

    private void DrawHeader()
    {
        var windowWidth = ImGui.GetWindowSize().X;
        var drawList = ImGui.GetWindowDrawList();
        var cursorPos = ImGui.GetCursorScreenPos();

        // Dark purple/magenta gradient background
        drawList.AddRectFilledMultiColor(
            cursorPos,
            cursorPos + new Vector2(windowWidth, 60),
            ImGui.ColorConvertFloat4ToU32(SoftMagenta),
            ImGui.ColorConvertFloat4ToU32(DarkerPurple),
            ImGui.ColorConvertFloat4ToU32(DarkerPurple),
            ImGui.ColorConvertFloat4ToU32(SoftMagenta)
        );

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
        ImGui.SetCursorPosX(20);

        // Title with emojis
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
        ImGui.SetWindowFontScale(1.2f);
        ImGui.TextUnformatted("✨ Dispeller ✨");
        ImGui.SetWindowFontScale(1.0f);
        ImGui.PopStyleColor();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5);
        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 50);
    }

    private void DrawScanButton()
    {
        var buttonWidth = ImGui.GetContentRegionAvail().X * 0.5f;
        var centerPos = (ImGui.GetContentRegionAvail().X - buttonWidth) / 2;
        ImGui.SetCursorPosX(centerPos);

        if (isScanning)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, LightPurple);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, LightPurple);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, LightPurple);
            ImGui.PushStyleColor(ImGuiCol.Text, BrightWhite);
            
            if (ImGui.Button("Scanning...", new Vector2(buttonWidth, 40)))
            {
                // Cancelled during scan
            }
            
            ImGui.PopStyleColor(4);
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button, SoftMagenta);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(SoftMagenta.X, SoftMagenta.Y, SoftMagenta.Z, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, DarkerPurple);
            ImGui.PushStyleColor(ImGuiCol.Text, BrightWhite);
            
            if (ImGui.Button("💖 Scan Glamour Dresser 💖", new Vector2(buttonWidth, 40)))
            {
                ScanDresser();
            }
            
            ImGui.PopStyleColor(4);
        }
    }

    private void DrawStatus()
    {
        var centerPos = (ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(statusMessage).X) / 2;
        ImGui.SetCursorPosX(centerPos);
        
        ImGui.PushStyleColor(ImGuiCol.Text, isScanning ? SoftMagenta : BrightWhite);
        ImGui.TextUnformatted(statusMessage);
        ImGui.PopStyleColor();
    }

    private void DrawResults()
    {
        if (sharedGroups == null || sharedGroups.Count == 0)
        {
            var message = "Click Scan to analyze your glamour dresser!";
            var centerPos = (ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(message).X) / 2;
            ImGui.SetCursorPosX(centerPos);
            
            ImGui.PushStyleColor(ImGuiCol.Text, BrightWhite);
            ImGui.TextUnformatted(message);
            ImGui.PopStyleColor();
            return;
        }

        if (!ImGui.BeginChild("Results", Vector2.Zero, false))
            return;

        foreach (var group in sharedGroups.Where(g => g.Items.Count > 0))
        {
            DrawSharedGroup(group);
            ImGui.Spacing();
        }

        ImGui.EndChild();
    }

    private void DrawSharedGroup(SharedModelGroup group)
    {
        using var id = ImRaii.PushId($"{group.SlotCategory}-{group.Items.Count}");

        // Get color based on slot category
        var groupColor = GetColorForSlot(group.SlotCategory);
        ImGui.PushStyleColor(ImGuiCol.Header, groupColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, groupColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, groupColor);
        ImGui.PushStyleColor(ImGuiCol.Text, AshBlack);

        var headerText = $"{group.SlotCategory} ({group.Items.Count} items)";
        
        if (ImGui.CollapsingHeader(headerText))
        {
            ImGui.PopStyleColor(4);

            string? previousModelId = null;
            foreach (var item in group.Items)
            {
                // Visual separator if model changes (items with matching models will be adjacent)
                if (previousModelId != null && previousModelId != item.ModelId)
                {
                    ImGui.Spacing();
                }
                previousModelId = item.ModelId;
                
                DrawItem(item, group.Items);
            }
        }
        else
        {
            ImGui.PopStyleColor(4);
        }
    }

    private void DrawItem(SharedModelItem item, List<SharedModelItem> allItemsInSlot)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 20);

        // Check if this item has matching models (more than one item with same model ID)
        var matchingModelCount = allItemsInSlot.Count(i => i.ModelId == item.ModelId);
        var hasMatchingModels = matchingModelCount > 1;

        // Try to get icon
        var icon = GetIcon((ushort)item.IconId);
        if (icon != null)
        {
            ImGui.Image(icon.Handle, new Vector2(32, 32));
            ImGui.SameLine();
        }
        else
        {
            // Draw a placeholder if icon is missing
            ImGui.Dummy(new Vector2(32, 32));
            ImGui.SameLine();
        }

        // Get display name - fallback if empty
        var displayName = string.IsNullOrWhiteSpace(item.Name) ? $"Item #{item.ItemId}" : item.Name;
        
        // Add indicator for matching models
        if (hasMatchingModels)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, SoftMagenta);
            ImGui.TextUnformatted($"🔗 {displayName}");
            ImGui.PopStyleColor();
            
            // Tooltip showing matching items
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted($"Matches {matchingModelCount} items with model: {item.ModelId}");
                ImGui.EndTooltip();
            }
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Text, BrightWhite);
            ImGui.TextUnformatted(displayName);
            ImGui.PopStyleColor();
        }

        // Draw dye slot indicators (circles) - similar to Glamaholic
        if (item.DyeCount > 0)
        {
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5);
            
            var drawList = ImGui.GetWindowDrawList();
            var basePos = ImGui.GetCursorScreenPos();
            var circleRadius = 4.0f;
            var circleSpacing = 8.0f;
            // Use white/light gray for empty circles (visible on dark background)
            var circleColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.85f, 0.85f, 0.85f, 1.0f));
            
            // Draw circles for each dye slot (1 or 2)
            for (int i = 0; i < item.DyeCount; i++)
            {
                var circleCenter = basePos + new Vector2(circleRadius + 2, circleRadius + 2) + new Vector2(i * circleSpacing, 0);
                // Draw empty circle outline (similar to Glamaholic - empty circles indicate available dye slots)
                drawList.AddCircle(circleCenter, circleRadius + 1, circleColor);
            }
            
            // Add spacing after circles and create invisible button for tooltip
            var circlesWidth = (item.DyeCount * circleSpacing) + 4;
            ImGui.InvisibleButton($"dye_{item.ItemId}", new Vector2(circlesWidth, circleRadius * 2 + 4));
            
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted($"{item.DyeCount} dye slot{(item.DyeCount > 1 ? "s" : "")} available");
                ImGui.EndTooltip();
            }
            
            // Move cursor past the circles
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + circlesWidth);
        }

        // Draw Armoire marker if item can be stored in Armoire
        if (item.CanGoInArmoire)
        {
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5);
            
            ImGui.PushStyleColor(ImGuiCol.Text, SoftMagenta);
            ImGui.TextUnformatted("[Armoire]");
            ImGui.PopStyleColor();
            
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted("This item can be stored in your Armoire instead of the Glamour Dresser!");
                ImGui.EndTooltip();
            }
        }
    }

    private IDalamudTextureWrap? GetIcon(ushort id)
    {
        var icon = Plugin.TextureProvider.GetFromGameIcon(new Dalamud.Interface.Textures.GameIconLookup(id)).GetWrapOrDefault();
        return icon;
    }

    private void DrawFooter()
    {
        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);
        
        var message = "Find shared models in your glamour dresser!";
        var centerPos = (ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(message).X) / 2;
        ImGui.SetCursorPosX(centerPos);
        
        ImGui.PushStyleColor(ImGuiCol.Text, BrightWhite);
        ImGui.TextUnformatted(message);
        ImGui.PopStyleColor();
    }

    private void ScanDresser()
    {
        isScanning = true;
        statusMessage = "Scanning your glamour dresser...";

        try
        {
            // Check if we have cached data first (before trying to refresh)
            int cachedCountBefore = DresserScanner.GetCachedItemCount();
            bool hasCachedData = cachedCountBefore > 0;
            
            Plugin.Log.Information($"ScanDresser: Starting scan - cached items before refresh: {cachedCountBefore}");
            
            // Try to refresh the dresser data if it's currently open (to get latest data)
            bool refreshed = false;
            unsafe
            {
                refreshed = DresserScanner.TryRefresh();
            }

            int cachedCountAfter = DresserScanner.GetCachedItemCount();
            var dresserItems = DresserScanner.GetDresserItems();
            
            Plugin.Log.Information($"Dresser scan: Found {dresserItems.Count} items (cached before: {cachedCountBefore}, cached after: {cachedCountAfter}, refreshed: {refreshed})");
            
            if (dresserItems.Count == 0)
            {
                if (!hasCachedData && !refreshed)
                {
                    statusMessage = "Your glamour dresser hasn't been opened yet! Please open your Glamour Dresser at least once, then try again.";
                    Plugin.Log.Warning("Dresser scan: No cached data and dresser not open");
                }
                else if (hasCachedData && !refreshed)
                {
                    statusMessage = "Using cached data, but dresser appears empty. Try opening the dresser to refresh.";
                    Plugin.Log.Warning($"Dresser scan: Had {cachedCountBefore} cached items but got 0 after refresh attempt");
                }
                else
                {
                    statusMessage = "Your glamour dresser appears to be empty!";
                    Plugin.Log.Warning("Dresser scan: Dresser is open but empty");
                }
                sharedGroups = null;
                return;
            }

            // Deduplicate by Slot + ItemId to prevent duplicates from race conditions
            var uniqueItems = dresserItems
                .GroupBy(item => new { item.Slot, item.ItemId })
                .Select(g => g.First())
                .ToList();

            // Filter out items with unknown slots
            var validItems = uniqueItems
                .Where(item => {
                    var slotName = GetSlotName(item.ItemId);
                    return !string.IsNullOrEmpty(slotName) && slotName != "Unknown Slot";
                })
                .ToList();

            // First, identify items with shared models by grouping by slot + model
            var itemsWithSharedModels = validItems
                .GroupBy(item => {
                    var slotName = GetSlotName(item.ItemId);
                    var modelId = GetItemModel(item.ItemId);
                    return $"{slotName}-{modelId}";
                })
                .Where(g => g.Count() > 1) // Only groups with matching models
                .SelectMany(g => g) // Flatten back to individual items
                .ToList();

            // Now group by slot category only
            var grouped = itemsWithSharedModels
                .GroupBy(item => GetSlotName(item.ItemId))
                .Select(g => {
                    // Sort items within this slot by model ID so matching models are adjacent
                    var sortedItems = g
                        .OrderBy(item => GetItemModel(item.ItemId))
                        .Select(item => {
                            // Always get item name from Lumina for accuracy
                            // Dresser name can be incorrect/outdated when dresser updates
                            var itemName = GetItemNameFromLumina(item.ItemId);
                            
                            // Get icon - use from Lumina if dresser icon is invalid
                            var iconId = item.IconId;
                            if (iconId == 0)
                            {
                                iconId = GetItemIconFromLumina(item.ItemId);
                            }
                            
                            // Get dye count from Lumina
                            var dyeCount = GetItemDyeCount(item.ItemId);
                            
                            // Check if item can be stored in Armoire
                            var canGoInArmoire = CanGoInArmoire(item.ItemId);
                            
                            return new SharedModelItem
                            {
                                Name = itemName,
                                ItemId = item.ItemId,
                                IconId = (int)iconId,
                                Slot = item.Slot,
                                ModelId = GetItemModel(item.ItemId),
                                DyeCount = dyeCount,
                                CanGoInArmoire = canGoInArmoire
                            };
                        })
                        .ToList();
                    
                    return new SharedModelGroup
                    {
                        ModelId = "", // Not used for slot-based grouping
                        SlotCategory = g.Key,
                        Items = sortedItems
                    };
                })
                .OrderBy(g => GetSlotOrder(g.SlotCategory)) // Sort slots in logical order
                .ToList();

            sharedGroups = grouped;
            var totalItems = grouped.Sum(g => g.Items.Count);
            statusMessage = $"Found {totalItems} items with shared models across {grouped.Count} slot categories!";
        }
        catch (Exception ex)
        {
            statusMessage = $"Error: {ex.Message}";
            sharedGroups = null;
            Plugin.Log.Error(ex, "Error during dresser scan");
        }
        finally
        {
            isScanning = false;
        }
    }

    private string GetItemModel(uint itemId)
    {
        var sheet = Plugin.DataManager.GetExcelSheet<Item>()!;
        if (!sheet.TryGetRow(itemId, out var item))
            return "Unknown";

        var model = ModelDetectionService.ExtractModelInfo(item.ModelMain);
        return ModelDetectionService.GetModelIdString(model);
    }

    private string GetSlotName(uint itemId)
    {
        if (itemId == 0)
            return "Unknown Slot";
            
        var sheet = Plugin.DataManager.GetExcelSheet<Item>()!;
        if (!sheet.TryGetRow(itemId, out var item))
            return "Unknown Slot";

        if (!item.EquipSlotCategory.IsValid)
            return "Unknown Slot";

        var category = item.EquipSlotCategory.Value;

        // Check each slot category in priority order
        if (category.MainHand > 0) return "Main Hand";
        if (category.OffHand > 0) return "Off Hand";
        if (category.Head > 0) return "Head";
        if (category.Body > 0) return "Body";
        if (category.Gloves > 0) return "Gloves";
        if (category.Legs > 0) return "Legs";
        if (category.Feet > 0) return "Feet";
        if (category.Ears > 0) return "Ears";
        if (category.Neck > 0) return "Neck";
        if (category.Wrists > 0) return "Wrists";
        if (category.FingerR > 0 || category.FingerL > 0) return "Ring";

        return "Unknown Slot";
    }

    private string GetItemNameFromLumina(uint itemId)
    {
        if (itemId == 0)
            return "Unknown Item";
            
        var sheet = Plugin.DataManager.GetExcelSheet<Item>()!;
        if (!sheet.TryGetRow(itemId, out var item))
            return "Unknown Item";
        
        return item.Name.ExtractText();
    }

    private uint GetItemIconFromLumina(uint itemId)
    {
        if (itemId == 0)
            return 0;
            
        var sheet = Plugin.DataManager.GetExcelSheet<Item>()!;
        if (!sheet.TryGetRow(itemId, out var item))
            return 0;
        
        return item.Icon;
    }

    private byte GetItemDyeCount(uint itemId)
    {
        if (itemId == 0)
            return 0;
            
        var sheet = Plugin.DataManager.GetExcelSheet<Item>()!;
        if (!sheet.TryGetRow(itemId, out var item))
            return 0;
        
        return item.DyeCount;
    }

    private bool CanGoInArmoire(uint itemId)
    {
        if (itemId == 0)
            return false;
            
        // Check if item exists in the Cabinet sheet (Armoire items)
        var cabinetSheet = Plugin.DataManager.GetExcelSheet<Cabinet>()!;
        return cabinetSheet.Any(row => row.Item.RowId == itemId);
    }

    private int GetSlotOrder(string slotName)
    {
        // Return order value for slot sorting (lower = appears first)
        return slotName switch
        {
            "Main Hand" => 1,
            "Off Hand" => 2,
            "Head" => 3,
            "Body" => 4,
            "Gloves" => 5,
            "Legs" => 6,
            "Feet" => 7,
            "Ears" => 8,
            "Neck" => 9,
            "Wrists" => 10,
            "Ring" => 11,
            _ => 99
        };
    }

    private Vector4 GetColorForSlot(string slotName)
    {
        // Accessories - purple
        if (slotName == "Ears" || slotName == "Neck" || slotName == "Wrists" || slotName == "Ring")
        {
            return LightPurple;
        }
        
        // Main gear - pink/magenta
        if (slotName == "Head" || slotName == "Body" || slotName == "Gloves" || slotName == "Legs" || slotName == "Feet")
        {
            return LightMagenta;
        }
        
        // Weapons - minty green
        if (slotName == "Main Hand" || slotName == "Off Hand")
        {
            return LightMintGreen;
        }
        
        // Default to purple if unknown
        return LightPurple;
    }
}

public class SharedModelGroup
{
    public string ModelId { get; set; } = string.Empty;
    public string SlotCategory { get; set; } = string.Empty;
    public List<SharedModelItem> Items { get; set; } = [];
}

public class SharedModelItem
{
    public string Name { get; set; } = string.Empty;
    public uint ItemId { get; set; }
    public int IconId { get; set; }
    public uint Slot { get; set; }
    public string ModelId { get; set; } = string.Empty;
    public byte DyeCount { get; set; }
    public bool CanGoInArmoire { get; set; }
}
