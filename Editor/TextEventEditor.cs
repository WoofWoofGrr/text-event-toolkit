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
        Dictionary<object, bool> storedToggles = new();

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
            GUILayout.BeginHorizontal();
            GUILayout.Label("Title");
            GUILayout.Label("Label");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            _event.Title = EditorGUILayout.TextField(_event.Title);
            _event.Label = EditorGUILayout.TextField(_event.Label);
            GUILayout.EndHorizontal();


            GUILayout.Label("Main body text");
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
                _choice.AddResult(new TextEvent.Result("", null, null, null, false));
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
                    int newIndex = System.Array.IndexOf(availableActions.Select(e => e.method.Name).ToArray(), _result.ActionMethodNamesAndArgs[i].Name);

                    EditorGUILayout.BeginHorizontal();
                    newIndex = EditorGUILayout.Popup(
                    "Action:",
                    newIndex,
                    availableActions.Select(e => e.attribute.Name).ToArray()
                    );
                    
                    if (newIndex < 0)
                    {
                        EditorGUILayout.EndHorizontal();
                        continue;
                    }
                    
                    //this was in if
                    _result.ActionMethodNamesAndArgs[i].Name = availableActions[newIndex].method.Name;

                    var methodParams = availableActions[newIndex].method.GetParameters();

                    object[] resultActionArgs = new object[methodParams.Length];

                    if (_result.ActionMethodNamesAndArgs[i].Args == null)
                    {
                        _result.ActionMethodNamesAndArgs[i].Args = new object[methodParams.Length];
                    }

                    //loop over the parameters of the selected action method and draw the respective feild for them
                    for (int j = 0; j < methodParams.Length; j++)
                    {
                        if (_result.ActionMethodNamesAndArgs[i].Args[j] == null)
                        {
                            _result.ActionMethodNamesAndArgs[i].Args[j] = methodParams[j].DefaultValue ?? default;
                        }
                        _result.ActionMethodNamesAndArgs[i].Args[j] = DrawArgField(methodParams[j].ParameterType, _result.ActionMethodNamesAndArgs[i].Args[j], methodParams[j].Name);
                    }

                    //if (GUILayout.Button("Delete", GUILayout.MaxWidth(50)))
                    //{
                    //    workQueue.Enqueue(() => _result.RemoveActionMethodNamesAndArgs(i));
                    //}
                    EditorGUILayout.EndHorizontal();


                }
                GUILayout.BeginHorizontal();
                GUILayout.Space(15.0f * EditorGUI.indentLevel);
                if (GUILayout.Button("Add action", GUILayout.MaxWidth(100)))
                {
                    _result.ActionMethodNamesAndArgs.Add(new MethodNameAndArgs("", null));
                }
                GUILayout.EndHorizontal();
            }

            //draw result choices

            object key = _result.ToString() + _result.GetHashCode().ToString() + " branching";
            if (!storedToggles.ContainsKey(key))
            {
                storedToggles.Add(key, false);
            }

            GUILayout.Space(10.0f);

            if (_result.Choices.Count == 0)
            {
                storedToggles[key] = EditorGUILayout.Toggle("Branching", storedToggles[key]);
            }
            else
            {
                storedToggles[key] = true;
            }

            if (storedToggles[key])
            {
                EditorGUILayout.LabelField("Choices");
                DrawAllChoices(_result.Choices, (choice) => _result.RemoveChoice(choice));

                GUILayout.BeginHorizontal();
                GUILayout.Space(15.0f * EditorGUI.indentLevel);
                if (GUILayout.Button("Add choice", GUILayout.MaxWidth(100)))
                {
                    _result.AddChoice(new TextEvent.Choice("A choice", null));
                }

                GUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel -= 2;

            GUILayout.Space(30.0f);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        //check a type and draw the appropriate feild
        object DrawArgField(System.Type _argType, object _val, string _label)
        {
            //there might be a better way to do this but I don't know any
            if (_argType == typeof(int))
            {
               return EditorGUILayout.IntField(_label, System.Convert.ToInt32(_val));
            }
            if (_argType == typeof(float))
            {
               return EditorGUILayout.FloatField(_label, (float)_val);
            }
            if (_argType == typeof(bool))
            {
               return EditorGUILayout.Toggle(_label, (bool)_val);
            }
            if (_argType == typeof(string))
            {
                return EditorGUILayout.TextField(_label, _val.ToString());
            }
            else
            {
                Debug.LogError("[TextEventAction] methods must only have parameters of type int, float, bool or string");
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


            GUILayout.BeginHorizontal();

            if (options.Length == 0)
            {
                return;
            }

            int newIndex = EditorGUILayout.Popup(
            "Event:",
            index,
            options
            );
            if (GUILayout.Button("+", GUILayout.MaxWidth(35.0f)))
            {
                selectedEvent = new TextEvent("New event", "", Serializer.HighestID + 1, null, null);
                newIndex = index = Serializer.SerializeEvent(selectedEvent);
                ChangeEvent(index);
            }
            GUILayout.EndHorizontal();

            if (newIndex != index)
            {
                Serializer.SerializeEvent(selectedEvent);

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
                if (EditorUtility.DisplayDialog("Delete event", "Are you sure you want to delete " + selectedEvent.Title + "?", "Delete", "Cancel"))
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
            selectedEvent = TextEvent.CreateFromIndex(_index, true, false);
        }
    }
}