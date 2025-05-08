using System.ComponentModel;
using Mono.Cecil.Cil;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Crpg.Module.Common.AmmoQuiverChange.AmmoQuiverChangeComponent;

namespace Crpg.Module.Common.AmmoQuiverChange;
internal class AmmoQuiverChangeBehaviorClient : MissionNetwork
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
    private const float QuiverChangeWindowSeconds = 1.5f;
    private const int QuiverChangeMaxCount = 3;
    private readonly string _changedSuccessSound = "event:/mission/combat/pickup_arrows";
    private readonly string _changeDeniedSound = "event:/ui/panels/previous";
    private MissionTime _lastMissileShotTime = MissionTime.Zero;
    private MissionTime _lastAmmoChangeTime = MissionTime.Zero;
    private int _quiverChangeCount = 0;

    private bool _wasMainAgentActive = false;
    private bool _quiverChangeRequested = false;
    private int _lastKnownTotalAmmo = -1;

    public AmmoQuiverChangeBehaviorClient()
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
            EquipmentIndex wieldedWeaponIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            if (wieldedWeaponIndex == EquipmentIndex.None)
            {
                return;
            }

            MissionWeapon mWeaponWielded = agent.Equipment[wieldedWeaponIndex];
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

        if (agent == null || !agent.IsActive())
        {
            return false;
        }

        if (!IsAgentWieldedWeaponRangedUsesQuiver(agent, out EquipmentIndex wieldedWeaponIndex, out MissionWeapon wieldedWeapon, out bool isThrowingWeapon))
        {
            LogDebug("RequestChangeRangedAmmo(): IsAgentWieldedWeaponRangedUsesQuiver() failed");
            return false;
        }

        if (!CheckAmmoChangeSpam())
        {
            LogDebug("RequestChangeRangedAmmo(): Rate limit exceeded. wait for cooldown.");
            return false;
        }

        if (_quiverChangeRequested == true)
        {
            LogDebug("RequestChangeRangedAmmo(): Already a request waiting.");
            PlaySoundForMainAgent(_changeDeniedSound);
            return false;
        }

        // check agent quivers
        if (!GetAgentQuiversWithAmmoEquippedForWieldedWeapon(agent, out List<int> ammoQuivers))
        {
            LogDebug("RequestChangeRangedAmmo(): GetAgentQuiversWithAmmoEquippedForWieldedWeapon() failed");
            PlaySoundForMainAgent(_changeDeniedSound);
            return false;
        }

        // not enough quivers with ammo found
        if (ammoQuivers.Count < 2)
        {
            LogDebug("RequestChangeRangedAmmo(): Only one or no quiver with ammo found, no change possible");
            PlaySoundForMainAgent(_changeDeniedSound);
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
                    case ItemObject.ItemTypeEnum.Crossbow when wieldedWeapon.ReloadPhase > 0:
                    case ItemObject.ItemTypeEnum.Musket when wieldedWeapon.ReloadPhase > 0:
                        LogDebug($"RequestChangeRangedAmmo(): reloadphase ineligible condition for {weaponItem.Name} ({wieldedWeapon.ReloadPhase})");
                        PlaySoundForMainAgent(_changeDeniedSound);
                        return false;
                }
                // attack release phase ineligible -- throwing release
                if (agent.GetCurrentActionType(1) == Agent.ActionCodeType.ReleaseThrowing)
                {
                    LogDebug($"RequestChangeRangedAmmo(): actionCode ineligible condition for {wieldedWeapon.Item.Name}");
                    PlaySoundForMainAgent(_changeDeniedSound);
                    return false;
                }
            }

            if (QuiverChangeMode == QuiverChangeModeEnum.Forced)
            {
                switch (weaponItem.Type)
                {
                    case ItemObject.ItemTypeEnum.Crossbow when wieldedWeapon.ReloadPhase == 2: // Loaded
                        LogDebug($"RequestChangeRangedAmmo() Bolt is already loaded in {wieldedWeapon.Item.Name}.");
                        PlaySoundForMainAgent(_changeDeniedSound);
                        return false;

                    case ItemObject.ItemTypeEnum.Musket when wieldedWeapon.ReloadPhase == 1: // Loaded
                        LogDebug($"RequestChangeRangedAmmo() Bullet is already loaded in {wieldedWeapon.Item.Name}.");
                        PlaySoundForMainAgent(_changeDeniedSound);
                        return false;
                }
                // attack release phase ineligible -- throwing release
                if (agent.GetCurrentActionType(1) == Agent.ActionCodeType.ReleaseThrowing)
                {
                    LogDebug($"RequestChangeRangedAmmo(): actionCode ineligible condition for {wieldedWeapon.Item.Name}");
                    PlaySoundForMainAgent(_changeDeniedSound);
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

    private bool CheckAmmoChangeSpam()
    {
        var now = MissionTime.Now;
        if (now - _lastAmmoChangeTime > MissionTime.Seconds(QuiverChangeWindowSeconds))
        {
            _lastAmmoChangeTime = now;
            _quiverChangeCount = 1;
            return true;
        }

        if (_quiverChangeCount < QuiverChangeMaxCount)
        {
            _quiverChangeCount++;
            return true;
        }

        return false;
    }

    private void PlaySoundForMainAgent(string soundEventString)
    {
        Agent agent = Agent.Main;
        if (agent == null || !agent.IsActive())
        {
            return;
        }

        Mission.Current.MakeSound(SoundEvent.GetEventIdFromString(soundEventString), agent.Position, false, true, -1, -1);
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

                // no sound if throwing
                if (IsAgentWieldedWeaponRangedUsesQuiver(Agent.Main, out _, out _, out bool isThrowingWeapon) && !isThrowingWeapon)
                {
                    PlaySoundForMainAgent(_changedSuccessSound);
                }

                break;
            case QuiverServerMessageAction.QuiverChangeCancelled:
                LogDebug("Quiver Change cancelled by server. (Changed weapons with loaded xbow/gun maybe)");
                _quiverChangeRequested = false;
                TriggerQuiverEvent(QuiverEventType.QuiverChangeCancelled);
                PlaySoundForMainAgent(_changeDeniedSound);
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
