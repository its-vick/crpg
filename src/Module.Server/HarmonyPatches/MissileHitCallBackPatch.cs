using Crpg.Module.Api.Models.Items;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;
using static TaleWorlds.MountAndBlade.Mission;

namespace Crpg.Module.HarmonyPatches;

// patches MissileHitCallback in Mission
// can modify the collisionData to make the arrow do no damage and bounce off

#if CRPG_SERVER
[HarmonyPatchCategory("Late")]
[HarmonyPatch(typeof(Mission), "MissileHitCallback")]
public static class MissileHitCallbackPatch
{
    [HarmonyPrefix]
    public static void Prefix_MissileHitCallback(ref int extraHitParticleIndex, ref AttackCollisionData collisionData, Vec3 missileStartingPosition, Vec3 missilePosition, Vec3 missileAngularVelocity, Vec3 movementVelocity, MatrixFrame attachGlobalFrame, MatrixFrame affectedShieldGlobalFrame, int numDamagedAgents, Agent attacker, Agent victim, GameEntity hitEntity)
    {
        if (victim == null || !victim.IsHuman)
        {
            return;
        }

        int missileIndex = collisionData.AffectorWeaponSlotOrMissileIndex;

        Mission.Missile missile = Mission.Current.Missiles
            .FirstOrDefault(m => m.Index == missileIndex);

        if (missile == null || missile.Weapon.IsEmpty || missile.Weapon.IsEqualTo(MissionWeapon.Invalid) || missile.Weapon.Item == null)
        {
            Debug.Print("missile is null or invalid missionWeapon or item!", 0, Debug.DebugColor.Purple);
            return;
        }

        MissionWeapon missileWeapon = missile.Weapon;
        ItemObject missileItem = missileWeapon.Item;

        /*
                DamageTypes.Invalid; // -1
                DamageTypes.Cut; // 0 // most likely to bounce
                DamageTypes.Pierce; // 1 most likely to penetrate
                DamageTypes.Blunt; // 2 // least chance to bounce off

                ItemObject.ItemTypeEnum.Arrows; // has cut pierce and blunt damage types
                ItemObject.ItemTypeEnum.Bolts; // has cut and pierce
                ItemObject.ItemTypeEnum.Bullets;
                ItemObject.ItemTypeEnum.Thrown; // has cut pierce and blunt damage types

                */

        // Logic for When missile should bounce?
        // if arrow/bolt/ and victim armor ?
        bool missileBounced = false;
        BoneBodyPartType bodyPartHit = collisionData.VictimHitBodyPart;
        MissionWeapon armorWeapon = MissionWeapon.Invalid;
        // int armorAmount = 0;
        float armorEffectiveness = victim.GetBaseArmorEffectivenessForBodyPart(bodyPartHit);
        float armorAmount = GetCombinedArmorForBodyPart(victim, bodyPartHit);
        switch (bodyPartHit)
        {
            case BoneBodyPartType.ArmLeft or BoneBodyPartType.ArmRight:
                armorWeapon = victim.Equipment[EquipmentIndex.Gloves];
                if (IsArmorWeaponPlate(armorWeapon))
                {
                    // armorAmount = armorWeapon.Item.ArmorComponent.ArmArmor;
                    // 25 is high enough
                }

                break;
            case BoneBodyPartType.Abdomen or BoneBodyPartType.Chest:
                armorWeapon = victim.Equipment[EquipmentIndex.Body];
                if (IsArmorWeaponPlate(armorWeapon))
                {
                    // armorAmount = armorWeapon.Item.ArmorComponent.BodyArmor;
                    // 55 is high enough
                }

                break;
            case BoneBodyPartType.Head:
                armorWeapon = victim.Equipment[EquipmentIndex.Head];
                if (IsArmorWeaponPlate(armorWeapon))
                {
                    // armorAmount = armorWeapon.Item.ArmorComponent.HeadArmor; // 52 is high enough
                }

                break;
            case BoneBodyPartType.Neck: // maybe check helmet and chest and cape

                break;
            case BoneBodyPartType.ShoulderLeft or BoneBodyPartType.ShoulderRight:
                armorWeapon = victim.Equipment[EquipmentIndex.Cape];
                if (IsArmorWeaponPlate(armorWeapon))
                {
                    // armorAmount = armorWeapon.Item.ArmorComponent.BodyArmor;
                }

                break;
            case BoneBodyPartType.Legs:
                armorWeapon = victim.Equipment[EquipmentIndex.Leg];
                if (IsArmorWeaponPlate(armorWeapon))
                {
                    // armorAmount = armorWeapon.Item.ArmorComponent.LegArmor;
                }

                break;
            default:
                break;
        }

        if (IsValidArmorWeaponItem(armorWeapon) && armorWeapon.Item.ArmorComponent.MaterialType == ArmorComponent.ArmorMaterialTypes.Plate) // CrpgArmorMaterialType.Plate?
        {

            missileBounced = true;
        }

        if (missileBounced)
        {
            int physicsMaterialIndex = PhysicsMaterial.GetFromName("metal").Index;

            collisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
                            collisionData.AttackBlockedWithShield,
                            collisionData.CorrectSideShieldBlock,
                            collisionData.IsAlternativeAttack,
                            false, // isColliderAgent
                            collisionData.CollidedWithShieldOnBack,
                            collisionData.IsMissile,
                            collisionData.MissileBlockedWithWeapon,
                            true, // missileHasPhysics
                            collisionData.EntityExists,
                            collisionData.ThrustTipHit,
                            collisionData.MissileGoneUnderWater,
                            collisionData.MissileGoneOutOfBorder,
                            CombatCollisionResult.None,
                            collisionData.AffectorWeaponSlotOrMissileIndex,
                            collisionData.StrikeType,
                            collisionData.DamageType,
                            collisionData.CollisionBoneIndex,
                            collisionData.VictimHitBodyPart,
                            collisionData.AttackBoneIndex,
                            collisionData.AttackDirection,
                            physicsMaterialIndex,
                            collisionData.CollisionHitResultFlags,
                            collisionData.AttackProgress,
                            collisionData.CollisionDistanceOnWeapon,
                            collisionData.AttackerStunPeriod,
                            0f, // collisionData.DefenderStunPeriod,
                            0f, // MissileTotalDamage
                            collisionData.MissileStartingBaseSpeed,
                            collisionData.ChargeVelocity,
                            collisionData.FallSpeed,
                            collisionData.WeaponRotUp,
                            collisionData.WeaponBlowDir,
                            collisionData.CollisionGlobalPosition,
                            collisionData.MissileVelocity,
                            collisionData.MissileStartingPosition,
                            collisionData.VictimAgentCurVelocity,
                            collisionData.CollisionGlobalNormal);

            Current.MakeSound(CombatSoundContainer.SoundCodeMissionCombatMetalShieldBash, collisionData.CollisionGlobalPosition, false, false, attacker.Index, victim.Index);
            SoundEventParameter soundEventParameter = new("Force", 1f);
            Current.MakeSound(ItemPhysicsSoundContainer.SoundCodePhysicsArrowlikeDefault, collisionData.CollisionGlobalPosition, false, false, attacker.Index, victim.Index, ref soundEventParameter);
            TaleWorlds.Library.Debug.Print("MissileHitPatched arrow bounced!!");
        }

