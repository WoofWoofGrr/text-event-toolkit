using System.Collections.Generic;
using System.Linq;

namespace DevonMillar.TextEvents
{
    public partial class TextEvent
    {
        [System.Serializable]
        public class Result
        {
            public List<string> ActionMethodNames { get; private set; } = new();
            public string Text { get; set; }
            public bool IsFinal => Text == null || Text == "";
            public float Chance { get; set; }

            public List<Choice> Choices { get; private set; } = new List<Choice>();
            public void AddChoice(Choice _newChoice) => Choices.Add(_newChoice);
            public void RemoveChoice(Choice _choiceToRemove) => Choices.Remove(_choiceToRemove);

            List<System.Action> actionMethods = new();
            System.Threading.Thread actionParseThread;

            public Result(string _text, float? chance, IEnumerable<TextEvent.Choice> _choices, IEnumerable<string> _resultActions)
            {
                ActionMethodNames = new();
                Choices = new();

                if (_resultActions != null)
                {
                    ActionMethodNames.AddRange(_resultActions);
                }
                if (_choices != null)
                {
                    Choices.AddRange(_choices);
                }
                Text = _text;
                Chance = chance ?? 100.0f;
                if (!IsFinal)
                {
                    Choices.Add(Serializer.CreateChoice(Choice.CreateDefaultChoiceNode()));
                }

                //this is really really slow so do it on it's own thread, hopefully it's done by the time the player is finished reading so there will be no lag
                actionParseThread = new System.Threading.Thread(ParseActions);
                actionParseThread.Start();
            }

            void ParseActions()
            {
                List<TextEventAction.AtributeAndMethod> methods = TextEventAction.GetAll();
                foreach (string actionStr in ActionMethodNames)
                {
                    System.Reflection.MethodInfo method = methods.Where(e => e.method.Name == actionStr).Select(e => e.method).First();
                    actionMethods.Add(() => method.Invoke(null, null));
                }
            }

            //run all the actions the result contains
            public void Execute()
            {
                //wait until the other thread has parsed the actions
                actionParseThread.Join();
                //run the actions once parsing them has finished
                actionMethods.ForEach(method => method.Invoke());
            }
        }
    }
}
