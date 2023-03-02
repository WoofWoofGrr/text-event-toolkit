using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DevonMillar.TextEvents
{
    public class TextEventCreator : MonoBehaviour
    {
        [SerializeField] Canvas prefab;
        private void Awake()
        {
            Instantiate(prefab);
            Destroy(gameObject);
        }
    }
}
