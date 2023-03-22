using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DevonMillar.TextEvents
{
    public class TextEventActions
    {
        [TextEventAction("Test method")]
        public static void TestMethod()
        {
            Debug.Log("Invoked");
        }
        [TextEventAction("Arg test")]
        public static void IntTest(int number = 5, bool tickbox = false, float anotherNumber = 1.0f)
        {
            Debug.Log("Int test: passed in " + number);
        }
        [TextEventAction("String test")]
        public static void StringTest(string str)
        {
            Debug.Log("String test: passed in " + str);
        }
    }

}
