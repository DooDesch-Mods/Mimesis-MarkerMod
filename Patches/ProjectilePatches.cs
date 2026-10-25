using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using MarkerMod.Config;
using MarkerMod.Managers;
using MelonLoader;
using MimicAPI.GameAPI;
using ReluProtocol;
#if DEBUG
using ReluProtocol.Enum;
#endif

namespace MarkerMod.Patches
{
    // Bug 2 (THE fix): in 0.3.0 HandlePhysicsProj re-includes the thrower in the projectile's hit detection once it
    // has travelled SelfHitGraceDistance*0.01 metres (VProjectileObject.cs:179-185). That grace distance is tiny for
    // paintballs, so the ball - still right next to the thrower just after launch - immediately "hits" them, lands at
    // their feet and spawns the puddle under them (it never flies, and used to damage them). Forcing
    // SelfHitGraceDistance to a huge value keeps `excludeActor` = `_parentActor` for the whole flight: the ball passes
    // through the thrower and flies to where it was aimed, and can never self-hit. Gated behind the
    // effective host preference at runtime; when off, the original grace distance is used.
    [HarmonyPatch(typeof(VProjectileObject), "HandlePhysicsProj")]
    internal static class VProjectileSelfHitGracePatch
    {
        private const float HugeGraceDistance = 1_000_000_000f;

        public static float ResolveGraceDistance(float originalDistance)
            => MarkerPreferences.PreventPaintballSelfDamage ? HugeGraceDistance : originalDistance;

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;

                if (instruction.opcode == OpCodes.Ldfld
                    && instruction.operand is System.Reflection.FieldInfo field
                    && field.Name == "SelfHitGraceDistance")
                {
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(VProjectileSelfHitGracePatch), nameof(ResolveGraceDistance)));
                }
            }
        }
    }

    // Bug 2 (backup): removes the thrower from the projectile's own OnHit list. Redundant while the grace transpiler
    // above is active (the thrower never enters the hit list), but harmless and cheap (runs only on projectile hits),
    // so it stays as a safety net if that transpiler is ever disabled or fails to apply.
    [HarmonyPatch(typeof(VProjectileObject), nameof(VProjectileObject.OnHit))]
    internal static class VProjectileSelfHitPatch
    {
        [HarmonyPrefix]
        public static void Prefix(VProjectileObject __instance, List<int> hitTargetActorIDs)
        {
            if (!MarkerPreferences.PreventPaintballSelfDamage || hitTargetActorIDs == null || hitTargetActorIDs.Count == 0)
            {
                return;
            }

            try
            {
                object parentActor = ReflectionHelper.GetFieldValue(__instance, "_parentActor");
                if (parentActor == null)
                {
                    return;
                }

                int parentActorID = ReflectionHelper.GetPropertyValue<int>(parentActor, "ObjectID");
                if (parentActorID != 0 && hitTargetActorIDs.RemoveAll(id => id == parentActorID) > 0)
                {
#if DEBUG
                    MelonLogger.Msg($"[PaintballSelfDamage] Prevented thrower (actorID={parentActorID}) from being hit by own projectile.");
#endif
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[PaintballSelfDamage] Projectile OnHit patch error: {ex.Message}");
            }
        }
    }

    // Keep zero-value paint hits: they apply the paint status that produces footprints.
    // Only exclude the caster from paint sequences that also change a stat.
    [HarmonyPatch(typeof(FieldSkillObject), nameof(FieldSkillObject.OnHit))]
    internal static class FieldSkillSelfHitPatch
    {
#if DEBUG
        // Debug-only: log each distinct field-skill master id once (is OnHit the splat's damage path? master id?
        // caster resolvable / among the targets?).
        private static readonly HashSet<int> DiagLoggedMasterIds = new HashSet<int>();
#endif

        [HarmonyPrefix]
        public static void Prefix(FieldSkillObject __instance, FieldSkillHitInputData hitInfo)
        {
            if (!MarkerPreferences.PreventPaintballSelfDamage
                || !PaintPersistenceManager.IsPaintFieldSkill(__instance.MasterID)
                || hitInfo?.SkillSequenceInfo == null || hitInfo.SkillSequenceInfo.MutableValue == 0
                || hitInfo?.Targets == null || hitInfo.Targets.Count == 0)
            {
                return;
            }

            try
            {
                object skillContext = ReflectionHelper.GetFieldValue(__instance, "_parentSkillContext");
                object contextInfo = skillContext != null ? ReflectionHelper.GetPropertyValue(skillContext, "ContextInfo") : null;
                object caster = contextInfo != null ? ReflectionHelper.GetFieldValue(contextInfo, "Creature") : null;
                int casterID = caster != null ? ReflectionHelper.GetPropertyValue<int>(caster, "ObjectID") : 0;

#if DEBUG
                int fieldSkillMasterID = __instance.MasterID;
                if (DiagLoggedMasterIds.Add(fieldSkillMasterID))
                {
                    string targets = string.Join(",", hitInfo.Targets.ConvertAll(t => t.targetObjectID.ToString()).ToArray());
                    MelonLogger.Msg($"[PaintballSelfDamage][DIAG] FieldSkill.OnHit masterID={fieldSkillMasterID}, isPaint={PaintPersistenceManager.IsPaintFieldSkill(fieldSkillMasterID)}, ctx={(skillContext != null)}, casterID={casterID}, targets=[{targets}]");
                }
#endif

                if (casterID != 0)
                {
                    hitInfo.Targets.RemoveAll(t => t.targetObjectID == casterID);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[PaintballSelfDamage] FieldSkill OnHit patch error: {ex.Message}");
            }
        }
    }

    // Bug 2 (experimental, opt-in): the 0.3.0 navmesh clamp force-lands a thrown projectile as soon as it crosses
    // a non-navmesh point (the "invisible wall"). Widening the navmesh-snap search radius lets projectiles bridge
    // small non-navmesh gaps instead of stopping short. This is server-authoritative and affects all projectiles,
    // so the runtime helper returns the original radius unless the effective host preference enables it.
    [HarmonyPatch(typeof(VProjectileObject), "HandlePhysicsProj")]
    internal static class VProjectileNavmeshPatch
    {
        private const float WidenedSnapRadius = 25f;

        public static float ResolveSnapRadius(float originalRadius)
            => MarkerPreferences.AllowLongPaintballThrows ? WidenedSnapRadius : originalRadius;

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                if (code.opcode != OpCodes.Ldc_R4 || i + 1 >= codes.Count)
                {
                    yield return code;
                    continue;
                }

                CodeInstruction next = codes[i + 1];
                bool isCall = next.opcode == OpCodes.Call || next.opcode == OpCodes.Callvirt;

                yield return code;
                if (isCall
                    && next.operand is System.Reflection.MethodInfo method
                    && method.Name == "GetNearestPointOnNavMesh")
                {
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(VProjectileNavmeshPatch), nameof(ResolveSnapRadius)));
                }
            }
        }
    }

