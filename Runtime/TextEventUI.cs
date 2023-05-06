using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static DevonMillar.TextEvents.TextEvent;

namespace DevonMillar.TextEvents
{
    public class TextEventUI : MonoBehaviour
    {
        //TextEvent currentTextEvent = null;
        [SerializeField] TextMeshProUGUI choicePrefab;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI bodyText;
        [SerializeField] Transform choiceContainer;
        [SerializeField] Image image;

        List<TextMeshProUGUI> activeChoices = new();

        [SerializeField] bool debugMode = false;

        // Start is called before the first frame update
        void Awake()
        {
            OnAnyTextEventEnter += TextEventEntered;
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
        private void CreateChoiceTexts(List<Choice> _choices)
        {
            float offset = 0.0f;

            foreach (Choice choice in _choices)
            {
                offset = CreateChoiceText(choice.Text, offset, () => choice.Pick());

            }
        }
        float CreateChoiceText(string _text, float _offset, System.Action _callback)
        {
            Vector2 anchor = Vector2.zero;

            TextMeshProUGUI newChoiceUI = Instantiate(choicePrefab, choiceContainer);
            newChoiceUI.text = "> " + _text;
            newChoiceUI.rectTransform.anchoredPosition = anchor + new Vector2(0.0f, _offset);
            newChoiceUI.GetComponent<Button>().onClick.AddListener(_callback.Invoke);
            activeChoices.Add(newChoiceUI);

            _offset += newChoiceUI.rectTransform.sizeDelta.y;
            return _offset;
        }

        private void CreatePostResultChoice(Result _result)
        {
            CreateChoiceText(_result.AcknowledgmentText, 0.0f, _result.AcknowledgedResult);
        }
        private void DestroyChoices()
        {
            activeChoices.ForEach(e => GameObject.Destroy(e.gameObject));
            activeChoices.Clear();
        }

        private void TextEventUpdateUI(string _title, string _body, List<Choice> _choices = null, Result _result = null)
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
            if (_choices is { Count: > 0 })
            {
                CreateChoiceTexts(_choices);
            }
            else
            {
                if (_result != null)
                {
                    CreatePostResultChoice(_result);
                }
                else
                {
                    //HACK: this is handling events with no choices, it works but shouldn't need to use ForceExitAllEvents
                    CreateChoiceText(TextEventToolkitSettings.Instance.DefaultAcknowledgmentText, 0.0f, ForceExitAllEvents);
                }
            }
        }

        //main hook
        private void TextEventEntered(TextEvent _event)
        {
            DestroyChoices();
            image.sprite = _event.Image;
            TextEventUpdateUI(_event.Title + (debugMode ? " (" + _event.ID + ")" : ""), _event.Text, _event.Choices);
            //_event.OnChoiceSelected += ChoiceSelected;
            _event.OnChoiceSelected += (_choice, _result) => TextEventUpdateUI(null, (_choice.PostText ?? "") + (_result.Text ?? ""), null, _result);
            _event.OnTextEventExit += Destroy;
        }

        private void ChoiceSelected(Choice _choice, Result _result)
        {
            UpdateBodyText((_choice.PostText ?? "") + (_result.Text ?? ""));
        }

        void Destroy(TextEvent _event)
        {
            OnAnyTextEventEnter -= TextEventEntered;
            _event.OnTextEventExit -= Destroy;
            Destroy(gameObject);
        }
    }
}