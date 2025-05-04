using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Crpg.Module.Common.AmmoQuiverChange;

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
internal sealed class QuiverServerMessage : GameNetworkMessage
{
    private static readonly CompressionInfo.Integer QuiverActionCompression = new(0, 10, true);

    public QuiverServerMessageAction Action { get; private set; }

    public QuiverServerMessage()
    {
        Action = QuiverServerMessageAction.None;
    }

    public QuiverServerMessage(QuiverServerMessageAction action)
    {
        Action = action;
    }

    protected override bool OnRead()
    {
        bool bufferReadValid = true;
        Action = (QuiverServerMessageAction)ReadIntFromPacket(QuiverActionCompression, ref bufferReadValid);
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
        return $"QuiverServerMessage - Action: {Action}";
    }
}
