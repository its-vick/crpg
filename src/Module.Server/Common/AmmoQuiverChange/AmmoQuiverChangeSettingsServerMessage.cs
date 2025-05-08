using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Crpg.Module.Common.AmmoQuiverChange;

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
internal sealed class AmmoQuiverChangeSettingsServerMessage : GameNetworkMessage
{
    private static readonly CompressionInfo.Integer QuiverActionCompression = new(0, 10, true);

    public AmmoQuiverChangeSettingsAction Action { get; private set; }

    public AmmoQuiverChangeSettingsServerMessage()
    {
        Action = AmmoQuiverChangeSettingsAction.None;
    }

    public AmmoQuiverChangeSettingsServerMessage(AmmoQuiverChangeSettingsAction action)
    {
        Action = action;
    }

    protected override bool OnRead()
    {
        bool bufferReadValid = true;
        Action = (AmmoQuiverChangeSettingsAction)ReadIntFromPacket(QuiverActionCompression, ref bufferReadValid);
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

public enum AmmoQuiverChangeSettingsAction : int
{
    None = 0,
    DisableQuiverChange = 1,
    EnableQuiverChange = 2,
    HideQuiverGui = 3,
    ShowQuiverGui = 4,
}