        return;
    }

    private static bool IsValidArmorWeaponItem(MissionWeapon armorWeapon)
    {
        if (armorWeapon.IsEmpty || armorWeapon.IsEqualTo(MissionWeapon.Invalid) || armorWeapon.Item == null)
        {
            return false;
        }

        if (armorWeapon.Item.ItemType == ItemObject.ItemTypeEnum.BodyArmor ||
            armorWeapon.Item.ItemType == ItemObject.ItemTypeEnum.Cape ||
            armorWeapon.Item.ItemType == ItemObject.ItemTypeEnum.ChestArmor ||
            armorWeapon.Item.ItemType == ItemObject.ItemTypeEnum.HandArmor ||
            armorWeapon.Item.ItemType == ItemObject.ItemTypeEnum.HeadArmor ||
            armorWeapon.Item.ItemType == ItemObject.ItemTypeEnum.LegArmor)
        {
            return true;
        }

        return false;
    }

    private static float GetCombinedArmorForBodyPart(Agent agent, BoneBodyPartType bodyPart)
    {
        float totalArmor = 0f;

        foreach (EquipmentIndex index in new[]
        {
            EquipmentIndex.Head,
            EquipmentIndex.Body,
            EquipmentIndex.Leg,
            EquipmentIndex.Gloves,
            EquipmentIndex.Cape,
        })
        {
            MissionWeapon armorWeapon = agent.Equipment[index];
            if (armorWeapon.IsEmpty || armorWeapon.Item == null || armorWeapon.Item.ArmorComponent == null)
            {
                continue;
            }

            ArmorComponent armor = armorWeapon.Item.ArmorComponent;

            switch (bodyPart)
            {
                case BoneBodyPartType.Head:
                    totalArmor += armor.HeadArmor;
                    break;

                case BoneBodyPartType.Chest:
                case BoneBodyPartType.Abdomen:
                    totalArmor += armor.BodyArmor;
                    break;

                case BoneBodyPartType.ArmLeft:
                case BoneBodyPartType.ArmRight:
                    totalArmor += armor.ArmArmor;
                    break;

                case BoneBodyPartType.Legs:
                    totalArmor += armor.LegArmor;
                    break;

                case BoneBodyPartType.ShoulderLeft:
                case BoneBodyPartType.ShoulderRight:
                case BoneBodyPartType.Neck:
                    totalArmor += armor.BodyArmor * 0.25f; // weighted or partial contribution
                    totalArmor += armor.ArmArmor * 0.25f;
                    totalArmor += armor.HeadArmor * 0.25f;
                    break;

                default:
                    break;
            }
        }

        return totalArmor;
    }

    private static bool IsArmorWeaponPlate(MissionWeapon armorWeapon)
    {
        if (!IsValidArmorWeaponItem(armorWeapon))
        {
            return false;
        }

        if (armorWeapon.Item.ArmorComponent.MaterialType == ArmorComponent.ArmorMaterialTypes.Plate)
        {
            return true;
        }

        return false;
    }


    /*
    ~1.0	Direct/perpendicular	Penetrates
    ~0.5	Moderate angle	Depends on armor
    ~0.2 or less	Shallow/glancing	Likely ricochet
    */
    private static bool IsLowAngleImpact(Vec3 missileVelocity, Vec3 surfaceNormal, float threshold = 0.3f)
    {
        if (missileVelocity.LengthSquared < 1e-6f)
        {
            return false; // Avoid division by zero or meaningless result
        }

        Vec3 normalizedMissileDir = missileVelocity.NormalizedCopy();
        float dot = Vec3.DotProduct(normalizedMissileDir, surfaceNormal);
        return dot < threshold;
    }
}
#endif

