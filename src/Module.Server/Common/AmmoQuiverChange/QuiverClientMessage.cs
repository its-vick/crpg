using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Crpg.Module.Common.AmmoQuiverChange;

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
internal sealed class QuiverClientMessage : GameNetworkMessage
{
    private static readonly CompressionInfo.Integer QuiverActionCompression = new(0, 10, true);
    public QuiverClientMessageAction Action { get; private set; }

    public QuiverClientMessage()
    {
        Action = QuiverClientMessageAction.None;
    }

    public QuiverClientMessage(QuiverClientMessageAction action)
    {
        Action = action;
    }

    protected override bool OnRead()
    {
        bool bufferReadValid = true;
        Action = (QuiverClientMessageAction)ReadIntFromPacket(QuiverActionCompression, ref bufferReadValid);
        return bufferReadValid;
    }

    protected override void OnWrite()
    {
        WriteIntToPacket((int)Action, QuiverActionCompression);
    }

    protected override MultiplayerMessageFilter OnGetLogFilter()
    {
        return MultiplayerMessageFilter.General;
    }

    protected override string OnGetLogFormat()
    {
        return $"QuiverClientMessage - Action: {Action}";
    }
}
