using Crpg.Module.Common;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.HarmonyPatches;

[HarmonyPatch(typeof(Mission))]
public static class Mission_ChargeDamageCallback_Patch
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

        WeaponComponentData shieldOnBack;
        CombatLogData combatLog;

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

        if (!attacker.IsEnemyOf(victim))
        {
            if (victim == null)
            {
                Debug.Print($"Charge damage mirrored for {attacker.Name} hitting a null victim with damage {collisionData.InflictedDamage} --cancelling");
                return true;
            }

            if (ChargeDamageControl.MirrorFriendlyChargeDamageAgent)
            {
                float mirroredDamage = collisionData.InflictedDamage * ChargeDamageControl.MirrorAgentDamageMultiplier;

                Debug.Print($"Charge damage mirrored for {attacker.Name} hitting {victim.Name} with damage {mirroredDamage}");
                // Apply mirrored damage to attacker (rider)
                CombatLogData dummyCombatLog = default;
                Blow mirroredBlowToRider = new Blow(attacker.Index)
                {
                    DamageType = blow.DamageType,
                    BaseMagnitude = mirroredDamage,
                    InflictedDamage = (int)mirroredDamage,
                    DamageCalculated = true,
                    BlowFlag = BlowFlags.None,
                    VictimBodyPart = BoneBodyPartType.Chest,
                    Direction = -attacker.LookDirection,
                };
                AttackCollisionData dummyCollision = default;
                dummyCollision.InflictedDamage = (int)mirroredDamage;

                __instance.RegisterBlow(attacker, attacker, null!, mirroredBlowToRider, ref dummyCollision, in MissionWeapon.Invalid, ref dummyCombatLog);
            }

            if (ChargeDamageControl.MirrorFriendlyChargeDamageMount)
            {
                // mirror the charge damage to the mount
                blow.SelfInflictedDamage = collisionData.InflictedDamage * ChargeDamageControl.MirrorMountDamageMultiplier; // Adjust the multiplier as needed
            }

            // blow.InflictedDamage = 0f; // No damage to the victim in this case but i think we should still hurt them
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
