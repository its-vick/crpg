using Crpg.Module.Common;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
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
        if (_weaponChangeBehavior == null)
        {
            Debug.Print("AmmoQuiverChangeMissionBehavior not found!");
            return;
        }

        // Subscribe to mission behavior events
        _weaponChangeBehavior.OnQuiverEvent -= HandleQuiverEvent;
        _weaponChangeBehavior.OnQuiverEvent += HandleQuiverEvent;

        // Initialize Gauntlet UI layer
        _gauntletLayer = new GauntletLayer(ViewOrderPriority);
        _gauntletLayer.LoadMovie("RangedWeaponAmmoHud", _viewModel);
        MissionScreen.AddLayer(_gauntletLayer);
        base.OnMissionScreenInitialize();
    }

    public override void OnMissionScreenFinalize()
    {
        if (_weaponChangeBehavior != null)
        {
            _weaponChangeBehavior.OnQuiverEvent -= HandleQuiverEvent;
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
                    _weaponChangeBehavior?.RequestChangeRangedAmmo();
                }
        */
        if (Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.C)) // C for now
        {
            _weaponChangeBehavior?.RequestChangeRangedAmmo();
        }

        _viewModel!.Tick(dt);
    }

    private void HandleQuiverEvent(AmmoQuiverChangeMissionBehavior.QuiverEventType type, object[] parameters)
    {
        string message = string.Empty;
        switch (type)
        {
            case AmmoQuiverChangeMissionBehavior.QuiverEventType.AmmoQuiverChanged:
                _viewModel?.UpdateQuiverImages();
                message = "AmmoQuiverChanged";
                break;
            case AmmoQuiverChangeMissionBehavior.QuiverEventType.WieldedItemChanged:
                _viewModel?.UpdateWieldedWeapon((EquipmentIndex)parameters[0], (MissionWeapon)parameters[1]);
                _viewModel?.UpdateQuiverImages();
                message = "WieldedItemChanged";
                break;
            case AmmoQuiverChangeMissionBehavior.QuiverEventType.ItemDrop:
                _viewModel?.UpdateQuiverImages();
                message = "ItemDrop";
                break;
            case AmmoQuiverChangeMissionBehavior.QuiverEventType.ItemPickup:
                _viewModel?.UpdateQuiverImages();
                message = "ItemPickup";
                break;
            case AmmoQuiverChangeMissionBehavior.QuiverEventType.AgentBuild:
                _viewModel?.UpdateQuiverImages();
                message = "AgentBuild";
                break;
            case AmmoQuiverChangeMissionBehavior.QuiverEventType.AgentRemoved:
                _viewModel?.UpdateQuiverImages();
                message = "AgentRemoved";
                break;
            case AmmoQuiverChangeMissionBehavior.QuiverEventType.AgentChanged:
                _viewModel?.UpdateQuiverImages();
                message = "AgentChanged";
                break;
            case AmmoQuiverChangeMissionBehavior.QuiverEventType.MissileShot:
                message = "MissileShot";
                _viewModel?.UpdateQuiverImages();
                break;
            default:
                message = "default";
                break;
        }

        // InformationManager.DisplayMessage(new InformationMessage($"HandleQuiverEvent: {message}"));
    }
}
