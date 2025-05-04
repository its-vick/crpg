using Crpg.Module.Common.AmmoQuiverChange;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.GUI;

internal class VickDebugVM : ViewModel
{
    private const float SpawnDelay = 3f;
    private readonly Mission _mission;
    private readonly Dictionary<int, string> _debugTextMap;
    private readonly bool _isInitialized;
    private float _spawnDelayTimer;

    private string _debugText0 = string.Empty;
    private string _debugText1 = string.Empty;
    private string _debugText2 = string.Empty;
    private string _debugText3 = string.Empty;
    private string _debugText4 = string.Empty;
    private string _debugText5 = string.Empty;
    private string _debugText6 = string.Empty;
    private string _debugText7 = string.Empty;
    private string _debugText8 = string.Empty;
    private string _debugText9 = string.Empty;

    public VickDebugVM(Mission mission)
    {
        _mission = mission ?? throw new ArgumentNullException(nameof(mission));
        _debugTextMap = new Dictionary<int, string>
        {
            { 0, _debugText0 },
            { 1, _debugText1 },
            { 2, _debugText2 },
            { 3, _debugText3 },
            { 4, _debugText4 },
            { 5, _debugText5 },
            { 6, _debugText6 },
            { 7, _debugText7 },
            { 8, _debugText8 },
            { 9, _debugText9 },
        };
        _isInitialized = true;
        // InformationManager.DisplayMessage(new InformationMessage($"[VickDebugVM] Constructed", Colors.Green));
    }

    public void Tick(float dt)
    {
        if (!_isInitialized || _mission == null || GameNetwork.IsServer)
        {
            // InformationManager.DisplayMessage(new InformationMessage("[VickDebugVM] Tick skipped: Invalid state", Colors.Yellow));
            return;
        }

        Agent agent = _mission.MainAgent;
        if (agent == null || !agent.IsActive() || agent.State != AgentState.Active)
        {
            // InformationManager.DisplayMessage(new InformationMessage("[VickDebugVM] Tick skipped: No active agent", Colors.Yellow));
            return;
        }

        if (_spawnDelayTimer < SpawnDelay)
        {
            _spawnDelayTimer += dt;
            // InformationManager.DisplayMessage(new InformationMessage($"[VickDebugVM] Tick waiting: {_spawnDelayTimer:F2}/{SpawnDelay}", Colors.Gray));
            return;
        }

        UpdateDebugTexts();
    }

    public int FindFirstEmptyDebugText()
    {
        foreach (var pair in _debugTextMap)
        {
            if (string.IsNullOrEmpty(pair.Value))
            {
                // InformationManager.DisplayMessage(new InformationMessage($"[VickDebugVM] FindFirstEmptyDebugText: Found empty slot at index {pair.Key}", Colors.Gray));
                return pair.Key;
            }
        }

        // InformationManager.DisplayMessage(new InformationMessage("[VickDebugVM] FindFirstEmptyDebugText: No empty slots found", Colors.Yellow));
        return -1;
    }

    public void SetDebugText(int index, string text)
    {
        if (index < 0 || index > 9 || string.IsNullOrEmpty(text))
        {
            // InformationManager.DisplayMessage(new InformationMessage($"[VickDebugVM] SetDebugText skipped: Invalid index {index} or empty text", Colors.Yellow));
            return;
        }

        if (!_debugTextMap.ContainsKey(index) || _debugTextMap[index] != text)
        {
            _debugTextMap[index] = text;
            // InformationManager.DisplayMessage(new InformationMessage($"[VickDebugVM] SetDebugText: Index {index}, Text: {text}", Colors.Gray));
            switch (index)
            {
                case 0:
                    DebugText0 = text;
                    break;
                case 1:
                    DebugText1 = text;
                    break;
                case 2:
                    DebugText2 = text;
                    break;
                case 3:
                    DebugText3 = text;
                    break;
                case 4:
                    DebugText4 = text;
                    break;
                case 5:
                    DebugText5 = text;
                    break;
                case 6:
                    DebugText6 = text;
                    break;
                case 7:
                    DebugText7 = text;
                    break;
                case 8:
                    DebugText8 = text;
                    break;
                case 9:
                    DebugText9 = text;
                    break;
            }
        }
        else
        {
            // InformationManager.DisplayMessage(new InformationMessage($"[VickDebugVM] SetDebugText: No update for index {index}, text unchanged", Colors.Gray));
        }
    }

    private static Agent.ActionCodeType GetAgentCurrentActionCode(Agent agent, int channel)
    {
        return agent.IsActive() ? agent.GetCurrentActionType(channel) : Agent.ActionCodeType.Other;
    }

    private static bool GetAgentWieldedWeaponInfo(Agent agent, out MissionWeapon wieldedWeapon, out EquipmentIndex wieldedIndex)
    {
        wieldedWeapon = MissionWeapon.Invalid;
        wieldedIndex = EquipmentIndex.None;

        if (!agent.IsActive() || agent.GetWieldedItemIndex(Agent.HandIndex.MainHand) == EquipmentIndex.None)
        {
            return false;
        }

        wieldedIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
        wieldedWeapon = agent.Equipment[wieldedIndex];

        if (wieldedWeapon.IsEmpty || wieldedWeapon.IsEqualTo(MissionWeapon.Invalid))
        {
            return false;
        }

        return true;
    }

