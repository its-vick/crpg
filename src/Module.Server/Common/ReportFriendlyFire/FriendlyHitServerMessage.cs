using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Crpg.Module.Common.ReportFriendlyFire;

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
internal sealed class FriendlyHitServerMessage : GameNetworkMessage
{
    public int AttackerAgentIndex { get; private set; }
    public int Damage { get; private set; }

    public FriendlyHitServerMessage()
    {
        // Default constructor for deserialization
    }

    public FriendlyHitServerMessage(int attackerAgentIndex, int damage)
    {
        AttackerAgentIndex = attackerAgentIndex;
        Damage = damage;
    }

    protected override bool OnRead()
    {
        bool bufferReadValid = true;
        AttackerAgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
        Damage = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.AgentHitDamageCompressionInfo, ref bufferReadValid);
        return bufferReadValid;
    }

    protected override void OnWrite()
    {
        GameNetworkMessage.WriteAgentIndexToPacket(AttackerAgentIndex);
        GameNetworkMessage.WriteIntToPacket(Damage, CompressionBasic.AgentHitDamageCompressionInfo);
    }

    protected override MultiplayerMessageFilter OnGetLogFilter()
    {
        return MultiplayerMessageFilter.General;
    }

    protected override string OnGetLogFormat()
    {
        return $"[FriendlyFire] Hit by agent index {AttackerAgentIndex} for {Damage} damage";
    }
}
