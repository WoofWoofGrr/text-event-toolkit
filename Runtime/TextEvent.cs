using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Linq;
using UnityEditor;
using System.Xml.Linq;

namespace DevonMillar.TextEvents
{
    [System.Serializable]
    public class TextEvent
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

        public List<Choice> Choices { get; private set; }
        public int ID { get; private set; }
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

        public void AddChoice(Choice _newChoice)
        {
            Choices.Add(_newChoice);
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

        [System.Serializable]
        public class Choice
        {
            public List<Result> Results { get; private set; }
            public string Text { get; set; }

            public string PostText { get; private set; }
            public event System.Action<Choice, Result> OnChoiceSelected;
            public event System.Action<Choice, Result> OnFinalChoice;

            public static XElement CreateDefaultChoiceNode()
            {
                XElement e = new XElement("Choice");
                e.SetAttributeValue("text", "Continue...");
                return e;
            }

            public Choice(string _text, IEnumerable<Result> _results, string _postText = "")
            {
                Text = _text;
                Results = new();

                if (_results != null)
                {
                    Results.AddRange(_results);
                }
            }

            public void AddResult(Result _newResult)
            {
                Results.Add(_newResult);
            }
            public void RemoveResult(Result _result)
            {
                Results.Remove(_result);
            }

            public Result Pick()
            {
                Result chosenResult = null;
                if (Results.Count > 0)
                {
                    chosenResult = Results.First();

                    if (Results.Count > 1)
                    {
                        float totalChance = Results.Sum(result => result.Chance.Value);
                        float roll = Random.Range(0, totalChance);

                        //pick a random result based on the chance weightings
                        foreach (Result result in Results)
                        {
                            roll -= result.Chance.Value;
                            if (roll <= 0.0f)
                            {
                                chosenResult = result;
                                break;
                            }
                        }
                    }

                    chosenResult.Execute();
                    OnChoiceSelected?.Invoke(this, chosenResult);
                }

                if (chosenResult == null || chosenResult.IsFinal)
                {
                    OnFinalChoice?.Invoke(this, chosenResult);
                    OnFinalChoice = null;
                }
                else if (chosenResult != null) //has result but not final result
                {
                    //pass final choice up the stack
                    chosenResult.Choices.ForEach(choice => choice.OnFinalChoice += (_choice, _result) =>
                    {
                        OnFinalChoice?.Invoke(_choice, _result);
                        OnFinalChoice = null;
                    });
                }

                OnChoiceSelected = null;
                return chosenResult;
            }

            public override string ToString()
            {
                string s = "\n";
                s += "Choice: ";
                s += Text;
                s += "\n";
                s += PostText;
                s += "\n";

                foreach (Result result in Results)
                {
                    s += result;
                }

                return s;
            }
        }

        //print
        public override string ToString()
        {
            string s = "";
            s += "Text event - " + ID + "\n";
            s += Text;
            s += "\n";

            if (result != null)
            {
                s += result;
            }
            foreach (Choice choice in Choices)
            {
                s += choice;

            }
            return s;
        }

        [System.Serializable]
        public class Result
        {
            public List<System.Action> Actions { get; private set; } = new();
            public string Text { get; set; }
            public bool IsFinal => Text == null || Text == "";
            public List<Choice> Choices { get; private set; } = new();
            public float? Chance { get; private set; } = null;

            public Result(string _text, float? chance, IEnumerable<TextEvent.Choice> _choices, IEnumerable<System.Action> _resultActions)
            {
                Actions = new();
                Choices = new();

                if (_resultActions != null)
                {
                    Actions.AddRange(_resultActions);
                }
                if (_choices != null)
                {
                    Choices.AddRange(_choices);
                }
                Text = _text;
                Chance = chance;
                if (!IsFinal)
                {
                    Choices.Add(Serializer.CreateChoice(Choice.CreateDefaultChoiceNode()));
                }
            }

            public void AddChoice(Choice _newChoice)
            {
                Choices.Add(_newChoice);
            }

            //run all the actions the result contains
            public void Execute()
            {
                foreach (System.Action action in Actions)
                {
                    action.Invoke();
                }
            }

            public override string ToString()
            {
                string s = "";
                s += "    Result: ";
                if (Chance != null)
                {
                    s += "(" + Chance + "%)";
                }
                s += "\n";
                if (Text != null)
                {
                    s += "        " + Text + "\n";
                }

                foreach (System.Action action in Actions)
                {
                    s += "        Action: " + action + "\n";
                }
                return s;
            }
        }
    }
}