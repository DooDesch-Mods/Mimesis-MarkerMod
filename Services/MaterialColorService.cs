using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MarkerMod.Services
{
    internal static class MaterialColorService
    {
        private sealed class RendererColors
        {
            internal readonly Dictionary<int, MaterialPropertyBlock> Original = new();
            internal readonly MaterialPropertyBlock Working = new();
        }

        // Weak renderer keys let scene-owned renderers and their saved property blocks be collected together.
        private static readonly ConditionalWeakTable<Renderer, RendererColors> Colors = new();

        internal static void ApplyColorToRenderer(Renderer renderer, Color color, bool useDefaultMaterial)
        {
            if (renderer == null) return;
            Material[] materials = renderer.sharedMaterials;
            if (useDefaultMaterial)
            {
                if (!Colors.TryGetValue(renderer, out var previous)) return;
                foreach (var entry in previous.Original)
                {
                    if (entry.Key < materials.Length)
                        renderer.SetPropertyBlock(entry.Value.isEmpty ? null : entry.Value, entry.Key);
                }
                Colors.Remove(renderer);
                return;
            }

            var state = Colors.GetValue(renderer, _ => new RendererColors());
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null) continue;
                string colorProperty = material.HasProperty("_Color") ? "_Color"
                    : material.HasProperty("_BaseColor") ? "_BaseColor"
                    : material.HasProperty("_TintColor") ? "_TintColor" : null;
                if (colorProperty == null) continue;

                if (!state.Original.ContainsKey(i))
                {
                    var original = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(original, i);
                    state.Original.Add(i, original);
                }

                renderer.GetPropertyBlock(state.Working, i);
                // Indexed blocks override the renderer-wide block; retain its other values when creating one.
                if (state.Working.isEmpty) renderer.GetPropertyBlock(state.Working);
                state.Working.SetColor(colorProperty, IsBlackColor(color) ? new Color(0.15f, 0.15f, 0.15f, 1f) : color);
                if (material.HasProperty("_MainTex")) state.Working.SetTexture("_MainTex", Texture2D.whiteTexture);
                if (material.HasProperty("_BaseMap")) state.Working.SetTexture("_BaseMap", Texture2D.whiteTexture);
                renderer.SetPropertyBlock(state.Working, i);
            }
        }

        internal static void ApplyColorToTransform(Transform transform, Color color, bool useDefaultMaterial)
        {
            if (transform == null) return;
            foreach (var renderer in transform.GetComponentsInChildren<Renderer>(true))
                ApplyColorToRenderer(renderer, color, useDefaultMaterial);
        }

        private static bool IsBlackColor(Color color) => color.r < 0.1f && color.g < 0.1f && color.b < 0.1f;
    }
}
