using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common.ReportFriendlyFire;

internal class ReportFriendlyFireBehaviorClient : MissionNetwork
{
    private const double ReportWindowSeconds = 5.0;
    private bool _ctrlMWasPressed;
    private DateTime? _lastHitMessageTime;

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

        double elapsedSeconds = (DateTime.UtcNow - _lastHitMessageTime.Value).TotalSeconds;
        if (elapsedSeconds > ReportWindowSeconds)
        {
            // Expired window, reset timer
            _lastHitMessageTime = null;
            InformationManager.DisplayMessage(new InformationMessage("Report friendly fire time expired.", Colors.Red));
            return;
        }

        bool isCtrlDown = Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl);
        bool isMPressed = Input.IsKeyPressed(InputKey.M);

        if (isCtrlDown && isMPressed && !_ctrlMWasPressed)
        {
            _ctrlMWasPressed = true;
            HandleCtrlMPressed();
            _lastHitMessageTime = null; // Reset after reporting
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
            registerer.Register<FriendlyHitServerMessage>(HandleFriendlyHitMessage);
            registerer.Register<FriendlyFireTextServerMessage>(HandleFriendlyFireTextMessage);
        }
    }

    private void HandleCtrlMPressed()
    {
        InformationManager.DisplayMessage(new InformationMessage("Sending Team Damage Report to Server.", Colors.Yellow));

        GameNetwork.BeginModuleEventAsClient();
        GameNetwork.WriteMessage(new TeamDamageReportClientMessage());
        GameNetwork.EndModuleEventAsClient();
    }

    private void HandleFriendlyHitMessage(FriendlyHitServerMessage message)
    {
        Agent? attacker = Mission.Current?.Agents.FirstOrDefault(a => a.Index == message.AttackerAgentIndex);
        string name = attacker?.Name?.ToString() ?? "Unknown";

        InformationManager.DisplayMessage(new InformationMessage($"Team-hit by {name} for {message.Damage} damage. Press Ctrl+M to report within {ReportWindowSeconds} seconds.", Colors.Red));

        // New team hit → allow a fresh Ctrl+M
        _ctrlMWasPressed = false;
        // Set the timer for when the report window opens
        _lastHitMessageTime = DateTime.UtcNow;
    }

    private void HandleFriendlyFireTextMessage(FriendlyFireTextServerMessage message)
    {
        InformationManager.DisplayMessage(new InformationMessage(message.Message, Colors.Red));
    }
}
