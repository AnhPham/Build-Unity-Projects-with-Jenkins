using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Demo : MonoBehaviour
{
    void Start()
    {
        var list = new List<string>();
#if A
        list.Add("A");
#endif
#if B
        list.Add("B");
#endif
#if C
        list.Add("C");
#endif
        GetComponent<Text>().text = "Scripting Define Symbols: " + string.Join(',', list);
    }
}
