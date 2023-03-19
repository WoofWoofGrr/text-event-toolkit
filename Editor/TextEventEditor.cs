using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace DevonMillar.TextEvents
{
    public class TextEventEditor : EditorWindow
    {
        [SerializeField] int index = 0;

        TextEvent selectedEvent = null;
        GUIStyle textAreaStyle;

        static System.Action OnReload;
        Queue<System.Action> actionQueue = new();

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

            DrawAllChoices(_event.Choices);

            if (GUILayout.Button("Add choice"))
            {
                _event.AddChoice(new TextEvent.Choice("A choice", null));
            }
        }

        void DrawAllChoices(IEnumerable<TextEvent.Choice> _choices)
        {
            int i = 1;
            foreach (TextEvent.Choice choice in _choices)
            {
                DrawChoice(choice, i);
                i++;
            }
        }

        void DrawChoice(TextEvent.Choice _choice, int _choiceIndex)
        {
            EditorGUILayout.LabelField("Choice " + _choiceIndex);
            _choice.Text = EditorGUILayout.TextArea(_choice.Text, textAreaStyle);

            //draw each possible result of this choice
            _choice.Results.ForEach(e => DrawResult(e, () => _choice.RemoveResult(e)));


            GUILayout.BeginHorizontal();
            GUILayout.Space(15.0f * EditorGUI.indentLevel);

            if (GUILayout.Button("Add result", GUILayout.MaxWidth(100)))
            {
                _choice.AddResult(new TextEvent.Result("", null, null, null));
            }
            GUILayout.EndHorizontal();
        }

        void DrawResult(TextEvent.Result _result, System.Action _deleteAction)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Result");
            _result.Text = EditorGUILayout.TextArea(_result.Text, new GUIStyle(EditorStyles.textArea));
            DrawAllChoices(_result.Choices);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add choice"))
            {
                _result.AddChoice(new TextEvent.Choice("A choice", null));
            }

            if (GUILayout.Button("Delete result", GUILayout.MaxWidth(100)))
            {
                //delete this later in case we're in a loop
                actionQueue.Enqueue(_deleteAction);
            }
            GUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        private void OnEnable()
        {
            Serializer.Init();
        }

        private void OnGUI()
        {
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

            while (actionQueue.Any()) actionQueue.Dequeue().Invoke();
        }

        void ChangeEvent(int _index)
        {
            selectedEvent = TextEvent.CreateFromIndex(_index);
        }
    }
}