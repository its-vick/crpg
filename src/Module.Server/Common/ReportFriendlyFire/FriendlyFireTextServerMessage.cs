using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Crpg.Module.Common.ReportFriendlyFire;

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
internal sealed class FriendlyFireTextServerMessage : GameNetworkMessage
{
    public string Message { get; private set; }
    public FriendlyFireTextColors Color { get; private set; }

    public FriendlyFireTextServerMessage()
    {
        Message = string.Empty; // Default constructor for deserialization
        Color = FriendlyFireTextColors.Red; // Default color
    }

    public FriendlyFireTextServerMessage(string message, FriendlyFireTextColors color)
    {
        Message = message;
        Color = color;
    }

    protected override bool OnRead()
    {
        bool bufferReadValid = true;
        Message = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
        Color = (FriendlyFireTextColors)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, sizeof(FriendlyFireTextColors), true), ref bufferReadValid);
        return bufferReadValid;
    }

    protected override void OnWrite()
    {
        GameNetworkMessage.WriteStringToPacket(Message);
        GameNetworkMessage.WriteIntToPacket((int)Color, new CompressionInfo.Integer(0, sizeof(FriendlyFireTextColors), true));
    }

    protected override MultiplayerMessageFilter OnGetLogFilter()
    {
        return MultiplayerMessageFilter.General;
    }

    protected override string OnGetLogFormat()
    {
        return $"[FriendlyFireTextServerMessage] ({Color}) {Message}";
    }
}
