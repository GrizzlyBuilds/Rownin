using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System;
using System.IO;
using Newtonsoft.Json;

public class SerialManager : MonoBehaviour
{
    public SerialController serialController;

    private bool isSetup = false;

    private IEnumerator handshakeCoroutine;
    private const string ROWNIN_HANDSHAKE_RESPONSE = "Rownin";

    private IEnumerator SendHandshake(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        serialController.SendSerialMessage(ROWNIN_HANDSHAKE_RESPONSE);  
    }

    // Invoked when a line of data is received from the serial device.
    void OnMessageArrived(string msg)
    {
        if (msg == ROWNIN_HANDSHAKE_RESPONSE)
        {
            Debug.Log("Arduino handshake complete");
            isSetup = true;
            GameManager.instance.Ready();
        }
        else
        {
            if (isSetup)
            {
                ProcessMessage(msg);
            }
        }
    }

    // Invoked when a connect/disconnect event occurs. The parameter 'success'
    // will be 'true' upon connection, and 'false' upon disconnection or
    // failure to connect.
    void OnConnectionEvent(bool success)
    {
        if (success)
        {
            Debug.Log("Connection established");
            handshakeCoroutine = SendHandshake(2.0f);
            StartCoroutine(handshakeCoroutine);
        }
        else
        {
            isSetup = false;
            Debug.Log("Connection attempt failed or disconnection detected");
        }
    }

    private void ProcessMessage(string message)
    {
        if (message != "A" && message != "B")
        {
            Debug.LogError("Unexpected message: " + message);
            return;
        }

        var button = message;

        Player.localPlayer.ProcessInput(button);
    }
}


