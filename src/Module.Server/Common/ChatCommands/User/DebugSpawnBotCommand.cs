using System.Linq;
using Crpg.Module.Common;
using Microsoft.VisualBasic;
using Mono.Cecil;
using NetworkMessages.FromServer;

using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Crpg.Module.Common.ChatCommands.User;

internal class DebugSpawnBotCommand : ChatCommand
{
    private readonly CrpgConstants constants = new(); // Create a new constants object

    public DebugSpawnBotCommand(ChatCommandsComponent chatComponent)
        : base(chatComponent)
    {
        Name = "bot";
        Overloads = new CommandOverload[]
         {
            new(Array.Empty<ChatCommandParameterType>(), ExecuteSuccess),
         };
    }

    private void ExecuteSuccess(NetworkCommunicator fromPeer, object[] arguments)
    {
        ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorSuccess, "Attempting to take bot and put in front!");

        Agent? closestEnemyBot = null;
        Agent peerAgent = fromPeer.ControlledAgent;
        float closestDistanceSquared = float.MaxValue;
        Vec3 myPosition = peerAgent.Position;

        foreach (Agent agent in Mission.Current.Agents)
        {
            if (agent.IsHuman && agent.IsAIControlled && !agent.IsMainAgent && agent.Team != null && agent.Team.IsEnemyOf(peerAgent.Team))
            {
                float distanceSquared = myPosition.DistanceSquared(agent.Position);
                if (distanceSquared < closestDistanceSquared)
                {
                    closestDistanceSquared = distanceSquared;
                    closestEnemyBot = agent;
                }
            }
        }

        // Now closestEnemyBot is the nearest enemy bot, or null if none were found.
        if (closestEnemyBot != null)
        {
            InformationManager.DisplayMessage(new InformationMessage($"Found closest enemy bot: {closestEnemyBot.Name} at distance {TaleWorlds.Library.MathF.Sqrt(closestDistanceSquared):F1} meters."));
        }
        else
        {
            InformationManager.DisplayMessage(new InformationMessage("No enemy bots found!"));
            return;
        }

        closestEnemyBot.SetTeam(Mission.Current.DefenderTeam, true);

        SetAgentInFrontOfPlayer(peerAgent, closestEnemyBot);

        // FindAndTakeOverNearestHorse(peerAgent);
    }

    private void SetAgentInFrontOfPlayer(Agent playerAgent, Agent movedAgent)
    {
        // Get the agent's current position and forward direction
        Vec3 agentPosition = playerAgent.Position;
        Vec3 forwardDirection = playerAgent.LookDirection.NormalizedCopy();

        // Calculate spawn position 10 meters in front
        float distance = 10.0f; // 10 meters
        Vec3 spawnPosition = agentPosition + (forwardDirection * distance);

        // Adjust spawn position to ground height
        Vec2 spawnPosition2D = new(spawnPosition.x, spawnPosition.y);
        Mission.Current.Scene.GetTerrainHeightAndNormal(spawnPosition2D, out float groundHeight, out Vec3 groundNormal);
        spawnPosition.z = groundHeight; // Set to ground level

        movedAgent.TeleportToPosition(spawnPosition);
    }

    private void FindAndTakeOverNearestHorse(Agent playerAgent)
    {
        if (playerAgent == null || !playerAgent.IsActive())
        {
            return;
        }

        Agent? closestHorse = null;
        float closestDistanceSquared = float.MaxValue;
        Vec3 playerPos = playerAgent.Position;

        foreach (Agent agent in Mission.Current.AllAgents)
        {
            if (agent.IsMount && agent.IsActive())
            {
                float distanceSquared = agent.Position.DistanceSquared(playerPos);
                if (distanceSquared < closestDistanceSquared)
                {
                    closestDistanceSquared = distanceSquared;
                    closestHorse = agent;
                }
            }
        }

        if (closestHorse == null)
        {
            Debug.Print("No horses found.");
            return;
        }

        // Kill the rider if present
        if (closestHorse.RiderAgent != null && closestHorse.RiderAgent.IsActive() && closestHorse.RiderAgent != playerAgent)
        {
            Agent rider = closestHorse.RiderAgent;
            Blow blow = new Blow(playerAgent.Index)
            {
                DamageType = DamageTypes.Cut,
                BaseMagnitude = 1000f,
                Direction = Vec3.Up,
                VictimBodyPart = BoneBodyPartType.Chest,
                DamageCalculated = true,
            };
            blow.InflictedDamage = (int)blow.BaseMagnitude;

            rider.Die(blow);
        }

        // Move horse to player's position to ensure mount range
        closestHorse.TeleportToPosition(playerAgent.Position);

        // Mount the player
        playerAgent.Mount(closestHorse);

        Debug.Print($"PlayerAgent mounted horse at distance {TaleWorlds.Library.MathF.Sqrt(closestDistanceSquared)}");
    }
}
