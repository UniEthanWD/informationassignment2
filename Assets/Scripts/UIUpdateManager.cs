using realvirtual;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIUpdateManager : MonoBehaviour
{
    [Header("Project Scripts")]
    public OPCUA_Interface[] interfacess;
    public NodeReader[] RFIDInNodeReaders;

    public SendOrder sendOrder;

    [Header("Project UI Elements")]
    public Image[] connectionImages;
    public TMP_Text[] RFIDInTMP;
    public TMP_Dropdown orderQuantityDropdown;
    public TMP_Dropdown partNumberDropdown;


    //[Header("Project AR Elements")]
   // public Image[] connectionImagesAR;

    // Method to update connection status images based on OPC UA interface connection
    public void UpdateConnectionImages(int interfaceToRead)
    {
        if (interfacess[interfaceToRead].IsConnected)
        {
            connectionImages[interfaceToRead].color = Color.green;
           // connectionImagesAR[interfaceToRead].color = Color.blue;
        }
        else
        {
            connectionImages[interfaceToRead].color = Color.red;
           // connectionImagesAR[interfaceToRead].color = Color.yellow;
        }
    }
   

    // Method to update text information from OPC UA node to TMP_Text component
    public void UpdateDataFromNodeTMP(int interfaceToRead, string node)
    {
        if (node == "RFIDIn")
        {
            Debug.LogWarning(interfaceToRead + 1 + " is reading: " + RFIDInNodeReaders[interfaceToRead].dataFromOPCUANode);
        }

    }

    // Method to send an order to the machine using values from UI dropdowns
    public void SendOrderToMachine()
    {
        sendOrder.partNumber = partNumberDropdown.options[partNumberDropdown.value].text;
        sendOrder.qty = orderQuantityDropdown.options[orderQuantityDropdown.value].text;
        sendOrder.SendOrderToFactory();
    }

    private void Update()
    {
        for (int i = 0; i < RFIDInNodeReaders.Length; i++)
        {
            RFIDInTMP[i].text = RFIDInNodeReaders[i].dataFromOPCUANode;
        }


    }
}
