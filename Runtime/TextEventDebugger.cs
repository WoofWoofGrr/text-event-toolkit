using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DevonMillar.TextEvents
{
    public class TextEventDebugger : MonoBehaviour
    {
        [SerializeField] Button randomButton;
        [SerializeField] Button refreshButton;
        [SerializeField] TMP_Dropdown selector;

        void CreateRandomEvent()
        {
            TextEvent.ForceExitAllEvents();
            TextEvent.CreateRandom().EnterEvent();
        }

        void LoadEvent(int num)
        {
            TextEvent.ForceExitAllEvents();
            TextEvent.CreateFromID(selector.options[num].text).EnterEvent();
        }

        private void Awake()
        {
            Init();
            selector.onValueChanged.AddListener(LoadEvent);
            refreshButton.onClick.AddListener(TextEvent.Refresh);
            refreshButton.onClick.AddListener(Init);
            randomButton.onClick.AddListener(CreateRandomEvent);
        }

        private void Init()
        {
            selector.ClearOptions();
            selector.AddOptions(TextEvent.GetAllEventIDs(true, true));
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}

