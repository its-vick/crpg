using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Crpg.Module.Common.ReportFriendlyFire;

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
internal sealed class FriendlyFireTextServerMessage : GameNetworkMessage
{
    public string Message { get; private set; }

    public FriendlyFireTextServerMessage()
    {
        Message = string.Empty; // Default constructor for deserialization
    }

    public FriendlyFireTextServerMessage(string message)
    {
        Message = message;
    }

    protected override bool OnRead()
    {
        bool bufferReadValid = true;
        Message = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
        return bufferReadValid;
    }

    protected override void OnWrite()
    {
        GameNetworkMessage.WriteStringToPacket(Message);
    }

    protected override MultiplayerMessageFilter OnGetLogFilter()
    {
        return MultiplayerMessageFilter.General;
    }

    protected override string OnGetLogFormat()
    {
        return $"[FriendlyFireTextServerMessage] {Message}";
    }
}
