using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;


namespace DevonMillar.TextEvents
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TextEventAction : Attribute
    {
        public string Name { get; private set; }
        //Type[] argTypes

        public struct AtributeAndMethod
        {
            public AtributeAndMethod(MethodInfo _method, TextEventAction _attribute)
            {
                method = _method;
                attribute = _attribute;
            }
            public MethodInfo method;
            public TextEventAction attribute;
        }

        //return a list of all public methods decorated with the TextEventAction attribute in _targetAssemblies, if null, check all non UnityEngine assemblies
        public static List<AtributeAndMethod> GetAll(List<Assembly> _targetAssemblies = null)
        {
            List<AtributeAndMethod> actionMethods = new();

            if (_targetAssemblies == null)
            {
                _targetAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(e => !e.FullName.StartsWith("Unity") && !e.FullName.StartsWith("System.")).ToList();

            }
            foreach (Assembly assembly in _targetAssemblies) 
            {
                Type[] types = assembly.GetTypes();
                
                foreach (Type type in types) 
                {
                    MethodInfo[] methods = type.GetMethods();
                    
                    //get all methods that use the TextEventAction attribute, create a AtributeAndMethod struct and add them to the list
                    actionMethods.AddRange(
                    from method in methods
                    where method.GetCustomAttributes(typeof(TextEventAction), true).Length > 0
                    select new AtributeAndMethod(method, (TextEventAction)method.GetCustomAttributes(typeof(TextEventAction), true)[0])
                    );
                }
            }

            return actionMethods;
        }

        public TextEventAction(string _name, params Type[] argTypes)
        {
            Name = _name;
        }
    }
}
