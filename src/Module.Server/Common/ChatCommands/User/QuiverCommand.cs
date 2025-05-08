using Crpg.Module.Common.AmmoQuiverChange;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Crpg.Module.Common.ChatCommands.User;

internal class QuiverCommand : ChatCommand
{
    public QuiverCommand(ChatCommandsComponent chatComponent)
        : base(chatComponent)
    {
        Name = "quiver";
        Description = $"'{ChatCommandsComponent.CommandPrefix}{Name} [off | on | hidegui | showgui] to change quiver settings";
        Overloads = new CommandOverload[]
        {
            new(new[] { ChatCommandParameterType.String }, ExecuteSuccess),
        };
    }

    private void ExecuteSuccess(NetworkCommunicator fromPeer, object[] arguments)
    {
        string message = (string)arguments[0];
        message = message.ToLower();

        // Toggle Quiver Change Off
        if (message == "off")
        {
            string outmessage = $"Quiver change feature disabled for you. Use '{ChatCommandsComponent.CommandPrefix}{Name} on' to enable it.";
            ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorSuccess, outmessage);
            ChangeQuiverSettings(fromPeer, AmmoQuiverChangeSettingsAction.DisableQuiverChange);
        }

        // Toggle Quiver Change On
        else if (message == "on")
        {
            string outmessage = $"Quiver change feature enabled for you. Use '{ChatCommandsComponent.CommandPrefix}{Name} off' to disable it.";
            ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorSuccess, outmessage);
            ChangeQuiverSettings(fromPeer, AmmoQuiverChangeSettingsAction.EnableQuiverChange);
        }

        // Hide Quiver GUI
        else if (message == "hidegui")
        {
            string outmessage = $"Quiver GUI hidden for you. Use '{ChatCommandsComponent.CommandPrefix}{Name} showgui' to show it again.";
            ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorSuccess, outmessage);
            ChangeQuiverSettings(fromPeer, AmmoQuiverChangeSettingsAction.HideQuiverGui);
        }

        // Show Quiver GUI
        else if (message == "showgui")
        {
            string outmessage = $"Quiver GUI shown for you. Use '{ChatCommandsComponent.CommandPrefix}{Name} hidegui' to hide it again.";
            ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorSuccess, outmessage);
            ChangeQuiverSettings(fromPeer, AmmoQuiverChangeSettingsAction.ShowQuiverGui);
        }

        // Invalid parameter
        else
        {
            string outmessage = $"Invalid parameter '{message}'. Use '{ChatCommandsComponent.CommandPrefix}{Name} [off | on | hidegui | showgui]' to change quiver settings.";
            ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorFatal, outmessage);
        }
    }

    private void ChangeQuiverSettings(NetworkCommunicator fromPeer, AmmoQuiverChangeSettingsAction action)
    {
        AmmoQuiverChangeSettingsServerMessage quiverMessage = new(action);

        GameNetwork.BeginModuleEventAsServer(fromPeer);
        GameNetwork.WriteMessage(quiverMessage);
        GameNetwork.EndModuleEventAsServer();
    }
}
