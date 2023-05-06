using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
namespace DevonMillar.TextEvents
{
    [System.Serializable]
    public class TextEventToolkitSettings : ScriptableObject
    {
        public enum LogLevel
        {
            None,
            Normal,
            Verbose,
        }

        static TextEventToolkitSettings CreateNewSettingsAsset()
        {
            TextEventToolkitSettings newObj = CreateInstance<TextEventToolkitSettings>();
            AssetDatabase.CreateAsset(newObj, FilePaths.SettingsPath);
            return Resources.Load<TextEventToolkitSettings>("TextEventToolkit/TextEventToolkitSettings");
        }
        static TextEventToolkitSettings instance;
        public static TextEventToolkitSettings Instance
        {
            get
            {
                instance ??= Resources.Load<TextEventToolkitSettings>("TextEventToolkit/TextEventToolkitSettings") ?? CreateNewSettingsAsset();
                return instance;
            }
        }

        [field: SerializeField] public Assembly ActionAssembly { get; set; }
        [field: SerializeField] public LogLevel LoggingLevel { get; set; }
        [field: SerializeField] public string DefaultAcknowledgmentText { get; set; } = "Continue...";
        [field: SerializeField] public bool DefaultFirstLabel { get; set; } = true;
        [field: SerializeField] public bool BanByDefault { get; set; } = false;
        [field: SerializeField] public List<string> Labels { get; set; }
    }
}