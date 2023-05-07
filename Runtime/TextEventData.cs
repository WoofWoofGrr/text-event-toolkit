using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static DevonMillar.TextEvents.TextEventToolkit;

namespace DevonMillar.TextEvents
{
    [CreateAssetMenu(fileName = "new Text Event", menuName = "Text Event", order = 0)]
    public class TextEventData : ScriptableObject
    {
        public static List<TextEventData> AllEventDatas { get; set; }
        public static IEnumerable<TextEventData> EnabledEventDatas => AllEventDatas.Where(e => e.Enabled);

        [field: SerializeField] public bool Enabled { get; set; } = true;
        [field: SerializeField] public bool BanAfterUse { get; set; }
        [field: SerializeField] public string Title { get; set; } = "New Text Event";
        [field: SerializeField] TextEvent.Result result;
        [field: SerializeField] public List<string> Labels { get; set; } = new();
        [field: SerializeField] public string Text { get; set; }
        [field: SerializeField] public int ID { get; set; }
        [field: SerializeField] public Sprite Image { get; set; }
        
        [field: SerializeField] public List<TextEvent.Choice> Choices { get; private set; } = new List<TextEvent.Choice>();

        public void AddChoice(TextEvent.Choice    _newChoice) => Choices.Add(_newChoice);
        public void RemoveChoice(TextEvent.Choice _choiceToRemove) => Choices.Remove(_choiceToRemove);

        void Register()
        {
            AllEventDatas ??= new();
            if (!AllEventDatas.Contains(this))
            {
                LogVerbose("Registering TextEventData: " + Title);
                AllEventDatas.Add(this);
            }
        }
        
        void Unregister()
        {
            AllEventDatas?.Remove(this);
        }
        
        void OnEnable()
        {
            Register();
        }

        void OnDestroy()
        {
            Unregister();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void ReloadAll()
        {
            LogVerbose("Reloading text event objects");
            foreach (TextEventData levelData in ScriptableObject.FindObjectsOfType<TextEventData>())
            {
                DestroyImmediate(levelData);
            }
            AllEventDatas ??= new();
            System.Diagnostics.Debug.Assert(AllEventDatas.Count == 0);
            AllEventDatas.Clear();

            Resources.LoadAll<TextEventData>("TextEventToolkit/TextEvents").ToList().ForEach(e => e.Register());

        }

        static TextEventData GetRandomData(System.Func<TextEventData, bool> _predicate = null)
        {
            return EnabledEventDatas.Where(_predicate ?? (_ => true)).GetRandom();
        }
        
        public static TextEventData GetRandomWhere(System.Func<TextEventData, bool> _predicate = null)
        {
            return GetRandomData(_predicate);
        }
        
        public static TextEvent CreateRandomWithLabel(string _label, System.Func<TextEventData, bool> _predicate = null)
        {
            Log("Creating TextEvent with label: " + _label);
            return EnabledEventDatas.Where(e => e.Labels.Contains(_label)).GetRandom().Create();
        }

        public TextEvent Create()
        {
            Log($"Creating TextEvent based on data of {Title}");

            TextEvent newEvent = new TextEvent(Title, Text, ID, result, Choices, Image);
            Log($"Successfully created TextEvent: {Title}");
            return newEvent;
        }
    }
}