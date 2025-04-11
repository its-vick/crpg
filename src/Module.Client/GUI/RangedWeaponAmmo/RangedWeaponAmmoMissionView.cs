using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ObjectSystem;

namespace Crpg.Module.GUI;

internal class RangedWeaponAmmoMissionView : MissionView
{
    private RangedWeaponAmmoViewModel? _dataSource;
    private GauntletLayer? _gauntletLayer;

    public RangedWeaponAmmoMissionView()
    {
        ViewOrderPriority = 2;
    }

    public override void OnMissionScreenInitialize()
    {
        base.OnMissionScreenInitialize();

        _dataSource = new RangedWeaponAmmoViewModel(Mission);
        _gauntletLayer = new GauntletLayer(ViewOrderPriority);
        _gauntletLayer.LoadMovie("RangedWeaponAmmoHud", _dataSource);
        MissionScreen.AddLayer(_gauntletLayer);
    }

    public override void OnMissionScreenFinalize()
    {
        MissionScreen.RemoveLayer(_gauntletLayer);
        _dataSource!.OnFinalize();
        base.OnMissionScreenFinalize();
    }

    public override void OnMissionScreenTick(float dt)
    {
        base.OnMissionScreenTick(dt);

        if (Input.IsGameKeyPressed(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey("ToggleWeaponMode").Id)) // default is X
        {
            _dataSource!.RequestChangeRangedAmmo();
        }

        _dataSource!.Tick(dt);
    }
}
