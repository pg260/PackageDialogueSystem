using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using System.Linq;
using UnityEngine;

public static class EditorLocalizationUtils
{
    public static string GetEditorLocalizedString(LocalizedString ls)
    {
        if (ls == null || ls.IsEmpty) return "";
        var locale = LocalizationEditorSettings.GetLocales().FirstOrDefault();
        if (locale == null)
        {
            Debug.LogWarning("Any languages not found");
            return "<i>empty</i>";
        }

        var table = LocalizationEditorSettings.GetStringTableCollection(ls.TableReference)
            ?.GetTable(locale.Identifier) as StringTable;

        if (table == null) 
        {
            Debug.LogWarning("Table not found");
            return "<i>empty</i>";
        }

        table.TryGetValue(ls.TableEntryReference.KeyId, out var entry);
        if (entry == null)
        {
            Debug.LogWarning($"Entry {ls.TableEntryReference.KeyId} not found");
            return "<i>empty</i>";
        }
        else
        {
            return entry.Value;
        }
        
    }
}