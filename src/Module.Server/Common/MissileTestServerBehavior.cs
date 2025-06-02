using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Crpg.Module.Common;

internal class MissileTestServerBehavior : MissionBehavior
{
    private const float Interval = 5f;
    private const float AfterStartInterval = 8f;
    private bool _isMissionStarted = false;
    private float _timer = 0f;
    private float _timer2 = 0f;

    private MissionWeapon arrowWeapon;
    private ItemObject? arrowItem;

    public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

    public MissileTestServerBehavior()
    {
        arrowWeapon = MissionWeapon.Invalid;
        arrowItem = null;
    }

    public override void OnMissionStateActivated()
    {
        Debug.Print("OnMissionStateActivated", 0, Debug.DebugColor.DarkBlue);
        // CreateMissileAmmo();
        _isMissionStarted = true;
    }

    public override void OnMissionTick(float dt)
    {
        // Debug.Print("OnMissionTick", 0, Debug.DebugColor.DarkBlue);
        _timer += dt;
        if (_timer >= Interval)
        {
            _timer = 0f;
            // TryFireMissile();
            // MoveAllTeammatesToEnemyTeam();
        }

        _timer2 += dt;
        if (_timer2 >= AfterStartInterval)
        {
            _timer2 = 0f;
            EquipAllEnemiesWithBowAndArrow();
        }
    }

