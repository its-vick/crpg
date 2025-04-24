using System.ComponentModel;
using System.Net.Mail;
using JetBrains.Annotations;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Crpg.Module.Common;
internal class AmmoQuiverChangeMissionBehavior : MissionBehavior
{
    public event Action<QuiverEventType, object[]>? OnQuiverEvent;

    public enum QuiverEventType
    {
        AmmoQuiverChanged,
        WieldedItemChanged,
        ItemDrop,
        ItemPickup,
        AgentBuild,
        AgentRemoved,
        AgentChanged,
        MissileShot,
    }

    public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
    private const bool IsDebugEnabled = false;
    private static int _instanceCount = 0;
    private readonly GameNetwork.NetworkMessageHandlerRegisterer _networkMessageHandlerRegisterer;
    private MissionTime _lastMissileShotTime = MissionTime.Zero;

    public AmmoQuiverChangeMissionBehavior()
    {
        _networkMessageHandlerRegisterer = new GameNetwork.NetworkMessageHandlerRegisterer(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
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
            TriggerQuiverEvent(QuiverEventType.AgentBuild, banner);
            OnMainAgentWieldedItemChangeHandler();
        }
    }

    public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
    {
        base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
        if (affectedAgent == Agent.Main)
        {
            TriggerQuiverEvent(QuiverEventType.AgentRemoved, affectedAgent, affectorAgent, agentState, blow);
        }
    }

    public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
    {
        // rate limit a bit
        if (shooterAgent == Agent.Main)
        {
            if (MissionTime.Now - _lastMissileShotTime < MissionTime.Seconds(2f / 60f))
            {
                return;
            }

            _lastMissileShotTime = MissionTime.Now;

            LogDebug($"MB: OnAgentShootMissile() - for main agent only");
            TriggerQuiverEvent(QuiverEventType.MissileShot, shooterAgent, weaponIndex, position, velocity, orientation, hasRigidBody, forcedMissileIndex);
        }
    }

    public override void OnBehaviorInitialize()
    {
        LogDebug($"MB: OnBehaviorInitialize()");
        if (!GameNetwork.IsServer)
        {
            Debug.Print("Registering CustomServerMessage handler on client.", 0, Debug.DebugColor.Green);
            _networkMessageHandlerRegisterer.Register<CustomServerMessage>(HandleCustomServerMessage);
        }

        if (Mission.Current != null)
        {
            Mission.Current.OnItemDrop += OnItemDropHandler;
            Mission.Current.OnItemPickUp += OnItemPickupHandler;
            Mission.Current.OnMainAgentChanged += OnMainAgentChangedHandler;

            OnMainAgentChangedHandler(null, null);
        }

        base.OnBehaviorInitialize();
    }

    public override void OnRemoveBehavior()
    {
        LogDebug($"MB: OnRemoveBehavior()");

        if (Agent.Main != null)
        {
            Agent.Main.OnMainAgentWieldedItemChange -= OnMainAgentWieldedItemChangeHandler;
        }

        if (Mission.Current != null)
        {
            Mission.Current.OnMainAgentChanged -= OnMainAgentChangedHandler;
            Mission.Current.OnItemDrop -= OnItemDropHandler;
            Mission.Current.OnItemPickUp -= OnItemPickupHandler;
        }

        _instanceCount--;
        base.OnRemoveBehavior();
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
        if (ammoQuivers.Count > 1)
        {
            LogDebug("RequestChangeRangedAmmo() Quivers Found: " + ammoQuivers.Count);
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new ClientRequestAmmoQuiverChange());
            GameNetwork.EndModuleEventAsClient();
            return true;
        }
        else
        {
            LogDebug("RequestChangeRangedAmmo(): Only one or no quiver with ammo found, no change needed");
        }

        return false;
    }

    private void TriggerQuiverEvent(QuiverEventType type, params object[] parameters)
    {
        OnQuiverEvent?.Invoke(type, parameters);
    }

    private void OnItemDropHandler(Agent agent, SpawnedItemEntity spawnedItem)
    {
        if (agent != null && spawnedItem != null && Mission.MainAgent != null && agent == Mission.MainAgent && agent.IsActive())
        {
            LogDebug($"MB: HandleItemDrop() - for main agent only");
            TriggerQuiverEvent(QuiverEventType.ItemDrop, agent, spawnedItem);
        }
    }

    private void OnItemPickupHandler(Agent agent, SpawnedItemEntity spawnedItem)
    {
        if (agent != null && spawnedItem != null && Mission.MainAgent != null && agent == Mission.MainAgent && Mission.MainAgent.IsActive())
        {
            LogDebug($"MB: HandleItemPickup() - for main agent only");
            TriggerQuiverEvent(QuiverEventType.ItemPickup, agent, spawnedItem);
        }
    }

    private void OnMainAgentChangedHandler(object? sender, PropertyChangedEventArgs? e)
    {
        LogDebug("MB: OnMainAgentChangedHandler()");
        if (Agent.Main != null)
        {
            // Prevent duplicate subscriptions
            Agent.Main.OnMainAgentWieldedItemChange -= OnMainAgentWieldedItemChangeHandler;
            Agent.Main.OnMainAgentWieldedItemChange += OnMainAgentWieldedItemChangeHandler;

            TriggerQuiverEvent(QuiverEventType.AgentChanged);

            OnMainAgentWieldedItemChangeHandler(); // called once when agent changes
        }
    }

    private void OnMainAgentWieldedItemChangeHandler()
    {
        LogDebug($"MB: OnMainAgentWieldedItemChangeHandler() ");

        Agent agent = Agent.Main;
        if (agent == null || !agent.IsActive() || agent.Equipment == null)
        {
            return;
        }

        EquipmentIndex wieldedWeaponIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
        MissionWeapon weapon = wieldedWeaponIndex == EquipmentIndex.None
            ? MissionWeapon.Invalid
            : agent.Equipment[wieldedWeaponIndex];

        TriggerQuiverEvent(QuiverEventType.WieldedItemChanged, wieldedWeaponIndex, weapon);
    }

    private void HandleCustomServerMessage(CustomServerMessage message)
    {
        LogDebug($"HandleCustomServerMessage: {message.Message}");
        if (message.Message == "AmmoQuiverChanged")
        {
            // OnAmmoQuiverChanged?.Invoke(Agent.Main);
            TriggerQuiverEvent(QuiverEventType.AmmoQuiverChanged);
        }
    }

#pragma warning disable CS0162 // Unreachable code if debug disabled
    private void LogDebug(string message)
    {
        if (IsDebugEnabled)
        {
            InformationManager.DisplayMessage(new InformationMessage($"[DEBUG] {message}"));
        }
    }
#pragma warning restore CS0162

}
