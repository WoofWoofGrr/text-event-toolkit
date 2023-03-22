using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;

namespace DevonMillar.TextEvents
{
    public static class Serializer
    {
        static XDocument textEventsData;

        static string fileName = "TextEventToolkit/TextEvents";
        static string FilePath => Application.dataPath + "/Resources/" + fileName + ".xml";
        public static int HighestID => int.Parse(GetAllNodes().Last().Attribute("id").Value);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            object data = Resources.Load(fileName);

            if (data == null && Application.isEditor)
            {
                CreateDataFile();
                data = Resources.Load(fileName);
            }

            textEventsData = XDocument.Parse(data.ToString());

            Debug.Log("Loaded text event XML data");

            if (textEventsData.Descendants("Event").Count() == 0)
            {
                CreateFirstEvent();
            }
        }

        public static IEnumerable<XElement> GetAllNodes(System.Func<XElement, bool> _predicate = null)
        {
            return textEventsData.Descendants("Event").Where(_predicate != null ? _predicate : _ => true);
        }
        public static XElement GetRandomNode(System.Func<XElement, bool> _predicate = null)
        {
            return GetAllNodes().Where(_predicate != null ? _predicate : _ => true).GetRandom();
        }
        public static XElement GetNode(int _id)
        {
            return GetAllNodes().ElementAtOrDefault(_id);
        }

        public static float? ParseResultChance(XElement _resultNode)
        {
            string chanceStr = _resultNode.Attribute("chance")?.Value;

            if (chanceStr != null)
            {
                return float.Parse(chanceStr);

            }
            else
            {
                return null;
            }
        }

        public static List<string> GetAllEventTitlesWithID(bool _includeDebug = false)
        {
            System.Func<XElement, bool> debugPredicate = (x) => _includeDebug || ((x.Attribute("debug")?.Value ?? "false") != "true");
            return Serializer.GetAllNodes(debugPredicate).Select(e => e.Attribute("title").Value + " (" + e.Attribute("id").Value + ")").ToList();
        }

        //given an XML element, create and return a Choice for any result element the element contains
        public static IEnumerable<TextEvent.Choice> ExtractChoices(XElement _element)
        {
            List<XElement> choiceNodes = _element.Elements("Choice").ToList();
            List<TextEvent.Choice> choices = new();

            //create default choice if the event has none
            if (choiceNodes.Count() == 0)
            {
                choiceNodes.Add(TextEvent.Choice.CreateDefaultChoiceNode());
            }

            //create a choice for each choice node and return the list
            return choiceNodes.Select(e => CreateChoice(e));
        }

        //given an XML element, create and return a Result for any result element the element contains
        public static IEnumerable<TextEvent.Result> ExtractResults(XElement _element)
        {
            //add the results to the choice
            return _element.Elements("Result").Select(e => CreateResult(e));
        }

        //deserialize an event and it's choices and results
        public static TextEvent CreateEvent(XElement _eventNode)
        {
            if (_eventNode == null)
            {
                //TODO: don't reference banning in the Serializer
                Debug.LogWarning("Could not find any events that aren't banned.");
                return null;
            }

            int ID = int.Parse(_eventNode.Attribute("id").Value);

            return new TextEvent(
                _title: _eventNode.Attribute("title")?.Value,
                _text: _eventNode.Attribute("text")?.Value,
                _id: ID,
                _result: ExtractResults(_eventNode).FirstOrDefault(),
                _choices: ExtractChoices(_eventNode)
                );
        }

        //deserialize a choice and it's results
        public static TextEvent.Choice CreateChoice(XElement _choiceNode)
        {
            string choiceText = _choiceNode.Attribute("text").Value;
            string choicePostText = _choiceNode.Attribute("postText")?.Value ?? "";
            return new TextEvent.Choice(choiceText, ExtractResults(_choiceNode), choicePostText);
        }

