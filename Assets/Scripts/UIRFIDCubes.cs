using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIRFIDCubes : MonoBehaviour
{
    public GameObject rfidInCube;
    public GameObject rfidOutCube;

    public NodeReader nodeReaderIn;
    public NodeReader nodeReaderOut;

    // Update is called once per frame
    void Update()
    {
        if (nodeReaderIn.dataFromOPCUANode == "True")
        {
            rfidInCube.SetActive(true);
        }
        else
        {
            rfidInCube.SetActive(false);
        }


        if (nodeReaderOut.dataFromOPCUANode == "True")
        {
            rfidOutCube.SetActive(true);
        }
        else
        {
            rfidOutCube.SetActive(false);
        }



    }
}
