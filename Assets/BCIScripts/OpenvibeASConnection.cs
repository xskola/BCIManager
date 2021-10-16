using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Net.Sockets;

public class OpenvibeASConnection : MonoBehaviour {
    private ulong TCP_FLAG_TIMESTAMP_CREATE = 4;
    public bool socketReady = false;
    TcpClient tcpSocket;
    NetworkStream tcpStream;

    public void Setup()
    {
        tcpSocket = new TcpClient(BCIManager.connectionHost, BCIManager.connectionPort);
        tcpStream = tcpSocket.GetStream();
        socketReady = true;
    }

    public string Read()
    {
        String result = "";
        if (tcpStream.DataAvailable)
        {
            Byte[] inStream = new Byte[tcpSocket.SendBufferSize];
            tcpStream.Read(inStream, 0, inStream.Length);
            result += System.Text.Encoding.UTF8.GetString(inStream);
        }
        return result;
    }

    public void Close()
    {
        if (!socketReady)
            return;
        tcpSocket.Close();
        socketReady = false;
    }

    public void Maintain()
    {
        if (!tcpStream.CanRead)
            Setup();
    }

    public void SendStimCode(ulong code)
    {
        if (!socketReady)
        {
            Debug.Log("BCIManager: Could not send the stimulation: socket not Ready");
            return;
        }

        byte[] msg = new byte[8];
        ulong flags = TCP_FLAG_TIMESTAMP_CREATE;

        msg = BitConverter.GetBytes(flags);
        tcpStream.Write(msg, 0, sizeof(ulong));
        msg = BitConverter.GetBytes(code);
        tcpStream.Write(msg, 0, sizeof(ulong));
        msg = BitConverter.GetBytes((ulong)0);
        tcpStream.Write(msg, 0, sizeof(ulong));
    }

}
