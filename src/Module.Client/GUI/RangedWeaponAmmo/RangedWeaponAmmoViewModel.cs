using Crpg.Module.Common;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection;

namespace Crpg.Module.GUI;

internal class RangedWeaponAmmoViewModel : ViewModel
{
    // private MissionMultiplayerGameModeBaseClient _gameMode;
    private const int MaxQuiverSlots = 4;
    private const bool IsDebugEnabled = false;
    private const float MarginRightThrow = 18;
    private const float MarginRightOther = 80;

    private readonly Mission _mission;
    private bool _showQuiverAmmoCount;
    private bool _showQuiverName;
    private bool _isQuiverAmmoCountAlertEnabled;
    private string _quiverName;
    private float _quiverAmmoCountMarginRight;

    private MissionWeapon _wieldedWeapon;
    private MissionWeapon _currentQuiver;
    private int _currentQuiverAmmo;
    private bool _rangedWeaponEquipped;

    private ImageIdentifierVM _quiverImage0;
    private ImageIdentifierVM _quiverImage1;
    private ImageIdentifierVM _quiverImage2;
    private ImageIdentifierVM _quiverImage3;

    public RangedWeaponAmmoViewModel(Mission mission)
    {
        _mission = mission;
        _quiverName = string.Empty;
        _currentQuiverAmmo = 0;
        _isQuiverAmmoCountAlertEnabled = false;
        _showQuiverAmmoCount = false;
        _showQuiverName = false;
        _wieldedWeapon = MissionWeapon.Invalid;
        _currentQuiver = MissionWeapon.Invalid;
        _rangedWeaponEquipped = false;

        _quiverImage0 = new ImageIdentifierVM(ImageIdentifierType.Item);
        _quiverImage1 = new ImageIdentifierVM(ImageIdentifierType.Item);
        _quiverImage2 = new ImageIdentifierVM(ImageIdentifierType.Item);
        _quiverImage3 = new ImageIdentifierVM(ImageIdentifierType.Item);

        RefreshValues();
    }

    public override void OnFinalize()
    {
        base.OnFinalize();
    }

    public sealed override void RefreshValues()
    {
        base.RefreshValues();
    }

    public void Tick(float dt)
    {
        if (_mission?.MainAgent?.IsActive() != true)
        {
            ShowQuiverAmmoCount = false;
            ShowQuiverName = false;
            return;
        }

        // UpdateWeaponStatuses();
    }

