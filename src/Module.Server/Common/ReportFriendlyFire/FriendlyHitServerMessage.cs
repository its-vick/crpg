using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Crpg.Module.Common.ReportFriendlyFire;

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
internal sealed class FriendlyHitServerMessage : GameNetworkMessage
{
    public static CompressionInfo.Integer AgentIndexCompressionInfo = new CompressionInfo.Integer(-1, 1024);
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
        AttackerAgentIndex = GameNetworkMessage.ReadIntFromPacket(AgentIndexCompressionInfo, ref bufferReadValid);
        Damage = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.AgentHitDamageCompressionInfo, ref bufferReadValid);
        return bufferReadValid;
    }

    protected override void OnWrite()
    {
        GameNetworkMessage.WriteIntToPacket(AttackerAgentIndex, AgentIndexCompressionInfo);
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
