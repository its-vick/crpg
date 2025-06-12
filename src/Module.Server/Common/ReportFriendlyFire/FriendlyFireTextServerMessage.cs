using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Crpg.Module.Common.ReportFriendlyFire;

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
internal sealed class FriendlyFireTextServerMessage : GameNetworkMessage
{
    public string Message { get; private set; }
    public MessageModes Mode { get; private set; }

    public FriendlyFireTextServerMessage()
    {
        Message = string.Empty; // Default constructor for deserialization
        Mode = MessageModes.Default; // Default mode
    }

    public FriendlyFireTextServerMessage(string message, MessageModes mode)
    {
        Message = message;
        Mode = mode;
    }

    protected override bool OnRead()
    {
        bool bufferReadValid = true;
        Message = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
        Mode = (MessageModes)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, sizeof(MessageModes), true), ref bufferReadValid);
        return bufferReadValid;
    }

    protected override void OnWrite()
    {
        GameNetworkMessage.WriteStringToPacket(Message);
        GameNetworkMessage.WriteIntToPacket((int)Mode, new CompressionInfo.Integer(0, sizeof(MessageModes), true));
    }

    protected override MultiplayerMessageFilter OnGetLogFilter()
    {
        return MultiplayerMessageFilter.General;
    }

    protected override string OnGetLogFormat()
    {
        return $"[FriendlyFireTextServerMessage] ({Mode}) {Message}";
    }
}
