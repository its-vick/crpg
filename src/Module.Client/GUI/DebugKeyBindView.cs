using Crpg.Module.Common.KeyBinder;
using Crpg.Module.Common.KeyBinder.Models;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Crpg.Module.GUI;

// [DefaultView]
public class DebugKeyBindView : MissionView, IUseKeyBinder
{
    private static string KeyCategoryId = "debugTest";
    BindedKeyCategory IUseKeyBinder.BindedKeys => new BindedKeyCategory
    {
        CategoryId = KeyCategoryId,
        Category = "Debug Test",
        Keys = new List<BindedKey>
        {
            new BindedKey()
            {
                Id = "key_debug_test",
                Name = "Debug Test",
                Description = "Open the debug key bind view.",
                DefaultInputKey = InputKey.C,
                KeyId = 0,
            },
        },
    };

    private GameKey? debugKey;

    public DebugKeyBindView()
    {
        debugKey = null;
    }

    public override void EarlyStart()
    {
        // TaleWorlds.Library.Debug.Print("DebugKeyBindView: EarlyStart()", 0, TaleWorlds.Library.Debug.DebugColor.Cyan);
        debugKey = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_debug_test");
    }

    public override void OnMissionTick(float dt)
    {
        if (debugKey != null && (Input.IsKeyPressed(debugKey.KeyboardKey.InputKey) || Input.IsKeyPressed(debugKey.ControllerKey.InputKey)))
        {
            InformationManager.DisplayMessage(new InformationMessage("Debug key bind view pressed!!"));
        }
    }
}
