using System;
using TMPro;
using UnityEngine;

namespace DevonMillar.TextEvents
{
    public class TextEventUIPopup : MonoBehaviour
    {
        TextMeshProUGUI text;
        internal string Text
        {
            get
            {
                return text.text;
            }
            set
            {
                text.text = value;
            }
        }

        internal TMP_FontAsset Font
        {
            set
            {
                text.font = value;
            }
        }
        
        void Awake()
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        
        
    }
}