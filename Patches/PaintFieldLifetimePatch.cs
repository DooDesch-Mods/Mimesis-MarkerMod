using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Bifrost.Cooked;
using HarmonyLib;
using MarkerMod.Config;
using MarkerMod.Managers;

namespace MarkerMod.Patches
{
    // The server field must remain active for a persistent puddle to keep painting players.
    [HarmonyPatch]
    internal static class PaintFieldLifetimePatch
    {
        private static readonly FieldInfo Duration = AccessTools.Field(typeof(FieldSkillMemberInfo), nameof(FieldSkillMemberInfo.Duration));
        private static readonly MethodInfo Resolve = AccessTools.Method(typeof(PaintFieldLifetimePatch), nameof(ResolveDuration));

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(FieldSkillObject), nameof(FieldSkillObject.Update));
            yield return AccessTools.DeclaredMethod(typeof(FieldSkillObject), "GetInfo");
        }

        internal static long ResolveDuration(FieldSkillMemberInfo member, FieldSkillObject field)
        {
            // Member 1 is the repeatable paint contact field; member 2 is a short, one-time effect.
            if (!MarkerPreferences.KeepPuddles || member.Index != 1
                || !PaintPersistenceManager.IsPaintPuddle(field.MasterID) || member.Duration <= 0)
                return member.Duration;

            return Math.Max(member.Duration, PaintPersistenceManager.PermanentLifetimeMilliseconds);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            int matches = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand, Duration))
                {
                    var instance = new CodeInstruction(OpCodes.Ldarg_0)
                        .MoveLabelsFrom(instruction).MoveBlocksFrom(instruction);
                    yield return instance;
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = Resolve;
                    matches++;
                }
                yield return instruction;
            }
            if (matches == 0)
                throw new InvalidOperationException($"Paint lifetime patch failed: {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name} has no field-duration reads.");
        }
    }
}
