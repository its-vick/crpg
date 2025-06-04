using Crpg.Module.Common;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.HarmonyPatches;

// Harmony patch for the ChargeDamageCallback method in the Mission class
// Needs MissionInternalHelper.cs to reverse patch the GetAttackCollisionResults method and RegisterBlow method

[HarmonyPatch(typeof(Mission))]
public static class ChargeDamageCallbackPatch
{
    [HarmonyPatch("ChargeDamageCallback")]
    [HarmonyPrefix]
    private static bool Prefix(
        Mission __instance,
        ref AttackCollisionData collisionData,
        Blow blow,
        Agent attacker,
        Agent victim)
    {
        if (victim.CurrentMortalityState == Agent.MortalityState.Invulnerable ||
            (attacker.RiderAgent != null && !ChargeDamageControl.ShouldAllowChargeDamage(attacker, victim)))
        {
            return false;
        }

#pragma warning disable IDE0018 // Inline variable declaration
        WeaponComponentData shieldOnBack;
        CombatLogData combatLog;
#pragma warning restore IDE0018 // Inline variable declaration

        // Call the reverse patched method to get collision results
        __instance.GetAttackCollisionResults(
             attacker,
             victim,
             null!, // GameEntity is not used in this context, so we can pass null
             1f,
             in MissionWeapon.Invalid,
             false,
             false,
             false,
             ref collisionData,
             out shieldOnBack,
             out combatLog);

        if (collisionData.CollidedWithShieldOnBack && shieldOnBack != null && victim != null && victim.IsMainAgent)
        {
            InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("ui_hit_shield_on_back").ToString(), Color.ConvertStringToColor("#FFFFFFFF")));
        }

        if ((double)collisionData.InflictedDamage <= 0.0)
        {
            return false;
        }

        blow.BaseMagnitude = collisionData.BaseMagnitude;
        blow.MovementSpeedDamageModifier = collisionData.MovementSpeedDamageModifier;
        blow.InflictedDamage = collisionData.InflictedDamage;
        blow.SelfInflictedDamage = collisionData.SelfInflictedDamage;
        blow.AbsorbedByArmor = (float)collisionData.AbsorbedByArmor;
        blow.DamageCalculated = true;

        if (MissionGameModels.Current.AgentApplyDamageModel.DecideAgentKnockedBackByBlow(attacker, victim, in collisionData, null, in blow))
        {
            blow.BlowFlag |= BlowFlags.KnockBack;
        }
        else
        {
            blow.BlowFlag &= ~BlowFlags.KnockBack;
        }

        if (MissionGameModels.Current.AgentApplyDamageModel.DecideAgentKnockedDownByBlow(attacker, victim, in collisionData, null, in blow))
        {
            blow.BlowFlag |= BlowFlags.KnockDown;
        }

        // Handle friendly charge damage
        if (!attacker.IsEnemyOf(victim))
        {
            if (victim == null)
            {
                Debug.Print($"Friendly charge damage for {attacker.Name} hitting a null victim with damage {collisionData.InflictedDamage} --cancelling", 0, Debug.DebugColor.Red);
                return true;
            }

            if (CrpgServerConfiguration.MirrorFriendlyChargeDamageAgent)
            {
                if (attacker.RiderAgent != null && attacker.RiderAgent.IsActive())
                {
                    Blow duplicateBlow = blow;

                    duplicateBlow.InflictedDamage = 0; // No damage to the victim in duplicate blow, only here to mirror the damage to the attacker agent
                    duplicateBlow.SelfInflictedDamage = collisionData.InflictedDamage * CrpgServerConfiguration.MirrorAgentDamageMultiplier; // Adjust the multiplier as needed

                    // Apply mirrored damage to attacker (rider) but not to the victim up here
                    __instance.RegisterBlow(attacker.RiderAgent, victim!, null!, duplicateBlow, ref collisionData, in MissionWeapon.Invalid, ref combatLog);
                }
            }

            if (CrpgServerConfiguration.MirrorFriendlyChargeDamageMount)
            {
                Blow duplicateBlow = blow;

                duplicateBlow.InflictedDamage = 0; // No damage to the victim in duplicate blow, only here to mirror the damage to the mount
                duplicateBlow.SelfInflictedDamage = collisionData.InflictedDamage * CrpgServerConfiguration.MirrorMountDamageMultiplier; // Adjust the multiplier as needed

                // Apply mirrored damage to mount but not to the victim up here
                __instance.RegisterBlow(attacker, victim!, null!, duplicateBlow, ref collisionData, in MissionWeapon.Invalid, ref combatLog);
            }

            // blow.InflictedDamage = 0f; // No damage to the victim in original blow to cancel victim being affected but i think we should still hurt them
        }

        __instance.RegisterBlow(attacker, victim!, null!, blow, ref collisionData, in MissionWeapon.Invalid, ref combatLog);

        // Return false to skip the original method entirely
        return false;
    }
}


/* original methods

   internal void ChargeDamageCallback(
      ref AttackCollisionData collisionData,
      Blow blow,
      Agent attacker,
      Agent victim)
    {
      if (victim.CurrentMortalityState == Agent.MortalityState.Invulnerable || attacker.RiderAgent != null && !attacker.IsEnemyOf(victim))
        return;
      WeaponComponentData shieldOnBack;
      CombatLogData combatLog;
      this.GetAttackCollisionResults(attacker, victim, (GameEntity) null, 1f, in MissionWeapon.Invalid, false, false, false, ref collisionData, out shieldOnBack, out combatLog);
      if (collisionData.CollidedWithShieldOnBack && shieldOnBack != null && victim != null && victim.IsMainAgent)
        InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("ui_hit_shield_on_back").ToString(), Color.ConvertStringToColor("#FFFFFFFF")));
      if ((double) collisionData.InflictedDamage <= 0.0)
        return;
      blow.BaseMagnitude = collisionData.BaseMagnitude;
      blow.MovementSpeedDamageModifier = collisionData.MovementSpeedDamageModifier;
      blow.InflictedDamage = collisionData.InflictedDamage;
      blow.SelfInflictedDamage = collisionData.SelfInflictedDamage;
      blow.AbsorbedByArmor = (float) collisionData.AbsorbedByArmor;
      blow.DamageCalculated = true;
      if (MissionGameModels.Current.AgentApplyDamageModel.DecideAgentKnockedBackByBlow(attacker, victim, in collisionData, (WeaponComponentData) null, in blow))
        blow.BlowFlag |= BlowFlags.KnockBack;
      else
        blow.BlowFlag &= ~BlowFlags.KnockBack;
      if (MissionGameModels.Current.AgentApplyDamageModel.DecideAgentKnockedDownByBlow(attacker, victim, in collisionData, (WeaponComponentData) null, in blow))
        blow.BlowFlag |= BlowFlags.KnockDown;
      this.RegisterBlow(attacker, victim, (GameEntity) null, blow, ref collisionData, new MissionWeapon(), ref combatLog);
    }
*/
