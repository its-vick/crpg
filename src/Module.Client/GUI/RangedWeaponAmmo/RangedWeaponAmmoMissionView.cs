using Crpg.Module.Common;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Crpg.Module.GUI;

internal class RangedWeaponAmmoMissionView : MissionView
{
    private RangedWeaponAmmoViewModel _viewModel;
    private AmmoQuiverChangeMissionBehavior? _weaponChangeBehavior;
    private GauntletLayer? _gauntletLayer;
    public RangedWeaponAmmoMissionView()
    {
        _viewModel = new RangedWeaponAmmoViewModel(Mission); // Guaranteed non-null
        ViewOrderPriority = 2;
    }

    public override void OnMissionScreenInitialize()
    {
        _viewModel = new RangedWeaponAmmoViewModel(Mission);
        _weaponChangeBehavior = Mission.GetMissionBehavior<AmmoQuiverChangeMissionBehavior>();

        // subscribe to mission behavior events
        _weaponChangeBehavior.OnMissileShot -= OnMissileShot;
        _weaponChangeBehavior.OnMissileShot += OnMissileShot;

        _weaponChangeBehavior.WieldedItemChanged -= OnWieldedItemChanged;
        _weaponChangeBehavior.WieldedItemChanged += OnWieldedItemChanged;

        // initialize Gauntlet UI layer
        _gauntletLayer = new GauntletLayer(ViewOrderPriority);
        _gauntletLayer.LoadMovie("RangedWeaponAmmoHud", _viewModel);
        MissionScreen.AddLayer(_gauntletLayer);
        base.OnMissionScreenInitialize();
    }

    public override void OnMissionScreenFinalize()
    {
        if (_weaponChangeBehavior != null)
        {
            _weaponChangeBehavior.OnMissileShot -= OnMissileShot;
            _weaponChangeBehavior.WieldedItemChanged -= OnWieldedItemChanged;
        }

        if (_gauntletLayer != null)
        {
            MissionScreen.RemoveLayer(_gauntletLayer);
            _gauntletLayer = null;
        }

        _viewModel!.OnFinalize();
        _viewModel = null!;
        base.OnMissionScreenFinalize();
    }

    public override void OnMissionScreenTick(float dt)
    {
        base.OnMissionScreenTick(dt);
        /*
                if (Input.IsGameKeyPressed(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey("ToggleWeaponMode").Id)) // default is X
                {
                    _viewModel!.RequestChangeRangedAmmo();
                }
        */
        if (Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.C)) // C for now
        {
            // _viewModel!.RequestChangeRangedAmmo();
            _weaponChangeBehavior?.RequestChangeRangedAmmo();
        }

        _viewModel!.Tick(dt);
    }

    private void OnMissileShot(Agent shooterAgent, EquipmentIndex weaponIndex)
    {
        _viewModel?.OnMissileShot(shooterAgent, weaponIndex);
    }

    private void OnWieldedItemChanged(EquipmentIndex newWeaponIndex, MissionWeapon missionWeapon)
    {
        _viewModel?.OnAgentWieldedWeaponChanged(newWeaponIndex, missionWeapon);
        // _viewModel.RangedWeaponEquipped = true;
    }
}
