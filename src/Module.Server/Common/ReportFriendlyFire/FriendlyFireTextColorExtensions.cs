using TaleWorlds.Library;

namespace Crpg.Module.Common.ReportFriendlyFire;

public static class FriendlyFireTextColorExtensions
{
    public static Color ToColor(this FriendlyFireTextColors ffColor)
    {
        return ffColor switch
        {
            FriendlyFireTextColors.Red => Colors.Red,
            FriendlyFireTextColors.Green => Colors.Green,
            FriendlyFireTextColors.Yellow => Colors.Yellow,
            FriendlyFireTextColors.Blue => Colors.Blue,
            FriendlyFireTextColors.Black => Colors.Black,
            FriendlyFireTextColors.Cyan => Colors.Cyan,
            FriendlyFireTextColors.Magenta => Colors.Magenta,
            FriendlyFireTextColors.Gray => Colors.Gray,
            FriendlyFireTextColors.White => Colors.White,
            _ => Colors.White,
        };
    }
}
