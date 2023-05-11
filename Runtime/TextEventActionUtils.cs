using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DevonMillar.TextEvents
{
    public class TextEventActionUtils : MonoBehaviour
    {
        public static void CreatePopup(string _text)
        {
            //todo: do this better
            GameObject.FindObjectOfType<TextEventUI>().CreatePopup(_text);
        }
    }
}
