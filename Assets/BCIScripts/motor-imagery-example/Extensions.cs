using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class Extensions
{
    public static System.Random rng = new System.Random();

    public static void Shuffle<T>(this IList<T> list)
    { // https://stackoverflow.com/questions/273313/randomize-a-listt
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static T PopAt<T>(this List<T> list, int index)
    { // https://stackoverflow.com/questions/24855908/how-to-dequeue-element-from-a-list
        
        T r = list[index];
        list.RemoveAt(index);
        
        return r;
    }
}