using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DevonMillar.TextEvents
{
    public class TextEventEditor : EditorWindow
    {
        [SerializeField] int index = 0;

        TextEvent selectedEvent = null;
        GUIStyle textAreaStyle;

        static System.Action OnReload;
        Queue<System.Action> workQueue = new();
        Dictionary<object, bool> foldouts = new();
        Dictionary<object, int> dropDownIndex = new();
        Dictionary<object, object> args = new();

        Color[] indentColors;
        List<TextEventAction.AtributeAndMethod> availableActions;

        [MenuItem("Tools/Text Event Editor")]
        public static void ShowWindow()
        {
            TextEventEditor instance = GetWindow<TextEventEditor>("Text Event Editor");
        }

        void ReloadInstance()
        {
            ChangeEvent(index);
        }

        void DrawEvent(TextEvent _event)
        {
            GUILayout.Label("Event title");
            _event.Title = EditorGUILayout.TextField(_event.Title);

            GUILayout.Label("Event body");
            _event.Text = EditorGUILayout.TextArea(_event.Text, new GUIStyle(EditorStyles.textArea), GUILayout.Height(position.height / 5.0f));

            GUILayout.Label("Choices");

            DrawAllChoices(_event.Choices, (choice) => _event.Choices.Remove(choice));

            if (GUILayout.Button("Add choice"))
            {
                _event.AddChoice(new TextEvent.Choice("A choice", null));
            }
        }

        void DrawAllChoices(IEnumerable<TextEvent.Choice> _choices, System.Action<TextEvent.Choice> _deleteAction)
        {
            int i = 1;
            foreach (TextEvent.Choice choice in _choices)
            {
                DrawChoice(choice, _deleteAction);
                i++;
            }
        }

        void DrawChoice(TextEvent.Choice _choice, System.Action<TextEvent.Choice> _deleteAction)
        {
            if (!foldouts.ContainsKey(_choice))
            {
                foldouts.Add(_choice, false);
            }

            GUIStyle vertColorStyle = GUI.skin.box;

            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, indentColors[EditorGUI.indentLevel % indentColors.Length]);
            tex.Apply();
            vertColorStyle.normal.background = tex;

            GUILayout.BeginHorizontal();
            _choice.Text = EditorGUILayout.TextField(_choice.Text, textAreaStyle);

            string infoText = "";

            if (_choice.Results.Count > 0)
            {
                infoText += " - " + _choice.Results.Count + " result" + (_choice.Results.Count > 1 ? "s" : "");
            }
            foldouts[_choice] = EditorGUILayout.Foldout(foldouts[_choice], infoText);

            GUILayout.Space(10);

            if (GUILayout.Button("Delete choice", GUILayout.MaxWidth(100)))
            {
                //add the delete action to the queue to delete later in case we're in a loop
                workQueue.Enqueue(() => _deleteAction(_choice));
            }

            GUILayout.EndHorizontal();

            if (!foldouts[_choice])
            {
                return;
            }
            GUILayout.BeginVertical(vertColorStyle);

            //draw each possible result of this choice
            _choice.Results.ForEach(e => DrawResult(e, () => _choice.RemoveResult(e)));

            GUILayout.BeginHorizontal();

            GUILayout.Space(15.0f * EditorGUI.indentLevel);

            if (GUILayout.Button("Add result", GUILayout.MaxWidth(100)))
            {
                _choice.AddResult(new TextEvent.Result("", null, null, null));
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        void DrawResult(TextEvent.Result _result, System.Action _deleteAction)
        {
            EditorGUI.indentLevel += 2;
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Result");
            if (GUILayout.Button("Delete result", GUILayout.MaxWidth(100)))
            {
                //delete this later in case we're in a loop
                workQueue.Enqueue(_deleteAction);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Text");
            EditorGUILayout.LabelField("Chance");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            _result.Text = EditorGUILayout.TextField(_result.Text);
            _result.Chance = EditorGUILayout.FloatField(_result.Chance);
            GUILayout.EndHorizontal();


            if (availableActions.Count > 0)
            {
                GUILayout.Space(15.0f * EditorGUI.indentLevel);
                EditorGUILayout.LabelField("Actions");

                //loop over each action in the result
                for (int i = 0; i < _result.ActionMethodNamesAndArgs.Count; i++) 
                {
                    string[] actionOptions = availableActions.Select(e => e.attribute.Name).ToArray();

                    object key = _result + _result.ActionMethodNamesAndArgs[i].Name + i;

                    //load saved dropdown index or create one if none exists
                    if (!dropDownIndex.ContainsKey(key))
                    {
                        dropDownIndex.Add(key, -1);
                    }

                    EditorGUILayout.BeginHorizontal();
                    int newIndex = EditorGUILayout.Popup(
                    "Action:",
                    dropDownIndex[key],
                    actionOptions
                    );
                    
                    if (newIndex < 0)
                    {
                        EditorGUILayout.EndHorizontal();
                        return;
                    }
                    

                    if (newIndex != dropDownIndex[key])
                    {
                        dropDownIndex[key] = newIndex;
                        _result.ActionMethodNamesAndArgs[i].Name = availableActions[newIndex].method.Name;
                    }

                    //loop over the parameters of the selected action method


                    var methodParams = availableActions[newIndex].method.GetParameters();

                    object[] resultActionArgs = new object[methodParams.Length];

                    for (int j = 0; j < methodParams.Length; j++)
                    {
                        object argKey = i + availableActions[newIndex].method.Name + methodParams[j].Name;
                        Debug.Log(argKey);
                        if (!args.ContainsKey(argKey))
                        {
                            args.Add(argKey, methodParams[j].DefaultValue);
                        }

                        args[argKey] = DrawArgField(methodParams[j].ParameterType, args[argKey]);
                        resultActionArgs[j] = args[argKey];
                    }

                    _result.ActionMethodNamesAndArgs[i].Args = resultActionArgs;

                    EditorGUILayout.EndHorizontal();


                }
                GUILayout.BeginHorizontal();
                GUILayout.Space(15.0f * EditorGUI.indentLevel);
                if (GUILayout.Button("Add action", GUILayout.MaxWidth(100)))
                {
                    //TODO: adder method
                    _result.ActionMethodNamesAndArgs.Add(new MethodNameAndArgs("", null));
                }
                GUILayout.EndHorizontal();
            }


            EditorGUILayout.LabelField("Choices");
            DrawAllChoices(_result.Choices, (choice) => _result.RemoveChoice(choice));

            GUILayout.BeginHorizontal();
            GUILayout.Space(15.0f * EditorGUI.indentLevel);
            if (GUILayout.Button("Add choice", GUILayout.MaxWidth(100)))
            {
                _result.AddChoice(new TextEvent.Choice("A choice", null));
            }

            GUILayout.EndHorizontal();
            EditorGUI.indentLevel -= 2;

            GUILayout.Space(30.0f);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        //welcome to hell
        object DrawArgField(System.Type _argType, object _val)
        {
            //there might be a better way to do this but I don't know any
            if (_argType == typeof(int))
            {
               return EditorGUILayout.IntField(System.Convert.ToInt32(_val));
            }
            if (_argType == typeof(float))
            {
               return EditorGUILayout.FloatField((float)_val);
            }
            if (_argType == typeof(bool))
            {
               return EditorGUILayout.Toggle((bool)_val);
            }
            return null;
        }

        void GetActions()
        {
            availableActions = TextEventAction.GetAll();
        }

        private void OnEnable()
        {
            System.Random rng = new System.Random(42);
            GetActions();
            Serializer.Init();
            indentColors = new Color[20];

            for (int i = 0; i < indentColors.Length; i++)
            {
                indentColors[i] = new Color((float)rng.NextDouble() * 0.5f, (float)rng.NextDouble() * 0.5f, (float)rng.NextDouble(), 0.3f);
            }

            if (selectedEvent == null)
            {
                ChangeEvent(0);
            }
        }

        Vector2 scrollPos = new Vector2();

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true);

            textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.wordWrap = true;
            //
            string[] options = Serializer.GetAllEventTitlesWithID(true).ToArray();

            if (GUILayout.Button("Create new event"))
            {
                selectedEvent = new TextEvent("New event", "", Serializer.HighestID + 1, null, null);
                index = Serializer.SerializeEvent(selectedEvent);
                ChangeEvent(index);
            }

            if (options.Length == 0)
            {
                return;
            }

            int newIndex = EditorGUILayout.Popup(
            "Event:",
            index,
            options
            );

            if (newIndex != index)
            {
                //load new event 
                index = newIndex;
                ChangeEvent(index);
            }

            if (selectedEvent == null)
            {
                return;
            }
            DrawEvent(selectedEvent);

            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            if (GUILayout.Button("Save"))
            {
                Serializer.SerializeEvent(selectedEvent);
            }

            if (GUILayout.Button("Delete event"))
            {
                Serializer.DeleteEvent(index);
                if (index + 1 == options.Length)
                {
                    index--;
                }
                if (index >= 0)
                {
                    ChangeEvent(index);
                }
            }

            if (GUILayout.Button("Refresh XML"))
            {
                Serializer.Init();
            }
            GUILayout.EndVertical();

            EditorGUILayout.EndScrollView();

            while (workQueue.Any()) workQueue.Dequeue().Invoke();
        }

        void ChangeEvent(int _index)
        {
            selectedEvent = TextEvent.CreateFromIndex(_index);
        }
    }
}