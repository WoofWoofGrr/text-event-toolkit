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
    public class SerializedMethodCall : ISerializationCallbackReceiver
    {
        [field: SerializeField] public string Name { get; set;}
        [SerializeField] public string argsJson;
        public ArgAndType[] Args { get; set; }
        
        public bool IsValid => !string.IsNullOrEmpty(Name) && Args != null;

        public MethodInfo GetMethodInfo(IEnumerable<MethodInfo> _methods)
        {
            return _methods.Where(e => e.Name == Name).Select(e => e).FirstOrDefault();
        }
        //args as object and it's type so we can serialize it and parse it back to the same object
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

        
        public SerializedMethodCall(string _name, params ArgAndType[] _args)
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
            if (Args == null) 
                return;
            
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
            [field: SerializeField] public string Text { get; set; }
            [field: SerializeField] public string AcknowledgmentText { get; set; } = TextEventToolkitSettings.Instance.DefaultAcknowledgmentText;
            [field: SerializeField] public float Chance { get; set; }

            [field: SerializeField] public List<SerializedMethodCall> ActionMethodNamesAndArgs { get; private set; }
            public bool IsFinal => string.IsNullOrEmpty(Text);

            List<System.Func<object>> actionMethods;
            System.Threading.Thread actionParseThread;

            public event System.Action OnResultAcknowledged; 
            
            public Result(string _text, float? chance, IEnumerable<SerializedMethodCall> _resultActions, bool _createDefaultChoice = true)
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
                foreach (SerializedMethodCall call in ActionMethodNamesAndArgs)
                {
                    if (!call.IsValid)
                        return;
                    
                    MethodInfo method = call.GetMethodInfo(methods);
                    
                    actionMethods ??= new();
                    actionMethods.Add(() => method.Invoke(null, call.Args.Select(e => e.arg).ToArray()));
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
