using System.Runtime.InteropServices;
using Mono.Cecil.Cil;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.PlayerServices;

namespace Crpg.Module.Common;
internal class AmmoQuiverChangeComponent : MissionNetwork
{
    // private AmmoQuiverChangeMissionBehavior? _weaponChangeBehavior;
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

    public static bool IsQuiverAmmoWeaponForRangedWeapon(MissionWeapon mAmmo, MissionWeapon mWeapon)
    {
        if (mAmmo.IsEmpty || mWeapon.IsEmpty)
        {
            return false;
        }

        mWeapon.GatherInformationFromWeapon(out bool weaponHasMelee, out bool weaponHasShield, out bool weaponHasPolearm, out bool weaponHasNonConsumableRanged, out bool weaponHasThrown, out WeaponClass rangedAmmoClass);

        // check for throwing
        if (weaponHasThrown && mAmmo.Item != null)
        {
            if (mAmmo.Item.ItemType == ItemObject.ItemTypeEnum.Thrown)
            {
                return true;
            }
        }
        else // not a throwing weapon but a quiver match
        {
            WeaponComponentData mAmmoData = mAmmo.GetWeaponComponentDataForUsage(0);
            WeaponClass mAmmoClass = mAmmoData.WeaponClass;

            if (mAmmoClass == rangedAmmoClass)
            {
                return true;
            }
        }

        return false;
    }

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

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
        }

        foreach (var peer in pendingChanges)
        {
            ExecuteClientAmmoQuiverChange(peer);
        }
    }

    public void SendMessageToClient(NetworkCommunicator targetPeer, string message)
    {
        if (GameNetwork.IsServer)
        {
            CustomServerMessage serverMessage = new(message);
            GameNetwork.BeginModuleEventAsServer(targetPeer);
            GameNetwork.WriteMessage(serverMessage);
            GameNetwork.EndModuleEventAsServer();
        }
    }

    protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
    {
        if (GameNetwork.IsServer)
        {
            registerer.Register<ClientRequestAmmoQuiverChange>(HandleClientEventRequestAmmoQuiverChange);
        }
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

    private bool HandleClientEventRequestAmmoQuiverChange(NetworkCommunicator peer, GameNetworkMessage baseMessage)
    {
        Agent agent = peer.ControlledAgent;

        if (agent == null || !agent.IsActive())
        {
            return false;
        }

        PlayerId playerId = peer.VirtualPlayer.Id;

        // check if loaded ammo in weapon -- set flag and wait until shot or weapon changed to execute WIP
        if (!_wantsToChangeAmmoQuiver.TryGetValue(playerId, out var lastActiveStatus))
        {
            _wantsToChangeAmmoQuiver[playerId] = new ChangeStatus
            {
                AmmoChangeRequested = true,
            };
        }

        return true;
    }

    /*
        private int ammoChange = 1;
        private void ExecuteClientAmmoQuiverChangeSimple(NetworkCommunicator peer)
        {
            Agent agent = peer.ControlledAgent;

            if (agent == null || !agent.IsActive())
            {
                return;
            }

            // Check if agent is wielding a weapon that uses quiver. bow xbow or musket
            if (agent == null || !agent.IsActive() || !AmmoQuiverChangeComponent.IsAgentWieldedWeaponRangedUsesQuiver(agent, out EquipmentIndex wieldedWeaponIndex, out MissionWeapon mWeaponWielded, out bool isThrowingWeapon))
            {
                // LogDebug("RequestChangeRangedAmmo(): IsAgentWieldedWeaponRangedUsesQuiver() failed");
                return;
            }

            // check agent quivers
            if (!AmmoQuiverChangeComponent.GetAgentQuiversWithAmmoEquippedForWieldedWeapon(agent, out List<int> ammoQuivers))
            {
                // LogDebug("RequestChangeRangedAmmo(): GetAgentQuiversWithAmmoEquippedForWieldedWeapon() failed");
                return;
            }

            MissionEquipment equipment = agent.Equipment;

            // try simple switch
            if (ammoQuivers.Count() > 1)
            {
                if (mWeaponWielded.IsEmpty)
                {
                    // LogDebug("mWeaponWielded is Empty");
                    TaleWorlds.Library.Debug.Print("mWeaponWielded is Empty", 0, Debug.DebugColor.Red);
                    return;
                }

                if (mWeaponWielded.AmmoWeapon.IsEmpty || mWeaponWielded.AmmoWeapon.Item == null)
                {
                    // LogDebug("AmmoWeapon is Empty or Item is null");
                    TaleWorlds.Library.Debug.Print("AmmoWeapon is Empty or Item is null", 0, Debug.DebugColor.Red);
                }

                // LogDebug("AmmoWeapon: " + mWeaponWielded.AmmoWeapon.Item.Name);
                if (ammoChange == 1)
                {
                    // LogDebug("AmmoWeapon2: " + mWeaponWielded.AmmoWeapon.Item.Name);
                    mWeaponWielded.SetAmmo(agent.Equipment[EquipmentIndex.Weapon3]);
                    // LogDebug("AmmoWeapon: using setAmmo to weapon3");
                    TaleWorlds.Library.Debug.Print("AmmoWeapon: using setAmmo to weapon3", 0, Debug.DebugColor.Red);
                    ammoChange = 2;
                }
                else
                {
                    mWeaponWielded.SetAmmo(agent.Equipment[EquipmentIndex.Weapon2]);
                    // LogDebug("AmmoWeapon: using setAmmo to weapon2");
                    TaleWorlds.Library.Debug.Print("AmmoWeapon: using setAmmo to weapon2", 0, Debug.DebugColor.Red);
                    ammoChange = 1;
                }

                // Handle the request to change ammo quiver
                var playerId = peer.VirtualPlayer.Id;
                if (_wantsToChangeAmmoQuiver.Remove(playerId))
                {
                    // If the request exists, we remove it
                }

                agent.UpdateWeapons();

                return;
            }
        }
    */
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
        if (ammoQuivers.Count > 1)
        {
            // check thrown
            if (IsAgentWieldedWeaponRangedUsesQuiver(agent, out EquipmentIndex wieldedWeaponIndex, out MissionWeapon wieldedWeapon, out bool isThrowingWeapon) && isThrowingWeapon == true)
            {
                CycleThrowingQuivers(agent, wieldedWeaponIndex, equipment, ammoQuivers);
            }
            else
            {
                SwapQuivers(agent, equipment, ammoQuivers);
            }
        }

        // Handle the request to change ammo quiver
        var playerId = peer.VirtualPlayer.Id;
        if (_wantsToChangeAmmoQuiver.Remove(playerId))
        {
            // If the request exists, we remove it
        }

        agent.UpdateWeapons();
        SendMessageToClient(peer, "AmmoQuiverChanged");
        /*
        EquipmentIndex ammoIndex = GetEquippedQuiverItemIndex(agent);
        int ammoCount = agent.Equipment[ammoIndex].Ammo;
        int ammoCount2 = agent.Equipment[ammoIndex].Amount;
        if (ammoIndex != EquipmentIndex.None)
        {
            TaleWorlds.Library.Debug.Print("ExecuteClientAmmoQuiverChange()  ammoIndex: " + ammoIndex + " ammoCount: " + ammoCount + " ammoCount2: " + ammoCount2);
            agent.AgentVisuals.UpdateQuiverMeshesWithoutAgent((int)ammoIndex, ammoCount2); // might need to do all quivers
        }
        */
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
        public bool HasLoadedWeapon { get; set; }
    }
}

// Handle Network Message
[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
internal sealed class ClientRequestAmmoQuiverChange : GameNetworkMessage
{
    protected override void OnWrite()
    {
    }

    protected override bool OnRead()
    {
        bool bufferReadValid = true;
        return bufferReadValid;
    }

    protected override MultiplayerMessageFilter OnGetLogFilter()
    {
        return MultiplayerMessageFilter.Mission;
    }

    protected override string OnGetLogFormat()
    {
        return "Request to change ammo quiver";
    }
}

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
internal sealed class CustomServerMessage : GameNetworkMessage
{
    public string Message { get; private set; }

    public CustomServerMessage()
    {
        Message = string.Empty; // default value
    }

    public CustomServerMessage(string message)
    {
        Message = message;
    }

    protected override bool OnRead()
    {
        bool bufferReadValid = true;
        Message = ReadStringFromPacket(ref bufferReadValid);
        return bufferReadValid;
    }

    protected override void OnWrite()
    {
        WriteStringToPacket(Message);
    }

    protected override MultiplayerMessageFilter OnGetLogFilter()
    {
        return MultiplayerMessageFilter.General;
    }

    protected override string OnGetLogFormat()
    {
        return "CustomServerMessage: " + Message;
    }
}
