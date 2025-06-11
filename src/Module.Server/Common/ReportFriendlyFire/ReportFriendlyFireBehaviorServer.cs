using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond;

namespace Crpg.Module.Common.ReportFriendlyFire;

internal class ReportFriendlyFireBehaviorServer : MissionNetwork
{
    // private const int TeamHitLimit = 5;
    private readonly Dictionary<NetworkCommunicator, int> _teamHitCounts = new();
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

        if (!CrpgServerConfiguration.IsControlMReportEnabled)
        {
            return; // If control M reporting is disabled, do not process team hits
        }

        if (affectedAgent == null || affectorAgent == null || affectedAgent == affectorAgent)
        {
            Debug.Print("[TeamHitTracker] Invalid agents involved in hit.", 0, Debug.DebugColor.Red);
            return;
        }

        if (!affectedAgent.IsPlayerControlled || !affectorAgent.IsPlayerControlled)
        {
            // Debug.Print($"[TeamHitTracker] {affectorAgent.Name} hit {affectedAgent.Name}, but one of them is not player controlled.", 0, Debug.DebugColor.Red);
            return;
        }

        if (affectedAgent.IsMount) // Check if victim a mount
        {
            Debug.Print($"[TeamHitTracker] {affectedAgent.Name} is a mount", 0, Debug.DebugColor.Red);
            if (affectedAgent.RiderAgent != null && affectedAgent.RiderAgent.IsActive())
            {
                Debug.Print($"[TeamHitTracker] {affectedAgent.Name} is riding {affectedAgent.Name}", 0, Debug.DebugColor.Red);
                affectedAgent = affectedAgent.RiderAgent; // use the rider as the victim
            }
            else
            {
                Debug.Print($"[TeamHitTracker] friendly mount with no rider.", 0, Debug.DebugColor.Red);
                return; // ignore hits to mounts without riders
            }
        }

        if (affectorAgent.IsMount) // Check if the attacker agent is a mount
        {
            Debug.Print($"[TeamHitTracker] {affectorAgent.Name} is a mount", 0, Debug.DebugColor.Red);
            if (affectorAgent.RiderAgent != null && affectorAgent.RiderAgent.IsActive())
            {
                Debug.Print($"[TeamHitTracker] {affectorAgent.Name} is ridden by {affectorAgent.RiderAgent.Name}", 0, Debug.DebugColor.Red);
                affectorAgent = affectorAgent.RiderAgent; // use the rider as the attacker
            }
            else
            {
                Debug.Print($"[TeamHitTracker] friendly mount with no rider.", 0, Debug.DebugColor.Red);
                return; // ignore hits from mounts without riders
            }
        }

        if (affectedAgent.Team?.TeamIndex != affectorAgent.Team?.TeamIndex)
        {
            string weaponName = affectorWeapon.Item?.Name.ToString() ?? "Unknown Weapon";
            Debug.Print($"[TeamHitTracker] {affectorAgent.Name} hit {affectedAgent.Name} with {weaponName} from different teams.", 0, Debug.DebugColor.Red);
            return; // not a team hit
        }

        NetworkCommunicator? affectorNetworkPeer = affectorAgent.MissionPeer?.GetNetworkPeer(); // attacker
        NetworkCommunicator? affectedNetworkPeer = affectedAgent.MissionPeer?.GetNetworkPeer(); // victim

        if (affectorNetworkPeer == null || affectedNetworkPeer == null || affectorNetworkPeer == affectedNetworkPeer)
        {
            Debug.Print("[TeamHitTracker] No valid network peers for affector or affected agent, or they are the same peer.", 0, Debug.DebugColor.Red);
            return; // No network peers available or same peer
        }

        // Track last team hitter for the affected agent
        _lastTeamHitBy[affectedNetworkPeer] = affectorNetworkPeer;

