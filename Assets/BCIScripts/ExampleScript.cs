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
        Logger.logEvent("Application start");
    }

    void Update()
    {
        if (Input.GetKeyDown("s"))              // if key "s" is pressed
        {
            Logger.logEvent("EEG phase 1");     // outputs logText to log file (created in Logger class)
            BCIManager.sendStim(1);             // sends stimulation code "1" to Openvibe Acquisition Server
        }

        if (Input.GetKeyDown("b"))
        {
            Logger.logEvent("Beginning of the recording");
            BCIManager.sendStim(OpenvibeStimCodes.OVTK_StimulationId_ExperimentStart);
        }

        if (Input.GetKeyDown("q"))
            BCIManager.disconnectAndQuit();     // "nice" quit method handled by BCIManager
                                                // to be used in case "regular quit" does not work properly
    }
}
