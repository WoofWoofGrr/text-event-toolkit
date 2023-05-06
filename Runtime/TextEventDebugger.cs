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
            TextEventToolkit.CreateRandom().EnterEvent();
        }

        private void Awake()
        {
            Init();
            refreshButton.onClick.AddListener(Init);
            randomButton.onClick.AddListener(CreateRandomEvent);
        }

        private void Init()
        {
            selector.ClearOptions();
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

 