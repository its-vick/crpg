using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Crpg.Module.GUI;

public class VickDebugMissionView : MissionView
{
    private GauntletLayer? _gauntletLayer;
    private VickDebugVM? _dataSource;

    public VickDebugMissionView()
    {
        ViewOrderPriority = 2;
    }

    public override void OnMissionScreenInitialize()
    {
        base.OnMissionScreenInitialize();
        if (GameNetwork.IsServer)
        {
            // InformationManager.DisplayMessage(new InformationMessage("[VickDebug] Skipped: Server mode", Colors.Yellow));
            return;
        }

        if (Mission == null)
        {
            // InformationManager.DisplayMessage(new InformationMessage("[VickDebug] Skipped: Mission is null, deferring to Tick", Colors.Yellow));
            return;
        }

        InitializeUI();
    }

    public override void OnMissionScreenTick(float dt)
    {
        base.OnMissionScreenTick(dt);
        if (GameNetwork.IsClient && _gauntletLayer == null && Mission != null)
        {
            InitializeUI();
        }

        if (GameNetwork.IsClient && _gauntletLayer != null)
        {
            _dataSource?.Tick(dt);
        }
        else
        {
            InformationManager.DisplayMessage(new InformationMessage($"[VickDebug] Tick skipped: Not client or UI not visible, GauntletLayer: {_gauntletLayer != null}, Mission: {Mission != null}", Colors.Yellow));
        }
    }

    public override void OnMissionScreenFinalize()
    {
        if (_gauntletLayer != null)
        {
            MissionScreen.RemoveLayer(_gauntletLayer);
            _gauntletLayer = null;
        }

        if (_dataSource != null)
        {
            _dataSource.OnFinalize();
            _dataSource = null;
        }

        base.OnMissionScreenFinalize();
    }

    private void InitializeUI()
    {
        try
        {
            _dataSource = new VickDebugVM(Mission);
            _gauntletLayer = new GauntletLayer(ViewOrderPriority);
            _gauntletLayer.LoadMovie("VickDebugHud", _dataSource);
            MissionScreen.AddLayer(_gauntletLayer);
            InformationManager.DisplayMessage(new InformationMessage("[VickDebug] UI Initialized", Colors.Green));
        }
        catch (Exception ex)
        {
            InformationManager.DisplayMessage(new InformationMessage($"[VickDebug] Init Error: {ex.Message}", Colors.Red));
        }
    }
}
