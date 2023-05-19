using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static DevonMillar.TextEvents.TextEvent;

namespace DevonMillar.TextEvents
{
    public class TextEventUI : MonoBehaviour
    {
        TMPro.TMP_FontAsset font;
        
        //TextEvent currentTextEvent = null;
        [SerializeField] TextMeshProUGUI choicePrefab;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI bodyText;
        [SerializeField] Transform choiceContainer;
        [SerializeField] Image image;
        [SerializeField] TextEventUIPopup popupPrefab;

        List<TextMeshProUGUI> activeChoices = new();

        [SerializeField] bool debugMode = false;

        int numPopups = 0;
        
        // Start is called before the first frame update
        void Awake()
        {
            OnAnyTextEventEnter += TextEventEntered;
            font = TextEventToolkitSettings.Instance.Font;

            if (font != null)
            {
                titleText.font = TextEventToolkitSettings.Instance.Font;
                bodyText.font = TextEventToolkitSettings.Instance.Font;
            }
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

            _choices.Reverse();
            foreach (Choice choice in _choices)
            {
                offset = CreateChoiceText(choice.Text, offset, () => choice.Pick(), choice.Condition.IsValid ? Color.yellow : Color.white);

            }
        }
        float CreateChoiceText(string _text, float _offset, System.Action _callback, Color _color)
        {
            Vector2 anchor = Vector2.zero;

            TextMeshProUGUI newChoiceUI = Instantiate(choicePrefab, choiceContainer);
            if (font != null)
            {
                newChoiceUI.font = TextEventToolkitSettings.Instance.Font;
            }
            newChoiceUI.text = "> " + _text;
            newChoiceUI.rectTransform.anchoredPosition = anchor + new Vector2(0.0f, _offset);
            newChoiceUI.GetComponent<Button>().onClick.AddListener(_callback.Invoke);
            newChoiceUI.color = _color;
            activeChoices.Add(newChoiceUI);

            _offset += newChoiceUI.rectTransform.sizeDelta.y;
            return _offset;
        }

        private void CreatePostResultChoice(Result _result)
        {
            CreateChoiceText(_result.AcknowledgmentText, 0.0f, _result.AcknowledgedResult, Color.white);
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
                    CreateChoiceText(TextEventToolkitSettings.Instance.DefaultAcknowledgmentText, 0.0f, ForceExitAllEvents, Color.white);
                }
            }
            if (TextEventToolkit.UsingController)
            {
                activeChoices.Last().GetComponent<Button>().Select();
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
        
        void OnDestroy()
        {
            ForceExitAllEvents();
        }

        void Destroy(TextEvent _event)
        {
            OnAnyTextEventEnter -= TextEventEntered;
            _event.OnTextEventExit -= Destroy;
            Destroy(gameObject);
        }

        internal void CreatePopup(string _text)
        {
            TextEventUIPopup popup = Instantiate(popupPrefab, transform);
            popup.Text = _text;
            popup.Font = font;
            popup.transform.position += Vector3.up * numPopups * 225.0f;
            numPopups++;
        }
    }
}