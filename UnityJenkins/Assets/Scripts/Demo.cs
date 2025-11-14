using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Demo : MonoBehaviour
{
    void Start()
    {
        var text = string.Empty;
#if A
        text += "A";
#endif
#if B
        text += "B";
#endif
#if C
        text += "C";
#endif
        GetComponent<Text>().text = "Scripting Define Symbols: " + text;
    }
}
