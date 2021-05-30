using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BCIManager : MonoBehaviour
{
    /*
     * BCIManager
     * 
     * Starts Openvibe Designer with desired scenario and controls the connection to Openvibe Acquisition Server (AS).
     * Optionally (when RECEIVE_DATA == true) handles data retrieval from Openvibe Designer (mainly for purposes of
     * real-time brain-computer interaction).
     * 
     * Details:
     * - Starts Openvibe Designer with a recording/interaction scenario using a script specified in OPENVIBE_SCENARIO_PATH.
     * - Starts and shuts down the connection to AS (and Designer) using connectOpenvibe() and disconnectOpenvibe() methods.
     * - Can be used to send stimulation codes (markers to EEG signals) with BCIManager.sendStim(code) and receive data.
     * 
     * Openvibe Acquisition Server must be started manually and put into Play mode.
     * 
     * Set connectionEnabled to false for disabling this class functionality (no Openvibe connections made).
     * 
     * In case of issues with Openvibe getting improperly disconnected (scenario does not stop, Openvibe not exiting, etc),
     * consider using BCIManager.disconnectAndQuit() method for quitting your application.
    */

    // ------------------------------------------------------------------------ //

    /*
     * Openvibe/signal acquisition control settings:
     * 
     */

    public static bool connectionEnabled = true;                                // if disabled, all Openvibe operations will be disabled

    private static string OPENVIBE_RUNNER_PATH = "OpenvibeScripts\\openvibe-record.cmd";                // path to Openvibe runner in the Unity project
    private static string OPENVIBE_INSTALLATION_PATH = "\"c:\\program files (x86)\\openvibe-3.0.0\"";   // path to Openvibe installation on the filesystem
                                                                                                        // note the format of the string (escaped \, in \") as in "\"c:\\program files\\openvibe-3.0.0\""
    private static string OPENVIBE_SCENARIO_NAME = "record_and_display.xml";    // selects a scenario file from the same folder as in OPENVIBE_RUNNER_PATH ("record_and_display.xml", "simple_record.xml", "record_advanced.xml", ...)
    public static bool QUIT_ON_CONNECTION_ERROR = true;                         /* if disabled, the application will not quit on unsuccessful connection to Openvibe AS
                                                                                 * while useful for debugging, it can cause experimental data loss if disabled in experiments */
    public static bool DO_NOT_START_OPENVIBE = false;                           /* if true, BCIManager will not start Openvibe application and connect to a running instance instead
                                                                                 * this can save a lot of time during setting up and debugging */
    private static int INITIAL_STIMCODE = 0;                                    // can be changed to e.g. OpenvibeStimCodes.OVTK_StimulationId_ExperimentStart
    private static int SECONDS_TO_WAIT_FOR_OPENVIBE = 2;                        // increase in case of connecting too early before Openvibe starts

    public static string connectionHost = "localhost";                          // change if running Openvibe on another computer
    public static int connectionPort = 15361;                                   // default AS port number is 15361

    /*
     * Data retreival (Openvibe -> Unity data flow) settings
     * 
     */
    
    static bool RECEIVE_DATA = false;                                           // set to true to enable data receiving from Openvibe scenario (both-ways interaction mode)
                                                                                // you need a corresponding Openvibe scenario (with a TCP Writer box) (see receiver_tester.xml)
    public static string receiver_connectionHost = "localhost";
    public static int receiver_connectionPort = 5678;                           // default Designer's TCP Writer Box port number is 5678

    // ------------------------------------------------------------------------ //

    static OpenvibeASConnection openvibeASConnection;
    static OpenvibeReceiver openvibeReceiverConnection;

    System.Diagnostics.Process ovApp = new System.Diagnostics.Process();

    void Start()
    {
        openvibeASConnection = gameObject.AddComponent<OpenvibeASConnection>();
        if (RECEIVE_DATA)
            openvibeReceiverConnection = gameObject.AddComponent<OpenvibeReceiver>();

        if (!DO_NOT_START_OPENVIBE)
            startOpenvibe();
        
        if (connectionEnabled)
        {
            Invoke("connectOpenvibeAS", SECONDS_TO_WAIT_FOR_OPENVIBE);
            Invoke("connectOpenvibeReceiver", SECONDS_TO_WAIT_FOR_OPENVIBE);
        }
    }

    public void connectOpenvibeAS()
    {
        if (openvibeASConnection.socketReady == false)
        {
            Debug.Log("BCIManager: Attempting to connect to Openvibe AS...");
            try
            {
                openvibeASConnection.setup();
                Debug.Log("BCIManager: Openvibe AS connected successfully");
                sendInitializationStim();
            }
            catch (Exception e)
            {
                Debug.Log("BCIManager: Could not connect to Openvibe AS: " + e);
                Debug.Log("BCIManager: Error while connecting to Openvibe Acquisition Server. Check that the server is running, is in Play mode, and the connection host and port are correct.");
                if (BCIManager.QUIT_ON_CONNECTION_ERROR)
                {
                    BCIManager.connectionEnabled = false;
                    Logger.logEvent("Quitting application due to errors while connecting to Openvibe AS");  // delete this line in case you do not want to use Logger in your app
                    Application.Quit();
                    Debug.Log("BCIManager: Stopping in-editor application run");
                    UnityEditor.EditorApplication.isPlaying = false;
                }
            }
        }
        else
        {
            Debug.Log("BCIManager: Not connecting to Openvibe AS: already connected");
        }
    }

    public void connectOpenvibeReceiver()
    {
        if (openvibeReceiverConnection.socketReady == false)
        {
            Debug.Log("BCIManager: Attempting to connect to Openvibe Designer...");
            try
            {
                openvibeReceiverConnection.setup();
                Debug.Log("BCIManager: Openvibe Designer connected successfully");
            }
            catch (Exception e)
            {
                Debug.Log("BCIManager: Could not connect to Openvibe Designer: " + e);
                Debug.Log("BCIManager: Error while connecting to Openvibe Designer. Check that correct scenario with TCP Writer box is loaded during startup/running in Openvibe.");
                if (BCIManager.QUIT_ON_CONNECTION_ERROR)
                {
                    BCIManager.connectionEnabled = false;
                    Logger.logEvent("Quitting application due to errors while connecting to Openvibe Designer");  // delete this line in case you do not want to use Logger in your app
                    Application.Quit();
                    Debug.Log("BCIManager: Stopping in-editor application run");
                    UnityEditor.EditorApplication.isPlaying = false;
                }
            }
        }
        else
        {
            Debug.Log("BCIManager: Not connecting to Openvibe Designer: already connected");
        }
    }

    public static void disconnectOpenvibe()
    {
        Debug.Log("BCIManager: Disconnecting from Openvibe AS");
        openvibeASConnection.close();
        if(RECEIVE_DATA)
            openvibeReceiverConnection.close();
    }

    public static void sendStim(ulong stimcode)
    {
        if (connectionEnabled)
            openvibeASConnection.sendStimCode(stimcode);
        else
            Debug.Log("BCIManager: Not sending stimulation: connectionEnabled=false");
    }

    public static void sendStim(int stimcode)
    {
        BCIManager.sendStim((ulong)stimcode);
    }

    public static bool receiverReady()
    {
        return openvibeReceiverConnection.socketReady;
    }

    public static OpenvibeSignal receiveData()
    {
        return openvibeReceiverConnection.read();
    }

    static void sendInitializationStim()
    {
        Debug.Log("BCIManager: Sending initial stimulation code with value " + BCIManager.INITIAL_STIMCODE.ToString());
        BCIManager.sendStim(BCIManager.INITIAL_STIMCODE);
    }

    void startOpenvibe()
    {
        if (connectionEnabled)
        {
            ovApp.StartInfo.FileName = OPENVIBE_RUNNER_PATH;
            ovApp.StartInfo.Arguments = OPENVIBE_SCENARIO_NAME + " " + OPENVIBE_INSTALLATION_PATH;
            ovApp.Start();
            StartCoroutine(checkOpenvibeDesignerRunning());
        }
    }

    IEnumerator checkOpenvibeDesignerRunning()
    {
        yield return new WaitForSeconds(2.5f);
        System.Diagnostics.Process[] pname = System.Diagnostics.Process.GetProcessesByName("openvibe-designer");
        if (pname.Length > 0)
            Debug.Log("BCIManager: Openvibe Designer seems to be running");
        else
        {
            Debug.Log("BCIManager: could not find openvibe-designer process, assuming Openvibe Designer could not start");
            if (BCIManager.QUIT_ON_CONNECTION_ERROR)
            {
                BCIManager.connectionEnabled = false;
                Logger.logEvent("Quitting application due to errors while running Openvibe");  // delete this line in case you do not want to use Logger in your app
                Application.Quit();
                Debug.Log("BCIManager: Stopping in-editor application run");
                UnityEditor.EditorApplication.isPlaying = false;
            }
        }
    }

    public static void disconnectAndQuit()
    {
        if (BCIManager.connectionEnabled)
        {
            Debug.Log("BCIManager: Sending ExperimentStop stimulation");
            BCIManager.sendStim(OpenvibeStimCodes.OVTK_StimulationId_ExperimentStop);
            BCIManager.disconnectOpenvibe();
            BCIManager.connectionEnabled = false;
        }       
        Application.Quit();
        UnityEditor.EditorApplication.isPlaying = false;
    }

    void OnApplicationQuit()
    {
        if (BCIManager.connectionEnabled)
        {
            Debug.Log("BCIManager: Sending ExperimentStop stimulation");
            BCIManager.sendStim(OpenvibeStimCodes.OVTK_StimulationId_ExperimentStop);
            BCIManager.disconnectOpenvibe();
        }
    }
}
