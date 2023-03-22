using System.Collections.Generic;
using UnityEngine;
using System.Xml.Linq;
using static DevonMillar.TextEvents.TextEvent;

namespace DevonMillar.TextEvents
{
    [System.Serializable]
    public partial class TextEvent
    {
        static List<string> bannedEventIDs = new List<string>();
        public static event System.Action<TextEvent> OnAnyTextEventEnter;
        static event System.Action ForceExitEvents;

        public static void Refresh() => Serializer.Init();

        public static void ForceExitAllEvents() => ForceExitEvents?.Invoke();

        //TODO: use GetAllEventIDs
        public static TextEvent CreateRandom(bool allowBanned = false)
        {
            //TODO: ban check
            return Serializer.CreateEvent(Serializer.GetRandomNode(x => !bannedEventIDs.Contains(x.Attribute("id").Value)));
        }

        public static TextEvent CreateFromIndex(int _index, bool allowBanned = false)
        {
            XElement eventNode;
            eventNode = Serializer.GetNode(_index);

            if (eventNode != null && !allowBanned && bannedEventIDs.Contains(eventNode.Attribute("id").Value))
            {
                return null;
            }
            
            return Serializer.CreateEvent(eventNode);
        }

        public List<Choice> Choices { get; private set; } = new List<Choice>();
        public void AddChoice(Choice _newChoice) => Choices.Add(_newChoice);
        public void RemoveChoice(Choice _choiceToRemove) => Choices.Remove(_choiceToRemove); public int ID { get; private set; }
        public string Title { get; set; }
        public string Text { get; set; }

        private Result result;
        public event System.Action OnTextEventEnter;
        public event System.Action<TextEvent> OnTextEventExit;
        public event System.Action<Choice, Result> OnChoiceSelected;

        public TextEvent(string _title, string _text, int _id, Result _result, IEnumerable<Choice> _choices)
        {
            Text = _text;
            Title = _title;
            result = _result;
            Choices = new ();
            ID = _id;
            
            ForceExitEvents += ExitEvent;

            if (_choices != null)
            {
                Choices.AddRange(_choices);
            }

            SubscribeToChoiceEvents(Choices);
        }

        void SubscribeToChoiceEvents(IEnumerable<Choice> _choices)
        {
            foreach (Choice choice in _choices)
            {
                choice.OnChoiceSelected += (choice, result) => OnChoiceSelected(choice, result);
                choice.OnFinalChoice += (choice, result) => ExitEvent();

                //recurse for any branching choices
                foreach (Result result in choice.Results)
                {
                    SubscribeToChoiceEvents(result.Choices);
                }
            }
        }

        public void EnterEvent()
        {
            GameObject.Instantiate(Resources.Load("TextEventCreator"));
            OnAnyTextEventEnter?.Invoke(this);
            OnTextEventEnter?.Invoke();

            if (result != null)
            {
                result.Execute();
            }
        }
        public void ExitEvent()
        {
            OnTextEventExit?.Invoke(this);
            ForceExitEvents -= ExitEvent;

            OnTextEventEnter = null;
            OnChoiceSelected = null;
            OnTextEventExit = null;
        }
    }
}