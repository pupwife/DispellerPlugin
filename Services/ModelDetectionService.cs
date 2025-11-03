using System.Collections.Generic;
using Lumina.Excel.Sheets;

namespace Dispeller.Services;

public class ModelDetectionService
{
    /// <summary>
    /// Extract model information from Item.ModelMain
    /// Based on Glamaholic's AlternativeFinder.ModelInfo
    /// </summary>
    public static (ushort, ushort, ushort, ushort) ExtractModelInfo(ulong raw)
    {
        var primaryKey = (ushort)(raw & 0xFFFF);
        var secondaryKey = (ushort)((raw >> 16) & 0xFFFF);
        var variant = (ushort)((raw >> 32) & 0xFFFF);
        var dye = (ushort)((raw >> 48) & 0xFFFF);

        if (variant != 0)
        {
            // weapon
            return (primaryKey, secondaryKey, variant, dye);
        }

        return (primaryKey, 0, 0, 0);
    }

    /// <summary>
    /// Check if two items share the same model
    /// </summary>
    public static bool ShareModel(Item item1, Item item2)
    {
        var model1 = ExtractModelInfo(item1.ModelMain);
        var model2 = ExtractModelInfo(item2.ModelMain);

        return model1 == model2;
    }

    /// <summary>
    /// Get all items that share a model with the given item
    /// </summary>
    public static List<Item> FindSharedModelItems(Item targetItem)
    {
        var targetModel = ExtractModelInfo(targetItem.ModelMain);
        var sharedItems = new List<Item>();

        foreach (var item in Plugin.DataManager.GetExcelSheet<Item>()!)
        {
            if (item.EquipSlotCategory.RowId != targetItem.EquipSlotCategory.RowId)
                continue;

            if (ExtractModelInfo(item.ModelMain) == targetModel)
            {
                sharedItems.Add(item);
            }
        }

        return sharedItems;
    }

    /// <summary>
    /// Get model ID string for display
    /// </summary>
    public static string GetModelIdString((ushort, ushort, ushort, ushort) model)
    {
        return $"{model.Item1}-{model.Item2}-{model.Item3}-{model.Item4}";
    }
}
