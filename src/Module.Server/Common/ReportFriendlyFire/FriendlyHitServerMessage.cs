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
        AttackerAgentIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.MaxNumberOfPlayersCompressionInfo, ref bufferReadValid);
        Damage = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.DebugIntNonCompressionInfo, ref bufferReadValid);
        return bufferReadValid;
    }

    protected override void OnWrite()
    {
        GameNetworkMessage.WriteIntToPacket(AttackerAgentIndex, CompressionBasic.MaxNumberOfPlayersCompressionInfo);
        GameNetworkMessage.WriteIntToPacket(Damage, CompressionBasic.DebugIntNonCompressionInfo);
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
