using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.HarmonyPatches;

// This patch modifies the ChargeDamageCallback method in the Mission class to enable friendly fire by bypassing conditional.

[HarmonyPatch(typeof(Mission), "ChargeDamageCallback")]
public static class ChargeDamageCallbackPatch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // Confirm patch is applied
        // Debug.Print("Patched ChargeDamageCallback ran!", 0, TaleWorlds.Library.Debug.DebugColor.Cyan);
        var codes = instructions.ToList();

        for (int i = 0; i < codes.Count - 2; i++)
        {
            // Look for: ldarg.3 (attacker), ldarg.s victim, callvirt IsEnemyOf
            if (codes[i].opcode == OpCodes.Ldarg_3 &&
                codes[i + 1].opcode == OpCodes.Ldarg_S &&
                codes[i + 2].opcode == OpCodes.Callvirt &&
                codes[i + 2].operand is MethodInfo method &&
                method.Name == "IsEnemyOf")
            {
                // Skip the next instruction: brfalse.s or brfalse
                if (codes[i + 3].opcode == OpCodes.Brfalse || codes[i + 3].opcode == OpCodes.Brfalse_S)
                {
                    // Remove the check: NOP out IsEnemyOf and branch
                    codes[i].opcode = OpCodes.Nop;
                    codes[i + 1].opcode = OpCodes.Nop;
                    codes[i + 2].opcode = OpCodes.Nop;
                    codes[i + 3].opcode = OpCodes.Nop;

                    // Optionally: or force it to always branch (disable damage for everyone) — not recommended
                }

                break; // Only patch first occurrence
            }
        }

        return codes;
    }
}