    public bool UpdateCurrentQuiver(out MissionWeapon ammoWeapon, out ItemObject.ItemTypeEnum currentAmmoType)
    {
        ammoWeapon = MissionWeapon.Invalid;
        currentAmmoType = ItemObject.ItemTypeEnum.Invalid;
        Agent agent = Agent.Main;

        if (agent == null || !agent.IsActive())
        {
            return false;
        }

        if (!RangedWeaponEquipped ||
            WieldedWeapon.IsEmpty ||
            WieldedWeapon.IsEqualTo(MissionWeapon.Invalid) ||
            WieldedWeapon.Item == null ||
            !agent.GetWieldedWeaponInfo(Agent.HandIndex.MainHand).IsRangedWeapon)
        {
            return false;
        }

        ItemObject.ItemTypeEnum currentWeaponType = WieldedWeapon.Item.Type;
        if (currentWeaponType != ItemObject.ItemTypeEnum.Bow &&
            currentWeaponType != ItemObject.ItemTypeEnum.Crossbow &&
            currentWeaponType != ItemObject.ItemTypeEnum.Thrown &&
            currentWeaponType != ItemObject.ItemTypeEnum.Musket)
        {
            return false;
        }

        currentAmmoType = ItemObject.GetAmmoTypeForItemType(currentWeaponType);
        // set for handguns
        if (currentWeaponType == ItemObject.ItemTypeEnum.Musket)
        {
            currentAmmoType = ItemObject.ItemTypeEnum.Bullets;
        }

        MissionWeapon originalQuiver = CurrentQuiver;

        // Handle Throwing
        if (currentWeaponType == ItemObject.ItemTypeEnum.Thrown)
        {
            ammoWeapon = WieldedWeapon;
        }

        // Not Throwing -- cycle through equipment and find first of ammo type
        else
        {
            for (EquipmentIndex eIndex = EquipmentIndex.WeaponItemBeginSlot; eIndex < EquipmentIndex.ExtraWeaponSlot; eIndex++)
            {
                MissionWeapon mWeaponAmmo = agent.Equipment[eIndex];
                if (!mWeaponAmmo.IsEmpty && !mWeaponAmmo.IsEqualTo(MissionWeapon.Invalid) && mWeaponAmmo.Item != null && mWeaponAmmo.Item.Type == currentAmmoType) // matches current ammo type neede
                {
                    if (mWeaponAmmo.Amount <= 0) // quiver has no ammo left
                    {
                        continue;
                    }

                    ammoWeapon = mWeaponAmmo;
                    break;
                }
            }
        }

        if (ammoWeapon.IsEmpty || ammoWeapon.IsEqualTo(MissionWeapon.Invalid) || ammoWeapon.Item == null)
        {
            return false;
        }

        bool changed = !CurrentQuiver.IsEqualTo(originalQuiver);

        CurrentQuiver = ammoWeapon;
        CurrentQuiverAmmo = ammoWeapon.Amount;

        return true;
    }

    public bool GetCurrentQuiverAmmoAmount(out int activeQuiverAmmo, out int maxQuiverAmmo, out MissionWeapon ammoWeapon, out bool hasThrowing, out bool hasChanged)
    {
        Agent agent = Agent.Main;
        ItemObject.ItemTypeEnum currentAmmoType = ItemObject.ItemTypeEnum.Invalid;
        activeQuiverAmmo = 0;
        maxQuiverAmmo = 0;
        ammoWeapon = MissionWeapon.Invalid;
        hasThrowing = false;
        hasChanged = false;

        if (agent == null || !agent.IsActive())
        {
            return false;
        }

        // Get current weapon and type, figure out ammo type
        if (agent.GetWieldedWeaponInfo(Agent.HandIndex.MainHand).IsRangedWeapon)
        {
            MissionWeapon mWeaponWielded = agent.Equipment[agent.GetWieldedItemIndex(Agent.HandIndex.MainHand)];
            if (!mWeaponWielded.IsEmpty)
            {
                currentAmmoType = ItemObject.GetAmmoTypeForItemType(mWeaponWielded.Item.Type);
                // set for handguns
                if (mWeaponWielded.Item.Type == ItemObject.ItemTypeEnum.Musket)
                {
                    currentAmmoType = ItemObject.ItemTypeEnum.Bullets;
                }
            }

            // Handle Thrown Quiver
            if (!mWeaponWielded.IsEmpty && mWeaponWielded.Item != null)
            {
                if (mWeaponWielded.Item.Type == ItemObject.ItemTypeEnum.Thrown)
                {
                    if (!CurrentQuiver.IsEqualTo(mWeaponWielded))
                    {
                        hasChanged = true;
                    }

                    CurrentQuiverAmmo = mWeaponWielded.Amount;
                    CurrentQuiver = mWeaponWielded;

                    activeQuiverAmmo = mWeaponWielded.Amount;
                    maxQuiverAmmo = mWeaponWielded.MaxAmmo;
                    ammoWeapon = mWeaponWielded;
                    hasThrowing = true;

                    QuiverAmmoCountMarginRight = MarginRightThrow;
                    return true;
                }
            }

            // Not Throwing Quiver -- cycle through equipment and find first of ammo type
            for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ExtraWeaponSlot; ++equipmentIndex)
            {
                MissionWeapon mWeaponAmmo = agent.Equipment[equipmentIndex];
                if (!mWeaponAmmo.IsEmpty) // go to next ammo if its empty
                {
                    if (mWeaponAmmo.Item.Type == currentAmmoType) // matches the current ammo type needed
                    {
                        if (mWeaponAmmo.Amount <= 0) // no ammo left in quiver
                        {
                            if (CurrentQuiver.IsEqualTo(mWeaponAmmo))
                            {
                                hasChanged = true;
                            }

                            continue;
                        }

                        if (!CurrentQuiver.IsEqualTo(mWeaponAmmo))
                        {
                            CurrentQuiver = mWeaponAmmo;
                        }

                        CurrentQuiver = mWeaponAmmo;
                        CurrentQuiverAmmo = mWeaponAmmo.Amount;

                        activeQuiverAmmo = mWeaponAmmo.Amount;
                        maxQuiverAmmo = mWeaponAmmo.MaxAmmo;
                        ammoWeapon = mWeaponAmmo;

                        QuiverAmmoCountMarginRight = MarginRightOther;

                        if (hasChanged)
                        {
                            LogDebug("VM: hasChanged=true in GetCurrentQuiverAmmoAmount");
                            UpdateQuiverImages();
                        }

                        return true;
                    }
                }
            }
        }
        else
        {
            // not ranged weapon
            if (!CurrentQuiver.IsEqualTo(MissionWeapon.Invalid))
            {
                hasChanged = true;
                UpdateQuiverImages();
            }

            CurrentQuiver = MissionWeapon.Invalid;
        }