#if DEBUG
    // Debug-only self-damage source logger (log only, no behaviour change). ALL server damage funnels through
    // StatController.ApplyDamage(ApplyDamageArgs), so this is the one place to identify what is hurting the player:
    // it logs each distinct (cause, skill, self?) combination of player damage once. Invaluable for diagnosing any
    // future self-damage report; compiled out of Release entirely (never patches this hot path in production).
    [HarmonyPatch(typeof(StatController), nameof(StatController.ApplyDamage))]
    internal static class StatControllerApplyDamageDiagnosticPatch
    {
        private static readonly HashSet<string> DiagLoggedSignatures = new HashSet<string>();

        [HarmonyPrefix]
        public static void Prefix(ApplyDamageArgs args)
        {
            try
            {
                if (args == null || args.Victim == null || args.Damage <= 0 || !(args.Victim is VPlayer))
                {
                    return;
                }

                int victimID = args.Victim.ObjectID;
                int attackerID = args.Attacker != null ? args.Attacker.ObjectID : 0;
                bool isSelf = args.Attacker != null && attackerID == victimID;

                string sig = $"{args.MutableStatChangeCause}:{args.SkillMasterID}:{isSelf}";
                if (DiagLoggedSignatures.Add(sig))
                {
                    MelonLogger.Msg($"[PaintballSelfDamage][DIAG] player {victimID} damaged: attacker={attackerID}, self={isSelf}, cause={args.MutableStatChangeCause}, skillMasterID={args.SkillMasterID}, dmg={args.Damage}");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[PaintballSelfDamage] ApplyDamage diagnostic error: {ex.Message}");
            }
        }
    }
#endif
}
