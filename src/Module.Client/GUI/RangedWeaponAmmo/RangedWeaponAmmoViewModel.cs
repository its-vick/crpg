using System.Security.Cryptography;
using Crpg.Module.Common;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection;

namespace Crpg.Module.GUI;

internal class RangedWeaponAmmoViewModel : ViewModel
{
    // private MissionMultiplayerGameModeBaseClient _gameMode;
    private const int MaxQuiverSlots = 4;
    private const bool IsDebugEnabled = false;
    private readonly Mission _mission;
    private readonly List<ImageIdentifierVM> _quiverImages;
    // private AmmoQuiverChangeMissionBehavior? _weaponChangeBehavior;
    private bool _showQuiverAmmoCount;
    private int _quiverAmmoCount;
    private bool _isQuiverAmmoCountAlertEnabled;
    private string _quiverName;
    private bool _showQuiverName;
    private MissionWeapon _wieldedWeapon;
    private ImageIdentifierVM _quiverImage0;
    private ImageIdentifierVM _quiverImage1;
    private ImageIdentifierVM _quiverImage2;
    private ImageIdentifierVM _quiverImage3;

    private bool _rangedWeaponEquipped;

    public RangedWeaponAmmoViewModel(Mission mission)
    {
        _mission = mission;
        _quiverName = string.Empty;
        _quiverAmmoCount = 0;
        _isQuiverAmmoCountAlertEnabled = false;
        _showQuiverAmmoCount = false;
        _showQuiverName = false;
        _wieldedWeapon = MissionWeapon.Invalid;

        _quiverImages = new List<ImageIdentifierVM>();
        for (int i = 0; i < MaxQuiverSlots; i++)
        {
            _quiverImages.Add(new ImageIdentifierVM(ImageIdentifierType.Item));
        }

        _quiverImage0 = new ImageIdentifierVM(ImageIdentifierType.Item);
        _quiverImage1 = new ImageIdentifierVM(ImageIdentifierType.Item);
        _quiverImage2 = new ImageIdentifierVM(ImageIdentifierType.Item);
        _quiverImage3 = new ImageIdentifierVM(ImageIdentifierType.Item);

        _rangedWeaponEquipped = false;

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
        bool isPlayerActive = _mission?.MainAgent?.IsActive() == true;

        if (isPlayerActive)
        {
            UpdateWeaponStatuses();
            UpdateQuiverImages();
        }
        else
        {
            ShowQuiverAmmoCount = false;
            ShowQuiverName = false;
        }
    }

    public bool GetCurrentQuiverAmmoAmount(out int currentQuiverAmmo, out int maxQuiverAmmo, out MissionWeapon ammoWeapon)
    {
        Agent agent = Agent.Main;
        ItemObject.ItemTypeEnum currentAmmoType = ItemObject.ItemTypeEnum.Invalid;
        currentQuiverAmmo = 0;
        maxQuiverAmmo = 0;
        ammoWeapon = MissionWeapon.Invalid;

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
                    QuiverAmmoLeft = mWeaponWielded.Amount;
                    currentQuiverAmmo = mWeaponWielded.Amount;
                    maxQuiverAmmo = mWeaponWielded.MaxAmmo;
                    ammoWeapon = mWeaponWielded;
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
                            continue;
                        }

                        QuiverAmmoLeft = mWeaponAmmo.Amount;
                        currentQuiverAmmo = mWeaponAmmo.Amount;
                        maxQuiverAmmo = mWeaponAmmo.MaxAmmo;
                        ammoWeapon = mWeaponAmmo;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public void OnAgentWieldedWeaponChanged(EquipmentIndex weaponIndex, MissionWeapon weapon)
    {
        Agent agent = Agent.Main;
        if (agent == null || !agent.IsActive())
        {
            return;
        }

        LogDebug("VM: OnAgentWieldedWeaponChanged");

        if (AmmoQuiverChangeComponent.IsAgentWieldedWeaponRangedUsesQuiver(agent, out EquipmentIndex wieldedWeaponIndex, out MissionWeapon wieldedWeapon, out bool isThrowingWeapon))
        {
            RangedWeaponEquipped = true;
        }
        else
        {
            RangedWeaponEquipped = false;
        }

        LogDebug($"VM: onAgentWieldedWeaponChanged: {RangedWeaponEquipped})");
    }

    public void OnMissileShot(Agent shooterAgent, EquipmentIndex weaponIndex)
    {
        LogDebug("VM: OnMissleShot");
    }

    public void UpdateQuiverImages()
    {
        Agent agent = Agent.Main;
        if (agent == null || !agent.IsActive())
        {
            return;
        }

        if (RangedWeaponEquipped == false)
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

        for (int i = 0; i < quiverImages.Length; i++)
        {
            if (i < ammoQuivers.Count)
            {
                // check throwing

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

    private void UpdateWeaponStatuses()
    {
        Agent agent = Agent.Main;
        if (agent == null || !agent.IsActive())
        {
            ShowQuiverAmmoCount = false;
            ShowQuiverName = false;
            return;
        }

        // Show Ammo count textwidget if there are multiple quivers and its not empty
        if (!GetCurrentQuiverAmmoAmount(out int currentQuiverAmmo, out int maxQuiverAmmo, out MissionWeapon ammoWeapon) || currentQuiverAmmo <= 0)
        {
            ShowQuiverAmmoCount = false;
            ShowQuiverName = false;
            return;
        }

        ShowQuiverAmmoCount = false;

        // check for this being the last quiver with ammo
        if (AmmoQuiverChangeComponent.GetAgentQuiversWithAmmoEquippedForWieldedWeapon(agent, out List<int> ammoQuivers))
        {
            // hide quiver ammo if its the only quiver or thrown weapon
            bool isThrown = ammoWeapon.Item.ItemType == ItemObject.ItemTypeEnum.Thrown;
            if (ammoQuivers.Count() > 1 || isThrown)
            {
                ShowQuiverAmmoCount = true;
                float f = (float)maxQuiverAmmo * 0.2f;
                IsQuiverAmmoCountAlertEnabled = maxQuiverAmmo != QuiverAmmoLeft && QuiverAmmoLeft <= MathF.Ceiling(f);
            }
        }

        // display quiver name, still display name if thrown but no ammo amount
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
    public int QuiverAmmoLeft
    {
        get => _quiverAmmoCount;
        set
        {
            if (value != _quiverAmmoCount)
            {
                _quiverAmmoCount = value;
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

    private void LogDebug(string message)
    {
        if (IsDebugEnabled)
        {
            InformationManager.DisplayMessage(new InformationMessage($"[DEBUG] {message}"));
        }
    }
}