        if (GameNetwork.IsServer)
        {
            Debug.Print($"[TeamHitTracker] Sending Message From Server to victim.", 0, Debug.DebugColor.Red);
            // Send a message From the server about the friendly hit
            GameNetwork.BeginModuleEventAsServer(affectedNetworkPeer);
            GameNetwork.WriteMessage(new FriendlyHitServerMessage(affectorAgent.Index, attackCollisionData.InflictedDamage));
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

        _teamHitCounts.Remove(networkPeer);

        // remove entries where this peer was the victim
        _lastTeamHitBy.Remove(networkPeer);

        // remove entries where this peer was the attacker
        var victims = _lastTeamHitBy
            .Where(kvp => kvp.Value == networkPeer)
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var victimPeer in victims)
        {
            SendClientDisplayMessage(victimPeer, $"Your last team–hit attacker {networkPeer.UserName} has left the match.");
            _lastTeamHitBy.Remove(victimPeer);
        }
    }

    protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
    {
        if (GameNetwork.IsServer)
        {
            base.AddRemoveMessageHandlers(registerer);
            registerer.Register<TeamDamageReportClientMessage>((peer, message) =>
            {
                OnTeamDamageReportReceived(peer, message);
                return true;
            });
        }
    }

    private void OnTeamDamageReportReceived(NetworkCommunicator peer, TeamDamageReportClientMessage message)
    {
        if (!CrpgServerConfiguration.IsControlMReportEnabled)
        {
            SendClientDisplayMessage(peer, "Control M reporting is currently disabled on this server.");
            return; // If control M reporting is disabled, do not process team hit reports
        }

        if (peer == null || !peer.IsConnectionActive)
        {
            Debug.Print("[TeamHitTracker] Received team damage report from an inactive peer.", 0, Debug.DebugColor.Red);
            return;
        }

        if (!_lastTeamHitBy.TryGetValue(peer, out NetworkCommunicator? attackingPeer))
        {
            Debug.Print($"[Server] No last team hit found for {peer.UserName}.", 0, Debug.DebugColor.Red);
            return; // No record of a team hit
        }

        if (attackingPeer == null || !attackingPeer.IsConnectionActive)
        {
            Debug.Print($"[Server] Attacking peer {attackingPeer?.UserName} is not active.", 0, Debug.DebugColor.Red);
            SendClientDisplayMessage(peer, "No active attacker found for your report.");
            return; // Attacker is not active
        }

        // Process report here, e.g. log or notify
        Debug.Print($"[Server] Received team damage report from {peer.UserName}", 0, Debug.DebugColor.Red);

        // Increment hit count for the hitting player
        if (_teamHitCounts.TryGetValue(attackingPeer, out int currentCount))
        {
            _teamHitCounts[attackingPeer] = currentCount + 1;
        }
        else
        {
            _teamHitCounts[attackingPeer] = 1;
        }

        int count = _teamHitCounts[attackingPeer];
        Debug.Print($"[TeamHitTracker] {attackingPeer.UserName} has {count} team hits.", 0, Debug.DebugColor.Yellow);

        // Notify the attacker about the report (server side)
        SendClientDisplayMessage(attackingPeer, $"{peer.UserName} reported a team hit by you. You have {count}/{CrpgServerConfiguration.ControlMReportMaxHitCount} team hits before kick.");

        // Notify the victim about the report
        SendClientDisplayMessage(peer, $"{attackingPeer.UserName} has been reported for team hitting you.");

        if (count >= CrpgServerConfiguration.ControlMReportMaxHitCount)
        {
            Debug.Print($"[TeamHitTracker] Kicking {attackingPeer.UserName} for exceeding team hit limit ({CrpgServerConfiguration.ControlMReportMaxHitCount}).", 0, Debug.DebugColor.Green);
            KickHelper.Kick(attackingPeer, DisconnectType.KickedDueToFriendlyDamage);
        }
    }

    private void SendClientDisplayMessage(NetworkCommunicator peer, string displayText)
    {
        if (peer == null || !peer.IsConnectionActive)
        {
            return;
        }

        GameNetwork.BeginModuleEventAsServer(peer);
        GameNetwork.WriteMessage(new FriendlyFireTextServerMessage(displayText));
        GameNetwork.EndModuleEventAsServer();
    }

    private void OnRoundStarted()
    {
        _teamHitCounts.Clear();
        _lastTeamHitBy.Clear();
        Debug.Print("[TeamHitTracker] Round started, data cleared.", 0, Debug.DebugColor.Green);
    }
}
