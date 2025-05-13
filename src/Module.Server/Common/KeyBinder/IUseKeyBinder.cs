using Crpg.Module.Common.KeyBinder.Models;

namespace Crpg.Module.Common.KeyBinder;

// Implement this interface if you use key binder.
public interface IUseKeyBinder
{
    public BindedKeyCategory BindedKeys { get; }
}
