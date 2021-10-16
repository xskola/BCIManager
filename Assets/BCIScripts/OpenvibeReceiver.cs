using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

public class OpenvibeSignal
{
    public int channels;
    public int samples;
    public double[,] signal;
}

public class OpenvibeReceiver : MonoBehaviour
{
    public bool socketReady = false;
    TcpClient tcpSocket;
    NetworkStream tcpStream;
    bool headerRead = false;
    bool getSignal = false;
    int sampleChannelSize;
    int sampleCount;
    int channelCount;

    public void Setup()
    {
        tcpSocket = new TcpClient(BCIManager.receiver_connectionHost, BCIManager.receiver_connectionPort);
        tcpStream = tcpSocket.GetStream();
        socketReady = true;
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
        {
            Setup();
        }
    }

    private void ReadHeader()
    {
        int headerSize = 32;
        byte[] buffer = new byte[headerSize];
        UInt32 version, endiannes, frequency, channels, samples;

        tcpStream.Read(buffer, 0, headerSize);

        byte[] v = new byte[4] { buffer[0], buffer[1], buffer[2], buffer[3] };
        byte[] e = new byte[4] { buffer[4], buffer[5], buffer[6], buffer[7] };
        byte[] f = new byte[4] { buffer[8], buffer[9], buffer[10], buffer[11] };
        byte[] c = new byte[4] { buffer[12], buffer[13], buffer[14], buffer[15] };
        byte[] s = new byte[4] { buffer[16], buffer[17], buffer[18], buffer[19] };

        Array.Reverse(e);
        Array.Reverse(v);

        version = BitConverter.ToUInt32(v, 0);
        endiannes = BitConverter.ToUInt32(e, 0);
        frequency = BitConverter.ToUInt32(f, 0);
        channels = BitConverter.ToUInt32(c, 0);
        samples = BitConverter.ToUInt32(s, 0);
        Debug.Log("BCIManager: Connection details to Openvibe Designer - " + 
            "sampling frequency of the signal: " + frequency + "\n" +
            "number of channels: " + channels + "\n" +
            "number of samples per chunk: " + samples + "\n"
            );

        headerRead = true;
        getSignal = true;
        sampleCount = (int)samples;
        channelCount = (int)channels;
        sampleChannelSize = sampleCount * channelCount * sizeof(double);
    }

    public OpenvibeSignal Read()
    {
        if (!socketReady)
            throw new InvalidOperationException("Cannot read from Openvibe: socket not ready");

        if (tcpStream.DataAvailable)
        {

            if (!headerRead)
                ReadHeader();

            if (getSignal)
            {
                OpenvibeSignal newSignal = new OpenvibeSignal();
                newSignal.samples = sampleCount;
                newSignal.channels = channelCount;

                double[,] newMatrix = new double[sampleCount, channelCount];
                byte[] buffer = new byte[sampleChannelSize];
                tcpStream.Read(buffer, 0, sampleChannelSize);

                int row = 0;
                int col = 0;
                for (int i = 0; i < sampleCount * channelCount * (sizeof(double)); i = i + (sizeof(double) * channelCount))
                {
                    for (int j = 0; j < channelCount * sizeof(double); j = j + sizeof(double))
                    {
                        byte[] temp = new byte[8];
                        for (int k = 0; k < 8; k++)
                            temp[k] = buffer[i + j + k];

                        if (BitConverter.IsLittleEndian)
                        {
                            double test = BitConverter.ToDouble(temp, 0);
                            newMatrix[row, col] = test;
                        }
                        col++;
                    }
                    row++;
                    col = 0;
                }

                newSignal.signal = newMatrix;
                return newSignal;
            }
        }
        return null;
    }
}