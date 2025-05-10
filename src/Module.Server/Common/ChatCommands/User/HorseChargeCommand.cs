using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common.ChatCommands.User;

internal class HorseChargeCommand : ChatCommand
{
    private readonly string[] flagNames =
    {
        "friend",
        "enemy",
        "global",
    };
    public HorseChargeCommand(ChatCommandsComponent chatComponent)
        : base(chatComponent)
    {
        Name = "hc";
        string listarray = string.Join(" ", flagNames);
        Description = $"'{ChatCommandsComponent.CommandPrefix}{Name} [flag] [true|false]' Available flags: {listarray}\n" +
            $"global:{ChargeDamageControl.DisableAllChargeDamage} enemy:{ChargeDamageControl.AllowChargeEnemies} friend:{ChargeDamageControl.AllowChargeFriends}";

        Overloads = new CommandOverload[]
        {
            new(new[] { ChatCommandParameterType.String, ChatCommandParameterType.String }, ExecuteSuccess),
        };
    }

    private void ExecuteSuccess(NetworkCommunicator fromPeer, object[] arguments)
    {
        void SendStatus()
        {
            string status = $"globaldisable:{ChargeDamageControl.DisableAllChargeDamage} " +
                            $"enemy:{ChargeDamageControl.AllowChargeEnemies} " +
                            $"friend:{ChargeDamageControl.AllowChargeFriends}";
            ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorWarning, status);
        }

        if (arguments.Length <= 1)
        {
            ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorFatal,
                $"Missing argument. Usage: !hc [flag] [true|false]. Available flags: {string.Join(", ", flagNames)}");
            SendStatus();
            return;
        }

        string strFlag = ((string)arguments[0]).ToLower();
        string strBool = ((string)arguments[1]).ToLower();

        if (!bool.TryParse(strBool, out bool boolFlag))
        {
            ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorFatal,
                $"Invalid argument: {strBool}. Expected 'true' or 'false'.");
            SendStatus();
            return;
        }

        bool isValidFlag = true;

        switch (strFlag)
        {
            case "friend":
                ChargeDamageControl.AllowChargeFriends = boolFlag;
                break;
            case "enemy":
                ChargeDamageControl.AllowChargeEnemies = boolFlag;
                break;
            case "global":
                ChargeDamageControl.DisableAllChargeDamage = boolFlag;
                break;
            default:
                isValidFlag = false;
                ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorFatal,
                    $"Invalid flag: {strFlag}. Available flags: {string.Join(", ", flagNames)}");
                break;
        }

        if (isValidFlag)
        {
            ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorSuccess, $"Set {strFlag} to {strBool}.");
        }

        SendStatus();
    }
}
