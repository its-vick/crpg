using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Crpg.Module.Common.ReportFriendlyFire;

[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
internal sealed class TeamDamageReportClientMessage : GameNetworkMessage
{
    public TeamDamageReportClientMessage()
    {
    }

    protected override bool OnRead()
    {
        return true; // No data to read, always valid
    }

    protected override void OnWrite()
    {
        // No data to write
    }

    protected override MultiplayerMessageFilter OnGetLogFilter()
    {
        return MultiplayerMessageFilter.General;
    }

    protected override string OnGetLogFormat()
    {
        return "TeamDamageReportClientMessage - Report Last Teamhit";
    }
}
