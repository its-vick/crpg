using Crpg.Module.Common;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Crpg.Module.GUI.AmmoQuiverChange;

internal class AmmoQuiverChangeMissionView : MissionView
{
    private const bool IsDebugEnabled = false;
    private AmmoQuiverChangeVM _viewModel;
    private AmmoQuiverChangeMissionBehaviorClient? _weaponChangeBehavior;
    private GauntletLayer? _gauntletLayer;
    public AmmoQuiverChangeMissionView()
    {
        _viewModel = new AmmoQuiverChangeVM(Mission); // Guaranteed non-null
        ViewOrderPriority = 2;
    }

    public override void OnMissionScreenInitialize()
    {
        _viewModel = new AmmoQuiverChangeVM(Mission);

        _weaponChangeBehavior = Mission.GetMissionBehavior<AmmoQuiverChangeMissionBehaviorClient>();
        if (_weaponChangeBehavior == null)
        {
            LogDebug("AmmoQuiverChangeMissionBehaviorClient not found!");
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

    private void HandleQuiverEvent(AmmoQuiverChangeMissionBehaviorClient.QuiverEventType type, object[] parameters)
    {
        string message = string.Empty;
        switch (type)
        {
            case AmmoQuiverChangeMissionBehaviorClient.QuiverEventType.AmmoQuiverChanged:
                message = "AmmoQuiverChanged";
                break;
            case AmmoQuiverChangeMissionBehaviorClient.QuiverEventType.WieldedItemChanged:
                _viewModel?.UpdateWieldedWeapon((EquipmentIndex)parameters[0], (MissionWeapon)parameters[1]);
                message = "WieldedItemChanged";
                break;
            case AmmoQuiverChangeMissionBehaviorClient.QuiverEventType.ItemDrop:
                message = "ItemDrop";
                break;
            case AmmoQuiverChangeMissionBehaviorClient.QuiverEventType.ItemPickup:
                message = "ItemPickup";
                break;
            case AmmoQuiverChangeMissionBehaviorClient.QuiverEventType.AgentBuild:
                message = "AgentBuild";
                break;
            case AmmoQuiverChangeMissionBehaviorClient.QuiverEventType.AgentRemoved:
                message = "AgentRemoved";
                break;
            case AmmoQuiverChangeMissionBehaviorClient.QuiverEventType.AgentChanged:
                message = "AgentChanged";
                break;
            case AmmoQuiverChangeMissionBehaviorClient.QuiverEventType.MissileShot:
                message = "MissileShot";
                break;
            case AmmoQuiverChangeMissionBehaviorClient.QuiverEventType.AgentStatusChanged:
                message = "AgentStatusChanged";
                break;
            case AmmoQuiverChangeMissionBehaviorClient.QuiverEventType.AmmoCountIncreased:
                message = "AmmoCountIncreased";
                break;
            case AmmoQuiverChangeMissionBehaviorClient.QuiverEventType.AmmoCountDecreased:
                message = "AmmoCountDecreased";
                break;
            default:
                message = "default";
                break;
        }

        _viewModel?.UpdateWeaponStatuses();
        _viewModel?.UpdateQuiverImages();

        LogDebug($"HandleQuiverEvent: {message}");
    }

#pragma warning disable CS0162 // Unreachable code if debug disabled
    private void LogDebug(string message)
    {
        if (IsDebugEnabled)
        {
            InformationManager.DisplayMessage(new InformationMessage($"[DEBUG] {message}"));
            Debug.Print(message);
        }
    }
}
#pragma warning restore CS0162
