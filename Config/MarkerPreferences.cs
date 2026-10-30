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
		private static HostSetting<bool> _hostEnabled;
		private static HostSetting<bool> _hostKeepFootprints;
		private static HostSetting<bool> _hostKeepPuddles;
		private static HostSetting<float> _hostLifetimeSeconds;
		private static HostSetting<bool> _hostColorChange;
		private static HostSetting<float> _longThrowSnapRadius;
		private static HostSetting<string> _paintballColors;
		private static MelonPreferences_Entry<int> _colorMouseButton;
		internal const string DefaultPalette = "#FF0000,#FFFF00,#00FF00,#0000FF,#FF8000,#8000FF,#FFFFFF,#000000";

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
			_infinitePaintballs = CreateEntry("infinitePaintballs", false, "Infinite Paintballs", "Host setting, live. Paintballs are not consumed when thrown. Clients use the host value; a host without MarkerMod uses normal consumption. No new save is needed. Default: false");
			_enablePaintballColorChange = CreateEntry("enablePaintballColorChange", true, "Enable Paintball Color Change", "Host setting, live. Permit cycling through PaintballColors while holding a paintball. The local ColorMouseButton selects the input. Default: true");
			_preventPaintballSelfDamage = CreateEntry("preventPaintballSelfDamage", true, "Prevent Paintball Self Damage", "Stop your own thrown paintball from hitting and damaging you. In game 0.3.0 projectiles can hit their thrower after a short distance; this excludes you from your own paintball hits. Default: true");
			_allowLongPaintballThrows = CreateEntry("allowLongPaintballThrows", false, "Allow Long Paintball Throws (experimental)", "Reduce the navmesh clamp that makes thrown paintballs stop early at an 'invisible wall'. Experimental and server-authoritative (host only) - verify in-game before relying on it. Default: false");

			_hostSettings = new HostSettings(CategoryId);
			_hostEnabled = _hostSettings.Register(CreateEntry("Enabled", true, "Enabled", "Host setting, next lobby. Master switch for MarkerMod. Existing timed paint keeps the lifetime assigned when it spawned."), false, legacy: false);
			_hostKeepFootprints = _hostSettings.Register(_keepFootprints, false, legacy: false);
			_hostKeepPuddles = _hostSettings.Register(_keepPuddles, false, legacy: false);
			_hostLifetimeSeconds = _hostSettings.Register(_lifetimeSeconds, 1f, value => value >= 1f, legacy: false, localValue: () => Mathf.Max(1f, _lifetimeSeconds.Value));
			_hostInfinitePaintballs = _hostSettings.Register(_infinitePaintballs, false, live: true);
			_hostPreventPaintballSelfDamage = _hostSettings.Register(_preventPaintballSelfDamage, false, live: true);
			_hostAllowLongPaintballThrows = _hostSettings.Register(_allowLongPaintballThrows, false, live: true);
			_hostColorChange = _hostSettings.Register(_enablePaintballColorChange, false, live: true, legacy: false);
			_longThrowSnapRadius = _hostSettings.Register(CreateEntry("LongThrowSnapRadius", 25f, "Long Throw Snap Radius", "Host setting, live. Navmesh search radius used when long throws are enabled. Positive world units. Default: 25"), 25f, value => value > 0, live: true, legacy: false);
			_paintballColors = _hostSettings.Register(CreateEntry("PaintballColors", DefaultPalette, "Paintball Color Palette", "Host setting, next lobby. Comma-separated HTML colors, 1-32 entries. The native eight colors are the default."), DefaultPalette, IsValidPalette, legacy: false);
			_colorMouseButton = CreateEntry("ColorMouseButton", 1, "Color Cycle Mouse Button", "Local input setting. Mouse button for changing paintball color: 0 left, 1 right, 2 middle, -1 disables cycling. Default: 1");
			_hostSettings.EffectiveValuesChanged += LogSessionSettings;
			_hostSettings.EffectiveValuesChanged += Managers.PaintBallColorManager.ResetColorSelection;
			_hostSettings.EffectiveValuesChanged += Managers.PaintPersistenceManager.ResetSession;
			_hostSettings.LiveValuesChanged += LogSessionSettings;
			_hostSettings.LiveValuesChanged += Patches.InfinitePaintballDisplayPatch.Refresh;
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
		internal static bool Enabled => _hostSettings?.UsingLegacyHost == true || (_hostEnabled?.Value ?? false);
		internal static bool KeepFootprints => Enabled && (_hostKeepFootprints?.Value ?? false);

		internal static bool KeepPuddles => Enabled && (_hostKeepPuddles?.Value ?? false);

		internal static float PermanentLifetimeSeconds => _hostLifetimeSeconds?.Value ?? 1f;

		internal static bool InfinitePaintballs => Enabled && (_hostInfinitePaintballs?.Value ?? false);

		internal static bool EnablePaintballColorChange => Enabled && (_hostColorChange?.Value ?? false);

		internal static bool PreventPaintballSelfDamage => Enabled && (_hostPreventPaintballSelfDamage?.Value ?? false);

		internal static bool AllowLongPaintballThrows => Enabled && (_hostAllowLongPaintballThrows?.Value ?? false);
		internal static float LongThrowSnapRadius => _longThrowSnapRadius?.Value ?? 25f;
		internal static string PaintballColors => _paintballColors?.Value ?? DefaultPalette;
		internal static int ColorMouseButton => Math.Clamp(_colorMouseButton?.Value ?? 1, -1, 2);
		private static bool IsValidPalette(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return false;
			var parts = value.Split(',');
			if (parts.Length > 32) return false;
			foreach (var part in parts)
				if (!ColorUtility.TryParseHtmlString(part.Trim(), out _)) return false;
			return true;
		}
	}
}

