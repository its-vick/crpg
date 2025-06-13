using Crpg.Module.Api.Models.Users;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond;

namespace Crpg.Module.Common.FriendlyFireReport;

internal class FriendlyFireReportServerBehavior : MissionNetwork
{
    // private const int TeamHitLimit = 5; // replaced by CrpgServerConfiguration.FriendlyFireReportMaxHits
    // private const int TeamHitDecaySeconds = 60; // Time it takes for a reported hit to no longer count towards kick // CrpgServerConfiguration.FriendlyFireReportDecaySeconds
    // private const int TeamHitReportWindowSeconds = 5; // Time window to report a team hit (0-200 based on compression i used in network message) // CrpgServerConfiguration.FriendlyFireReportWindowSeconds

    private class TeamHitRecord
    {
        public DateTime Time { get; set; }
        public NetworkCommunicator Victim { get; set; }
        public float Damage { get; set; }
        public bool WasReported { get; set; }
        public bool HasDecayed { get; set; }

        public TeamHitRecord(DateTime time, NetworkCommunicator victim, float damage, bool wasReported, bool hasDecayed = false)
        {
            Time = time;
            Victim = victim;
            Damage = damage;
            WasReported = wasReported;
            HasDecayed = hasDecayed;
        }

        public bool CheckAndMarkDecay(DateTime now, double decaySeconds)
        {
            if (!HasDecayed && (now - Time).TotalSeconds > decaySeconds)
            {
                HasDecayed = true;
            }

            return HasDecayed;
        }
    }

    private readonly Dictionary<NetworkCommunicator, List<TeamHitRecord>> _teamHitHistory = new();
    // Track which peer last team-damaged a specific peer
    private readonly Dictionary<NetworkCommunicator, NetworkCommunicator> _lastTeamHitBy = new();

