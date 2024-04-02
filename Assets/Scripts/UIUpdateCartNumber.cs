using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIUpdateCartNumber : MonoBehaviour
{
    public TextMeshProUGUI cartText;
    public NodeReader nodeReader;

    // Update is called once per frame
    void Update()
    {
        cartText.text = nodeReader.dataFromOPCUANode;
    }
}