        return false;
    }

    public void UpdateWieldedWeapon(EquipmentIndex weaponIndex, MissionWeapon weapon)
    {
        Agent agent = Agent.Main;
        if (agent == null || !agent.IsActive())
        {
            return;
        }

        RangedWeaponEquipped = AmmoQuiverChangeComponent.IsAgentWieldedWeaponRangedUsesQuiver(agent, out EquipmentIndex wieldedWeaponIndex, out MissionWeapon wieldedWeapon, out bool isThrowingWeapon);
        string weaponName = string.Empty;
        if (!wieldedWeapon.IsEmpty)
        {
            WieldedWeapon = wieldedWeapon;
            weaponName = WieldedWeapon.Item?.Name.ToString() ?? string.Empty;
        }
        else
        {
            WieldedWeapon = MissionWeapon.Invalid;
        }

        UpdateQuiverImages();

        LogDebug($"VM: onAgentWieldedWeaponChanged: {weaponName} Ranged: {RangedWeaponEquipped})");
    }

    public void UpdateQuiverImages()
    {
        Agent agent = Agent.Main;
        if (agent == null)
        {
            // return;
        }

        LogDebug("VM: UpdateQuiverImages()");

        if (agent == null || !agent.IsActive() || RangedWeaponEquipped == false || WieldedWeapon.IsEmpty || WieldedWeapon.IsEqualTo(MissionWeapon.Invalid) || WieldedWeapon.Item == null)
        {
            QuiverImage0 = new ImageIdentifierVM(ImageIdentifierType.Item);
            QuiverImage1 = new ImageIdentifierVM(ImageIdentifierType.Item);
            QuiverImage2 = new ImageIdentifierVM(ImageIdentifierType.Item);
            QuiverImage3 = new ImageIdentifierVM(ImageIdentifierType.Item);
            return;
        }

        AmmoQuiverChangeComponent.GetAgentQuiversWithAmmoEquippedForWieldedWeapon(agent, out List<int> ammoQuivers);
        // LogDebug($"VM: UpdateQuiverImages: ammoQuivers: {ammoQuivers.Count})");

        ImageIdentifierVM[] quiverImages = new ImageIdentifierVM[4]; // assuming max 4 quivers

        // Throwing - remove wielded weapon from ammoQuivers list since there is a big picture of the weapon already
        if (WieldedWeapon.Item.Type == ItemObject.ItemTypeEnum.Thrown)
        {
            int? matchingIndex = ammoQuivers
                .FirstOrDefault(index => agent.Equipment[(EquipmentIndex)index].IsEqualTo(WieldedWeapon));

            if (matchingIndex.HasValue)
            {
                ammoQuivers.Remove(matchingIndex.Value);
            }
        }

        for (int i = 0; i < quiverImages.Length; i++)
        {
            if (i < ammoQuivers.Count)
            {
                // check that matches ammo
                MissionWeapon mQuiver = agent.Equipment[ammoQuivers[i]];
                quiverImages[i] = mQuiver.IsEmpty ? new ImageIdentifierVM() : new ImageIdentifierVM(mQuiver.Item);
            }
            else
            {
                quiverImages[i] = new ImageIdentifierVM(ImageIdentifierType.Item);
            }
        }

        QuiverImage0 = quiverImages[0];
        QuiverImage1 = quiverImages[1];
        QuiverImage2 = quiverImages[2];
        QuiverImage3 = quiverImages[3];
    }

    public void UpdateWeaponStatuses()
    {
        Agent agent = Agent.Main;
        if (agent == null || !agent.IsActive())
        {
            ShowQuiverAmmoCount = false;
            ShowQuiverName = false;
            return;
        }

        // Show Ammo count textwidget if there are multiple quivers and its not empty
        if (!GetCurrentQuiverAmmoAmount(out int currentQuiverAmmo, out int maxQuiverAmmo, out MissionWeapon ammoWeapon, out bool hasThrowing, out bool hasChanged) || currentQuiverAmmo <= 0)
        {
            if (hasChanged)
            {
                UpdateQuiverImages();
            }

            ShowQuiverAmmoCount = false;
            ShowQuiverName = false;
            return;
        }

        ShowQuiverAmmoCount = false;

        // check for this being the last quiver with ammo
        if (AmmoQuiverChangeComponent.GetAgentQuiversWithAmmoEquippedForWieldedWeapon(agent, out List<int> ammoQuivers))
        {
            // hide quiver ammo if its the only quiver or thrown weapon
            if (ammoQuivers.Count() > 1 || hasThrowing)
            {
                ShowQuiverAmmoCount = true;
                float f = (float)maxQuiverAmmo * 0.2f;
                IsQuiverAmmoCountAlertEnabled = maxQuiverAmmo != CurrentQuiverAmmo && CurrentQuiverAmmo <= MathF.Ceiling(f);
            }
        }

        // display quiver name,
        if (!ammoWeapon.IsEqualTo(MissionWeapon.Invalid))
        {
            // check if the ammo weapon is the wielded weapon ie throwing
            EquipmentIndex wieldedWeaponIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            if (wieldedWeaponIndex != EquipmentIndex.None)
            {
                MissionWeapon wieldedWeapon = agent.Equipment[wieldedWeaponIndex];
                if (!wieldedWeapon.IsEmpty && wieldedWeapon.Item != null)
                {
                    QuiverName = wieldedWeapon.Item.Type == ItemObject.ItemTypeEnum.Thrown
                        ? wieldedWeapon.Item.Name.ToString()
                        : ammoWeapon.Item.Name.ToString();

                    ShowQuiverName = true;
                }
            }
        }

        // different quiver than CurrentQuiver used to be
        if (hasChanged)
        {
            UpdateQuiverImages();
        }
    }

    [DataSourceProperty]
    public bool ShowQuiverAmmoCount
    {
        get
        {
            return _showQuiverAmmoCount;
        }
        set
        {
            if (value != _showQuiverAmmoCount)
            {
                _showQuiverAmmoCount = value;
                OnPropertyChangedWithValue(value);
            }
        }
    }

    [DataSourceProperty]
    public int CurrentQuiverAmmo
    {
        get => _currentQuiverAmmo;
        set
        {
            if (value != _currentQuiverAmmo)
            {
                _currentQuiverAmmo = value;
                OnPropertyChangedWithValue(value);
            }
        }
    }

    [DataSourceProperty]
    public bool IsQuiverAmmoCountAlertEnabled
    {
        get => _isQuiverAmmoCountAlertEnabled;
        set
        {
            if (value != _isQuiverAmmoCountAlertEnabled)
            {
                _isQuiverAmmoCountAlertEnabled = value;
                OnPropertyChangedWithValue(value);
            }
        }
    }

    [DataSourceProperty]
    public string QuiverName
    {
        get => _quiverName;
        set
        {
            if (value != _quiverName)
            {
                _quiverName = value;
                OnPropertyChangedWithValue(value);
            }
        }
    }

    [DataSourceProperty]
    public bool ShowQuiverName
    {
        get => _showQuiverName;
        set
        {
            if (value != _showQuiverName)
            {
                _showQuiverName = value;
                OnPropertyChangedWithValue(value);
            }
        }
    }

    [DataSourceProperty]
    public float QuiverAmmoCountMarginRight
    {
        get => _quiverAmmoCountMarginRight;
        set
        {
            if (value != _quiverAmmoCountMarginRight)
            {
                _quiverAmmoCountMarginRight = value;
                OnPropertyChangedWithValue(value);
            }
        }
    }

    public bool RangedWeaponEquipped
    {
        get => _rangedWeaponEquipped;
        set
        {
            if (value != _rangedWeaponEquipped)
            {
                _rangedWeaponEquipped = value;
                OnPropertyChangedWithValue(value);
            }
        }
    }

    public MissionWeapon WieldedWeapon
    {
        get => _wieldedWeapon;
        set
        {
            if (!_wieldedWeapon.IsEqualTo(value))
            {
                _wieldedWeapon = value;
                OnPropertyChanged(nameof(_wieldedWeapon));
            }
        }
    }

    public MissionWeapon CurrentQuiver
    {
        get => _currentQuiver;
        set
        {
            if (!_currentQuiver.IsEqualTo(value))
            {
                _currentQuiver = value;
                OnPropertyChanged(nameof(_currentQuiver));
            }
        }
    }

    [DataSourceProperty]
    public ImageIdentifierVM QuiverImage0
    {
        get => _quiverImage0;
        set
        {
            if (value != _quiverImage0)
            {
                _quiverImage0 = value;
                OnPropertyChangedWithValue<ImageIdentifierVM>(value, nameof(QuiverImage0));
            }
        }
    }

    [DataSourceProperty]
    public ImageIdentifierVM QuiverImage1
    {
        get => _quiverImage1;
        set
        {
            if (value != _quiverImage1)
            {
                _quiverImage1 = value;
                OnPropertyChangedWithValue<ImageIdentifierVM>(value, nameof(QuiverImage1));
            }
        }
    }

    [DataSourceProperty]
    public ImageIdentifierVM QuiverImage2
    {
        get => _quiverImage2;
        set
        {
            if (value != _quiverImage2)
            {
                _quiverImage2 = value;
                OnPropertyChangedWithValue<ImageIdentifierVM>(value, nameof(QuiverImage2));
            }
        }
    }

    [DataSourceProperty]
    public ImageIdentifierVM QuiverImage3
    {
        get => _quiverImage3;
        set
        {
            if (value != _quiverImage3)
            {
                _quiverImage3 = value;
                OnPropertyChangedWithValue<ImageIdentifierVM>(value, nameof(QuiverImage3));
            }
        }
    }

#pragma warning disable CS0162 // Unreachable code if debug disabled
    private void LogDebug(string message)
    {
        if (IsDebugEnabled)
        {
            InformationManager.DisplayMessage(new InformationMessage($"[DEBUG] {message}"));
        }
    }
}
#pragma warning restore CS0162
