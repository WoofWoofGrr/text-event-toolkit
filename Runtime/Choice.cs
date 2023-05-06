using System;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using Random = UnityEngine.Random;

namespace DevonMillar.TextEvents
{

    public partial class TextEvent
    {
        [System.Serializable]
        public class Choice
        {
            [field: SerializeField] public string Text { get; set; }
            [field: SerializeField] public string HoverText { get; set; }
            [field: SerializeField] public string PostText { get; private set; }
            [field: SerializeField] public List<Result> Results { get; private set; }
            [field: SerializeField] public SerializedMethodCall Condition { get; private set; } = new("", null);
            
            public event System.Action<Choice, Result> OnChoiceSelected;
            public event System.Action<Choice, Result> OnFinalChoice;
            
            public Choice(string _text, IEnumerable<Result> _results, string _postText = "")
            {
                Text = _text;
                Results = new();

                if (_results != null)
                {
                    Results.AddRange(_results);
                }
            }
            public Choice()
            {
                Text = "New choice";
                Results = new();
            }

            public void AddResult(Result _newResult)
            {
                Results.Add(_newResult);
            }
            public void RemoveResult(Result _result)
            {
                Results.Remove(_result);
            }
            public void RemoveCondition()
            {
                Condition = null;
            }

            public Result Pick()
            {
                Result chosenResult = null;
                if (Results.Count > 0)
                {
                    chosenResult = Results.First();

                    //if there are multiple results, pick one based on their chance
                    if (Results.Count > 1)
                    {
                        float[] cdf = new float[Results.Count];
                        float sum = 0.0f;
                        for (int i = 0; i < Results.Count; i++)
                        {
                            sum += Results[i].Chance;
                            cdf[i] = sum;
                        }

                        float roll = Random.value;
                        int chosenIndex = Array.BinarySearch(cdf, roll);
                        if (chosenIndex < 0)
                        {
                            chosenIndex = ~chosenIndex;
                        }
                        if (chosenIndex >= Results.Count)
                        {
                            chosenIndex = Results.Count - 1;
                        }
                        chosenResult = Results[chosenIndex];

                    }

                    chosenResult.Execute();
                    OnChoiceSelected?.Invoke(this, chosenResult);
                }

                if (chosenResult == null || chosenResult.IsFinal)
                {
                    OnFinalChoice?.Invoke(this, chosenResult);
                    OnFinalChoice = null;
                }
                else
                {
                    chosenResult.OnResultAcknowledged += () => OnFinalChoice?.Invoke(this, chosenResult);
                }
                OnChoiceSelected = null;
                return chosenResult;
            }

            public override string ToString()
            {
                string s = "\n";
                s += "Choice: ";
                s += Text;
                s += "\n";
                s += PostText;
                s += "\n";

                foreach (Result result in Results)
                {
                    s += result;
                }

                return s;
            }
            public SerializedMethodCall CreateCondition()
            {
               return Condition = new("", null);
            }
        }
    }


}