    private void TryFireMissile()
    {
        Debug.Print("TryFireMissile", 0, Debug.DebugColor.DarkBlue);
        if (!_isMissionStarted || Mission.Current == null)
        {
            Debug.Print("Mission not started or Mission.Current is null", 0, Debug.DebugColor.Red);
            return;
        }

        Agent player = Mission.Current.Agents.FirstOrDefault(a => a.IsPlayerControlled);
        if (player == null || player.Team == null || !player.IsActive())
        {
            Debug.Print("No valid player found", 0, Debug.DebugColor.Red);
            return;
        }

        Team enemyTeam = Mission.Teams.FirstOrDefault(t => t.IsEnemyOf(player.Team));
        if (enemyTeam == null)
        {
            Debug.Print("No enemy team found", 0, Debug.DebugColor.Red);
            return;
        }

        Agent shooter = enemyTeam.ActiveAgents
            .Where(a => !a.IsPlayerControlled && a.IsHuman && a.IsActive())
            .OrderBy(_ => MBRandom.RandomFloat)
            .FirstOrDefault();

        if (shooter == null || shooter.Mission == null || shooter.Team == null || !shooter.IsActive())
        {
            Debug.Print("No valid shooter found", 0, Debug.DebugColor.Red);
            return;
        }

        // Ensure arrowItem is valid
        if (arrowItem == null || arrowWeapon.IsEmpty || arrowWeapon.IsEqualTo(MissionWeapon.Invalid))
        {
            Debug.Print("arrowItem or arrowWeapon is invalid, reinitializing", 0, Debug.DebugColor.Red);
            CreateMissileAmmo();
            if (arrowItem == null || arrowWeapon.IsEmpty || arrowWeapon.IsEqualTo(MissionWeapon.Invalid))
            {
                Debug.Print("Failed to reinitialize arrowWeapon", 0, Debug.DebugColor.Red);
                return;
            }
        }

        // Verify arrowItem's WeaponComponent
        if (arrowItem.WeaponComponent == null ||
            arrowItem.WeaponComponent.PrimaryWeapon == null ||
            arrowItem.WeaponComponent.Weapons == null ||
            arrowItem.WeaponComponent.Weapons.Count == 0 ||
            arrowItem.WeaponComponent.PrimaryWeapon.WeaponClass == WeaponClass.Undefined ||
            string.IsNullOrEmpty(arrowItem.WeaponComponent.PrimaryWeapon.ItemUsage))
        {
            Debug.Print("arrowItem has invalid WeaponComponent, PrimaryWeapon, Weapons, WeaponClass, or ItemUsage", 0, Debug.DebugColor.Red);
            return;
        }

        // Create a fresh MissionWeapon for the missile
        MissionWeapon missileWeapon = new MissionWeapon(arrowItem, null, null, (short)1);

        if (missileWeapon.IsEmpty || missileWeapon.Item == null || missileWeapon.WeaponsCount <= 0)
        {
            Debug.Print("missileWeapon is empty, has null Item, or invalid WeaponsCount", 0, Debug.DebugColor.Red);
            return;
        }

        // pick random bow
        ItemObject randomBowItem = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
            .Where(item => item.Type == ItemObject.ItemTypeEnum.Bow)
            .OrderBy(_ => MBRandom.RandomFloat)
            .FirstOrDefault();

        if (randomBowItem == null)
        {
            Debug.Print("randomBowItem is null", 0, Debug.DebugColor.Red);
            return;
        }

        // create bow MissionWeapon
        MissionWeapon bowWeapon = new MissionWeapon(randomBowItem, null, null, (short)1);

        if (bowWeapon.IsEmpty || bowWeapon.Item == null)
        {
            Debug.Print("bowWeapon is empty, has null Item", 0, Debug.DebugColor.Red);
            return;
        }

        // give the shooter weapons
        shooter.EquipWeaponWithNewEntity(EquipmentIndex.Weapon0, ref bowWeapon);
        shooter.EquipWeaponWithNewEntity(EquipmentIndex.Weapon1, ref missileWeapon);

        shooter.TryToWieldWeaponInSlot(EquipmentIndex.Weapon0, Agent.WeaponWieldActionType.Instant, false);

        // Additional validation for WeaponComponent
        if (missileWeapon.Item.WeaponComponent == null ||
            missileWeapon.Item.WeaponComponent.PrimaryWeapon == null ||
            missileWeapon.Item.WeaponComponent.PrimaryWeapon.WeaponClass == WeaponClass.Undefined ||
            string.IsNullOrEmpty(missileWeapon.Item.WeaponComponent.PrimaryWeapon.ItemUsage))
        {
            Debug.Print("missileWeapon has invalid WeaponComponent, PrimaryWeapon, WeaponClass, or ItemUsage", 0, Debug.DebugColor.Red);
            return;
        }

        // Validate WeaponStatsData
        try
        {
            WeaponStatsData weaponStatsData = missileWeapon.GetWeaponStatsDataForUsage(0);
            if (weaponStatsData.MissileSpeed <= 0)
            {
                Debug.Print("WeaponStatsData has invalid MissileSpeed", 0, Debug.DebugColor.Red);
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.Print($"GetWeaponStatsDataForUsage ERROR: {ex.Message}\n{ex.StackTrace}", 0, Debug.DebugColor.Red);
            return;
        }

        // Log PrimaryWeapon details
        Debug.Print($"Arrow: {missileWeapon.Item.Name}, WeaponsCount: {missileWeapon.WeaponsCount}, MissileSpeed: {missileWeapon.Item.WeaponComponent.PrimaryWeapon.MissileSpeed}, WeaponClass: {missileWeapon.Item.WeaponComponent.PrimaryWeapon.WeaponClass}, ItemUsage: {missileWeapon.Item.WeaponComponent.PrimaryWeapon.ItemUsage}", 0, Debug.DebugColor.Green);

        if (missileWeapon.CurrentUsageItem == null)
        {
            Debug.Print("missileWeapon.CurrentUsageItem is null", 0, Debug.DebugColor.Red);
            return;
        }

        // Find a target
        Agent target = player;
        if (target == null || !target.IsActive())
        {
            Debug.Print("No valid target found", 0, Debug.DebugColor.Red);
            return;
        }

        // Calculate position and direction
        Vec3 origin = shooter.GetEyeGlobalPosition();
        Vec3 direction = (target.GetEyeGlobalPosition() - origin).NormalizedCopy();

        // Check if direction is zero
        if (direction.x == 0f && direction.y == 0f && direction.z == 0f)
        {
            Debug.Print("Direction vector is zero, adjusting origin", 0, Debug.DebugColor.Yellow);
            origin.z += 0.1f;
            direction = (target.GetEyeGlobalPosition() - origin).NormalizedCopy();
        }

        // Set orientation
        Mat3 orientation = Mat3.Identity;
        orientation.f = direction;
        orientation.Orthonormalize();

        if (!orientation.f.IsNonZero || !orientation.f.IsValid)
        {
            Debug.Print("Orientation vector f is not normalized", 0, Debug.DebugColor.Red);
            return;
        }

        // Set speeds
        float baseSpeed = missileWeapon.Item.WeaponComponent.PrimaryWeapon.MissileSpeed;
        float actualSpeed = baseSpeed;

        if (baseSpeed <= 0)
        {
            Debug.Print("Invalid missile speed, using fallback", 0, Debug.DebugColor.Yellow);
            baseSpeed = actualSpeed = 50f; // Typical arrow speed
        }

        if (shooter.Equipment[EquipmentIndex.Weapon0].IsEmpty || shooter.Equipment[EquipmentIndex.Weapon1].IsEmpty)
        {
            Debug.Print("Shooter equipment is invalid: Weapon0 or Weapon1 is empty", 0, Debug.DebugColor.Red);
            return;
        }

        Debug.Print($"Firing missile: Shooter={shooter.Name}, Arrow={missileWeapon.Item.Name}, Origin={origin}, Direction={direction}, Speed={baseSpeed}", 0, Debug.DebugColor.Green);

        try
        {
            WeaponData weaponData = missileWeapon.GetWeaponData(true);
            Debug.Print("GetWeaponData succeeded", 0, Debug.DebugColor.Green);

            WeaponStatsData weaponStatsData = missileWeapon.GetWeaponStatsDataForUsage(0);
            if (weaponStatsData.WeaponClass == (int)WeaponClass.Undefined || weaponStatsData.MissileSpeed <= 0)
            {
                Debug.Print("WeaponStatsData is invalid", 0, Debug.DebugColor.Red);
                weaponData.DeinitializeManagedPointers();
                return;
            }

            Debug.Print($"WeaponStatsData: WeaponClass={weaponStatsData.WeaponClass}, MissileSpeed={weaponStatsData.MissileSpeed}", 0, Debug.DebugColor.Green);



            Mission.AddCustomMissile(
                shooter,
                missileWeapon,
                origin,
                direction,
                orientation,
                baseSpeed,
                actualSpeed,
                false,
                null,
                -1
            );
            Debug.Print("Missile fired successfully", 0, Debug.DebugColor.Green);
            weaponData.DeinitializeManagedPointers();
        }
        catch (Exception ex)
        {
            Debug.Print($"[AddCustomMissile ERROR] Exception: {ex.Message}\n{ex.StackTrace}", 0, Debug.DebugColor.Red);
        }
    }

    private void CreateMissileAmmo()
    {
        // Try vanilla arrow first
        arrowItem = MBObjectManager.Instance.GetObject<ItemObject>("default_arrow");

        if (arrowItem == null || !arrowItem.HasWeaponComponent ||
            arrowItem.WeaponComponent?.PrimaryWeapon == null ||
            arrowItem.WeaponComponent.PrimaryWeapon.MissileSpeed <= 0 ||
            arrowItem.WeaponComponent.PrimaryWeapon.WeaponClass == WeaponClass.Undefined ||
            string.IsNullOrEmpty(arrowItem.WeaponComponent.PrimaryWeapon.ItemUsage))
        {
            Debug.Print("Default arrow is invalid or unavailable, trying random arrow", 0, Debug.DebugColor.Yellow);
            arrowItem = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                .Where(item =>
                    item.Type == ItemObject.ItemTypeEnum.Arrows &&
                    item.HasWeaponComponent &&
                    item.WeaponComponent?.Weapons != null &&
                    item.WeaponComponent.Weapons.Count > 0 &&
                    item.WeaponComponent.PrimaryWeapon != null &&
                    item.WeaponComponent.PrimaryWeapon.MissileSpeed > 0 &&
                    item.WeaponComponent.PrimaryWeapon.WeaponClass != WeaponClass.Undefined &&
                    !string.IsNullOrEmpty(item.WeaponComponent.PrimaryWeapon.ItemUsage))
                .OrderBy(_ => MBRandom.RandomFloat)
                .FirstOrDefault();
        }

        if (arrowItem == null)
        {
            Debug.Print("Failed to find a valid arrowItem", 0, Debug.DebugColor.Red);
            return;
        }

        arrowWeapon = new MissionWeapon(arrowItem, null, null, (short)1);

        if (arrowWeapon.IsEmpty || arrowWeapon.CurrentUsageItem == null || arrowWeapon.WeaponsCount <= 0)
        {
            Debug.Print("arrowWeapon is empty, has null CurrentUsageItem, or invalid WeaponsCount", 0, Debug.DebugColor.Red);
            arrowItem = null;
        }
        else
        {
            Debug.Print($"Created arrowWeapon: {arrowWeapon.Item.Name}, usage: {arrowWeapon.CurrentUsageItem}, WeaponsCount: {arrowWeapon.WeaponsCount}, MissileSpeed: {arrowWeapon.Item.WeaponComponent?.PrimaryWeapon?.MissileSpeed}, WeaponClass: {arrowWeapon.Item.WeaponComponent?.PrimaryWeapon?.WeaponClass}, ItemUsage: {arrowWeapon.Item.WeaponComponent?.PrimaryWeapon?.ItemUsage}", 0, Debug.DebugColor.Green);
        }
    }

    private void MoveAllTeammatesToEnemyTeam()
    {
        if (Mission.Current == null)
        {
            Debug.Print("Mission is null", 0, Debug.DebugColor.Red);
            return;
        }

        Agent player = Mission.Current.Agents.FirstOrDefault(a => a.IsPlayerControlled);
        if (player == null || player.Team == null)
        {
            Debug.Print("No valid player or team found", 0, Debug.DebugColor.Red);
            return;
        }

        Team playerTeam = player.Team;
        Team enemyTeam = Mission.Teams.FirstOrDefault(t => t.IsEnemyOf(playerTeam));

        if (enemyTeam == null)
        {
            Debug.Print("No enemy team found", 0, Debug.DebugColor.Red);
            return;
        }

        // Count current active enemy agents
        int currentEnemyCount = enemyTeam.ActiveAgents.Count(a => a.IsHuman);

        try
        {
            foreach (Agent agent in Mission.Current.Agents)
            {
                if (agent != null && agent.Team == playerTeam && agent != player && agent.IsHuman)
                {
                    if (currentEnemyCount >= 10)
                    {
                        //DamageHelper.DamageAgent(agent, 50);
                        Debug.Print($"Killed {agent.Name} instead of moving (enemy team full)", 0, Debug.DebugColor.Purple);
                    }
                    else
                    {
                        agent.SetTeam(enemyTeam, true);
                        currentEnemyCount++;
                        Debug.Print($"Moved {agent.Name} to enemy team", 0, Debug.DebugColor.Yellow);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Print($"Exception in Equip/Move logic: {ex}", 0, Debug.DebugColor.Red);
        }
    }

    private void EquipAllEnemiesWithBowAndArrow()
    {
        if (Mission.Current == null)
        {
            Debug.Print("Mission is null", 0, Debug.DebugColor.Red);
            return;
        }

        Agent player = Mission.Current.Agents.FirstOrDefault(a => a.IsPlayerControlled);
        if (player == null || player.Team == null)
        {
            Debug.Print("No valid player or team found", 0, Debug.DebugColor.Red);
            return;
        }

        Team enemyTeam = Mission.Teams.FirstOrDefault(t => t.IsEnemyOf(player.Team));
        if (enemyTeam == null)
        {
            Debug.Print("No enemy team found", 0, Debug.DebugColor.Red);
            return;
        }

        try
        {
            foreach (Agent agent in enemyTeam.ActiveAgents.Where(a => a.IsHuman && !a.IsPlayerControlled && a.IsActive() && !AgentHasBow(a)))
            {
                if (agent.Team == null)
                {
                    Debug.Print($"Agent {agent.Name} has no team in equipment change", 0, Debug.DebugColor.Red);
                    continue;
                }

                if (agent.Origin == null)
                {
                    Debug.Print($"Agent {agent.Name} has no Origin in equipment change", 0, Debug.DebugColor.Red);
                    continue;
                }

                if (agent.Equipment == null)
                {
                    Debug.Print($"Agent {agent.Name} has null equipment", 0, Debug.DebugColor.Red);
                    continue;
                }

                for (int i = 0; i < 4; i++)
                {
                    if (agent.Equipment == null)
                    {
                        continue;
                    }

                    if (!agent.Equipment[i].IsEmpty || !agent.Equipment[i].IsEqualTo(MissionWeapon.Invalid))
                    {
                        agent.RemoveEquippedWeapon((EquipmentIndex)i);
                    }
                }

                var bowAndArrows = GetRandomBowAndArrows();
                if (bowAndArrows != null)
                {
                    MissionWeapon bowWeapon = bowAndArrows.Value.bow;
                    MissionWeapon arrowWeapon = bowAndArrows.Value.arrows;

                    // Use bow and arrows...
                    if (!bowWeapon.IsEmpty && !arrowWeapon.IsEmpty)
                    {
                        agent.EquipWeaponWithNewEntity(EquipmentIndex.Weapon0, ref bowWeapon);
                        agent.EquipWeaponWithNewEntity(EquipmentIndex.Weapon1, ref arrowWeapon);
                        agent.EquipWeaponWithNewEntity(EquipmentIndex.Weapon2, ref arrowWeapon);
                        agent.EquipWeaponWithNewEntity(EquipmentIndex.Weapon3, ref arrowWeapon);

                        agent.TryToWieldWeaponInSlot(EquipmentIndex.Weapon0, Agent.WeaponWieldActionType.Instant, false);
                        Debug.Print($"Equipped {agent.Name} with {bowWeapon.Item.Name} and {arrowWeapon.Item.Name}", 0, Debug.DebugColor.Green);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Print($"Exception in Equip team with bow/arrow logic: {ex}", 0, Debug.DebugColor.Red);
        }
    }

    private bool AgentHasBow(Agent agent)
    {
        try
        {
            if (agent == null || !agent.IsActive())
            {
                return false;
            }

            for (int i = 0; i < 4; i++)
            {
                MissionWeapon element = agent.Equipment[i];
                if (element.Item != null && element.Item.Type == ItemObject.ItemTypeEnum.Bow)
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Print($"Exception in Equip/Move logic: {ex}", 0, Debug.DebugColor.Red);
        }

        return false;
    }

    private (MissionWeapon bow, MissionWeapon arrows)? GetRandomBowAndArrows()
    {
        // Get a random bow
        ItemObject randomBowItem = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
            .Where(item => item.Type == ItemObject.ItemTypeEnum.Bow)
            .OrderBy(_ => MBRandom.RandomFloat)
            .FirstOrDefault();

        // Get a random or default arrow
        ItemObject arrowItem = MBObjectManager.Instance.GetObject<ItemObject>("default_arrow");
        if (arrowItem == null || !arrowItem.HasWeaponComponent)
        {
            arrowItem = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                .Where(item =>
                    item.Type == ItemObject.ItemTypeEnum.Arrows &&
                    item.HasWeaponComponent &&
                    item.WeaponComponent.PrimaryWeapon.MissileSpeed > 0 &&
                    !item.Name.Contains("Ballista Arrows"))
                .OrderBy(_ => MBRandom.RandomFloat)
                .FirstOrDefault();
        }

        if (randomBowItem == null || arrowItem == null)
        {
            Debug.Print("Could not find a valid bow or arrow", 0, Debug.DebugColor.Red);
            return null;
        }

        MissionWeapon bowWeapon = new MissionWeapon(randomBowItem, null, null, 1);
        MissionWeapon arrowWeapon = new MissionWeapon(arrowItem, null, null, 1);
        arrowWeapon.Amount = 20;

        return (bowWeapon, arrowWeapon);
    }
}
