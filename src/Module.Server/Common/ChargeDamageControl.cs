using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common;

/*
    // This class controls the charge damage behavior in the game through ChargeDamageCallbackPatch class

    // Console commands/ServerConfigurations to control charge damage behavior

    crpg_charge_damage_disable_all = False // Disable all charge damage ***overrides other flags***
    crpg_charge_damage_allow_enemy = True // Allow charge damage to enemies
    crpg_charge_damage_allow_friendly = True // Allow charge damage to friends
    crpg_charge_damage_mirror_friendly_to_mount = True // Mirror charge damage from friendlies to mount
    crpg_charge_damage_mirror_friendly_to_agent = True // Mirror charge damage from friendlies to rider
    crpg_charge_damage_mirror_mount_damage_multiplier = 3 // Multiplier for charge damage to mount
    crpg_charge_damage_mirror_agent_damage_multiplier = 3 // Multiplier for charge damage to rider

    // ServerConfiguration values are used to control the charge damage behavior

    CrpgServerConfiguration.DisableAllChargeDamage = false
    CrpgServerConfiguration.AllowChargeEnemies = true
    CrpgServerConfiguration.AllowFriendlyChargeDamage = true
    CrpgServerConfiguration.MirrorFriendlyChargeDamageMount = true
    CrpgServerConfiguration.MirrorFriendlyChargeDamageAgent = true
    CrpgServerConfiguration.MirrorMountDamageMultiplier = 3
    CrpgServerConfiguration.MirrorAgentDamageMultiplier = 3

*/

public static class ChargeDamageControl
{
    public static bool ShouldAllowChargeDamage(Agent attacker, Agent victim)
    {
        if (CrpgServerConfiguration.DisableAllChargeDamage)
        {
            return false;
        }

        if (!CrpgServerConfiguration.AllowChargeEnemies && attacker.IsEnemyOf(victim))
        {
            return false;
        }

        if (!CrpgServerConfiguration.AllowFriendlyChargeDamage && !attacker.IsEnemyOf(victim))
        {
            return false;
        }

        // Allow charge damage if no blocking rule applies
        return true;
    }
}