    private MultiplayerRoundController? _roundController;
    public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();
    }

    public override void AfterStart()
    {
        _roundController = Mission.Current.GetMissionBehavior<MultiplayerRoundController>();
        if (_roundController != null)
        {
            _roundController.OnRoundStarted += OnRoundStarted;
        }
    }

    public override void OnRemoveBehavior()
    {
        if (_roundController != null)
        {
            _roundController.OnRoundStarted -= OnRoundStarted;
        }
    }

    public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
    {
        base.OnAgentHit(affectedAgent, affectorAgent, affectorWeapon, blow, attackCollisionData);

        if (!CrpgServerConfiguration.IsFriendlyFireReportEnabled)
        {
            return; // If control M reporting is disabled, do not process team hits
        }

        if (affectedAgent == null || affectorAgent == null || affectedAgent == affectorAgent)
        {
            Debug.Print("[FF Server] Invalid agents involved in hit.", 0, Debug.DebugColor.Red);
            return;
        }

        if (affectedAgent.IsMount) // Check if victim a mount
        {
            Debug.Print($"[FF Server] {affectedAgent.Name} is a mount", 0, Debug.DebugColor.Red);
            if (affectedAgent.RiderAgent != null && affectedAgent.RiderAgent.IsActive())
            {
                Debug.Print($"[FF Server] {affectedAgent.Name} is riding {affectedAgent.Name}", 0, Debug.DebugColor.Red);
                affectedAgent = affectedAgent.RiderAgent; // use the rider as the victim
            }
            else
            {
                Debug.Print($"[FF Server] friendly mount with no rider.", 0, Debug.DebugColor.Red);
                return; // ignore hits to mounts without riders
            }
        }

        if (affectorAgent.IsMount) // Check if the attacker agent is a mount
        {
            Debug.Print($"[FF Server] {affectorAgent.Name} is a mount", 0, Debug.DebugColor.Red);
            if (affectorAgent.RiderAgent != null && affectorAgent.RiderAgent.IsActive())
            {
                Debug.Print($"[FF Server] {affectorAgent.Name} is ridden by {affectorAgent.RiderAgent.Name}", 0, Debug.DebugColor.Red);
                affectorAgent = affectorAgent.RiderAgent; // use the rider as the attacker
            }
            else
            {
                Debug.Print($"[FF Server] friendly mount with no rider.", 0, Debug.DebugColor.Red);
                return; // ignore hits from mounts without riders
            }
        }

        // Check if both agents are player controlled, use rider (set above) if mount was the attacker or victim
        if (!affectedAgent.IsPlayerControlled || !affectorAgent.IsPlayerControlled)
        {
            Debug.Print($"[FF Server] {affectorAgent.Name} hit {affectedAgent.Name}, but one of them is not player controlled.", 0, Debug.DebugColor.Red);
            return;
        }

        // Same team
        if (affectedAgent.Team?.TeamIndex != affectorAgent.Team?.TeamIndex)
        {
            string weaponName = affectorWeapon.Item?.Name.ToString() ?? "Unknown Weapon";
            Debug.Print($"[FF Server] {affectorAgent.Name} hit {affectedAgent.Name} with {weaponName} from different teams.", 0, Debug.DebugColor.Red);
            return; // not a team hit
        }

        NetworkCommunicator? affectorNetworkPeer = affectorAgent.MissionPeer?.GetNetworkPeer(); // attacker
        NetworkCommunicator? affectedNetworkPeer = affectedAgent.MissionPeer?.GetNetworkPeer(); // victim

        if (affectorNetworkPeer == null || affectedNetworkPeer == null || affectorNetworkPeer == affectedNetworkPeer)
        {
            Debug.Print("[FF Server] No valid network peers for affector or affected agent, or they are the same peer.", 0, Debug.DebugColor.Red);
            return; // No network peers available or same peer
        }

        if (!_teamHitHistory.TryGetValue(affectorNetworkPeer, out var hitList))
        {
            hitList = new List<TeamHitRecord>();
            _teamHitHistory[affectorNetworkPeer] = hitList;
        }

        hitList.Add(new TeamHitRecord(DateTime.UtcNow, affectedNetworkPeer, attackCollisionData.InflictedDamage, false, false));

        // Track last team hitter for the affected agent
        _lastTeamHitBy[affectedNetworkPeer] = affectorNetworkPeer;

        if (GameNetwork.IsServer)
        {
            Debug.Print($"[FF Server] Sending FF Hit Message From Server to victim.", 0, Debug.DebugColor.Red);
            // Send a message From the server about the friendly hit
            GameNetwork.BeginModuleEventAsServer(affectedNetworkPeer);
            GameNetwork.WriteMessage(new FriendlyFireHitMessage(affectorAgent.Index, attackCollisionData.InflictedDamage, CrpgServerConfiguration.FriendlyFireReportWindowSeconds));
            GameNetwork.EndModuleEventAsServer();
        }
    }

    public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
    {
        base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
    }

    public override void OnPlayerDisconnectedFromServer(NetworkCommunicator networkPeer)
    {
        base.OnPlayerDisconnectedFromServer(networkPeer);

        // Remove hits where this peer was the attacker
        _teamHitHistory.Remove(networkPeer);

        // Remove hits where this peer was the victim inside other players' hit records
        foreach (var hitList in _teamHitHistory.Values)
        {
            hitList.RemoveAll(hit => hit.Victim == networkPeer);
        }

        // remove entries where this peer was the victim
        _lastTeamHitBy.Remove(networkPeer);

        // remove entries where this peer was the attacker
        var victims = _lastTeamHitBy
            .Where(kvp => kvp.Value == networkPeer)
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var victimPeer in victims)
        {
            SendClientDisplayMessage(victimPeer, $"[FF] Your last teamhit attacker {networkPeer.UserName} has left the match.", FriendlyFireMessageMode.TeamDamageReportError);
            _lastTeamHitBy.Remove(victimPeer);
        }
    }

    protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
    {
        if (GameNetwork.IsServer)
        {
            base.AddRemoveMessageHandlers(registerer);
            registerer.Register<FriendlyFireReportClientMessage>((peer, message) =>
            {
                OnTeamDamageReportReceived(peer, message);
                return true;
            });
        }
    }

    private void OnTeamDamageReportReceived(NetworkCommunicator peer, FriendlyFireReportClientMessage message)
    {
        if (!CrpgServerConfiguration.IsFriendlyFireReportEnabled)
        {
            SendClientDisplayMessage(peer, "[FF] Control M reporting is currently disabled on this server.", FriendlyFireMessageMode.TeamDamageReportError);
            return; // If control M reporting is disabled, do not process team hit reports
        }

        if (peer == null || !peer.IsConnectionActive)
        {
            Debug.Print("[FF Server] Received team damage report from an inactive peer.", 0, Debug.DebugColor.Red);
            return;
        }

        if (!_lastTeamHitBy.TryGetValue(peer, out NetworkCommunicator? attackingPeer))
        {
            Debug.Print($"[FF Server] No last team hit found for {peer.UserName}.", 0, Debug.DebugColor.Red);
            return; // No record of a team hit
        }

        if (attackingPeer == null || !attackingPeer.IsConnectionActive)
        {
            Debug.Print($"[FF Server] Attacking peer {attackingPeer?.UserName} is not active.", 0, Debug.DebugColor.Red);
            SendClientDisplayMessage(peer, "No active attacker found for your report.", FriendlyFireMessageMode.TeamDamageReportError);
            return; // Attacker is not active
        }

        // Process report here, e.g. log or notify
        Debug.Print($"[FF Server] Received team damage report from {peer.UserName}", 0, Debug.DebugColor.Red);

        // Mark the last hit as reported
        var recentHit = _teamHitHistory[attackingPeer]
     .LastOrDefault(h =>
         h.Victim == peer &&
         !h.WasReported &&
         (CrpgServerConfiguration.FriendlyFireReportWindowSeconds == 0 ||
         (DateTime.UtcNow - h.Time).TotalSeconds < CrpgServerConfiguration.FriendlyFireReportWindowSeconds));

        if (recentHit != null)
        {
            recentHit.WasReported = true;
        }
        else
        {
            Debug.Print($"[FF Server] No recent unreported team hit found for {attackingPeer.UserName} hitting {peer.UserName}.", 0, Debug.DebugColor.Red);
            SendClientDisplayMessage(peer, "No recent team hit found to report.", FriendlyFireMessageMode.TeamDamageReportError);
            return; // No recent unreported hit found
        }

        // Get Teamhits by attacker
        var (countActive, countDecayed, countNotReported) = GetReportedTeamHitBreakdown(peer);

        Debug.Print($"[FF Server] {attackingPeer.UserName} has {countActive} active reported team hits.", 0, Debug.DebugColor.Yellow);

        // Notify the attacker about the report (server side)
        SendClientDisplayMessage(attackingPeer, $"[FF] {peer.UserName} has reported you for team hitting them. You have {countActive}/{CrpgServerConfiguration.FriendlyFireReportMaxHits} team hits before getting kicked.", FriendlyFireMessageMode.TeamDamageReportForAttacker);

        // Notify the victim about the report
        SendClientDisplayMessage(peer, $"{attackingPeer.UserName} has been reported for team hitting you.");

        // Notify all admins about the report
        SendClientDisplayMessageToAdmins($"[FF] {attackingPeer.UserName} reported by {peer.UserName}. [{countActive}/{CrpgServerConfiguration.FriendlyFireReportMaxHits}] decayed: {countDecayed} not reported: {countNotReported}");

        if (countActive >= CrpgServerConfiguration.FriendlyFireReportMaxHits)
        {
            Debug.Print($"[FF Server] Kicking {attackingPeer.UserName} for exceeding team hit limit ({CrpgServerConfiguration.FriendlyFireReportMaxHits}).", 0, Debug.DebugColor.Green);
            SendClientDisplayMessage(attackingPeer, $"[FF] You have been kicked for exceeding the team hit limit of {CrpgServerConfiguration.FriendlyFireReportMaxHits}", FriendlyFireMessageMode.TeamDamageReportKick);
            SendClientDisplayMessageToAllExcept(attackingPeer, $"[FF] {attackingPeer.UserName} has been kicked for exceeding the team hit limit of {CrpgServerConfiguration.FriendlyFireReportMaxHits}.");

            KickHelper.Kick(attackingPeer, DisconnectType.KickedDueToFriendlyDamage);
        }
    }

    private void SendClientDisplayMessage(NetworkCommunicator peer, string displayText, FriendlyFireMessageMode messageMode = FriendlyFireMessageMode.Default)
    {
        if (peer == null || !peer.IsConnectionActive)
        {
            return;
        }

        if (!Enum.IsDefined(typeof(FriendlyFireMessageMode), messageMode))
        {
            Debug.Print($"[FF Server] Invalid MessageMode '{messageMode}' used in SendClientDisplayMessage.", 0, Debug.DebugColor.Red);
            messageMode = FriendlyFireMessageMode.Default; // fallback to safe default
        }

        GameNetwork.BeginModuleEventAsServer(peer);
        GameNetwork.WriteMessage(new FriendlyFireNotificationMessage(displayText, messageMode));
        GameNetwork.EndModuleEventAsServer();
    }

    private void SendClientDisplayMessageToAll(string displayText, FriendlyFireMessageMode messageMode = FriendlyFireMessageMode.TeamDamageReportForAll)
    {
        foreach (var peer in GameNetwork.NetworkPeers)
        {
            if (peer.IsConnectionActive)
            {
                SendClientDisplayMessage(peer, displayText, messageMode);
            }
        }
    }

    private void SendClientDisplayMessageToAllExcept(NetworkCommunicator excludedPeer, string displayText, FriendlyFireMessageMode messageMode = FriendlyFireMessageMode.Default)
    {
        foreach (var peer in GameNetwork.NetworkPeers)
        {
            if (peer.IsConnectionActive && peer != excludedPeer)
            {
                SendClientDisplayMessage(peer, displayText, messageMode);
            }
        }
    }

    private void SendClientDisplayMessageToAdmins(string displayText)
    {
        foreach (var peer in GameNetwork.NetworkPeers)
        {
            if (peer.IsConnectionActive)
            {
                var crpgUser = peer.GetComponent<CrpgPeer>()?.User;
                if (crpgUser != null && crpgUser.Role is CrpgUserRole.Moderator or CrpgUserRole.Admin)
                {
                    SendClientDisplayMessage(peer, displayText, FriendlyFireMessageMode.TeamDamageReportForAdmins);
                }
            }
        }
    }

    private (int activeReported, int decayedReported, int notReported) GetReportedTeamHitBreakdown(NetworkCommunicator peer)
    {
        if (peer == null || !_teamHitHistory.TryGetValue(peer, out var hitList))
        {
            return (0, 0, 0); // No hits recorded
        }

        int activeReported = 0;
        int decayedReported = 0;
        int notReported = 0;
        DateTime now = DateTime.UtcNow;

        foreach (var hit in hitList)
        {
            hit.CheckAndMarkDecay(now, CrpgServerConfiguration.FriendlyFireReportDecaySeconds);

            if (hit.WasReported)
            {
                if (hit.HasDecayed)
                {
                    decayedReported++;
                }
                else
                {
                    activeReported++;
                }
            }
            else
            {
                notReported++;
            }
        }

        Debug.Print($"[FF Server] {peer.UserName} teamhits: ActiveReported={activeReported}, DecayedReported={decayedReported}, NotReported={notReported}", 0, Debug.DebugColor.Red);

        return (activeReported, decayedReported, notReported);
    }

    private void OnRoundStarted()
    {
        _lastTeamHitBy.Clear(); // clear last person to hit a peer

        // mark all reported hits as decayed on round start but preserve data
        foreach (var hitList in _teamHitHistory.Values)
        {
            foreach (var hit in hitList)
            {
                if (hit.WasReported)
                {
                    hit.HasDecayed = true;
                }
            }
        }

        // _teamHitHistory.Clear(); // <-- clear all list of team hits per attacker peer

        Debug.Print("[FF Server] Round started, data cleared.", 0, Debug.DebugColor.Green);
    }
}
