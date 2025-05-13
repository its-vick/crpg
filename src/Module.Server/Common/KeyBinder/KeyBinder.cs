using System.Reflection;
using Crpg.Module.Common.KeyBinder;
using Crpg.Module.Common.KeyBinder.Models;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;

namespace Crpg.Module.Common.KeyBinder;
public static class KeyBinder
{
    public static readonly ICollection<BindedKeyCategory> KeysCategories = new List<BindedKeyCategory>();
    public static readonly IDictionary<string, GameKeyBinderContext> KeyContexts = new Dictionary<string, GameKeyBinderContext>();

    public static void RegisterKeyGroup(BindedKeyCategory group)
    {
        KeysCategories.Add(group);
    }

    public static void Initialize()
    {
        TaleWorlds.Library.Debug.Print("KeyBinder.Initialize", 0, TaleWorlds.Library.Debug.DebugColor.Cyan);
        AutoRegister();

        var textManager = TaleWorlds.MountAndBlade.Module.CurrentModule.GlobalTextManager;
        var emptyTags = new List<GameTextManager.ChoiceTag>();

        foreach (var category in KeysCategories)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.CategoryId))
            {
                continue;
            }

            KeyContexts[category.CategoryId] = new GameKeyBinderContext(category.CategoryId, category.Keys);

            // Category display name
            textManager.GetGameText("str_key_category_name")
                       .AddVariationWithId(category.CategoryId, new TextObject(category.Category), emptyTags);

            foreach (var key in category.Keys)
            {
                string variationId = $"{category.CategoryId}_{key.KeyId}";

                textManager.GetGameText("str_key_name")
                           .AddVariationWithId(variationId, new TextObject(key.Name), emptyTags);

                textManager.GetGameText("str_key_description")
                           .AddVariationWithId(variationId, new TextObject(key.Description), emptyTags);
            }
        }
    }

    public static void RegisterContexts()
    {
        // Retrieve existing categories from HotKeyManager
        var keyList = HotKeyManager.GetAllCategories().ToList();

        // Add our custom contexts if they don't already exist in the list
        foreach (var context in KeyContexts.Values)
        {
            if (!keyList.Contains(context))
            {
                keyList.Add(context);
            }

            // Register all contexts, including custom ones
            HotKeyManager.RegisterInitialContexts(keyList, true); // Assuming this accepts the list
        }
    }

    private static void AutoRegister()
    {
        var binderTypes = Assembly.GetExecutingAssembly()
            .DefinedTypes
            .Where(t => typeof(IUseKeyBinder).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var type in binderTypes)
        {
            if (Activator.CreateInstance(type) is IUseKeyBinder binder && binder.BindedKeys != null)
            {
                KeysCategories.Add(binder.BindedKeys);
            }
        }
    }
}
