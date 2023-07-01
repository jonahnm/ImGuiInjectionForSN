using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Plsworkthistime
{
    public class MainThread : MonoBehaviour
    {
        private static MainThread inst;
        void Awake()
        {
            inst = this;
        }
        public static void RunInMainThread(Action a)
        {
            inst.StartCoroutine(Wrapper(a));
        }
        private static IEnumerator Wrapper(Action a)
        {
            a();
            yield return null;
        }
    }
}
