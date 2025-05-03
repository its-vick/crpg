using System.ComponentModel;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using static Crpg.Module.Common.AmmoQuiverChangeComponent;

namespace Crpg.Module.Common;
internal class AmmoQuiverChangeMissionBehaviorClient : MissionNetwork
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
        AgentStatusChanged,
        AmmoCountIncreased,
        AmmoCountDecreased,
        QuiverChangeCancelled,
    }

    public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
    private const bool IsDebugEnabled = true;
    private MissionTime _lastMissileShotTime = MissionTime.Zero;

    private bool _wasMainAgentActive = false;
    private bool _quiverChangeRequested = false;
    private int _lastKnownTotalAmmo = -1;

    public AmmoQuiverChangeMissionBehaviorClient()
    {
    }

    public override void OnMissionTick(float dt)
    {
        Agent mainAgent = Agent.Main;
        bool isActive = mainAgent?.IsActive() == true;

        if (_wasMainAgentActive != isActive)
        {
            _wasMainAgentActive = isActive;
            TriggerQuiverEvent(QuiverEventType.AgentStatusChanged, isActive);
        }

        if (mainAgent != null && isActive)
        {
            int currentAmmo = GetTotalAmmoCount();

            if (currentAmmo > _lastKnownTotalAmmo)
            {
                TriggerQuiverEvent(QuiverEventType.AmmoCountIncreased);
            }
            else if (currentAmmo < _lastKnownTotalAmmo)
            {
                TriggerQuiverEvent(QuiverEventType.AmmoCountDecreased);
            }

            _lastKnownTotalAmmo = currentAmmo;
        }
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
        if (shooterAgent == Agent.Main)
        {
            // rate limit a bit was getting double triggered
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

        base.OnRemoveBehavior();
    }

    public void SendQuiverMessageToServer(QuiverClientMessageAction action)
    {
        if (GameNetwork.IsClient)
        {
            QuiverClientMessage quiverMessage = new(action);

            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(quiverMessage);
            GameNetwork.EndModuleEventAsClient();
        }
    }

    public bool RequestChangeRangedAmmo()
    {
        Agent agent = Agent.Main;

        // Check if agent is wielding a weapon that uses quiver. bow xbow or musket
        if (agent == null || !agent.IsActive() ||
            !IsAgentWieldedWeaponRangedUsesQuiver(agent, out EquipmentIndex wieldedWeaponIndex, out MissionWeapon wieldedWeapon, out bool isThrowingWeapon))
        {
            LogDebug("RequestChangeRangedAmmo(): IsAgentWieldedWeaponRangedUsesQuiver() failed");
            return false;
        }

        if (_quiverChangeRequested == true)
        {
            LogDebug("RequestChangeRangedAmmo(): Already a request waiting.");
            return false;
        }

        // check agent quivers
        if (!GetAgentQuiversWithAmmoEquippedForWieldedWeapon(agent, out List<int> ammoQuivers))
        {
            LogDebug("RequestChangeRangedAmmo(): GetAgentQuiversWithAmmoEquippedForWieldedWeapon() failed");
            return false;
        }

        // not enough quivers with ammo found
        if (ammoQuivers.Count < 2)
        {
            LogDebug("RequestChangeRangedAmmo(): Only one or no quiver with ammo found, no change possible");
            return false;
        }

        if (!wieldedWeapon.IsEmpty &&
            !wieldedWeapon.IsEqualTo(MissionWeapon.Invalid) &&
            wieldedWeapon.Item is { } weaponItem)
        {
            if (QuiverChangeMode == QuiverChangeModeEnum.ConditionsMet)
            {
                switch (weaponItem.Type)
                {
                    case ItemObject.ItemTypeEnum.Bow when wieldedWeapon.ReloadPhase > 0:
                    case ItemObject.ItemTypeEnum.Crossbow when wieldedWeapon.ReloadPhase > 1:
                    case ItemObject.ItemTypeEnum.Musket when wieldedWeapon.ReloadPhase > 0:
                        LogDebug($"RequestChangeRangedAmmo(): reloadphase ineligible condition for {weaponItem.Name} ({wieldedWeapon.ReloadPhase})");
                        return false;
                }
            }

            if (QuiverChangeMode == QuiverChangeModeEnum.Forced)
            {
                switch (weaponItem.Type)
                {
                    case ItemObject.ItemTypeEnum.Crossbow when wieldedWeapon.ReloadPhase == 2:
                        LogDebug($"RequestChangeRangedAmmo() Bolt is already loaded in {wieldedWeapon.Item.Name}.");
                        return false;

                    case ItemObject.ItemTypeEnum.Musket when wieldedWeapon.ReloadPhase == 1:
                        LogDebug($"RequestChangeRangedAmmo() Bullet is already loaded in {wieldedWeapon.Item.Name}.");
                        return false;
                }
            }
        }

        LogDebug("RequestChangeRangedAmmo() Quivers Found: " + ammoQuivers.Count);
        SendQuiverMessageToServer(QuiverClientMessageAction.QuiverChangeRequest);

        _quiverChangeRequested = true;
        return true;
    }

    public int GetTotalAmmoCount()
    {
        Agent agent = Agent.Main;
        if (agent == null || !agent.IsActive())
        {
            return -1;
        }

        int totalAmmo = 0;

        // Iterate through the agent's equipment
        for (int i = 0; i < 4; i++)
        {
            var eItem = agent.Equipment[i];
            if (eItem.IsEmpty || eItem.Item == null)
            {
                continue;
            }

            totalAmmo += eItem.Amount;
        }

        return totalAmmo;
    }

    protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
    {
        if (GameNetwork.IsClient)
        {
            base.AddRemoveMessageHandlers(registerer);
            registerer.Register<QuiverServerMessage>(HandleQuiverServerMessage);
        }
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
            _lastKnownTotalAmmo = GetTotalAmmoCount();
            _quiverChangeRequested = false;
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

    private void HandleQuiverServerMessage(QuiverServerMessage message)
    {
        LogDebug($"HandleQuiverServerMessage: {message.Action}");
        switch (message.Action)
        {
            case QuiverServerMessageAction.None:

                break;
            case QuiverServerMessageAction.QuiverChangeSuccess:
                _quiverChangeRequested = false;
                TriggerQuiverEvent(QuiverEventType.AmmoQuiverChanged);
                break;
            case QuiverServerMessageAction.QuiverChangeCancelled:
                LogDebug("Quiver Change cancelled by server. (Changed weapons with loaded xbow/gun maybe)");
                _quiverChangeRequested = false;
                TriggerQuiverEvent(QuiverEventType.QuiverChangeCancelled);
                break;
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
