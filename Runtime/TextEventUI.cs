using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace DevonMillar.TextEvents
{
    public class TextEventUI : MonoBehaviour
    {
        //TextEvent currentTextEvent = null;
        [SerializeField] TextMeshProUGUI choicePrefab;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI bodyText;
        [SerializeField] Transform choiceContainer;

        List<TextMeshProUGUI> activeChoices = new();

        [SerializeField] bool debugMode = false;

        // Start is called before the first frame update
        void Awake()
        {
            TextEvent.OnAnyTextEventEnter += TextEventEntered;
        }
        private void Start()
        {

        }

        private void UpdateBodyText(string _text)
        {
            bodyText.text = _text;
        }

        private void CreateContinueChoice()
        {

        }
        private void CreateChoiceTexts(List<TextEvent.Choice> _choices)
        {
            float offset = 0.0f;
            Vector2 anchor = Vector2.zero;

            foreach (TextEvent.Choice choice in _choices)
            {
                TextMeshProUGUI newChoiceUI = Instantiate(choicePrefab, choiceContainer);
                newChoiceUI.text = "> " + choice.Text;
                newChoiceUI.rectTransform.anchoredPosition = anchor + new Vector2(0.0f, offset);
                newChoiceUI.GetComponent<Button>().onClick.AddListener(() => choice.Pick());
                activeChoices.Add(newChoiceUI);

                offset += newChoiceUI.rectTransform.sizeDelta.y; ;
            }
        }
        private void DestroyChoices()
        {
            activeChoices.ForEach(e => GameObject.Destroy(e.gameObject));
            activeChoices.Clear();
        }

        private void TextEventUpdateUI(string _title, string _body, List<TextEvent.Choice> _choices = null)
        {
            DestroyChoices();
            if (_body != null)
            {
                bodyText.text = _body;
            }
            if (_title != null)
            {
                titleText.text = _title;

            }
            if (_choices != null)
            {
                CreateChoiceTexts(_choices);
            }
        }

        //main hook
        private void TextEventEntered(TextEvent _event)
        {
            DestroyChoices();

            TextEventUpdateUI(_event.Title + (debugMode ? " (" + _event.ID + ")" : ""), _event.Text, _event.Choices);
            //_event.OnChoiceSelected += ChoiceSelected;
            _event.OnChoiceSelected += (_choice, _result) => TextEventUpdateUI(null, (_choice.PostText ?? "") + (_result.Text ?? ""), _result.Choices);
            _event.OnTextEventExit += Destroy;
        }

        private void ChoiceSelected(TextEvent.Choice _choice, TextEvent.Result _result)
        {
            UpdateBodyText((_choice.PostText ?? "") + (_result.Text ?? ""));
        }

        void Destroy(TextEvent _event)
        {
            TextEvent.OnAnyTextEventEnter -= TextEventEntered;
            _event.OnTextEventExit -= Destroy;
            Destroy(gameObject);
        }
    }
}
