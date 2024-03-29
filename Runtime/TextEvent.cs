using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using static DevonMillar.TextEvents.TextEvent;

namespace DevonMillar.TextEvents
{
    [System.Serializable]
    public partial class TextEvent
    {
        static List<string> bannedEventIDs = new List<string>();
        public static event System.Action<TextEvent> OnAnyTextEventEnter;
        static event System.Action ForceExitEvents;

        public static void ForceExitAllEvents() => ForceExitEvents?.Invoke();

        internal List<Choice> Choices { get; private set; }
        internal void AddChoice(Choice _newChoice) => Choices.Add(_newChoice);
        internal void RemoveChoice(Choice _choiceToRemove) => Choices.Remove(_choiceToRemove);
        internal int ID { get; private set; }
        internal string Title { get; set; }
        internal string Label { get; set; }
        internal string Text { get; set; }
        internal Sprite Image { get; set; }

        private Result result;
        public event System.Action OnTextEventEnter;
        public event System.Action<TextEvent> OnTextEventExit;
        public event System.Action<Choice, Result> OnChoiceSelected;
        
        internal event System.Action OnReadyToSelectChoice;
        public void ReadyToSelectChoice()
        {
            OnReadyToSelectChoice?.Invoke();
            OnReadyToSelectChoice = null;
        }

        public TextEvent(string _title, string _text, int _id, Result _result, IEnumerable<Choice> _choices, Sprite _image = null)
        {
            Text = _text;
            Title = _title;
            result = _result;
            Choices = new ();
            ID = _id;
            Image = _image;
            
            ForceExitEvents += ExitEvent;

            if (_choices != null)
            {
                List<MethodInfo> methods = TextEventPredicate.GetAll().Select(e => e.method).ToList();;

                Choices.AddRange(_choices.Where(e => !e.Condition.IsValid ||  (bool) e.Condition.GetMethodInfo(methods).
                                                                                      Invoke(null, e.Condition.Args.Select(argAndType => argAndType.arg).ToArray())));
            }

            SubscribeToChoiceEvents(Choices);
        }

        void SubscribeToChoiceEvents(IEnumerable<Choice> _choices)
        {
            foreach (Choice choice in _choices)
            {
                choice.OnChoiceSelected += (choice, result) => OnChoiceSelected?.Invoke(choice, result);
                choice.OnFinalChoice += (choice, result) => ExitEvent();
            }
        }

        public void EnterEvent()
        {
            GameObject.Instantiate(Resources.Load("TextEventCreator"));
            OnAnyTextEventEnter?.Invoke(this);
            OnTextEventEnter?.Invoke();

            result?.Execute();
        }
        public void ExitEvent()
        {
            OnTextEventExit?.Invoke(this);
            ForceExitEvents -= ExitEvent;

            OnTextEventEnter = null;
            OnChoiceSelected = null;
            OnTextEventExit = null;
            OnReadyToSelectChoice = null;
        }
    }
}