using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common.ChatCommands.User;

internal class HorseChargeCommand : ChatCommand
{
    private readonly string[] flagNames =
    {
        "friendly",
        "enemy",
        "global",
    };
    public HorseChargeCommand(ChatCommandsComponent chatComponent)
        : base(chatComponent)
    {
        Name = "hc";
        string listarray = string.Join(" ", flagNames);
        Description = $"'{ChatCommandsComponent.CommandPrefix}{Name} [flag] [true|false]' - Change horse charge damage behavior. Available flags: {listarray}";
        Overloads = new CommandOverload[]
        {
            new(new[] { ChatCommandParameterType.String, ChatCommandParameterType.String }, ExecuteSuccess),
        };
    }

    private void ExecuteSuccess(NetworkCommunicator fromPeer, object[] arguments)
    {
        string outmessage = string.Empty;

        if (arguments.Length < 1)
        {
            outmessage = $"Missing argument. Usage: [flag] [true|false]. Available flags: {string.Join(", ", flagNames)}";
            ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorFatal, outmessage);
            outmessage = $"AllowChargeFriends={ChargeDamageControl.AllowChargeFriends}, DisableChargeEnemies={ChargeDamageControl.DisableChargeEnemies}, DisableAllChargeDamage={ChargeDamageControl.DisableAllChargeDamage}";
            ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorSuccess, outmessage);
            return;
        }

        string strFlag = ((string)arguments[0]).ToLower();

        // Only one argument passed — show current flag value
        if (arguments.Length == 1)
        {
            switch (strFlag)
            {
                case "friendly":
                    outmessage = $"ChargeDamageControl | AllowChargeFriends is: {ChargeDamageControl.AllowChargeFriends}";
                    break;
                case "enemy":
                    outmessage = $"ChargeDamageControl | DisableChargeEnemies is: {ChargeDamageControl.DisableChargeEnemies}";
                    break;
                case "global":
                    outmessage = $"ChargeDamageControl | DisableAllChargeDamage is: {ChargeDamageControl.DisableAllChargeDamage}";
                    break;
                default:
                    outmessage = $"Invalid flag: {strFlag}. Available flags: {string.Join(", ", flagNames)}";
                    break;
            }

            ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorSuccess, outmessage);
            outmessage = $"AllowChargeFriends={ChargeDamageControl.AllowChargeFriends}, DisableChargeEnemies={ChargeDamageControl.DisableChargeEnemies}, DisableAllChargeDamage={ChargeDamageControl.DisableAllChargeDamage}";
            ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorSuccess, outmessage);
            return;
        }

        // Expecting 2 arguments from here
        string strBool = ((string)arguments[1]).ToLower();
        bool boolFlag;

        if (strBool == "true")
        {
            boolFlag = true;
        }
        else if (strBool == "false")
        {
            boolFlag = false;
        }
        else
        {
            outmessage = $"Invalid argument: {strBool}. Expected 'true' or 'false'.";
            ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorFatal, outmessage);
            outmessage = $"AllowChargeFriends={ChargeDamageControl.AllowChargeFriends}, DisableChargeEnemies={ChargeDamageControl.DisableChargeEnemies}, DisableAllChargeDamage={ChargeDamageControl.DisableAllChargeDamage}";
            ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorSuccess, outmessage);
            return;
        }

        switch (strFlag)
        {
            case "friendly":
                ChargeDamageControl.AllowChargeFriends = boolFlag;
                break;
            case "enemy":
                ChargeDamageControl.DisableChargeEnemies = boolFlag;
                break;
            case "global":
                ChargeDamageControl.DisableAllChargeDamage = boolFlag;
                break;
            default:
                outmessage = $"Invalid flag: {strFlag}. Available flags: {string.Join(", ", flagNames)}";
                ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorFatal, outmessage);
                outmessage = $"AllowChargeFriends={ChargeDamageControl.AllowChargeFriends}, DisableChargeEnemies={ChargeDamageControl.DisableChargeEnemies}, DisableAllChargeDamage={ChargeDamageControl.DisableAllChargeDamage}";
                ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorSuccess, outmessage);
                return;
        }

        outmessage = $"AllowChargeFriends={ChargeDamageControl.AllowChargeFriends}, DisableChargeEnemies={ChargeDamageControl.DisableChargeEnemies}, DisableAllChargeDamage={ChargeDamageControl.DisableAllChargeDamage}";
        ChatComponent.ServerSendMessageToPlayer(fromPeer, ColorSuccess, outmessage);
    }
}