        //deserialize a result
        public static TextEvent.Result CreateResult(XElement _resultNode)
        {
            string text = _resultNode.Attribute("text").Value;
            float? chance = Serializer.ParseResultChance(_resultNode);

            List<MethodNameAndArgs> resultActions = new();
            foreach (XElement actionNode in _resultNode.Elements("Action"))
            {
                string methodName = actionNode.Attribute("MethodName").Value;

                XAttribute[] argAtributes = actionNode.Attributes().Where(e => e.Name.ToString().StartsWith("arg")).ToArray();
                object[] args = new object[argAtributes.Length];

                //loop over the arg atributes in the xml file and parse them into the correct type
                for (int i = 0; i < argAtributes.Length; i++)
                {
                    string argValStr = argAtributes[i].Value.Substring(argAtributes[i].Value.IndexOf(':') + 1);

                    if (argAtributes[i].Value.StartsWith("int:"))
                    {
                        args[i] = int.Parse(argValStr);
                    }
                    else if (argAtributes[i].Value.StartsWith("float:"))
                    {
                        args[i] = float.Parse(argValStr);
                    }
                    else if (argAtributes[i].Value.StartsWith("bool:"))
                    {
                        args[i] = bool.Parse(argValStr);
                    }
                    else if (argAtributes[i].Value.StartsWith("string:"))
                    {
                        args[i] = argValStr;
                    }
                }

                resultActions.Add(new MethodNameAndArgs(methodName, args));
            }

            return new TextEvent.Result(text, chance, ExtractChoices(_resultNode), resultActions);
        }

#if UNITY_EDITOR

        //create a XML file to store the text event data
        public static void CreateDataFile()
        {
            System.IO.Directory.CreateDirectory(FilePath.Substring(0, FilePath.LastIndexOf('/') + 1));

            if (!System.IO.File.Exists(FilePath))
            {
                System.IO.File.WriteAllText(FilePath, "<TextEvents>\n </TextEvents>");
            }
            AssetDatabase.Refresh();
        }

        public static void CreateFirstEvent()
        {
            textEventsData.Root.Add
                (
                new XElement("Event", 
                    new XAttribute("id", "0"),
                    new XAttribute("text", "New event text"), 
                    new XAttribute("title", "New event title")
                    )
                );
            SaveXML();
        }
        
        //delete an event at _index
        public static void DeleteEvent(int _index)
        {
            textEventsData.Root.Descendants("Event").ElementAt(_index).Remove();
            SaveXML();
        }

        public static XElement SerializeResult(TextEvent.Result _result)
        {
            XElement element = new XElement("Result", new XAttribute("text", _result.Text));

            element.Add(new XAttribute("chance", _result.Chance));

            if (_result.ActionMethodNamesAndArgs != null)
            {
                for (int i = 0; i < _result.ActionMethodNamesAndArgs.Count; i++)
                {
                    element.Add(new XElement("Action", new XAttribute("MethodName", _result.ActionMethodNamesAndArgs[i].Name)));
                    XElement actionElement = element.Elements("Action").Last();
                    for (int j = 0; j < _result.ActionMethodNamesAndArgs[i].Args.Length; j++)
                    {
                        object arg = _result.ActionMethodNamesAndArgs[i].Args[j];
                        string tytpeStr = null;

                        if (arg is int)
                        {
                            tytpeStr = "int:";
                        }
                        else if (arg is float)
                        {
                            tytpeStr = "float:";
                        }
                        else if (arg is bool)
                        {
                            tytpeStr = "bool:";
                        }
                        else if (arg is string)
                        {
                            tytpeStr = "string:";
                        }
                        actionElement.Add(new XAttribute("arg" + j, tytpeStr + arg));
                    }
                }
            }

            //serialize the choices
            _result.Choices.ForEach(e => element.Add(SerializeChoice(e)));

            return element;
        }


        public static XElement SerializeChoice(TextEvent.Choice _choice)
        {
            XElement element = new XElement("Choice",
                new XAttribute("text", _choice.Text)
                );

            if (_choice.PostText != null)
            {
                element.Add(new XAttribute("postText", _choice.PostText));
            }

            foreach (TextEvent.Result result in _choice.Results)
            {
                element.Add(SerializeResult(result));
            }

            return element;
        }


        public static int SerializeEvent(TextEvent _event)
        {
            int _id = _event.ID;
            XElement element = GetNode(_id);

            if (element == null)
            {
                element = new XElement("Event");
                textEventsData.Root.Add(element);
            }

            element.ReplaceAttributes(
                new XAttribute("id", _id),
                new XAttribute("title", _event.Title),
                new XAttribute("text", _event.Text)
                );

            //remove all child elements 
            element.Nodes().Remove();

            //TODO: serialize result

            //add choices
            _event.Choices.ForEach(e => element.Add(SerializeChoice(e)));

            SaveXML();

            return element.ElementsBeforeSelf().Count();//
        }

        public static void WriteBody(int _index, string _bodyText)
        {
            GetNode(_index).Attribute("text").Value = _bodyText;
            SaveXML();
        }

        public static void SaveXML()
        {
            textEventsData.Save(FilePath, SaveOptions.None);
            AssetDatabase.Refresh();
        }

#endif

    }
}