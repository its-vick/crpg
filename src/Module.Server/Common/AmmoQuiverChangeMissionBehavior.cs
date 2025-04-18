using System.ComponentModel;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common;
internal class AmmoQuiverChangeMissionBehavior : MissionBehavior
{
    public delegate void MissileShotHandler(Agent shooterAgent, EquipmentIndex weaponIndex);
    public event MissileShotHandler OnMissileShot = (index, data) => { }; // Lambda initialization
    public delegate void WieldedItemChangedHandler(EquipmentIndex newWeaponIndex, MissionWeapon weapon);
    public event WieldedItemChangedHandler WieldedItemChanged = (index, data) => { };

    public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
    private const bool IsDebugEnabled = false;
    private static int _instanceCount = 0;
    private MissionTime _lastMissileShotTime = MissionTime.Zero;
    // private MissionTime _lastWeaponChangeTime = MissionTime.Zero;

    public AmmoQuiverChangeMissionBehavior()
    {
        _instanceCount++;
        LogDebug($"AmmoQuiverChangeMissionBehavior instance {_instanceCount} created (Hash: {GetHashCode()})");
    }

    public override void OnAgentBuild(Agent agent, Banner banner)
    {
        if (agent != null && agent.IsActive() && agent == Mission.MainAgent)
        {
            EquipmentIndex wieldedWeaponIndex = EquipmentIndex.None;
            MissionWeapon mWeaponWielded = MissionWeapon.Invalid;

            wieldedWeaponIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            if (wieldedWeaponIndex == EquipmentIndex.None)
            {
                return;
            }

            mWeaponWielded = agent.Equipment[wieldedWeaponIndex];
            if (mWeaponWielded.IsEmpty || mWeaponWielded.Item == null)
            {
                return;
            }

            // Notify listeners (e.g MissionView)
            LogDebug($"MB: OnAgentBuild()");
            // OnWeaponChanged(wieldedWeaponIndex, mWeaponWielded);
            OnMainAgentWieldedItemChange();
        }
    }

    public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
    {
        // notify listeners
        if (shooterAgent == Agent.Main)
        {
            if (MissionTime.Now - _lastMissileShotTime < MissionTime.Seconds(2f / 60f))
            {
                return;
            }

            _lastMissileShotTime = MissionTime.Now;

            if (GameNetwork.IsServer && shooterAgent == Agent.Main)
            {
                return;
            }

            LogDebug($"MB: OnAgentShootMissile()");
            OnMissileShot(shooterAgent, weaponIndex);
        }
    }

    public override void OnBehaviorInitialize()
    {
        LogDebug($"MB: OnBehaviorInitialize()");
        base.OnBehaviorInitialize();
        InitializeMainAgentPropterties();
    }

    public override void OnRemoveBehavior()
    {
        LogDebug($"MB: OnRemoveBehavior()");
        Mission.Current.OnMainAgentChanged -= OnMainAgentChanged;
        if (Agent.Main != null)
        {
            Agent.Main.OnMainAgentWieldedItemChange -= OnMainAgentWieldedItemChange;
        }

        OnMissileShot = (index, data) => { };
        WieldedItemChanged = (index, data) => { };

        _instanceCount--;
        base.OnRemoveBehavior();
    }

    public void InitializeMainAgentPropterties()
    {
        LogDebug($"MB: InitializeMainAgentPropterties()");
        Mission.Current.OnMainAgentChanged -= OnMainAgentChanged;
        Mission.Current.OnMainAgentChanged += OnMainAgentChanged;
        OnMainAgentChanged(null, null);
    }

    public bool RequestChangeRangedAmmo()
    {
        LogDebug("RequestChangeRangedAmmo()");
        Agent agent = Agent.Main;

        // Check if agent is wielding a weapon that uses quiver. bow xbow or musket
        if (agent == null || !agent.IsActive() || !AmmoQuiverChangeComponent.IsAgentWieldedWeaponRangedUsesQuiver(agent, out EquipmentIndex wieldedWeaponIndex, out MissionWeapon mWeaponWielded, out bool isThrowingWeapon))
        {
            LogDebug("RequestChangeRangedAmmo(): IsAgentWieldedWeaponRangedUsesQuiver() failed");
            return false;
        }

        // check agent quivers
        if (!AmmoQuiverChangeComponent.GetAgentQuiversWithAmmoEquippedForWieldedWeapon(agent, out List<int> ammoQuivers))
        {
            LogDebug("RequestChangeRangedAmmo(): GetAgentQuiversWithAmmoEquippedForWieldedWeapon() failed");
            return false;
        }

        // multiple quivers with ammo found
        if (ammoQuivers.Count() > 1)
        {
            LogDebug("RequestChangeRangedAmmo() Quivers Found: " + ammoQuivers.Count());
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new ClientRequestAmmoQuiverChange());
            GameNetwork.EndModuleEventAsClient();
            return true;
        }
        else
        {
            LogDebug("RequestChangeRangedAmmo(): ammoQuivers.Count() < 1");
        }

        return false;
    }

    protected override void OnEndMission()
    {
        // not needed so far
    }

    private void OnMainAgentChanged(object? sender, PropertyChangedEventArgs? e)
    {
        LogDebug("MB: OnMainAgentChanged()");
        if (Agent.Main != null)
        {
            // Prevent duplicate subscriptions
            Agent.Main.OnMainAgentWieldedItemChange -= OnMainAgentWieldedItemChange;
            Agent.Main.OnMainAgentWieldedItemChange += OnMainAgentWieldedItemChange;

            OnMainAgentWieldedItemChange(); // called once when agent changes
        }
    }

    private void OnMainAgentWieldedItemChange()
    {
        /*
        if (MissionTime.Now - _lastWeaponChangeTime < MissionTime.Seconds(2f / 60f))
        {
            return;
        }

        _lastWeaponChangeTime = MissionTime.Now;
        */
        LogDebug($"MB: OnMainAgentWieldedItemChange() ");

        Agent agent = Agent.Main;
        if (agent == null || !agent.IsActive() || agent.Equipment == null)
        {
            return;
        }

        EquipmentIndex wieldedWeaponIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
        MissionWeapon weapon = wieldedWeaponIndex == EquipmentIndex.None
            ? MissionWeapon.Invalid
            : agent.Equipment[wieldedWeaponIndex];

        WieldedItemChanged?.Invoke(wieldedWeaponIndex, weapon);
    }

    private void LogDebug(string message)
    {
        if (IsDebugEnabled)
        {
            InformationManager.DisplayMessage(new InformationMessage($"[DEBUG] {message}"));
        }
    }
}
