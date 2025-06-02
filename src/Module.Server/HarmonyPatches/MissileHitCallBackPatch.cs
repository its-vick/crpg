using System.Reflection;
using Crpg.Module.Common.Models;
using HarmonyLib;
using NetworkMessages.FromServer;
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
        // Validate victim
        if (victim == null || !victim.IsHuman || !victim.IsActive())
        {
            return;
        }

        // Get missile
        int missileIndex = collisionData.AffectorWeaponSlotOrMissileIndex;
        if (missileIndex < 0)
        {
            Debug.Print("missile index is invalid", 0, Debug.DebugColor.Purple);
            return;
        }

        Mission.Missile missile = Mission.Current.Missiles
            .FirstOrDefault(m => m.Index == missileIndex);

        // Validate missile
        if (missile == null || missile.Weapon.IsEmpty || missile.Weapon.IsEqualTo(MissionWeapon.Invalid) || missile.Weapon.Item == null)
        {
            Debug.Print("missile is null or invalid missionWeapon or item!", 0, Debug.DebugColor.Purple);
            return;
        }

        // Skip if blocked by shield
        if (collisionData.AttackBlockedWithShield)
        {
            Debug.Print("missile hit shield.", 0, Debug.DebugColor.Purple);
            return;
        }

        // Gather missile and armor data
        BoneBodyPartType bodyPartHit = collisionData.VictimHitBodyPart;
        EquipmentElement armorHit = GetArmorEquipmentElementFromBodyPart(victim, bodyPartHit);

        // Skip if no armor on bodypart
        if (armorHit.Item == null)
        {
            Debug.Print($"bodyPart:{bodyPartHit} armorHit.Item == null  (no armor on bodypart)!", 0, Debug.DebugColor.Purple);
            return;
        }

        // Gather more armor data
        ArmorComponent.ArmorMaterialTypes armorMaterial = GetArmorMaterial(armorHit);

        // Only a chance of bounce if plate or chainmail armor
        if (armorMaterial != ArmorComponent.ArmorMaterialTypes.Plate && armorMaterial != ArmorComponent.ArmorMaterialTypes.Chainmail)
        {
            Debug.Print($"armorMaterial:{armorMaterial} not capable of arrow bounce!", 0, Debug.DebugColor.Purple);
            return;
        }

        // Gather values
        float dot = GetImpactCosine(collisionData.MissileVelocity, collisionData.CollisionGlobalNormal);
        float armorEffectiveness = victim.GetBaseArmorEffectivenessForBodyPart(bodyPartHit);
        BoneBodyPartType bodyPart = bodyPartHit;
        ArmorComponent.ArmorMaterialTypes material = armorMaterial;
        DamageTypes damageType = (DamageTypes)collisionData.DamageType;
        ItemObject.ItemTypeEnum missileType = missile.Weapon.Item.Type;
        float damage = collisionData.InflictedDamage;
        float speed = collisionData.MissileVelocity.Length;

        Debug.Print($"BodyPart: {bodyPartHit} ArmorItem: {armorHit.Item.Name} Material: {armorMaterial} effective: {armorEffectiveness}", 0, Debug.DebugColor.Yellow);

        // Translate to abstract input
        BounceInputs inputs = MissileBounceInputTranslator.FromGame(
            dot,
            armorEffectiveness,
            material,
            damageType,
            missileType,
            damage,
            speed,
            bodyPart);

        float bounceChance = PureMissileBounceCalculator.ComputeBounceChance(inputs);

        // float randomFloat = MBRandom.RandomFloatRanged(0f, 1.0f);
        var random = new Random();
        float randomFloat = (float)random.NextDouble(); // returns value in [0.0f, 1.0f)

        bool missileBounced = randomFloat < bounceChance;

        Debug.Print($"////////////// Bounce Calculations //////////////", 0, Debug.DebugColor.White);
        Debug.Print($"bounceChance: {bounceChance:F2}, missileBounced: {missileBounced}", 0, Debug.DebugColor.Purple);
        Debug.Print($"////////////// +++++++++++++++++++ //////////////", 0, Debug.DebugColor.White);

        missileBounced = true; // make true for now debug testing

        if (missileBounced)
        {
            int physicsMaterialIndex = PhysicsMaterial.GetFromName("metal").Index;
            collisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
                            false, // collisionData.AttackBlockedWithShield,
                            false, // collisionData.CorrectSideShieldBlock,
                            collisionData.IsAlternativeAttack,
                            true, // isColliderAgent
                            collisionData.CollidedWithShieldOnBack,
                            collisionData.IsMissile,
                            collisionData.MissileBlockedWithWeapon,
                            false, // missileHasPhysics
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

            // Notify shooter and victim

            return;
        }

        return;
    }

    /*
        45-64 headarmor
        45-80 bodyarmor
        20-30 handarmor
        20-30 footarmor
    */

    /*  Decide to bounce
        missileSpeed // speed on impact
        glancingAngle // true if less than 0.3f or defined in IsLowAngleImpact()
        impactCosine // [~1.0 - Direct/perpendicular] [~0.5 - Moderate angle] [~0.2 or less - Shallow/glancing] (if you want to fine tune impact angle calculations)

        armorEffectiveness // agent.GetAgentDrivenPropertyValue(DrivenProperty.[bodypart]) armor amount for bodypart hit

        damageType // DamageTypes.
        missileItem.Type //  ItemObject.ItemTypeEnum.
        bodyPartHit // BodyPartType.
        armorMaterial // ArmorComponent.ArmorMaterialTypes.

        DamageTypes.Invalid; // -1
        DamageTypes.Cut; // 0 // most likely to bounce?
        DamageTypes.Pierce; // 1 most likely to penetrate?
        DamageTypes.Blunt; // 2 // least chance to bounce off?

        ItemObject.ItemTypeEnum.Arrows; // has cut pierce and blunt damage types
        ItemObject.ItemTypeEnum.Bolts; // has cut and pierce
        ItemObject.ItemTypeEnum.Bullets;
        ItemObject.ItemTypeEnum.Thrown; // has cut pierce and blunt damage types

        BoneBodyPartType.None // -1
        BoneBodyPartType.Head // 0
        BoneBodyPartType.Neck // 1 // doesnt have its own armor category
        BoneBodyPartType.Chest // 2
        BoneBodyPartType.Abdomen // 3
        BoneBodyPartType.ShoulderLeft // 4
        BoneBodyPartType.ShoulderRight // 5
        BoneBodyPartType.ArmLeft // 6
        BoneBodyPartType.ArmRight // 7
        BoneBodyPartType.Legs // 8
        BoneBodyPartType.CriticalBodyPartsBegin // 0
        BoneBodyPartType.CriticalBodyPartsEnd // 6

        ArmorComponent.ArmorMaterialTypes.None // 0
        ArmorComponent.ArmorMaterialTypes.Cloth // 1
        ArmorComponent.ArmorMaterialTypes.Leather // 2
        ArmorComponent.ArmorMaterialTypes.Chainmail // 3
        ArmorComponent.ArmorMaterialTypes.Plate // 4
    */

    // collisionData = SetAttackCollisionData(collisionData, false, false, false, true, physicsMaterialIndex, true, CombatCollisionResult.None);
    // public static AttackCollisionData SetAttackCollisionData(AttackCollisionData data, bool attackBlockedWithShield, bool collidedWithShieldOnBack, bool missileBlockedWithWeapon, bool missileHasPhysics, int physicsMaterialIndex, bool isColliderAgent, CombatCollisionResult collisionResult)

    private static bool IsValidArmorWeaponItem(EquipmentElement armorWeapon)
    {
        if (armorWeapon.IsEmpty || armorWeapon.IsEqualTo(EquipmentElement.Invalid) || armorWeapon.Item == null)
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

    private static ArmorComponent.ArmorMaterialTypes GetArmorMaterial(EquipmentElement armorWeapon)
    {
        ArmorComponent.ArmorMaterialTypes result = ArmorComponent.ArmorMaterialTypes.None;

        if (IsValidArmorWeaponItem(armorWeapon) && armorWeapon.Item.ArmorComponent != null)
        {
            result = armorWeapon.Item.ArmorComponent.MaterialType;
        }

        return result;
    }

    /*
    ~1.0 - Direct/perpendicular - Penetrates
    ~0.5 - Moderate angle - Depends on armor
    ~0.2 or less - Shallow/glancing - Likely ricochet
    */

    private static bool IsLowAngleImpact(Vec3 missileVelocity, Vec3 surfaceNormal, float threshold = 0.3f)
    {
        if (missileVelocity.LengthSquared < 1e-6f)
        {
            return false; // Avoid division by zero or meaningless result
        }

        Vec3 normalizedMissileDir = missileVelocity.NormalizedCopy();
        Vec3 normalizedSurfaceNormal = surfaceNormal.NormalizedCopy(); // Ensure normal is normalized
        float dot = Vec3.DotProduct(normalizedMissileDir, normalizedSurfaceNormal);
        bool result = Math.Abs(dot) < threshold; // Use absolute value for angle check
        Debug.Print($"IsLowAngleImpact: {result}, Dot: {Math.Abs(dot):F3}", 0, Debug.DebugColor.Green);
        return result;
    }

    private static float GetImpactCosine(Vec3 missileVelocity, Vec3 surfaceNormal)
    {
        if (missileVelocity.LengthSquared < 1e-6f)
        {
            return 1f; // Assume direct hit (cos(0°) = 1) for invalid velocity
        }

        Vec3 normalizedMissileDir = missileVelocity.NormalizedCopy();
        Vec3 normalizedSurfaceNormal = surfaceNormal.NormalizedCopy();
        float dot = Vec3.DotProduct(normalizedMissileDir, normalizedSurfaceNormal);
        return Math.Abs(dot); // Return absolute dot product for angle magnitude
    }

    private static EquipmentElement GetArmorEquipmentElementFromBodyPart(Agent agent, BoneBodyPartType bodyPart)
    {
        if (agent == null || !agent.IsActive() || agent.SpawnEquipment == null)
        {
            return EquipmentElement.Invalid;
        }

        EquipmentIndex? index = bodyPart switch
        {
            BoneBodyPartType.Head or BoneBodyPartType.Neck => EquipmentIndex.Head,
            BoneBodyPartType.Chest or BoneBodyPartType.Abdomen => EquipmentIndex.Body,
            BoneBodyPartType.ArmLeft or BoneBodyPartType.ArmRight => EquipmentIndex.Gloves,
            BoneBodyPartType.Legs => EquipmentIndex.Leg,
            BoneBodyPartType.ShoulderLeft or BoneBodyPartType.ShoulderRight => EquipmentIndex.Cape,
            _ => null,
        };

        if (index.HasValue)
        {
            EquipmentElement element = agent.SpawnEquipment[index.Value];
            if (!element.IsEmpty && !element.IsEqualTo(EquipmentElement.Invalid))
            {
                return element;
            }
        }

        return EquipmentElement.Invalid;
    }

    /*
        private static EquipmentElement GetArmorEquipmentElementFromBodyPart(Agent agent, BoneBodyPartType bodyPart)
        {
            EquipmentElement returnElement = EquipmentElement.Invalid;
            if (agent == null || !agent.IsActive() || agent.SpawnEquipment == null)
            {
                return returnElement;
            }

            Equipment spawnEquipment = agent.SpawnEquipment;
            EquipmentElement aE = EquipmentElement.Invalid;

            switch (bodyPart)
            {
                case BoneBodyPartType.Head:
                    aE = spawnEquipment[EquipmentIndex.Head];
                    if (!aE.IsEmpty && !aE.IsEqualTo(EquipmentElement.Invalid))
                    {
                        returnElement = aE;
                    }

                    break;

                case BoneBodyPartType.Chest:
                case BoneBodyPartType.Abdomen:
                    aE = spawnEquipment[EquipmentIndex.Body];
                    if (!aE.IsEmpty && !aE.IsEqualTo(EquipmentElement.Invalid))
                    {
                        returnElement = aE;
                    }

                    break;

                case BoneBodyPartType.ArmLeft:
                case BoneBodyPartType.ArmRight:
                    aE = spawnEquipment[EquipmentIndex.Gloves];
                    if (!aE.IsEmpty && !aE.IsEqualTo(EquipmentElement.Invalid))
                    {
                        returnElement = aE;
                    }

                    break;

                case BoneBodyPartType.Legs:
                    aE = spawnEquipment[EquipmentIndex.Leg];
                    if (!aE.IsEmpty && !aE.IsEqualTo(EquipmentElement.Invalid))
                    {
                        returnElement = aE;
                    }

                    break;

                case BoneBodyPartType.ShoulderLeft:
                case BoneBodyPartType.ShoulderRight:
                    aE = spawnEquipment[EquipmentIndex.Cape];
                    if (!aE.IsEmpty && !aE.IsEqualTo(EquipmentElement.Invalid))
                    {
                        returnElement = aE;
                    }

                    break;

                case BoneBodyPartType.Neck: // just do head for now
                    aE = spawnEquipment[EquipmentIndex.Head];
                    if (!aE.IsEmpty && !aE.IsEqualTo(EquipmentElement.Invalid))
                    {
                        returnElement = aE;
                    }

                    break;
            }

            return returnElement;
        }
    */
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
