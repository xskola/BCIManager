using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleScript : MonoBehaviour
    /*
     * ExampleScript
     * 
     * Simple example showing Logger and BCIManager.sendStim() functionality
     * 
     * Press "s" or "b" keys to send a stimulation to EEG stream and an arbitrary message to log.
    */
{
    void Start()
    {
        Logger.NewLog(this, "main", new string[] { "Event" }, true);
        Logger.Add("Application start");
    }

    void Update()
    {
        if (Input.GetKeyDown("s"))              // if key "s" is pressed
        {
            Logger.Add("EEG phase 1");     // outputs logText to log file (created in Logger class)
            BCIManager.SendStim(1);             // sends stimulation code "1" to Openvibe Acquisition Server
        }

        if (Input.GetKeyDown("b"))
        {
            Logger.Add("Beginning of the recording");
            BCIManager.SendStim(OpenvibeStimCodes.OVTK_StimulationId_ExperimentStart);
        }

        if (Input.GetKeyDown("q"))
            BCIManager.DisconnectAndQuit();     // "nice" quit method handled by BCIManager
                                                // to be used in case "regular quit" does not work properly
    }
}
