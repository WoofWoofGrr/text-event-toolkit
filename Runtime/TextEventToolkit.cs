using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using UnityEditor;
namespace DevonMillar.TextEvents
{
    public static class FilePaths
    {
        public static string HomePath => Path.Combine("Assets", "Resources", "TextEventToolkit");
        public static string SettingsPath => Path.Combine(HomePath, "TextEventToolkitSettings.asset");
        public static string TextEventPath => Path.Combine(HomePath, "TextEvents");
        #if UNITY_EDITOR
        public static string NewTextEventPath => AssetDatabase.GenerateUniqueAssetPath(Path.Combine(TextEventPath, "New Text Event.asset"));
        #endif
    }
    public static class TextEventToolkit
    {
        static HashSet<TextEventData> BannedEventDatas = new();
        
        public static TextEvent CreateRandom() => CreateFromData(TextEventData.GetRandomWhere(e => !BannedEventDatas.Contains(e)));
        public static TextEvent CreateRandomWithLabel(params string[] _labels) => CreateFromData(TextEventData.GetRandomWhere(e => !BannedEventDatas.Contains(e) && _labels.Any(l => e.Labels.Contains(l))));
        public static TextEvent CreateRandomWithAllLabels(params string[] _labels) => CreateFromData(TextEventData.GetRandomWhere(e => !BannedEventDatas.Contains(e) && _labels.All(l => e.Labels.Contains(l))));
        public static TextEvent CreateRandomWhere(System.Func<TextEventData, bool> _predicate) => CreateFromData(TextEventData.GetRandomWhere(e => !BannedEventDatas.Contains(e) && _predicate(e)));

        internal static TextEvent lastEvent;

        public static bool UsingController { get; set; } = false;
        
        static TextEvent CreateFromData(TextEventData newEventData)
        {
            if (newEventData == null)
            {
                Debug.LogWarning("TextEventToolkit: Tried to create a null event. You are either trying to create an event with a label that does not exist, an invalid predicate, or you have banned all events.");
                return null;
            }
            BanIfShouldBan(newEventData);
            
            return lastEvent = newEventData.Create();
        }

        static void BanIfShouldBan(TextEventData _data)
        {
            if (!_data.BanAfterUse)
                return;
            
            Log("Banning " + _data.Title + " from future use.");
            BannedEventDatas.Add(_data);
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Setup()
        {
            Application.quitting += Reset;
            BannedEventDatas = new();
        }
        static void Reset()
        {
            Log("TextEventToolKit: Resetting");
            Application.quitting -= Reset;
            BannedEventDatas.Clear();
        }
        public static void Log(string _msg)
        {
            if (TextEventToolkitSettings.Instance.LoggingLevel > 0)
            {
                Debug.Log("TextEventToolKit: " + _msg);
            }
        }
        public static void LogVerbose(string _msg)
        {
            if (TextEventToolkitSettings.Instance.LoggingLevel == TextEventToolkitSettings.LogLevel.Verbose)
            {
                Debug.Log("TextEventToolKit: " + _msg);
            }
        }
    }
}