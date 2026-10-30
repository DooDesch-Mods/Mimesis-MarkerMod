using System.Collections.Generic;
using HarmonyLib;
using MarkerMod.Config;
using Mimic;

namespace MarkerMod.Patches
{
    [HarmonyPatch(typeof(UIPrefab_Inventory), nameof(UIPrefab_Inventory.UpdateSlot))]
    internal static class InfinitePaintballDisplayPatch
    {
        private static readonly System.Reflection.FieldInfo InventoryUi = AccessTools.Field(typeof(GameMainBase), "inventoryui")
            ?? throw new System.MissingFieldException("GameMainBase", "inventoryui");
        internal static void Refresh()
        {
            var main = Hub.Main;
            var avatar = main?.GetMyAvatar();
            if (avatar != null && InventoryUi.GetValue(main) != null) main.UpdateInventoryUI(avatar);
        }

        private static void Postfix(List<InventoryItem> inventoryItems, List<int> visibleSlotIndices,
            List<UIPrefab_Inventory.Slot> ___inventorySlots)
        {
            if (!MarkerPreferences.InfinitePaintballs || visibleSlotIndices == null)
                return;

            foreach (int index in visibleSlotIndices)
            {
                if (index < 0 || index >= inventoryItems.Count || index >= ___inventorySlots.Count)
                    continue;
                var item = inventoryItems[index];
                var label = ___inventorySlots[index]?.stackCount;
                if (item?.ItemMasterID == 2030 && label != null)
                    label.text = "\u221e";
            }
        }
    }
}
