using System.Collections.Generic;
using UnityEngine;
using MimicAPI.GameAPI;
using MarkerMod.Services;
using MelonLoader;
using MarkerMod.Config;

namespace MarkerMod.Managers
{
    internal static class PaintBallColorManager
    {
        private static readonly List<Color> ColorPalette = new()
        {
            new Color(1f, 0f, 0f, 1f),     // Rot
            new Color(1f, 1f, 0f, 1f),     // Gelb
            new Color(0f, 1f, 0f, 1f),     // Grün
            new Color(0f, 0f, 1f, 1f),     // Blau
            new Color(1f, 0.5f, 0f, 1f),  // Orange
            new Color(0.5f, 0f, 1f, 1f),  // Lila
            new Color(1f, 1f, 1f, 1f),    // Weiß
            new Color(0f, 0f, 0f, 1f),    // Schwarz
        };

        private static int currentColorIndex = -1;
        private static string cachedPaletteText;
        private static List<Color> cachedPalette;
        private static bool hasColorBeenSelected = false;
        private static readonly HashSet<int> PaintballMasterIDs = new()
        {
            2030, 70010, 70011, 70012, 70013, 70014, 70015, 70016, 70017, 70018
        };

        internal static void CycleColor()
        {
            var palette = GetPalette();
            hasColorBeenSelected = true;
            if (currentColorIndex == -1)
            {
                currentColorIndex = 0;
            }
            else if (currentColorIndex == palette.Count - 1)
            {
                currentColorIndex = -1;
            }
            else
            {
                currentColorIndex = currentColorIndex + 1;
            }
        }
        
        internal static void ResetColorSelection()
        {
            hasColorBeenSelected = false;
            currentColorIndex = -1;
        }

        internal static Color GetCurrentColor()
        {
            var palette = GetPalette();
            if (currentColorIndex == -1)
            {
                return Color.white;
            }
            return palette[currentColorIndex];
        }

        internal static string GetCurrentColorName()
        {
            var palette = GetPalette();
            if (currentColorIndex >= 0 && MarkerPreferences.PaintballColors != MarkerPreferences.DefaultPalette)
                return "#" + ColorUtility.ToHtmlStringRGBA(palette[currentColorIndex]);
            return currentColorIndex switch
            {
                -1 => "Default",
                0 => "Rot",
                1 => "Gelb",
                2 => "Grün",
                3 => "Blau",
                4 => "Orange",
                5 => "Lila",
                6 => "Weiß",
                7 => "Schwarz",
                _ => "Unbekannt"
            };
        }

        private static List<Color> GetPalette()
        {
            string text = MarkerPreferences.PaintballColors;
            if (text == MarkerPreferences.DefaultPalette)
            {
                if (currentColorIndex >= ColorPalette.Count) currentColorIndex = -1;
                return ColorPalette;
            }
            if (text != cachedPaletteText)
            {
                var palette = new List<Color>();
                foreach (string part in text.Split(','))
                {
                    if (!ColorUtility.TryParseHtmlString(part.Trim(), out Color color))
                        throw new System.InvalidOperationException("Effective paint palette contains an invalid HTML color.");
                    palette.Add(color);
                }
                cachedPalette = palette;
                cachedPaletteText = text;
            }
            if (currentColorIndex >= cachedPalette.Count) currentColorIndex = -1;
            return cachedPalette;
        }

        internal static bool IsPaintball(int itemMasterID)
        {
            return PaintballMasterIDs.Contains(itemMasterID);
        }

        internal static void ApplyColorToPaintball(object inventoryItem)
        {
            if (inventoryItem == null)
            {
                return;
            }

            try
            {
                Transform itemTransform = ReflectionHelper.GetPropertyValue<Transform>(inventoryItem, "Transform");
                if (itemTransform == null)
                {
                    return;
                }

                bool useDefaultMaterial = !hasColorBeenSelected || currentColorIndex == -1;
                Color color = GetCurrentColor();
                
                Renderer[] renderers = itemTransform.GetComponentsInChildren<Renderer>(true);
                if (renderers == null || renderers.Length == 0)
                {
                    return;
                }

                foreach (Renderer renderer in renderers)
                {
                    MaterialColorService.ApplyColorToRenderer(renderer, color, useDefaultMaterial);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[PaintBallColorManager] Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        internal static bool HasColorBeenSelected()
        {
            return hasColorBeenSelected;
        }

        internal static int GetCurrentColorIndex()
        {
            return currentColorIndex;
        }
    }
}

