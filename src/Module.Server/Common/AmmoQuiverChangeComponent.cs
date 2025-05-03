using JetBrains.Annotations;
using psai.Editor;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.PlayerServices;

namespace Crpg.Module.Common;
internal class AmmoQuiverChangeComponent : MissionNetwork
{
    public enum QuiverChangeModeEnum
    {
        None = 0,
        ConditionsMet = 1, // only changes ammo if conditions are right when you press the button. doesnt interrupt animations
        Queued = 2, // changes ammo quiver after shot, unless conditions were right when you pressed the button. Cancels if u change weapons. doesnt interrupt animations
        Forced = 3, // forces quiver change when you press the button, Unless xbow or gun is already loaded in which case doesnt work. Works later in reload phases than conditionsMet
    }

    public static QuiverChangeModeEnum QuiverChangeMode { get; set; } = QuiverChangeModeEnum.Forced;

    private readonly Dictionary<PlayerId, ChangeStatus> _wantsToChangeAmmoQuiver;
    public AmmoQuiverChangeComponent()
    {
        _wantsToChangeAmmoQuiver = new Dictionary<PlayerId, ChangeStatus>();
    }

    public static bool IsQuiverItem(ItemObject item)
    {
        return item != null && (
               item.Type == ItemObject.ItemTypeEnum.Arrows ||
               item.Type == ItemObject.ItemTypeEnum.Bolts ||
               item.Type == ItemObject.ItemTypeEnum.Bullets ||
               item.Type == ItemObject.ItemTypeEnum.Thrown);
    }

    public static bool GetCurrentQuiver(Agent agent, out MissionWeapon quiverWeapon, out EquipmentIndex quiverIndex)
    {
        quiverWeapon = MissionWeapon.Invalid;
        quiverIndex = EquipmentIndex.None;

        if (agent == null || !agent.IsActive())
        {
            return false;
        }

        EquipmentIndex wieldedItemIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
        MissionWeapon wieldedWeapon = MissionWeapon.Invalid;

        if (wieldedItemIndex == EquipmentIndex.None)
        {
            return false;
        }

        wieldedWeapon = agent.Equipment[wieldedItemIndex];
        if (wieldedWeapon.IsEmpty || wieldedWeapon.IsEqualTo(MissionWeapon.Invalid) || wieldedWeapon.Item == null)
        {
            return false;
        }

        if (!GetAgentQuiversWithAmmoEquippedForWieldedWeapon(agent, out List<int> ammoQuivers))
        {
            return false;
        }

        if (wieldedWeapon.Item.Type == ItemObject.ItemTypeEnum.Thrown)
        {
            quiverWeapon = wieldedWeapon;
            quiverIndex = wieldedItemIndex;
        }
        else
        {
            if (ammoQuivers.Count <= 0)
            {
                return false;
            }

            quiverWeapon = agent.Equipment[ammoQuivers[0]];
            quiverIndex = (EquipmentIndex)ammoQuivers[0];
        }

        return true;
    }

    public static bool GetAgentQuiversEquipped(Agent agent, out List<int> ammoQuivers)
    {
        // List to store quiver indexes
        ammoQuivers = new System.Collections.Generic.List<int>();

        if (agent == null || !agent.IsActive())
        {
            return false;
        }

        MissionEquipment equipment = agent.Equipment;

        // Loop through equipment and find quivers
        for (int i = 0; i < 4; i++)
        {
            var item = equipment[i].Item;
            // Check if item is a quiver and not empty
            if (item != null && !equipment[i].IsEmpty && IsQuiverItem(item))
            {
                ammoQuivers.Add(i);
            }
        }

        return true;
    }

    public static bool GetAgentQuiversWithAmmoEquippedForWieldedWeapon(Agent agent, out List<int> ammoQuivers)
    {
        // List to store quiver indexes
        ammoQuivers = new System.Collections.Generic.List<int>();

        if (agent == null || !agent.IsActive())
        {
            return false;
        }

        if (!IsAgentWieldedWeaponRangedUsesQuiver(agent, out EquipmentIndex wieldedWeaponIndex, out MissionWeapon wieldedWeapon, out bool isThrowingWeapon))
        {
            return false;
        }

        MissionEquipment equipment = agent.Equipment;
        // Loop through equipment and find quivers
        for (int i = 0; i < 4; i++)
        {
            MissionWeapon iWeapon = equipment[i];
            ItemObject item = iWeapon.Item;
            // Check if item is a quiver and not empty
            if (item != null && !iWeapon.IsEmpty && IsQuiverItem(item))
            {
                // check that its a quiver for weapon
                if (!IsQuiverAmmoWeaponForRangedWeapon(iWeapon, wieldedWeapon))
                {
                    continue;
                }

                // check that it has ammo left in quiver
                if (iWeapon.Amount > 0)
                {
                    ammoQuivers.Add(i);
                }
            }
        }

        return true;
    }

