using UnityEngine;

namespace MysticCombat.Data
{
    /// <summary>
    /// Static utility class for loading character data from JSON files.
    /// </summary>
    public static class CharacterDataLoader
    {
        #region Constants

        private const string CHARACTERS_SUBFOLDER = "Characters";

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads a CharacterData from a JSON file matching the given character ID.
        /// Searches StreamingAssets/Characters/ first, then falls back to Assets/Data/Characters/.
        /// </summary>
        /// <param name="characterId">The unique identifier for the character (matches the JSON filename without extension).</param>
        /// <returns>The deserialized CharacterData, or null if the file was not found or failed to parse.</returns>
        public static CharacterData Load(string characterId)
        {
            string fileName = characterId + ".json";

            // Primary path: StreamingAssets/Characters/
            string primaryPath = System.IO.Path.Combine(Application.streamingAssetsPath, CHARACTERS_SUBFOLDER, fileName);
            // Fallback path: Assets/Data/Characters/
            string fallbackPath = System.IO.Path.Combine(Application.dataPath, "Data", CHARACTERS_SUBFOLDER, fileName);

            string json = null;

            if (System.IO.File.Exists(primaryPath))
            {
                json = System.IO.File.ReadAllText(primaryPath);
                Debug.Log($"[CharacterDataLoader] Loaded '{characterId}' from StreamingAssets.");
            }
            else if (System.IO.File.Exists(fallbackPath))
            {
                json = System.IO.File.ReadAllText(fallbackPath);
                Debug.Log($"[CharacterDataLoader] Loaded '{characterId}' from Data fallback path.");
            }
            else
            {
                Debug.LogError($"[CharacterDataLoader] Character JSON not found for '{characterId}'.\n" +
                               $"  Searched: {primaryPath}\n  Searched: {fallbackPath}");
                return null;
            }

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"[CharacterDataLoader] JSON file for '{characterId}' was empty.");
                return null;
            }

            try
            {
                CharacterData data = JsonUtility.FromJson<CharacterData>(json);
                Debug.Log($"[CharacterDataLoader] Successfully parsed character: {data.displayName ?? characterId}");
                return data;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CharacterDataLoader] Failed to parse JSON for '{characterId}': {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
