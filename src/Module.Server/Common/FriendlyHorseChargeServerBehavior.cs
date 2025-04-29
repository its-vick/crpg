using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common;
public class FriendlyHorseChargeServerBehavior : MissionNetwork
{
    private const float MinHorseSpeedSq = 4f;
    private const float ChargeRangeSq = 2.25f;
    private const float ChargeCooldown = 1.0f;

    // Define reasonable constants for limiting distances and velocities
    private const float MaxDistanceSquared = 10f; // Example: maximum squared distance for charge range
    private const float MaxVelocitySquared = 1600;  // Example: maximum squared velocity for the horse

    private readonly Dictionary<Agent, float> _lastHitTimes = new();

    public override void OnMissionStateActivated()
    {
        // CrpgConstants constants = new CrpgConstants(); // Create a new constants object
        // DebugSpawnBehavior debugSpawnBehavior = new DebugSpawnBehavior(constants);

        base.OnMissionStateActivated();
    }

    public override void OnMissionTick(float dt)
    {
        if (!GameNetwork.IsServer || Mission.Current == null)
        {
            return;
        }

        float now = Mission.Current.CurrentTime;

        // Iterate through all agents
        foreach (Agent horse in Mission.Current.Agents)
        {
            // Skip agents that are not active mounts or lack riders
            if (!horse.IsMount || !horse.IsActive() || horse.RiderAgent == null || horse.Velocity.LengthSquared < MinHorseSpeedSq)
            {
                continue;
            }

            Agent rider = horse.RiderAgent;

            // Check all agents for possible charge damage
            foreach (Agent victim in Mission.Current.Agents)
            {
                if (victim.IsHuman && victim.IsActive() && victim.Team != null && victim != rider && victim.MountAgent == null)
                {
                    // Ensure the rider and victim are on the same team and avoid repeated hits
                    if (rider.Team == victim.Team)
                    {
                        HandleChargeDamage(rider, horse, victim, now);
                    }
                }
            }
        }
    }

    private void HandleChargeDamage(Agent rider, Agent horse, Agent victim, float now)
    {
        // Check cooldown for last hit time on victim
        if (_lastHitTimes.TryGetValue(victim, out float lastHitTime) && now - lastHitTime < ChargeCooldown)
        {
            return;
        }

        // Clamp distance squared to avoid overflow
        float distanceSq = victim.Position.DistanceSquared(horse.Position);
        distanceSq = Math.Min(distanceSq, MaxDistanceSquared); // Limit the max distance

        // Skip if distance is beyond the effective range
        if (distanceSq > ChargeRangeSq)
        {
            return;
        }

        // Clamp velocity squared to avoid overflow
        float velocitySquared = horse.Velocity.LengthSquared;
        velocitySquared = Math.Min(velocitySquared, MaxVelocitySquared); // Limit max velocity

        // Skip if velocity is below the threshold for charge damage
        if (velocitySquared < MinHorseSpeedSq)
        {
            return;
        }

        _lastHitTimes[victim] = now;

        // Apply charge damage
        ApplyChargeDamage(horse, rider, victim, velocitySquared);
    }

    private void ApplyChargeDamage(Agent horse, Agent rider, Agent victim, float velocitySquared)
    {
        // Check if the victim is invulnerable
        if (victim.CurrentMortalityState == Agent.MortalityState.Invulnerable)
        {
            return;
        }

        // Check for null references
        if (horse == null || rider == null || victim == null)
        {
            Debug.PrintError("One or more agents are null: horse=" + horse + ", rider=" + rider + ", victim=" + victim);
            return;
        }

        // Check for valid agent indexes for network packets
        if (rider.Index < -1 || horse.Index < -1 || victim.Index < -1)
        {
            Debug.PrintError($"Invalid agent index for network message. rider={rider.Index} victim={victim.Index} horse={horse.Index}");
            return;
        }

        // Log key agent data for diagnostics
        Debug.Print($"Applying charge damage: horseSpeed={horse.Velocity.Length}, rider={rider.Index}, victim={victim.Index}, victimPosition={victim.Position}");

        // Validate agent velocity and position
        if (float.IsNaN(horse.Velocity.Length) || float.IsNaN(victim.Position.X) || float.IsNaN(victim.Position.Y) || float.IsNaN(victim.Position.Z))
        {
            Debug.PrintError("Invalid velocity or position detected. Horse velocity or victim position contains NaN.");
            return;
        }

        // Calculate charge speed factor for damage scaling
        float horseSpeed = horse.Velocity.Length;
        float speedFactor = Math.Min(horseSpeed / 8f, 1.5f); // Clamp the scaling factor
        float baseDamage = 8f;
        float scaledDamage = baseDamage * (0.5f + speedFactor);

        // Cap damage to prevent excessively high values
        float maxDamage = 100f; // Set a reasonable max damage cap
        scaledDamage = Math.Min(scaledDamage, maxDamage);

        // Log calculated damage
        Debug.Print($"Calculated damage: baseDamage={baseDamage}, speedFactor={speedFactor}, scaledDamage={scaledDamage}");

        // Create blow object for applying damage
        Blow blow = new Blow(rider.Index)
        {
            VictimBodyPart = BoneBodyPartType.Chest,
            DamageType = DamageTypes.Blunt,
            BaseMagnitude = scaledDamage,
            InflictedDamage = (int)scaledDamage,
            SelfInflictedDamage = 0,
            MovementSpeedDamageModifier = 1f,
            AbsorbedByArmor = 0f,
            DamageCalculated = true,
            BlowFlag = BlowFlags.None,
        };

        // Apply knockdown or knockback based on speed
        blow.BlowFlag |= horseSpeed > 4f ? BlowFlags.KnockDown : BlowFlags.KnockBack;

        // Normalize the horse's velocity for impact direction
        Vec3 weaponBlowDir = horse.Velocity;
        float length = weaponBlowDir.Length;
        if (length > 0f)
        {
            weaponBlowDir /= length; // Normalize the vector
        }

        // Log normalized direction
        Debug.Print($"Normalized weapon blow direction: {weaponBlowDir}");

        // Construct AttackCollisionData
        // CombatCollisionResult collisionResult = new CombatCollisionResult();

        AttackCollisionData collisionData = BuildSafeCollisionData(rider, victim, weaponBlowDir, horse.Velocity.Length);
    }