    public static bool IsAgentWieldedWeaponRangedUsesQuiver(Agent agent, out EquipmentIndex wieldedWeaponIndex, out MissionWeapon wieldedWeapon, out bool isThrowingWeapon) // Bow Xbow or Musket
    {
        wieldedWeaponIndex = EquipmentIndex.None;
        wieldedWeapon = MissionWeapon.Invalid;
        isThrowingWeapon = false;

        if (agent == null || !agent.IsActive())
        {
            return false;
        }

        wieldedWeaponIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
        if (wieldedWeaponIndex == EquipmentIndex.None)
        {
            return false;
        }

        wieldedWeapon = agent.Equipment[wieldedWeaponIndex];
        if (wieldedWeapon.IsEmpty || wieldedWeapon.Item == null)
        {
            return false;
        }

        var type = wieldedWeapon.Item.Type;
        if (type == ItemObject.ItemTypeEnum.Thrown) // bypass .IsRangedWeapon for throwing in alt usage melee mode
        {
            isThrowingWeapon = true;
            return type == ItemObject.ItemTypeEnum.Thrown;
        }

        if (!agent.GetWieldedWeaponInfo(Agent.HandIndex.MainHand).IsRangedWeapon) // is ranged weapon
        {
            // TaleWorlds.Library.Debug.Print("IsAgentWieldedWeaponRangedUsesQuiver.IsRangedWeapon failed", 0, Debug.DebugColor.Red);
            return false;
        }

        return type == ItemObject.ItemTypeEnum.Bow ||
                   type == ItemObject.ItemTypeEnum.Crossbow ||
                   type == ItemObject.ItemTypeEnum.Musket ||
                   type == ItemObject.ItemTypeEnum.Thrown;
    }

    public static bool IsQuiverAmmoWeaponForRangedWeapon(MissionWeapon ammo, MissionWeapon rangedWeapon)
    {
        if (ammo.IsEmpty || rangedWeapon.IsEmpty)
        {
            return false;
        }

        rangedWeapon.GatherInformationFromWeapon(
            out bool hasMelee, out bool hasShield, out bool hasPolearm,
            out bool hasNonConsumableRanged, out bool hasThrown,
            out WeaponClass expectedAmmoClass);

        // Thrown weapons
        if (hasThrown && ammo.Item != null && ammo.Item.ItemType == ItemObject.ItemTypeEnum.Thrown)
        {
            return true;
        }

        // Regular ranged weapons (bow, crossbow, musket)
        if (!ammo.IsEmpty && ammo.GetWeaponComponentDataForUsage(0)?.WeaponClass == expectedAmmoClass)
        {
            return true;
        }

        return false;
    }

    public static bool IsAgentWeaponLoaded(Agent agent)
    {
        if (agent == null || !agent.IsActive())
        {
            return false;
        }

        if (!IsAgentWieldedWeaponRangedUsesQuiver(agent, out _, out MissionWeapon weapon, out bool isThrowingWeapon))
        {
            return false;
        }

        ItemObject item = weapon.Item;
        if (item == null)
        {
            return false;
        }

        if (weapon.AmmoWeapon.IsEmpty || weapon.AmmoWeapon.Item == null)
        {
            return false;
        }

        // has an ammo weapon (arrow drawn or bow/bullet attached)
        return true;
    }

    public static bool IsWeaponStateAbleChangeAmmo(MissionWeapon weapon)
    {
        if (weapon.IsEmpty || weapon.Equals(MissionWeapon.Invalid) || weapon.Item == null)
        {
            return false;
        }

        switch (weapon.Item.Type)
        {
            case ItemObject.ItemTypeEnum.Bow:
                if (weapon.ReloadPhase > 0)
                {
                    return false;
                }

                break;
            case ItemObject.ItemTypeEnum.Crossbow:
                if (weapon.ReloadPhase > 1)
                {
                    return false;
                }

                break;
            case ItemObject.ItemTypeEnum.Musket:
                if (weapon.ReloadPhase > 0)
                {
                    return false;
                }

                break;
            case ItemObject.ItemTypeEnum.Thrown:
                {
                    return true;
                }
        }

        return true;
    }

