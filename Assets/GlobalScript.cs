using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GlobalScript
{
    [RuntimeInitializeOnLoadMethod]
    public static void OnLoad()
    {
        System.AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
        System.AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
    }

    private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
    {
        Debug.Log(args.LoadedAssembly.FullName);
    }
}
