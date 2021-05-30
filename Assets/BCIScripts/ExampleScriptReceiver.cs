using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleScriptReceiver : MonoBehaviour
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
        if (BCIManager.receiverReady())
        {
            OpenvibeSignal data = BCIManager.receiveData();                   // OpenvibeSignal data class is defined in OpenvibeReceiver.cs file
            if(data != null)                                                  // check if new data were received in current frame
                for (int sample = 0; sample < data.samples; sample++)         // each OpenvibeSignal matrix can contain more than 1 sample
                    for (int channel = 0; channel < data.channels; channel++) // number of channels corresponds to the number of channels in Openvibe scenario
                        Debug.Log(data.signal[sample, channel]);

        }


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
                                                // to be used in case "regular quit" (i.e., stopping/closing the app) does not work properly
    }
}
