using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Linq;
using System.Xml.Linq;

namespace DevonMillar.TextEvents
{

    public partial class TextEvent
    {
        [System.Serializable]
        public class Choice
        {
            public List<Result> Results { get; private set; }
            public string Text { get; set; }

            public string PostText { get; private set; }
            public event System.Action<Choice, Result> OnChoiceSelected;
            public event System.Action<Choice, Result> OnFinalChoice;

            public static XElement CreateDefaultChoiceNode()
            {
                XElement e = new XElement("Choice");
                e.SetAttributeValue("text", "Continue...");
                return e;
            }

            public Choice(string _text, IEnumerable<Result> _results, string _postText = "")
            {
                Text = _text;
                Results = new();

                if (_results != null)
                {
                    Results.AddRange(_results);
                }
            }

            public void AddResult(Result _newResult)
            {
                Results.Add(_newResult);
            }
            public void RemoveResult(Result _result)
            {
                Results.Remove(_result);
            }

            public Result Pick()
            {
                Result chosenResult = null;
                if (Results.Count > 0)
                {
                    chosenResult = Results.First();

                    if (Results.Count > 1)
                    {
                        float totalChance = Results.Sum(result => result.Chance);
                        float roll = Random.Range(0, totalChance);

                        //pick a random result based on the chance weightings
                        foreach (Result result in Results)
                        {
                            roll -= result.Chance;
                            if (roll <= 0.0f)
                            {
                                chosenResult = result;
                                break;
                            }
                        }
                    }

                    chosenResult.Execute();
                    OnChoiceSelected?.Invoke(this, chosenResult);
                }

                if (chosenResult == null || chosenResult.IsFinal)
                {
                    OnFinalChoice?.Invoke(this, chosenResult);
                    OnFinalChoice = null;
                }
                else if (chosenResult != null) //has result but not final result
                {
                    //pass final choice up the stack
                    chosenResult.Choices.ForEach(choice => choice.OnFinalChoice += (_choice, _result) =>
                    {
                        OnFinalChoice?.Invoke(_choice, _result);
                        OnFinalChoice = null;
                    });
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
        }
    }


}