    private static void GetAnimationInfo(Agent agent, int channel,
        out float actionProgress,
        out Agent.ActionCodeType actionCode,
        out Agent.ActionStage actionStage,
        out ActionIndexValueCache actionValue,
        out ActionIndexCache action)
    {
        actionProgress = -1;
        actionCode = Agent.ActionCodeType.Other;
        actionStage = Agent.ActionStage.None;
        actionValue = ActionIndexValueCache.act_none;
        action = ActionIndexCache.act_none;

        if (agent != null && agent.IsActive())
        {
            actionProgress = agent.GetCurrentActionProgress(channel);
            actionCode = agent.GetCurrentActionType(channel);
            actionStage = agent.GetCurrentActionStage(channel);
            actionValue = agent.GetCurrentActionValue(channel);
            action = agent.GetCurrentAction(channel);
        }
    }

    private void UpdateDebugTexts()
    {
        Agent agent = _mission.MainAgent;
        if (agent == null || !agent.IsActive() || agent.State != AgentState.Active)
        {
            return;
        }

        GetAgentWieldedWeaponInfo(agent, out MissionWeapon wieldedWeapon, out EquipmentIndex wieldedIndex);
        // lower body
        GetAnimationInfo(agent, 0,
            out float actionProgress0,
            out Agent.ActionCodeType actionCode0,
            out Agent.ActionStage actionStage0,
            out ActionIndexValueCache actionValue0,
            out ActionIndexCache action0);
        // upper body
        GetAnimationInfo(agent, 1,
            out float actionProgress,
            out Agent.ActionCodeType actionCode,
            out Agent.ActionStage actionStage,
            out ActionIndexValueCache actionValue,
            out ActionIndexCache action);

        string wName = string.Empty;
        string wType = string.Empty;
        string aName = string.Empty;

        if (!wieldedWeapon.IsEmpty && wieldedWeapon.Item != null)
        {
            wName = $"{wieldedWeapon.Item.Name}";
            wType = $"{wieldedWeapon.Item.ItemType}";

            MissionWeapon ammoWeapon = wieldedWeapon.AmmoWeapon;

            if (!ammoWeapon.IsEmpty && ammoWeapon.Item != null)
            {
                aName = $"{ammoWeapon.Item.Name}";
            }
        }

        // InformationManager.DisplayMessage(new InformationMessage("[VickDebugVM] Populate Debug Texts", Colors.Gray));
        SetDebugText(0, $"ReloadPhase: {wieldedWeapon.ReloadPhase} IsReloading: {wieldedWeapon.IsReloading}");
        SetDebugText(1, $"Weapon: {wName} Type: {wType}");
        SetDebugText(2, $"AmmoWeapon: {aName}");
        SetDebugText(3, $"Action Code(1): {actionCode} Stage: {actionStage} Progress: {actionProgress:F3}");
        SetDebugText(4, $"Index action(1): {action.Name} val: {actionValue.Name}");
        SetDebugText(5, $"Action Code(0): {actionCode0} Stage: {actionStage0} Progress: {actionProgress0:F3}");
        SetDebugText(6, $"Index action(0): {action0.Name} val: {actionValue0.Name}");
        SetDebugText(7, $"QuiverChangeMode: ({AmmoQuiverChangeComponent.QuiverChangeMode})");
    }

    [DataSourceProperty]
    public string DebugText0
    {
        get => _debugText0;
        set
        {
            if (_debugText0 != value)
            {
                _debugText0 = value;
                _debugTextMap[0] = value;
                OnPropertyChanged(nameof(DebugText0));
            }
        }
    }

    [DataSourceProperty]
    public string DebugText1
    {
        get => _debugText1;
        set
        {
            if (_debugText1 != value)
            {
                _debugText1 = value;
                _debugTextMap[1] = value;
                OnPropertyChanged(nameof(DebugText1));
            }
        }
    }

    [DataSourceProperty]
    public string DebugText2
    {
        get => _debugText2;
        set
        {
            if (_debugText2 != value)
            {
                _debugText2 = value;
                _debugTextMap[2] = value;
                OnPropertyChanged(nameof(DebugText2));
            }
        }
    }

    [DataSourceProperty]
    public string DebugText3
    {
        get => _debugText3;
        set
        {
            if (_debugText3 != value)
            {
                _debugText3 = value;
                _debugTextMap[3] = value;
                OnPropertyChanged(nameof(DebugText3));
            }
        }
    }

    [DataSourceProperty]
    public string DebugText4
    {
        get => _debugText4;
        set
        {
            if (_debugText4 != value)
            {
                _debugText4 = value;
                _debugTextMap[4] = value;
                OnPropertyChanged(nameof(DebugText4));
            }
        }
    }

    [DataSourceProperty]
    public string DebugText5
    {
        get => _debugText5;
        set
        {
            if (_debugText5 != value)
            {
                _debugText5 = value;
                _debugTextMap[5] = value;
                OnPropertyChanged(nameof(DebugText5));
            }
        }
    }

    [DataSourceProperty]
    public string DebugText6
    {
        get => _debugText6;
        set
        {
            if (_debugText6 != value)
            {
                _debugText6 = value;
                _debugTextMap[6] = value;
                OnPropertyChanged(nameof(DebugText6));
            }
        }
    }

    [DataSourceProperty]
    public string DebugText7
    {
        get => _debugText7;
        set
        {
            if (_debugText7 != value)
            {
                _debugText7 = value;
                _debugTextMap[7] = value;
                OnPropertyChanged(nameof(DebugText7));
            }
        }
    }

    [DataSourceProperty]
    public string DebugText8
    {
        get => _debugText8;
        set
        {
            if (_debugText8 != value)
            {
                _debugText8 = value;
                _debugTextMap[8] = value;
                OnPropertyChanged(nameof(DebugText8));
            }
        }
    }

    [DataSourceProperty]
    public string DebugText9
    {
        get => _debugText9;
        set
        {
            if (_debugText9 != value)
            {
                _debugText9 = value;
                _debugTextMap[9] = value;
                OnPropertyChanged(nameof(DebugText9));
            }
        }
    }
}
