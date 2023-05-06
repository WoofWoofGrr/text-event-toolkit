using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = System.Object;

namespace DevonMillar.TextEvents
{
    [System.Serializable]
    public class MethodNameAndArgs : ISerializationCallbackReceiver
    {
        [field: SerializeField] public string Name { get; set;}
        [SerializeField] public string argsJson;
        public ArgAndType[] Args { get; set; }

        public MethodInfo GetMethodInfo(IEnumerable<MethodInfo> _methods)
        {
            return _methods.Where(e => e.Name == Name).Select(e => e).First();
        }
        [System.Serializable]
        public class ArgAndType
        {
            public ArgAndType()
            {
                arg = null;
                type = null;
            }
            public Object arg;
            public System.Type type;
        }

        
        public MethodNameAndArgs(string _name, params ArgAndType[] _args)
        {
            Name = _name;
            Args = _args;
        }
        public void OnBeforeSerialize()
        {
            argsJson = JsonConvert.SerializeObject(Args, Formatting.None, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
            });
        }

        public void OnAfterDeserialize()
        {
            Args = JsonConvert.DeserializeObject<ArgAndType[]>(argsJson);
            foreach (ArgAndType argAndType in Args)
            {
                if (argAndType.type.IsEnum)
                {
                    argAndType.arg = (argAndType.arg is DBNull) ? Enum.GetValues(argAndType.type).GetValue(0) : Enum.Parse(argAndType.type, argAndType.arg.ToString());
                }
                else
                {
                    argAndType.arg = Convert.ChangeType(argAndType.arg, argAndType.type); //this is just to make sure the type is correct (it's not always correct
                }
            }
        }
    }

    public partial class TextEvent
    {
        [System.Serializable]
        public class Result : ISerializationCallbackReceiver
        {
            [field: SerializeField] public string Text { get; set; } = "New result";
            [field: SerializeField] public string AcknowledgmentText { get; set; } = TextEventToolkitSettings.Instance.DefaultAcknowledgmentText;
            [field: SerializeField] public float Chance { get; set; } = 100.0f;

            [field: SerializeField] public List<MethodNameAndArgs> ActionMethodNamesAndArgs { get; private set; } = new();
            public bool IsFinal => string.IsNullOrEmpty(Text);

            List<System.Func<object>> actionMethods;
            System.Threading.Thread actionParseThread;

            public event System.Action OnResultAcknowledged; 
            
            public Result(string _text, float? chance, IEnumerable<MethodNameAndArgs> _resultActions, bool _createDefaultChoice = true)
            {
                ActionMethodNamesAndArgs = new();

                if (_resultActions != null)
                {
                    ActionMethodNamesAndArgs.AddRange(_resultActions);
                }
                Text = _text;
                Chance = chance ?? 100.0f;
            }
            public void UpdateActions()
            {
                //this is really really slow so do it on it's own thread, hopefully it's done by the time the player is finished reading so there will be no lag
                actionParseThread = new System.Threading.Thread(ParseActions);
                actionParseThread.Start();
                actionParseThread?.Join();
            }

            public void RemoveActionMethodNamesAndArgs(int _index)
            {
                ActionMethodNamesAndArgs.RemoveAt(_index);
            }
            void ParseActions()
            {
                List<MethodInfo> methods = TextEventAction.GetAll().Select(e => e.method).ToList();
                foreach (MethodNameAndArgs methodNameAndArgs in ActionMethodNamesAndArgs)
                {
                    MethodInfo method = methodNameAndArgs.GetMethodInfo(methods);
                    
                    actionMethods ??= new();
                    actionMethods.Add(() => method.Invoke(null, methodNameAndArgs.Args.Select(e => e.arg).ToArray()));
                }
            }

            //run all the actions the result contains
            public List<object> Execute()
            {
                List<object> returns = new();

                //wait until the other thread has parsed the actions
                //run the actions once parsing them has finished
                actionMethods?.ForEach(method => returns.Add(method.Invoke()));
                return returns;
            }
            public void OnBeforeSerialize()
            {
                
            }
            public void OnAfterDeserialize()
            {
                UpdateActions();
            }
            //called by UI
            public void AcknowledgedResult()
            {
                OnResultAcknowledged?.Invoke();
            }
        }
    }
}
