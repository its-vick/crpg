using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common;

// This class controls the charge damage behavior in the game through ChargeDamageCallbackPatch
/*
DisableAllChargeDamage = true   No charge damage at all *overrides other flags*
DisableChargeEnemies = true     You cant bump enemies
AllowChargeFriends = true       You can bump allies
All false (default)             Only bump enemies (vanilla)
*/
public static class ChargeDamageControl
{
    public static bool DisableAllChargeDamage { get; set; } = false;
    public static bool DisableChargeEnemies { get; set; } = false;
    public static bool AllowChargeFriends { get; set; } = false;

    public static bool ShouldAllowChargeDamage(Agent attacker, Agent victim)
    {
        if (DisableAllChargeDamage)
        {
            return false;
        }

        if (DisableChargeEnemies && attacker.IsEnemyOf(victim))
        {
            return false;
        }

        if (!AllowChargeFriends && !attacker.IsEnemyOf(victim))
        {
            return false;
        }

        // Allow charge damage if no blocking rule applies
        return true;
    }
}