    public static void CycleQuiverChangeMode()
    {
        int nextMode = ((int)QuiverChangeMode + 1) % 3; // Assumes 3 valid values (1–3)
        QuiverChangeMode = (QuiverChangeModeEnum)(nextMode + 1); // Skip 'None' (0)

        if (GameNetwork.IsServer)
        {
            foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
            {
                SendQuiverMessageToClient(peer, QuiverServerMessageAction.UpdateQuiverChangeMode);
            }
        }
    }

    public static void SendQuiverMessageToClient(NetworkCommunicator targetPeer, QuiverServerMessageAction action)
    {
        if (GameNetwork.IsServer)
        {
            QuiverServerMessage quiverMessage = new(action);
            GameNetwork.BeginModuleEventAsServer(targetPeer);
            GameNetwork.WriteMessage(quiverMessage);
            GameNetwork.EndModuleEventAsServer();
        }
    }

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

        if (QuiverChangeMode == QuiverChangeModeEnum.Queued)
        {
            ProcessQueueTick(dt);
        }
    }

    protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
    {
        if (GameNetwork.IsServer)
        {
            registerer.Register<QuiverClientMessage>((msg, peer) => HandleQuiverClientMessage(msg, peer));
        }
        else if (GameNetwork.IsClient)
        {
            registerer.Register<QuiverServerMessage>(HandleQuiverServerMessage);
        }
    }

    private static bool IsAgentRangedWeaponLoadedOrLoading(Agent agent, out bool notRangedWeapon)
    {
        notRangedWeapon = false;

        if (agent == null || !agent.IsActive())
        {
            return false;
        }

        if (!IsAgentWieldedWeaponRangedUsesQuiver(agent, out _, out MissionWeapon weapon, out _))
        {
            notRangedWeapon = true;
            return false;
        }

        var item = weapon.Item;
        if (item == null)
        {
            return false;
        }

        var type = item.Type;

        // Bows: check reload phase
        if (type == ItemObject.ItemTypeEnum.Bow)
        {
            return weapon.ReloadPhase != 0;
        }

        // Crossbows & Muskets: check reload animation or reload phase
        if (type == ItemObject.ItemTypeEnum.Crossbow || type == ItemObject.ItemTypeEnum.Musket)
        {
            return agent.GetCurrentActionType(1) == Agent.ActionCodeType.Reload || weapon.ReloadPhase != 0;
        }

        return false;
    }

    // Handle weapon change queue
    private void ProcessQueueTick(float dt)
    {
        var pendingChanges = new List<NetworkCommunicator>();
        var cancelledChanges = new List<NetworkCommunicator>();

        foreach (var kvp in _wantsToChangeAmmoQuiver)
        {
            var playerId = kvp.Key;
            var quiverRequest = kvp.Value;

            if (!quiverRequest.AmmoChangeRequested)
            {
                continue;
            }

            var peer = GameNetwork.NetworkPeers.FirstOrDefault(p => p.VirtualPlayer.Id == playerId);
            if (peer == null)
            {
                continue;
            }

            var agent = peer.ControlledAgent;
            if (agent == null || !agent.IsActive())
            {
                cancelledChanges.Add(peer);
                continue;
            }

            if (!IsAgentRangedWeaponLoadedOrLoading(agent, out bool notRangedWeapon))
            {
                if (notRangedWeapon)
                {
                    cancelledChanges.Add(peer);
                }
                else
                {
                    pendingChanges.Add(peer);
                }
            }
        }

        foreach (var peer in cancelledChanges)
        {
            _wantsToChangeAmmoQuiver.Remove(peer.VirtualPlayer.Id);
            SendQuiverMessageToClient(peer, QuiverServerMessageAction.QuiverChangeCancelled);
        }

        foreach (var peer in pendingChanges)
        {
            ExecuteClientAmmoQuiverChange(peer);
        }
    }

    private void HandleQuiverServerMessage(QuiverServerMessage message)
    {
        switch (message.Action)
        {
            case QuiverServerMessageAction.UpdateQuiverChangeMode:
                CycleQuiverChangeMode();
                break;
        }
    }

    private bool HandleQuiverClientMessage(NetworkCommunicator peer, QuiverClientMessage message)
    {
        switch (message.Action)
        {
            case QuiverClientMessageAction.None:
                break;
            case QuiverClientMessageAction.QuiverChangeRequest:
                if (QuiverChangeMode == QuiverChangeModeEnum.Queued)
                {
                    PlayerId playerId = peer.VirtualPlayer.Id;
                    // check if loaded ammo in weapon -- set flag and wait until shot or weapon changed to execute WIP
                    if (!_wantsToChangeAmmoQuiver.TryGetValue(playerId, out var lastActiveStatus))
                    {
                        _wantsToChangeAmmoQuiver[playerId] = new ChangeStatus
                        {
                            AmmoChangeRequested = true,
                        };
                    }
                }
                else if (QuiverChangeMode == QuiverChangeModeEnum.ConditionsMet)
                {
                    ExecuteClientAmmoQuiverChange(peer);
                }
                else if (QuiverChangeMode == QuiverChangeModeEnum.Forced)
                {
                    ResetReloadAnimationsAndWeapon(peer);
                    ExecuteClientAmmoQuiverChange(peer);
                }

                break;
            case QuiverClientMessageAction.QuiverCancelReload:

                break;
        }

        return true;
    }

    private void ResetReloadAnimationsAndWeapon(NetworkCommunicator peer)
    {
        Agent agent = peer.ControlledAgent;
        if (agent == null || !agent.IsActive())
        {
            return;
        }

        EquipmentIndex wieldedIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
        MissionWeapon wieldedWeapon = agent.WieldedWeapon;

        if (!wieldedWeapon.IsEmpty && !wieldedWeapon.IsEqualTo(MissionWeapon.Invalid) && wieldedIndex >= EquipmentIndex.Weapon0 && wieldedIndex <= EquipmentIndex.Weapon3)
        {
            // stops reload for bows and early stages of xbow/gun reload
            agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
            agent.TryToWieldWeaponInSlot(wieldedIndex, Agent.WeaponWieldActionType.Instant, true);
        }
    }

    private void ExecuteClientAmmoQuiverChange(NetworkCommunicator peer)
    {
        Agent agent = peer.ControlledAgent;

        if (agent == null || !agent.IsActive())
        {
            return;
        }

        MissionEquipment equipment = agent.Equipment;

        // List to store quiver indexes
        var ammoQuivers = new System.Collections.Generic.List<int>();

        // Loop through equipment and find quivers
        for (int i = 0; i < 4; i++)
        {
            var item = equipment[i].Item;
            // Check if item is a quiver and not empty
            if (item != null && !equipment[i].IsEmpty && IsQuiverItem(item))
            {
                ammoQuivers.Add(i);
            }
        }

        // TaleWorlds.Library.Debug.Print(" execQuiverChange: ammoQuivers.Count: " + ammoQuivers.Count, 0, Debug.DebugColor.Red);

        // If there are more than 1 quivers, perform swaps
        if (ammoQuivers.Count < 2)
        {
            return;
        }

        // Verify ranged weapon wielded
        if (!IsAgentWieldedWeaponRangedUsesQuiver(agent, out EquipmentIndex wieldedWeaponIndex, out MissionWeapon wieldedWeapon, out bool isThrowingWeapon))
        {
            return;
        }

        // handle throwing
        if (isThrowingWeapon == true)
        {
            CycleThrowingQuivers(agent, wieldedWeaponIndex, equipment, ammoQuivers);
        }
        else // bow,xbow/musket
        {
            SwapQuivers(agent, equipment, ammoQuivers);
        }

        if (QuiverChangeMode == QuiverChangeModeEnum.Queued)
        {
            // Handle the request to change ammo quiver
            var playerId = peer.VirtualPlayer.Id;
            if (_wantsToChangeAmmoQuiver.Remove(playerId))
            {
                // If the request exists, we remove it
            }
        }

        agent.UpdateWeapons();
        SendQuiverMessageToClient(peer, QuiverServerMessageAction.QuiverChangeSuccess);
    }

    private void CycleThrowingQuivers(Agent agent, EquipmentIndex wieldedWeaponIndex, MissionEquipment equipment, List<int> ammoQuivers)
    {
        if (agent == null || !agent.IsActive() || ammoQuivers == null || ammoQuivers.Count <= 1)
        {
            return;
        }

        int currentIndex = ammoQuivers.IndexOf((int)wieldedWeaponIndex);
        if (currentIndex == -1)
        {
            return; // current index isn't in the list
        }

        for (int i = 1; i < ammoQuivers.Count; i++)
        {
            int checkIndex = (currentIndex + i) % ammoQuivers.Count;
            var checkEquipmentIndex = (EquipmentIndex)ammoQuivers[checkIndex];
            var checkWeapon = equipment[checkEquipmentIndex];

            if (!checkWeapon.IsEmpty && checkWeapon.Amount > 0)
            {
                agent.TryToWieldWeaponInSlot(checkEquipmentIndex, Agent.WeaponWieldActionType.WithAnimation, false);
                return;
            }
        }
    }

    private void SwapQuivers(Agent agent, MissionEquipment equipment, System.Collections.Generic.List<int> ammoQuivers)
    {
        if (agent == null || !agent.IsActive() || ammoQuivers == null || ammoQuivers.Count < 2)
        {
            return;
        }

        int count = ammoQuivers.Count;

        // Cache the original MissionWeapons in order
        MissionWeapon[] cachedWeapons = new MissionWeapon[count];
        for (int i = 0; i < count; i++)
        {
            cachedWeapons[i] = equipment[ammoQuivers[i]];
        }

        // Rotate left: shift each weapon to the previous index
        for (int i = 0; i < count; i++)
        {
            int fromIndex = (i + 1) % count;
            agent.EquipWeaponWithNewEntity((EquipmentIndex)ammoQuivers[i], ref cachedWeapons[fromIndex]);
        }
    }

    private struct ChangeStatus
    {
        public bool AmmoChangeRequested { get; set; }
    }
}

