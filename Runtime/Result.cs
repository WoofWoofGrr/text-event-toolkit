using System.Collections.Generic;
using System.Linq;

namespace DevonMillar.TextEvents
{
    public class MethodNameAndArgs
    {
        public string Name { get; set;}
        public object[] Args { get; set; }

        public MethodNameAndArgs(string _name, params object[] _args)
        {
            Name = _name;
            Args = _args;
        }
    }

    public partial class TextEvent
    {
        [System.Serializable]
        public class Result
        {
            public List<MethodNameAndArgs> ActionMethodNamesAndArgs { get; private set; } = new();
            public string Text { get; set; }
            public bool IsFinal => Text == null || Text == "";
            public float Chance { get; set; }

            public List<Choice> Choices { get; private set; } = new List<Choice>();
            public void AddChoice(Choice _newChoice) => Choices.Add(_newChoice);
            public void RemoveChoice(Choice _choiceToRemove) => Choices.Remove(_choiceToRemove);

            List<System.Action> actionMethods = new();
            System.Threading.Thread actionParseThread;

            public Result(string _text, float? chance, IEnumerable<TextEvent.Choice> _choices, IEnumerable<MethodNameAndArgs> _resultActions)
            {
                ActionMethodNamesAndArgs = new();
                Choices = new();

                if (_resultActions != null)
                {
                    ActionMethodNamesAndArgs.AddRange(_resultActions);
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
                foreach (MethodNameAndArgs methodNameAndArgs in ActionMethodNamesAndArgs)
                {
                    System.Reflection.MethodInfo method = methods.Where(e => e.method.Name == methodNameAndArgs.Name).Select(e => e.method).First();
                    actionMethods.Add(() => method.Invoke(null, methodNameAndArgs.Args));
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