/* original
    [UsedImplicitly]
    [MBCallback]
    internal bool MissileHitCallback(
      out int extraHitParticleIndex,
      ref AttackCollisionData collisionData,
      Vec3 missileStartingPosition,
      Vec3 missilePosition,
      Vec3 missileAngularVelocity,
      Vec3 movementVelocity,
      MatrixFrame attachGlobalFrame,
      MatrixFrame affectedShieldGlobalFrame,
      int numDamagedAgents,
      Agent attacker,
      Agent victim,
      GameEntity hitEntity)
    {
      Mission.Missile missile = this._missiles[collisionData.AffectorWeaponSlotOrMissileIndex];
      MissionWeapon attackerWeapon = missile.Weapon;
      WeaponFlags weaponFlags1 = attackerWeapon.CurrentUsageItem.WeaponFlags;
      float momentumRemaining = 1f;
      WeaponComponentData shieldOnBack = (WeaponComponentData) null;
      MissionGameModels.Current.AgentApplyDamageModel.DecideMissileWeaponFlags(attacker, missile.Weapon, ref weaponFlags1);
      extraHitParticleIndex = -1;
      bool flag1 = !GameNetwork.IsSessionActive;
      bool missileHasPhysics = collisionData.MissileHasPhysics;
      PhysicsMaterial fromIndex = PhysicsMaterial.GetFromIndex(collisionData.PhysicsMaterialIndex);
      int flags = fromIndex.IsValid ? (int) fromIndex.GetFlags() : 0;
      bool flag2 = (weaponFlags1 & WeaponFlags.AmmoSticksWhenShot) > (WeaponFlags) 0;
      bool flag3 = (flags & 1) == 0;
      bool flag4 = (flags & 8) != 0;
      MissionObject attachedMissionObject = (MissionObject) null;
      if (victim == null && (NativeObject) hitEntity != (NativeObject) null)
      {
        GameEntity gameEntity = hitEntity;
        do
        {
          attachedMissionObject = gameEntity.GetFirstScriptOfType<MissionObject>();
          gameEntity = gameEntity.Parent;
        }
        while (attachedMissionObject == null && (NativeObject) gameEntity != (NativeObject) null);
        hitEntity = attachedMissionObject?.GameEntity;
      }
      Mission.MissileCollisionReaction collisionReaction1 = !flag4 ? (!weaponFlags1.HasAnyFlag<WeaponFlags>(WeaponFlags.Burning) ? (!flag3 || !flag2 ? Mission.MissileCollisionReaction.BounceBack : Mission.MissileCollisionReaction.Stick) : Mission.MissileCollisionReaction.BecomeInvisible) : Mission.MissileCollisionReaction.PassThrough;
      bool isCanceled = false;
      bool flag5 = victim != null && victim.CurrentMortalityState == Agent.MortalityState.Invulnerable;
      Mission.MissileCollisionReaction collisionReaction2;
      CombatLogData combatLog1;
      if (((collisionData.MissileGoneUnderWater ? 1 : (collisionData.MissileGoneOutOfBorder ? 1 : 0)) | (flag5 ? 1 : 0)) != 0)
        collisionReaction2 = Mission.MissileCollisionReaction.BecomeInvisible;
      else if (victim == null)
      {
        if ((NativeObject) hitEntity != (NativeObject) null)
        {
          CombatLogData combatLog2;
          this.GetAttackCollisionResults(attacker, victim, hitEntity, momentumRemaining, in attackerWeapon, false, false, false, ref collisionData, out shieldOnBack, out combatLog2);
          Blow missileBlow = this.CreateMissileBlow(attacker, in collisionData, in attackerWeapon, missilePosition, missileStartingPosition);
          this.RegisterBlow(attacker, (Agent) null, hitEntity, missileBlow, ref collisionData, in attackerWeapon, ref combatLog2);
        }
        collisionReaction2 = collisionReaction1;
      }
      else if (collisionData.AttackBlockedWithShield)
      {
        this.GetAttackCollisionResults(attacker, victim, hitEntity, momentumRemaining, in attackerWeapon, false, false, false, ref collisionData, out shieldOnBack, out combatLog1);
        if (!collisionData.IsShieldBroken)
          this.MakeSound(ItemPhysicsSoundContainer.SoundCodePhysicsArrowlikeStone, collisionData.CollisionGlobalPosition, false, false, -1, -1);
        bool flag6 = false;
        if (weaponFlags1.HasAnyFlag<WeaponFlags>(WeaponFlags.CanPenetrateShield))
        {
          if (!collisionData.IsShieldBroken)
          {
            EquipmentIndex wieldedItemIndex = victim.GetWieldedItemIndex(Agent.HandIndex.OffHand);
            if ((double) collisionData.InflictedDamage > (double) ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.ShieldPenetrationOffset) + (double) ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.ShieldPenetrationFactor) * (double) victim.Equipment[wieldedItemIndex].GetGetModifiedArmorForCurrentUsage())
              flag6 = true;
          }
          else
            flag6 = true;
        }
        if (flag6)
        {
          victim.MakeVoice(SkinVoiceManager.VoiceType.Pain, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
          float num = momentumRemaining * (float) (0.40000000596046448 + (double) MBRandom.RandomFloat * 0.20000000298023224);
          collisionReaction2 = Mission.MissileCollisionReaction.PassThrough;
        }
        else
          collisionReaction2 = collisionData.IsShieldBroken ? Mission.MissileCollisionReaction.BecomeInvisible : collisionReaction1;
      }
      else if (collisionData.MissileBlockedWithWeapon)
      {
        this.GetAttackCollisionResults(attacker, victim, hitEntity, momentumRemaining, in attackerWeapon, false, false, false, ref collisionData, out shieldOnBack, out combatLog1);
        collisionReaction2 = Mission.MissileCollisionReaction.BounceBack;
      }
      else
      {
        if (attacker != null && attacker.IsFriendOf(victim))
        {
          if (this.ForceNoFriendlyFire)
            isCanceled = true;
          else if (!missileHasPhysics)
          {
            if (flag1)
            {
              if (attacker.Controller == Agent.ControllerType.AI)
                isCanceled = true;
            }
            else if (MultiplayerOptions.OptionType.FriendlyFireDamageRangedFriendPercent.GetIntValue() <= 0 && MultiplayerOptions.OptionType.FriendlyFireDamageRangedSelfPercent.GetIntValue() <= 0 || this.Mode == MissionMode.Duel)
              isCanceled = true;
          }
        }
        else if (victim.IsHuman && attacker != null && !attacker.IsEnemyOf(victim))
          isCanceled = true;
        else if (flag1 && attacker != null && attacker.Controller == Agent.ControllerType.AI && victim.RiderAgent != null && attacker.IsFriendOf(victim.RiderAgent))
          isCanceled = true;
        if (isCanceled)
        {
          if (flag1 && attacker != null && attacker == Agent.Main && attacker.IsFriendOf(victim))
            InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("ui_you_hit_a_friendly_troop").ToString(), Color.ConvertStringToColor("#D65252FF")));
          collisionReaction2 = Mission.MissileCollisionReaction.BecomeInvisible;
        }
        else
        {
          bool flag7 = (weaponFlags1 & WeaponFlags.MultiplePenetration) > (WeaponFlags) 0;
          CombatLogData combatLog3;
          this.GetAttackCollisionResults(attacker, victim, (GameEntity) null, momentumRemaining, in attackerWeapon, false, false, false, ref collisionData, out shieldOnBack, out combatLog3);
          Blow blow = this.CreateMissileBlow(attacker, in collisionData, in attackerWeapon, missilePosition, missileStartingPosition);
          if (collisionData.IsColliderAgent & flag7 && numDamagedAgents > 0)
          {
            blow.InflictedDamage /= numDamagedAgents;
            blow.SelfInflictedDamage /= numDamagedAgents;
            combatLog3.InflictedDamage = blow.InflictedDamage - combatLog3.ModifiedDamage;
          }
          if (collisionData.IsColliderAgent)
          {
            if (MissionGameModels.Current.AgentApplyDamageModel.DecideAgentShrugOffBlow(victim, collisionData, in blow))
              blow.BlowFlag |= BlowFlags.ShrugOff;
            else if (victim.IsHuman)
            {
              Agent mountAgent = victim.MountAgent;
              if (mountAgent != null)
              {
                if (mountAgent.RiderAgent == victim && MissionGameModels.Current.AgentApplyDamageModel.DecideAgentDismountedByBlow(attacker, victim, in collisionData, attackerWeapon.CurrentUsageItem, in blow))
                  blow.BlowFlag |= BlowFlags.CanDismount;
              }
              else
              {
                if (MissionGameModels.Current.AgentApplyDamageModel.DecideAgentKnockedBackByBlow(attacker, victim, in collisionData, attackerWeapon.CurrentUsageItem, in blow))
                  blow.BlowFlag |= BlowFlags.KnockBack;
                if (MissionGameModels.Current.AgentApplyDamageModel.DecideAgentKnockedDownByBlow(attacker, victim, in collisionData, attackerWeapon.CurrentUsageItem, in blow))
                  blow.BlowFlag |= BlowFlags.KnockDown;
              }
            }
          }
          if (victim.State == AgentState.Active)
            this.RegisterBlow(attacker, victim, (GameEntity) null, blow, ref collisionData, in attackerWeapon, ref combatLog3);
          extraHitParticleIndex = MissionGameModels.Current.DamageParticleModel.GetMissileAttackParticle(attacker, victim, in blow, in collisionData);
          if (flag7 && numDamagedAgents < 3)
          {
            collisionReaction2 = Mission.MissileCollisionReaction.PassThrough;
          }
          else
          {
            collisionReaction2 = collisionReaction1;
            if (collisionReaction1 == Mission.MissileCollisionReaction.Stick && !collisionData.CollidedWithShieldOnBack)
            {
              bool flag8 = this.CombatType == Mission.MissionCombatType.Combat;
              if (flag8)
              {
                bool flag9 = victim.IsHuman && collisionData.VictimHitBodyPart == BoneBodyPartType.Head;
                flag8 = victim.State != AgentState.Active || !flag9;
              }
              if (flag8)
              {
                float managedParameter = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.MissileMinimumDamageToStick);
                float num = 2f * managedParameter;
                if ((double) blow.InflictedDamage < (double) managedParameter && (double) blow.AbsorbedByArmor > (double) num && !GameNetwork.IsClientOrReplay)
                  collisionReaction2 = Mission.MissileCollisionReaction.BounceBack;
              }
              else
                collisionReaction2 = Mission.MissileCollisionReaction.BecomeInvisible;
            }
          }
        }
      }
      if (collisionData.CollidedWithShieldOnBack && shieldOnBack != null && victim != null && victim.IsMainAgent)
        InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("ui_hit_shield_on_back").ToString(), Color.ConvertStringToColor("#FFFFFFFF")));
      bool isAttachedFrameLocal;
      MatrixFrame attachLocalFrame;
      if (!collisionData.MissileHasPhysics && collisionReaction2 == Mission.MissileCollisionReaction.Stick)
      {
        attachLocalFrame = this.CalculateAttachedLocalFrame(in attachGlobalFrame, collisionData, missile.Weapon.CurrentUsageItem, victim, hitEntity, movementVelocity, missileAngularVelocity, affectedShieldGlobalFrame, true, out isAttachedFrameLocal);
      }
      else
      {
        attachLocalFrame = attachGlobalFrame;
        attachLocalFrame.origin.z = Math.Max(attachLocalFrame.origin.z, -100f);
        attachedMissionObject = (MissionObject) null;
        isAttachedFrameLocal = false;
      }
      Vec3 velocity = Vec3.Zero;
      Vec3 angularVelocity = Vec3.Zero;
      if (collisionReaction2 == Mission.MissileCollisionReaction.BounceBack)
      {
        WeaponFlags weaponFlags2 = weaponFlags1 & WeaponFlags.AmmoBreakOnBounceBackMask;
        if (weaponFlags2 == WeaponFlags.AmmoCanBreakOnBounceBack && (double) collisionData.MissileVelocity.Length > (double) ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BreakableProjectileMinimumBreakSpeed) || weaponFlags2 == WeaponFlags.AmmoBreaksOnBounceBack)
        {
          collisionReaction2 = Mission.MissileCollisionReaction.BecomeInvisible;
          extraHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_broken_arrow");
        }
        else
          missile.CalculateBounceBackVelocity(missileAngularVelocity, collisionData, out velocity, out angularVelocity);
      }
      this.HandleMissileCollisionReaction(collisionData.AffectorWeaponSlotOrMissileIndex, collisionReaction2, attachLocalFrame, isAttachedFrameLocal, attacker, victim, collisionData.AttackBlockedWithShield, collisionData.CollisionBoneIndex, attachedMissionObject, velocity, angularVelocity, -1);
      foreach (MissionBehavior missionBehavior in this.MissionBehaviors)
        missionBehavior.OnMissileHit(attacker, victim, isCanceled, collisionData);
      return collisionReaction2 != Mission.MissileCollisionReaction.PassThrough;
    }
*/
