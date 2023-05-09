using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
[assembly: InternalsVisibleTo("devonmillar.TextEventToolkit.Editor")]
namespace DevonMillar.TextEvents
{
    [System.Serializable]
    internal class TextEventToolkitSettings : ScriptableObject
    {
        internal enum LogLevel
        {
            None,
            Normal,
            Verbose,
        }

        static TextEventToolkitSettings CreateNewSettingsAsset()
        {
            TextEventToolkitSettings newObj = CreateInstance<TextEventToolkitSettings>();
            #if UNITY_EDITOR
            AssetDatabase.CreateAsset(newObj, FilePaths.SettingsPath);
            #endif           
            return Resources.Load<TextEventToolkitSettings>("TextEventToolkit/TextEventToolkitSettings");
        }
        static TextEventToolkitSettings instance;
        internal static TextEventToolkitSettings Instance
        {
            get
            {
                instance ??= Resources.Load<TextEventToolkitSettings>("TextEventToolkit/TextEventToolkitSettings") ?? CreateNewSettingsAsset();
                return instance;
            }
        }

        [field: SerializeField] internal LogLevel LoggingLevel { get; private set; }
        [field: SerializeField] internal string DefaultAcknowledgmentText { get; private set; } = "Continue...";
        [field: SerializeField] internal bool DefaultFirstLabel { get; private set; } = true;
        [field: SerializeField] internal bool BanByDefault { get; private set; } = false;
        [field: SerializeField] internal List<string> Labels { get; private set; }
        [field: SerializeField] internal TMPro.TMP_FontAsset Font { get; private set; }
        
        [field: SerializeField] internal string ActionAssembly { get; private set; }
        [field: SerializeField] internal string PredicateAssembly { get; private set; }
        
    }
}