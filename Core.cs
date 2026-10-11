using System.Linq;
using HarmonyLib;
using MarkerMod.Config;
using MelonLoader;
#if DEBUG
using System;
using UnityEngine.InputSystem;
#endif

[assembly: MelonInfo(typeof(MarkerMod.Core), "MarkerMod", DooDesch.ModVersion.Current, "DooDesch", null)]
[assembly: MelonGame("ReLUGames", "MIMESIS")]
[assembly: MelonOptionalDependencies("MimicAPI")]
// Stop MelonLoader from auto-applying our Harmony patches. We call PatchAll() ourselves in OnInitializeMelon,
// AFTER preferences and host settings are initialized. Without this, MelonLoader would auto-patch
// before init AND we'd patch again = double-apply.
[assembly: MelonLoader.HarmonyDontPatchAll]

namespace MarkerMod
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MarkerPreferences.Initialize();
            // Single patch application: MelonLoader's auto-patch is disabled via [assembly: HarmonyDontPatchAll],
            // so this runs exactly once, after preferences and host settings exist.
            HarmonyInstance.PatchAll();

            LoggerInstance.Msg($"MarkerMod initialized. Lifespan={MarkerPreferences.PermanentLifetimeSeconds:F0}s, Footprints={MarkerPreferences.KeepFootprints}, Puddles={MarkerPreferences.KeepPuddles}, InfinitePaintballs={MarkerPreferences.InfinitePaintballs}");
        }

#if DEBUG
        // Debug-only: press F8 to drop paintballs (master id 2030) at your feet via the existing "spawnitem" admin
        // command; the normal pickup flow puts them in your inventory and syncs the client. Works on host and client.
        private const int DebugPaintballMasterID = 2030;
        private const int DebugGivePaintballCount = 5;
#endif

        public override void OnUpdate()
        {
            DooDesch.HostSettings.Update();
#if DEBUG
            try
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard[Key.F8].wasPressedThisFrame)
                {
                    GiveDebugPaintballs(DebugGivePaintballCount);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"[DebugGive] OnUpdate error: {ex.Message}");
            }
#endif
        }

#if DEBUG
        private void GiveDebugPaintballs(int count)
        {
            if (!Hub.TryGetMain(out GameMainBase main) || main == null)
            {
                LoggerInstance.Warning("[DebugGive] GameMainBase not available yet.");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                main.SendPacket(new AdminCommandReq
                {
                    command = "spawnitem",
                    args = $"masterid={DebugPaintballMasterID}"
                });
            }

            LoggerInstance.Msg($"[DebugGive] Requested {count}x paintball (masterid={DebugPaintballMasterID}).");
        }
#endif
    }
}
