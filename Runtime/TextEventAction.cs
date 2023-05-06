using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

namespace DevonMillar.TextEvents
{
    public class TextEventAttribute : Attribute
    {
        public string Name { get; protected set; }

        public struct AttributeAndMethod
        {
            public AttributeAndMethod(MethodInfo _method, TextEventAttribute _attribute)
            {
                method = _method;
                attribute = _attribute;
            }
            public MethodInfo method;
            public TextEventAttribute attribute;
        }
    }
    
    
    [AttributeUsage(AttributeTargets.Method)]
    public class TextEventAction : TextEventAttribute
    {
        //return a list of all public methods decorated with the TextEventAction attribute in _targetAssemblies, if null, check all non UnityEngine assemblies
        public static List<AttributeAndMethod> GetAll(List<Assembly> _targetAssemblies = null)
        {
            List<AttributeAndMethod> actionMethods = new();

            //get all assemblies if _targetAssemblies is null, filtering out unity and system assemblies
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
                    select new AttributeAndMethod(method, (TextEventAction)method.GetCustomAttributes(typeof(TextEventAction), true)[0])
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
    
    [AttributeUsage(AttributeTargets.Method)]
    public class TextEventPredicate : TextEventAttribute
    {
        //return a list of all public methods decorated with the TextEventPredicate attribute in _targetAssemblies, if null, check all non UnityEngine assemblies
        public static List<AttributeAndMethod> GetAll(List<Assembly> _targetAssemblies = null)
        {
            List<AttributeAndMethod> predicates = new();

            //get all assemblies if _targetAssemblies is null, filtering out unity and system assemblies
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
                    
                    //get all methods that use the TextEventAction attribute, create a AttributeAndMethod struct and add them to the list
                    predicates.AddRange(
                        from method in methods
                        where method.GetCustomAttributes(typeof(TextEventPredicate), true).Length > 0
                        select new AttributeAndMethod(method, (TextEventPredicate)method.GetCustomAttributes(typeof(TextEventPredicate), true)[0])
                    );
                }
            }

            return predicates;
        }

        public TextEventPredicate(string _name, params Type[] argTypes)
        {
            Name = _name;
        }
    }
    

}
