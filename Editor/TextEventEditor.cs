using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DevonMillar.TextEvents
{
    public class TextEventEditor : EditorWindow
    {
        [SerializeField] int index = 0;

        TextEventData selectedEvent = null;
        List<TextEventData> AvailableEvents  => TextEventData.AllEventDatas;
        GUIStyle textAreaStyle;

        static System.Action OnReload;
        Queue<System.Action> workQueue = new();
        Dictionary<object, bool> foldouts = new();
        Dictionary<object, bool> storedToggles = new();

        Color[] indentColors;
        List<TextEventAction.AttributeAndMethod> availableActions;
        List<TextEventAction.AttributeAndMethod> availablePredicates;

        [MenuItem("Tools/Text Event Editor")]
        public static void ShowWindow()
        {
            TextEventEditor instance = GetWindow<TextEventEditor>("Text Event Editor");
        }

        void ReloadInstance()
        {
            ChangeEvent(index);
        }

        void DrawEvent(TextEventData _event)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Title");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            _event.Title = EditorGUILayout.TextField(_event.Title);
            GUILayout.EndHorizontal();


            GUILayout.Label("Main body text");
            _event.Text = EditorGUILayout.TextArea(_event.Text, new GUIStyle(EditorStyles.textArea), GUILayout.Height(position.height / 5.0f));

            _event.Image = EditorGUILayout.ObjectField(_event.Image, typeof(Sprite), false) as Sprite;
            
            if (_event.Image != null)
            {
                //calculate aspect ratio
                float aspectRatio = _event.Image.texture.width / _event.Image.texture.height;
                
                int width = Mathf.Min(_event.Image.texture.width, 300);
                int height = (int) (width / aspectRatio);

                GUILayout.Label(_event.Image.texture, GUILayout.Width(width), GUILayout.Height(height));
            }
            
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
            if (_choice.Condition != null && _choice.Condition.IsValid)
            {
                infoText += " - " + _choice.Condition.Name;
            }
            foldouts[_choice] = EditorGUILayout.Foldout(foldouts[_choice], infoText);

            GUILayout.Space(10);

            if (GUILayout.Button("Delete choice", GUILayout.MaxWidth(100)))
            {
                //add the delete action to the queue to delete later in case we're in a loop
                workQueue.Enqueue(() => _deleteAction(_choice));
            }

            GUILayout.EndHorizontal();

            //stop here if not folded out
            if (!foldouts[_choice])
            {
                return;
            }
            GUILayout.BeginVertical(vertColorStyle);

            DrawConditionalField(_choice.Condition, _choice.RemoveCondition, _choice.CreateCondition);
            
            GUILayout.Label("Hover text");
            _choice.HoverText = GUILayout.TextField(_choice.HoverText, textAreaStyle);
            
            //draw each possible result of this choice
            _choice.Results.ForEach(e => DrawResult(e, () => _choice.RemoveResult(e)));

            GUILayout.BeginHorizontal();

            GUILayout.Space(15.0f * EditorGUI.indentLevel);

            if (GUILayout.Button("Add result", GUILayout.MaxWidth(100)))
            {
                _choice.AddResult(new TextEvent.Result("New result", null, null, false));
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        void DrawConditionalField(SerializedMethodCall _call, System.Action _removeCondition, System.Func<SerializedMethodCall> _createCondition)
        {
            if (availablePredicates == null || !availablePredicates.Any())
                return;
            
            int conditionIndex = -1;
            if (_call != null)
            {
                conditionIndex = Array.IndexOf(availablePredicates.Select(e => e.method.Name).ToArray(), _call.Name);
            }

            EditorGUILayout.BeginHorizontal();
            conditionIndex = EditorGUILayout.Popup(
            "Condition:",
            conditionIndex,
            availablePredicates.Select(e => e.attribute.Name).ToArray()
            );
                
            if (conditionIndex < 0)
            {
                EditorGUILayout.EndHorizontal();
                return;
            }

            if (_call == null)
            {
                _call = _createCondition();
            }
            
            bool methodChanged = false;
                
            if (_call.Name != availablePredicates[conditionIndex].method.Name)
            {
                _call.Name = availablePredicates[conditionIndex].method.Name;
                methodChanged = true;
            }
            
                
            ParameterInfo[] methodParams = availablePredicates[conditionIndex].method.GetParameters();
            methodChanged = methodChanged || _call.Args.Length != methodParams.Length;

            object[] resultActionArgs = new object[methodParams.Length];

            if (_call.Args == null || methodChanged)
            {
                _call.Args = new SerializedMethodCall.ArgAndType[methodParams.Length];
            }

            //loop over the parameters of the selected action method and draw the respective filed for them
            for (int j = 0; j < methodParams.Length; j++)
            {
                if (_call.Args[j] == null)
                {
                    _call.Args[j] = new SerializedMethodCall.ArgAndType()
                    {
                        arg = methodParams[j].DefaultValue != DBNull.Value ? methodParams[j].DefaultValue :  Activator.CreateInstance(methodParams[j].ParameterType),
                        type = methodParams[j].ParameterType,
                    };
                }
                _call.Args[j].arg = DrawArgField(methodParams[j].ParameterType, _call.Args[j].arg, methodParams[j].Name);
            }

            if (GUILayout.Button("-", GUILayout.MaxWidth(35)))
            {
                _removeCondition();
            }
            EditorGUILayout.EndHorizontal();
                
            GUILayout.BeginHorizontal();
            GUILayout.Space(15.0f * EditorGUI.indentLevel);
            GUILayout.EndHorizontal();
        }
        
        void DrawResult(TextEvent.Result _result, System.Action _deleteAction)
        {
            GUILayout.Space(40);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Result");
            if (GUILayout.Button("Delete result", GUILayout.MaxWidth(100)))
            {
                //delete this later in case we're in a loop
                workQueue.Enqueue(_deleteAction);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Result text");
            EditorGUILayout.LabelField("Chance");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            
            GUIStyle textFieldStyle = new GUIStyle(EditorStyles.textArea);
            textFieldStyle.wordWrap = true;
            GUILayoutOption[] resultTextField = { GUILayout.Height(50) };
            _result.Text = EditorGUILayout.TextField(_result.Text, textFieldStyle, resultTextField);

            _result.Chance = EditorGUILayout.FloatField(_result.Chance);
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Acknowledgment Text");
            EditorGUILayout.BeginHorizontal();
            GUILayoutOption[] halfWidth = { GUILayout.Width(EditorGUIUtility.currentViewWidth / 2f) };
            _result.AcknowledgmentText = EditorGUILayout.TextField(_result.AcknowledgmentText, halfWidth);
            EditorGUILayout.EndHorizontal();

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

                    bool methodChanged = false;
                    
                    if (_result.ActionMethodNamesAndArgs[i].Name != availableActions[newIndex].method.Name)
                    {
                        _result.ActionMethodNamesAndArgs[i].Name = availableActions[newIndex].method.Name;
                        methodChanged = true;
                    }

                    var methodParams = availableActions[newIndex].method.GetParameters();

                    object[] resultActionArgs = new object[methodParams.Length];

                    if (_result.ActionMethodNamesAndArgs[i].Args == null || methodChanged)
                    {
                        _result.ActionMethodNamesAndArgs[i].Args = new SerializedMethodCall.ArgAndType[methodParams.Length];
                    }

                    //loop over the parameters of the selected action method and draw the respective feild for them
                    for (int j = 0; j < methodParams.Length; j++)
                    {
                        if (_result.ActionMethodNamesAndArgs[i].Args[j] == null)
                        {
                            _result.ActionMethodNamesAndArgs[i].Args[j] = new SerializedMethodCall.ArgAndType()
                            {
                                arg = methodParams[j].DefaultValue ?? default,
                                type = methodParams[j].ParameterType,
                            };
                        }
                        _result.ActionMethodNamesAndArgs[i].Args[j].arg = DrawArgField(methodParams[j].ParameterType, _result.ActionMethodNamesAndArgs[i].Args[j].arg, methodParams[j].Name);
                    }

                    if (GUILayout.Button("-", GUILayout.MaxWidth(35)))
                    {
                        int delIndex = i;
                        workQueue.Enqueue(() => _result.RemoveActionMethodNamesAndArgs(delIndex));
                    }
                    EditorGUILayout.EndHorizontal();


                }
                GUILayout.BeginHorizontal();
                GUILayout.Space(15.0f * EditorGUI.indentLevel);
                if (GUILayout.Button("Add action", GUILayout.MaxWidth(100)))
                {
                    _result.ActionMethodNamesAndArgs.Add(new SerializedMethodCall("", null));
                }
                GUILayout.EndHorizontal();
            }
        }

        //check a type and draw the appropriate field
        object DrawArgField(System.Type _argType, object _val, string _label)
        {
            if (_val == DBNull.Value)
            {
                _val = default(object);
            }
            if (_argType == typeof(int))
            {
               return EditorGUILayout.IntField(_label, System.Convert.ToInt32(_val ?? default(int)));
            }
            if (_argType == typeof(float))
            {
               return EditorGUILayout.FloatField(_label, (float)(_val ?? default(float)));
            }
            if (_argType == typeof(bool))
            {
               return EditorGUILayout.Toggle(_label, (bool)(_val ?? default(bool)));
            }
            if (_argType == typeof(string))
            {
                return EditorGUILayout.TextField(_label, _val?.ToString() ?? "");
            }
            if (_argType.IsEnum)
            {
                object enumValue = (_val is DBNull or null) ? Enum.GetValues(_argType).GetValue(0) : Enum.Parse(_argType, _val.ToString());
                return enumValue = EditorGUILayout.EnumPopup(enumValue as Enum);

                //return EditorGUILayout.EnumPopup(_label, (System.Enum));
            }
            if (_argType.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                return EditorGUILayout.ObjectField(_val as UnityEngine.Object, _argType, false);
            }
            return null;
        }

        void GetActions()
        {
            availableActions = TextEventAction.GetAll();
            availablePredicates = TextEventPredicate.GetAll();
            
        }

        private void OnEnable()
        {
            TextEventData.ReloadAll();
            System.Random rng = new System.Random(42);
            GetActions();
            indentColors = new Color[20];

            if (AvailableEvents.Count == 0)
            {
                CreateNewData();
            }
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
            if (selectedEvent != null && GUI.changed)
            {
                EditorUtility.SetDirty(selectedEvent);
            }
            
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);

            // Set the padding for the scrollable area
            const int padding = 20;
            Rect paddingRect = new Rect(padding, padding, position.width - padding * 2, position.height - padding * 2);
            //GUILayout.BeginArea(paddingRect);
            
            textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.wordWrap = true;
            //
            string[] options = AvailableEvents.Select(e => AvailableEvents.IndexOf(e).ToString() + ": " + e.Title).ToArray();

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
                newIndex = CreateNewData();
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField("Labels");
            
            List<string> labelOptions = TextEventToolkitSettings.Instance.Labels.ToList();
            for (int i = 0; i < selectedEvent.Labels.Count(); i++)
            {
                GUILayout.BeginHorizontal();
                int labelIndex = labelOptions.IndexOf(selectedEvent.Labels[i]);
                
                labelIndex = EditorGUILayout.Popup(
                    $"Label {i+1}:",
                    labelIndex,
                    labelOptions.ToArray()
                );
                
                selectedEvent.Labels[i] = labelIndex >= 0 ? 
                    labelOptions[labelIndex]
                    : null;
                
                //remove label button
                if (selectedEvent.Labels.Count > 0 && GUILayout.Button("-", GUILayout.MaxWidth(35.0f)))
                {
                    selectedEvent.Labels.RemoveAt(i);
                }
                GUILayout.EndHorizontal();
            }
            

            if (GUILayout.Button("+", GUILayout.MaxWidth(35.0f)))
            {
                selectedEvent.Labels.Add(TextEventToolkitSettings.Instance.DefaultFirstLabel ? labelOptions.FirstOrDefault() : null);
            }

            selectedEvent.BanAfterUse = EditorGUILayout.Toggle("Ban after use", selectedEvent.BanAfterUse);
            
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
                foreach (TextEvent.Result result in selectedEvent.Choices.SelectMany(selectedEventChoice => selectedEventChoice.Results))
                    result.UpdateActions();
                
                AssetDatabase.SaveAssets();
                //Serializer.SerializeEvent(selectedEvent);
            }

            if (GUILayout.Button("Settings"))
            {
                TextEventToolkitSettings settingsAsset = AssetDatabase.LoadAssetAtPath<TextEventToolkitSettings>(FilePaths.SettingsPath);
                Selection.activeObject = settingsAsset;
            }
            if (GUILayout.Button("Delete event"))
            {
                if (EditorUtility.DisplayDialog("Delete event", "Are you sure you want to delete " + selectedEvent.Title + "?", "Delete", "Cancel"))
                {
                    string pathToDelete = AssetDatabase.GetAssetPath(selectedEvent);
                    AssetDatabase.DeleteAsset(pathToDelete);
                    TextEventData.ReloadAll();
                    
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

            // if (GUILayout.Button("Refresh"))
            // {
            //     Serializer.Init();
            //     
            // }
            GUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
            //GUILayout.EndArea();

            while (workQueue.Any()) workQueue.Dequeue().Invoke();
        }
        int CreateNewData()
        {
            int newIndex;
            // Create a new instance of the ScriptableObject
            TextEventData newObj = CreateInstance<TextEventData>();
            newObj.BanAfterUse = TextEventToolkitSettings.Instance.BanByDefault;
            if (TextEventToolkitSettings.Instance.DefaultFirstLabel && TextEventToolkitSettings.Instance.Labels is {Count: >  0})
            {
                newObj.Labels.Add(TextEventToolkitSettings.Instance.Labels.First());
            }

            // Create a new asset file for the ScriptableObject
             
            if (!Directory.Exists(FilePaths.TextEventPath))
            {
                Directory.CreateDirectory(FilePaths.TextEventPath);
            }
            AssetDatabase.CreateAsset(newObj, FilePaths.NewTextEventPath);
            AssetDatabase.SaveAssets();

            TextEventData.ReloadAll();
            newIndex = AvailableEvents.Count - 1;
            return newIndex;
        }

        void ChangeEvent(int _index)
        {
            if (selectedEvent != null)
            {
                AssetDatabase. SaveAssets();
            }
            selectedEvent = AvailableEvents[_index];
            
            //throw new System.NotImplementedException();
            //selectedEvent = TextEvent.CreateFromIndex(_index, true, false);
        }
    }
}