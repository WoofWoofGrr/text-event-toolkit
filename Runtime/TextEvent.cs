using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;

namespace DevonMillar.TextEvents
{
    public class XMLNodeBased
    {
        public string Text { get; protected set; }

        public XMLNodeBased(XElement _node)
        {
            Text = _node.Attribute("text")?.Value;
        }
    }

    public class TextEvent : XMLNodeBased
    {
        static bool initilized = false;
        static XDocument textEventsData;
        static List<string> bannedEventIDs = new List<string>();
        public static event System.Action<TextEvent> OnAnyTextEventEnter;
        static event System.Action ForceExitEvents;

        private static void Init(bool _force = false)
        {
            if (!_force && initilized)
            {
                return;
            }
            Object file = (Object)AssetDatabase.LoadAssetAtPath("/TextEvents", typeof(Object));

            textEventsData = XDocument.Parse(Resources.Load("TextEvents").ToString());
            Debug.Log("Loaded text event XML data");
            initilized = true;
        }

        public static void Refresh() => Init(true);

        public static void ForceExitAllEvents() => ForceExitEvents?.Invoke();

        //TODO: use GetAllEventIDs
        public static TextEvent CreateRandom()
        {
            Init();

            XElement eventNode = textEventsData.Descendants("Event").Where(x => !bannedEventIDs.Contains(x.Attribute("id").Value)).GetRandom();

            if (eventNode == null)
            {
                Debug.LogWarning("Could not find any events that aren't banned.");
                return null;
            }

            return new TextEvent(eventNode);
        }
        public static TextEvent CreateFromID(string _id, bool allowBanned = false)
        {
            Init();

            XElement eventNode;
            if (!allowBanned)
            {
                eventNode = textEventsData.Descendants("Event").Where(x => !bannedEventIDs.Contains(x.Attribute("id").Value) && x.Attribute("id").Value == _id).FirstOrDefault();
            }
            else
            {
                eventNode = textEventsData.Descendants("Event").Where(x => x.Attribute("id").Value == _id).FirstOrDefault();
            }

            return new TextEvent(eventNode);

        }

        public static List<string> GetAllEventIDs(bool _includeBanned = false, bool _includeDebug = false)
        {
            List<string> eventIDs = new List<string>();
            Init();

            System.Func<XElement, bool> bannedPredicate = (x) => _includeBanned || !bannedEventIDs.Contains(x.Attribute("id").Value);
            System.Func<XElement, bool> debugPredicate = (x) => _includeBanned || ((x.Attribute("debug")?.Value ?? "false") != "true");

            eventIDs.AddRange(textEventsData.Descendants("Event").Where(x => bannedPredicate(x) && debugPredicate(x)).Select(x => x.Attribute("id").Value));


            return eventIDs;

        }

        public List<Choice> Choices { get; private set; }
        public string ID { get; private set; }
        public string Title => title != null ? title : ID;

        private string title;

        private Result result;
        public event System.Action OnTextEventEnter;
        public event System.Action<TextEvent> OnTextEventExit;
        public event System.Action<Choice, Result> OnChoiceSelected;


        //create instance from XML event
        private TextEvent(XElement _eventNode) : base(_eventNode)
        {
            ID = _eventNode.Attribute("id").Value;
            title = _eventNode.Attribute("title")?.Value;
            ForceExitEvents += ExitEvent;

            XElement resultNode = _eventNode.Elements("Result").FirstOrDefault();
            if (resultNode != null)
            {
                result = new Result(resultNode);
            }

            List<XElement> choiceNodes = _eventNode.Elements("Choice").ToList();

            Choices = new();

            //create default choice if the event has none
            if (choiceNodes.Count() == 0)
            {
                choiceNodes.Add(Choice.CreateDefaultChoiceNode());
            }
            foreach (XElement choiceNode in choiceNodes)
            {
                Choice newChoice = new Choice(choiceNode);
                newChoice.OnChoiceSelected += (choice, result) => OnChoiceSelected(choice, result);
                newChoice.OnFinalChoice += (choice, result) => ExitEvent();
                Choices.Add(newChoice);
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

        public class Choice : XMLNodeBased
        {

            public static XElement CreateDefaultChoiceNode()
            {
                XElement e = new XElement("Choice");
                e.SetAttributeValue("text", "Continue...");
                return e;
            }
            List<Result> results = new();
            public string PostText { get; private set; }
            public event System.Action<Choice, Result> OnChoiceSelected;
            public event System.Action<Choice, Result> OnFinalChoice;

            public Choice(XElement _choiceNode) : base(_choiceNode)
            {
                results.AddRange(
                    _choiceNode.Elements("Result").Select(node => new Result(node))
                    );
                PostText = _choiceNode.Attribute("postText")?.Value ?? "";
            }

            public Result Pick()
            {
                Result chosenResult = null;
                if (results.Count > 0)
                {
                    chosenResult = results.First();

                    if (results.Count > 1)
                    {
                        float totalChance = results.Sum(result => result.Chance.Value);
                        float roll = Random.Range(0, totalChance);

                        //pick a random result based on the chance weightings
                        foreach (Result result in results)
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

                foreach (Result result in results)
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

        //optional text & actions parsed from the xml data to perform on event enter or choice
        public class Result : XMLNodeBased
        {
            List<System.Action> resultActions = new();
            public bool IsFinal => Text == null;
            public List<Choice> Choices { get; private set; } = new();

            public float? Chance { get; private set; } = null;

            public Result(XElement _resultNode) : base(_resultNode)
            {
                string chanceStr = _resultNode.Attribute("chance")?.Value;
                if (chanceStr != null)
                {
                    Chance = float.Parse(chanceStr);
                }
                if (!IsFinal)
                {
                    Choices.Add(new Choice(Choice.CreateDefaultChoiceNode()));
                }

                foreach (XElement actionNode in _resultNode.Elements("Action"))
                {
                    var method = typeof(TextEventActions).GetMethod("GiveCard");
                    System.Action call = () => method.Invoke(null, new object[] { });

                    resultActions.Add(call);

                }
                //Debug.Log(_resultNode.Elements("Action").First().FirstNode.ToString());
            }

            //run all the actions
            public void Execute()
            {
                foreach (System.Action action in resultActions)
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

                foreach (System.Action action in resultActions)
                {
                    s += "        Action: " + action + "\n";
                }
                return s;
            }
        }
    }
}