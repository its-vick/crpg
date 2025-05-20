using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

using static TaleWorlds.MountAndBlade.Mission;

namespace Crpg.Module.HarmonyPatches;

#if CRPG_SERVER
[HarmonyPatchCategory("Late")]
[HarmonyPatch(typeof(Mission), "MissileHitCallback")]
public static class MissileHitCallbackPatch
{
    [HarmonyPrefix]
    public static bool Prefix_MissileHitCallback(ref int extraHitParticleIndex, ref AttackCollisionData collisionData, Vec3 missileStartingPosition, Vec3 missilePosition, Vec3 missileAngularVelocity, Vec3 movementVelocity, MatrixFrame attachGlobalFrame, MatrixFrame affectedShieldGlobalFrame, int numDamagedAgents, Agent attacker, Agent victim, GameEntity hitEntity)
    {
        if (victim == null)
        {
            return true;
        }

        // Logic for When missile should bounce?
        // if arrow/bolt/ and victim armor ?

        bool missileBounced = true;

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
            return true; // false cancels everyting
        }

        return true;
    }
}
#endif