// Handle Network Message
public enum QuiverClientMessageAction : int
{
    None = 0,
    QuiverChangeRequest = 1,
    QuiverCancelReload = 2,
}

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
internal sealed class QuiverClientMessage : GameNetworkMessage
{
    private static readonly CompressionInfo.Integer QuiverActionCompression = new(0, 10, true);
    public QuiverClientMessageAction Action { get; private set; }

    public QuiverClientMessage()
    {
        Action = QuiverClientMessageAction.None;
    }

    public QuiverClientMessage(QuiverClientMessageAction action)
    {
        Action = action;
    }

    protected override bool OnRead()
    {
        bool bufferReadValid = true;
        Action = (QuiverClientMessageAction)ReadIntFromPacket(QuiverActionCompression, ref bufferReadValid);
        return bufferReadValid;
    }

    protected override void OnWrite()
    {
        WriteIntToPacket((int)Action, QuiverActionCompression);
    }

    protected override MultiplayerMessageFilter OnGetLogFilter()
    {
        return MultiplayerMessageFilter.General;
    }

    protected override string OnGetLogFormat()
    {
        return $"QuiverClientMessage - Action: {Action}";
    }
}

public enum QuiverServerMessageAction : int
{
    None = 0,
    QuiverChangeSuccess = 1,
    QuiverChangeCancelled = 2,
    UpdateQuiverChangeMode = 3,
}

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
internal sealed class QuiverServerMessage : GameNetworkMessage
{
    private static readonly CompressionInfo.Integer QuiverActionCompression = new(0, 10, true);

    public QuiverServerMessageAction Action { get; private set; }

    public QuiverServerMessage()
    {
        Action = QuiverServerMessageAction.None;
    }

    public QuiverServerMessage(QuiverServerMessageAction action)
    {
        Action = action;
    }

    protected override bool OnRead()
    {
        bool bufferReadValid = true;
        Action = (QuiverServerMessageAction)ReadIntFromPacket(QuiverActionCompression, ref bufferReadValid);
        return bufferReadValid;
    }

    protected override void OnWrite()
    {
        WriteIntToPacket((int)Action, QuiverActionCompression);
    }

    protected override MultiplayerMessageFilter OnGetLogFilter()
    {
        return MultiplayerMessageFilter.General;
    }

    protected override string OnGetLogFormat()
    {
        return $"QuiverServerMessage - Action: {Action}";
    }
}
