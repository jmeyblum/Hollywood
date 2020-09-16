using BSpace;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CSpace
{
    public class ImplementationC : IBInterface2
    {
       // [UnityEditor.MenuItem("Jean/context")]
        public static void DoLoad()
        {
            /*Debug.LogError("Test");

            var warmupTyp = Type.GetType("__Hollywood.Assembly.C.dll.__WarmUpInjectedInterfaces");

            var interf = warmupTyp.GetField("__interfaces", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            var valueIn = interf.GetValue(null);

            Debug.Log(valueIn);*/

			//var t 
        }

    }

	public interface IChose
	{
		HashSet<object> __ownedInstances { get; set; }
	}

	public class A : IChose
	{
		public HashSet<object> __ownedInstances { get; set; }
	}

	public class B : A, IChose
	{
		
	}
}