using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using Random = UnityEngine.Random;

namespace DevonMillar
{
    public static class Utils
    {
        public static T GetRandom<T>(List<T> _list)
        {
            if (_list.Count == 0)
            {
                Debug.LogWarning("Passed a empty list to GetRandom, check the list is not empty first");
                return default(T);
            }

            int rand = Random.Range(0, _list.Count);

            return _list[rand];
        }
        public static T GetRandom<T>(T[] _array)
        {
            int rand = Random.Range(0, _array.Length);
            return _array[rand];
        }
    }
    static class ListExtentions
    {
        public static T GetRandom<T>(this List<T> _list)
        {
            return Utils.GetRandom<T>(_list);
        }

        //TODO: test and use this for lists too
        public static T GetRandom<T>(this IEnumerable<T> _collection)
        {
            return _collection.ElementAt(Random.Range(0, _collection.Count()));
        }
    }
}
