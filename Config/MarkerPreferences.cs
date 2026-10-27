using System;
using DooDesch;
using MelonLoader;
using UnityEngine;

namespace MarkerMod.Config
{
	internal static class MarkerPreferences
	{
		private const string CategoryId = "MarkerMod";

		private static MelonPreferences_Category _category;
		private static MelonPreferences_Entry<bool> _keepFootprints;
		private static MelonPreferences_Entry<bool> _keepPuddles;
		private static MelonPreferences_Entry<float> _lifetimeSeconds;
		private static MelonPreferences_Entry<bool> _infinitePaintballs;
		private static MelonPreferences_Entry<bool> _enablePaintballColorChange;
		private static MelonPreferences_Entry<bool> _preventPaintballSelfDamage;
		private static MelonPreferences_Entry<bool> _allowLongPaintballThrows;
		private static HostSettings _hostSettings;
		private static HostSetting<bool> _hostInfinitePaintballs;
		private static HostSetting<bool> _hostPreventPaintballSelfDamage;
		private static HostSetting<bool> _hostAllowLongPaintballThrows;

		internal static void Initialize()
		{
			if (_category != null)
			{
				return;
			}

			_category = MelonPreferences.CreateCategory(CategoryId, "Marker Mod");
			_keepFootprints = CreateEntry("keepFootprints", true, "Keep Footprints", "Keep paint footprints after puddles dry. When enabled, paint footprints will persist even after the paint puddles have dried up. Default: true");
			_keepPuddles = CreateEntry("keepPuddles", false, "Keep Puddles", "Keep paint puddles for the configured lifetime. On the host, their contact effect also stays active so they can keep producing footprints. Footprints are controlled separately by keepFootprints. Default: false");
			_lifetimeSeconds = CreateEntry("permanentLifetimeSeconds", 86400f, "Lifetime", "Lifetime in seconds for persistent paint. This determines how long paint marks will remain visible. Default: 86400 (24 hours)");
			_infinitePaintballs = CreateEntry("infinitePaintballs", false, "Infinite Paintballs", "Paintballs are not consumed when thrown. Host setting: edit the host's config before creating the next lobby. Clients use the host value; a host without MarkerMod uses normal consumption. No new save is needed. Default: false");
			_enablePaintballColorChange = CreateEntry("enablePaintballColorChange", true, "Enable Paintball Color Change", "Allow changing paintball color by right-clicking. When enabled, you can cycle through colors (Red, Yellow, Green, Blue, Orange, Purple, White, Black) by right-clicking while holding a paintball. Default: true");
			_preventPaintballSelfDamage = CreateEntry("preventPaintballSelfDamage", true, "Prevent Paintball Self Damage", "Stop your own thrown paintball from hitting and damaging you. In game 0.3.0 projectiles can hit their thrower after a short distance; this excludes you from your own paintball hits. Default: true");
			_allowLongPaintballThrows = CreateEntry("allowLongPaintballThrows", false, "Allow Long Paintball Throws (experimental)", "Reduce the navmesh clamp that makes thrown paintballs stop early at an 'invisible wall'. Experimental and server-authoritative (host only) - verify in-game before relying on it. Default: false");

			_hostSettings = new HostSettings(CategoryId);
			_hostInfinitePaintballs = _hostSettings.Register(_infinitePaintballs, false);
			_hostPreventPaintballSelfDamage = _hostSettings.Register(_preventPaintballSelfDamage, false);
			_hostAllowLongPaintballThrows = _hostSettings.Register(_allowLongPaintballThrows, false);
			_hostSettings.EffectiveValuesChanged += LogSessionSettings;
		}

		private static void LogSessionSettings()
		{
			MelonLogger.Msg($"[MarkerMod] Session settings: inLobby={_hostSettings.IsInSession}, host={_hostSettings.IsHost}, infinitePaintballs={InfinitePaintballs} (local={_infinitePaintballs.Value}), keepFootprints={KeepFootprints}, keepPuddles={KeepPuddles}, lifetimeSeconds={PermanentLifetimeSeconds:F0}.");
		}

		private static MelonPreferences_Entry<T> CreateEntry<T>(string identifier, T defaultValue, string displayName, string description = null)
		{
			if (_category == null)
			{
				throw new InvalidOperationException("Preference category not initialized.");
			}

			return _category.CreateEntry(identifier, defaultValue, displayName, description);
		}

		// Gameplay settings use vanilla when the joined host has no MarkerMod snapshot.
		internal static bool KeepFootprints => _keepFootprints?.Value ?? true;

		internal static bool KeepPuddles => _keepPuddles?.Value ?? false;

		internal static float PermanentLifetimeSeconds => Mathf.Max(1f, _lifetimeSeconds?.Value ?? 86400f);

		internal static bool InfinitePaintballs => _hostInfinitePaintballs?.Value ?? false;

		internal static bool EnablePaintballColorChange => _enablePaintballColorChange?.Value ?? true;

		internal static bool PreventPaintballSelfDamage => _hostPreventPaintballSelfDamage?.Value ?? false;

		internal static bool AllowLongPaintballThrows => _hostAllowLongPaintballThrows?.Value ?? false;
	}
}

