using Game.SceneFlow;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BelzontWE
{
    public partial class WETemplateManager
    {
        #region System Communication API
        // Internal API for communication between WETemplateManager and the new systems

        /// <summary>
        /// Checks if the game is in a loading or initializing state
        /// </summary>
        internal bool IsGameLoadingOrInitializing =>
            GameManager.instance.isGameLoading || !WriteEverywhereCS2Mod.IsInitializationComplete;

        /// <summary>
        /// Flag indicating templates have changed and entities need updating
        /// </summary>
        internal bool TemplatesDirty
        {
            get => m_templatesDirty;
            set => m_templatesDirty = value;
        }

        /// <summary>
        /// Tries to get a city template by name
        /// </summary>
        internal bool TryGetCityTemplate(FixedString128Bytes name, out WETextDataXmlTree template)
        {
            return RegisteredTemplates.TryGetValue(name, out template);
        }

        /// <summary>
        /// Tries to get a prefab template by index
        /// </summary>
        internal bool TryGetPrefabTemplate(long prefabIndex, out WETextDataXmlTree template)
        {
            return PrefabTemplates.TryGetValue(prefabIndex, out template);
        }

        /// <summary>
        /// Tries to get a mod subtemplate
        /// </summary>
        internal bool TryGetModSubtemplate(string modId, string templateName, out WETextDataXmlTree template)
        {
            template = null;
            return ModsSubTemplates.TryGetValue(modId, out var modTemplates)
                && modTemplates.TryGetValue(templateName, out template);
        }

        /// <summary>
        /// Gets the entity query for template-based entities
        /// </summary>
        internal EntityQuery GetTemplateBasedEntitiesQuery() => m_templateBasedEntities;

        /// <summary>
        /// Gets read-only access to prefab templates dictionary
        /// </summary>
        internal IReadOnlyDictionary<long, WETextDataXmlTree> GetPrefabTemplatesReadOnly() => PrefabTemplates;

        /// <summary>
        /// Clears the templates dirty flag
        /// </summary>
        internal void ClearTemplatesDirty() => m_templatesDirty = false;

        /// <summary>
        /// Gets whether entities are currently being updated on main thread
        /// </summary>
        internal Coroutine UpdatingEntitiesOnMain => m_updatingEntitiesOnMain;

        /// <summary>
        /// Tries to resolve a template by name, checking both city templates and mod subtemplates.
        /// Supports both simple names and "modId:templateName" format.
        /// </summary>
        internal bool TryGetTargetTemplate(FixedString128Bytes layoutName, out WETextDataXmlTree targetTemplate)
        {
            targetTemplate = null;
            return layoutName.ToString().Split(":", 2) is string[] modEntryName && modEntryName.Length == 2
                                      ? ModsSubTemplates.TryGetValue(modEntryName[0], out var modTemplates) && modTemplates.TryGetValue(modEntryName[1], out targetTemplate)
                                      : RegisteredTemplates.TryGetValue(layoutName, out targetTemplate);
        }

        #endregion
    }
}