    private AttackCollisionData BuildSafeCollisionData(
        Agent rider,
        Agent victim,
        Vec3 weaponBlowDir,
        float horseSpeed,
        float attackProgress = 1f,
        float collisionDistanceOnWeapon = 0.5f)
    {
        // We make local, editable copies of the Vec3s
        Vec3 victimPosition = victim.Position;
        Vec3 victimVelocity = victim.Velocity;
        Vec3 missileVelocity = Vec3.Zero;
        Vec3 missileStartingPosition = Vec3.Zero;
        Vec3 groundNormal = Vec3.Zero;

        // Clamp values
        ClampAndLogCollisionDataFields(
            ref horseSpeed,
            ref attackProgress,
            ref collisionDistanceOnWeapon,
            ref weaponBlowDir,
            ref victimPosition,
            ref victimVelocity,
            ref missileVelocity,
            ref missileStartingPosition,
            ref groundNormal);

        CombatCollisionResult collisionResult = new CombatCollisionResult();

        return AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
            _attackBlockedWithShield: false,
            _correctSideShieldBlock: false,
            _isAlternativeAttack: false,
            _isColliderAgent: true,
            _collidedWithShieldOnBack: false,
            _isMissile: false,
            _isMissileBlockedWithWeapon: false,
            _missileHasPhysics: false,
            _entityExists: true,
            _thrustTipHit: false,
            _missileGoneUnderWater: false,
            _missileGoneOutOfBorder: false,
            collisionResult: collisionResult,
            affectorWeaponSlotOrMissileIndex: rider.Index,  // this could be a problem
            StrikeType: 0,
            DamageType: (int)DamageTypes.Blunt,
            CollisionBoneIndex: (sbyte)BoneBodyPartType.Chest,
            VictimHitBodyPart: BoneBodyPartType.Chest,
            AttackBoneIndex: (sbyte)BoneBodyPartType.Chest,
            AttackDirection: Agent.UsageDirection.AttackAny,
            PhysicsMaterialIndex: 0,
            CollisionHitResultFlags: CombatHitResultFlags.NormalHit,
            AttackProgress: attackProgress,
            CollisionDistanceOnWeapon: collisionDistanceOnWeapon,
            AttackerStunPeriod: 0f,
            DefenderStunPeriod: 0f,
            MissileTotalDamage: 0f,
            MissileInitialSpeed: 0f,
            ChargeVelocity: horseSpeed,
            FallSpeed: 0f,
            WeaponRotUp: Vec3.Zero,
            _weaponBlowDir: weaponBlowDir,
            CollisionGlobalPosition: victim.Position,
            MissileVelocity: Vec3.Zero,
            MissileStartingPosition: Vec3.Zero,
            VictimAgentCurVelocity: victim.Velocity,
            GroundNormal: Vec3.Zero);
    }

    private void ClampAndLogCollisionDataFields(
        ref float horseSpeed,
        ref float attackProgress,
        ref float collisionDistanceOnWeapon,
        ref Vec3 weaponBlowDir,
        ref Vec3 victimPosition,
        ref Vec3 victimVelocity,
        ref Vec3 missileVelocity,
        ref Vec3 missileStartingPosition,
        ref Vec3 groundNormal)
    {
        // Clamp float values using Math.Min/Max (works on doubles, cast back to float)
        horseSpeed = (float)Math.Min(Math.Max(horseSpeed, 0f), 100f);
        attackProgress = (float)Math.Min(Math.Max(attackProgress, 0f), 1f);
        collisionDistanceOnWeapon = (float)Math.Min(Math.Max(collisionDistanceOnWeapon, 0f), 1.5f);

        // Sanitize Vec3 vectors
        weaponBlowDir = FixVec3(weaponBlowDir);
        victimPosition = FixVec3(victimPosition);
        victimVelocity = FixVec3(victimVelocity);
        missileVelocity = FixVec3(missileVelocity);
        missileStartingPosition = FixVec3(missileStartingPosition);
        groundNormal = FixVec3(groundNormal);
    }

    private Vec3 FixVec3(Vec3 v)
    {
        if (float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z))
        {
            return Vec3.Zero;
        }

        return v;
    }
}
