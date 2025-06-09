using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common.FriendlyFireReport;

internal class FriendlyFireReportClientBehavior : MissionNetwork
{
    private int _reportWindowSeconds = 0; // default unlimited, updated via FriendlyFireHitMessage
    private bool _ctrlMWasPressed;
    private DateTime? _lastHitMessageTime;
    private int? _lastAttackerAgentIndex;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

        if (_lastHitMessageTime == null)
        {
            // InformationManager.DisplayMessage(new InformationMessage("No team hit reported. _lastHitMessageTime was null.", Colors.Red));
            return;
        }

        // time window to report an attack is not unlimited if > 0
        if (_reportWindowSeconds > 0)
        {
            double elapsedSeconds = (DateTime.UtcNow - _lastHitMessageTime.Value).TotalSeconds;
            if (elapsedSeconds > _reportWindowSeconds)
            {
                if (_lastAttackerAgentIndex == null || Mission.Current == null)
                {
                    return;
                }

                Agent agent = Mission.Current.FindAgentWithIndex((int)_lastAttackerAgentIndex);
                if (agent == null)
                {
                    return;
                }

                string name = agent?.Name?.ToString() ?? "Unknown";

                // Expired window, reset timer
                _lastHitMessageTime = null;
                _lastAttackerAgentIndex = null;

                InformationManager.DisplayMessage(new InformationMessage($"[FF] Time expired to report {name} for teamhit.", Colors.Yellow));
                return;
            }
        }

        bool isCtrlDown = Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl);
        bool isMPressed = Input.IsKeyPressed(InputKey.M);

        if (isCtrlDown && isMPressed && !_ctrlMWasPressed)
        {
            _ctrlMWasPressed = true;
            HandleCtrlMPressed();
            _lastHitMessageTime = null; // Reset after reporting
            _lastAttackerAgentIndex = null;
        }

        if (!isCtrlDown || !Input.IsKeyDown(InputKey.M))
        {
            _ctrlMWasPressed = false;
        }
    }

    protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
    {
        if (GameNetwork.IsClient)
        {
            base.AddRemoveMessageHandlers(registerer);
            registerer.Register<FriendlyFireHitMessage>(HandleFriendlyFireHitMessage);
            registerer.Register<FriendlyFireNotificationMessage>(HandleFriendlyFireTextMessage);
        }
    }

    private void HandleCtrlMPressed()
    {
        // InformationManager.DisplayMessage(new InformationMessage("[FF] Sending Team Damage Report to Server.", Colors.Yellow));

        GameNetwork.BeginModuleEventAsClient();
        GameNetwork.WriteMessage(new FriendlyFireReportClientMessage());
        GameNetwork.EndModuleEventAsClient();
    }

    private string GetAgentNameByIndex(int agentIndex)
    {
        Agent? agent = Mission.Current?.Agents?.FirstOrDefault(a => a.Index == agentIndex);
        return agent?.Name?.ToString() ?? "Unknown";
    }

    private void HandleFriendlyFireHitMessage(FriendlyFireHitMessage message)
    {
        // Update _reportWindowSeconds set in Crpg.serverConfiguration
        _reportWindowSeconds = message.ReportWindow;
        _lastAttackerAgentIndex = message.AttackerAgentIndex;

        if (_lastAttackerAgentIndex == null || Mission.Current == null)
        {
            return;
        }

        Agent agent = Mission.Current.FindAgentWithIndex((int)_lastAttackerAgentIndex);
        if (agent == null)
        {
            return;
        }

        string name = agent?.Name?.ToString() ?? "Unknown";
        string outString = $"[FF] Team hit by {name} (Dmg: {message.Damage}). Press Ctrl+M to mark that you believe this was intentional.";

        if (_reportWindowSeconds > 0)
        {
            outString = $"[FF] Team hit by {name} (Dmg: {message.Damage}). Press Ctrl+M to mark that you believe this was intentional. {_reportWindowSeconds} seconds remaining.";
        }

        InformationManager.DisplayMessage(new InformationMessage(outString, Colors.Red));

        // New team hit → allow a fresh Ctrl+M
        _ctrlMWasPressed = false;
        // Set the timer for when the report window opens
        _lastHitMessageTime = DateTime.UtcNow;
    }

    private void HandleFriendlyFireTextMessage(FriendlyFireNotificationMessage message)
    {
        FriendlyFireMessageMode mode = message.Mode;
        Color msgColor;
        switch (mode)
        {
            case FriendlyFireMessageMode.Default:
                msgColor = Colors.Yellow;
                break;
            case FriendlyFireMessageMode.TeamDamageReportForVictim:
                msgColor = Colors.Red;
                break;
            case FriendlyFireMessageMode.TeamDamageReportForAdmins:
                msgColor = Colors.Magenta;
                break;
            case FriendlyFireMessageMode.TeamDamageReportForAttacker:
                msgColor = Colors.Yellow;
                break;
            case FriendlyFireMessageMode.TeamDamageReportKick:
                msgColor = Colors.Magenta;
                break;
            case FriendlyFireMessageMode.TeamDamageReportError:
                msgColor = Colors.Yellow;
                break;
            default:
                msgColor = Colors.Yellow;
                break;
        }

        InformationManager.DisplayMessage(new InformationMessage(message.Message, msgColor));
    }
}
