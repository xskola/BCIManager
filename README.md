# BCIManager
Library aiding with the development of brain-computer interfaces (BCI) with 3D graphics scenes or virtual reality feedback. It bridges the development in Unity game engine with the development of BCIs in [Openvibe](openvibe.inria.fr).

## Main features

- Starting Openvibe Designer process with the desired scenario from Unity apps
- Sending event markers (stimulations) to Openvibe (for saving with the recorded EEG data)
- Receiving data from Openvibe (e.g., real-time classification results)
- Logging with autosave functionality
- Shutdown in case of Unity-Openvibe connection issues

## Installation

As a prerequisite, Openvibe must be installed on your system. Path to Openvibe can be set in OPENVIBE_INSTALLATION_PATH in BCIManager.cs, the default value is C:\Program Files (x86)\Openvibe-3.0.0 (32-bit version of Openvibe is recommended due to issues with data retrieval with the 64-bit version).

To use BCIManager in your project, copy Assets\BCIScripts to Assets folder in your project. Also copy OpenvibeScripts to your project folder (not to Assets). SampleScene.unity in Assets\Scenes can be also used for a quick start.

If you are new to Openvibe, there is a copy of signal_monitoring.xml scenario in OpenvibeScripts to perform fast check of the quality of your EEG signals. Just open Openvibe Acquisition Server and connect to your EEG (or use Generic Oscillator driver for testing) and press Play, connect to Openvibe Designer and load the scenario, and start it using "play" button. You should see the EEG signals in real-time.


## Usage

Two files need to be attached to a game object: BCIManager.cs and Logger.cs. BCIManager takes care of running Openvibe Designer pre-loaded with the recording scenario (this part records the EEG signals to a GDF file). Logger creates a simple CSV logfile. Logger is not strictly required for running BCIManager.

You need Openvibe Acquisition Server (AS) running and in Play mode before starting Unity app with BCIManager. For debugging, just start the AS and choose Generic Oscillator as a driver, click Connect, then click Play. For experiments, the driver for your EEG device must be selected and configured.

BCIManager starts Openvibe Designer just after the Unity application is started (or put into play mode in Unity Editor), using openvibe-record.cmd script in OpenvibeScripts. A console window running the Openvibe starting script will be visible in the foreground, you need to click on Unity/your app window to switch back to your Unity application. Designer is loaded with the desired scenario (XML file), which is passed as an argument to openvibe-record.cmd. Both can be changed in BCIManager.cs source. If "record_and_display.xml" scenario is used (the default one), real-time signal monitoring window will be displayed alongside your Unity application.

Recorded EEG signals from Openvibe are saved to OpenvibeScripts/Signals folder.

Starting Openvibe from Unity can be disabled altogether with DO_NOT_START_OPENVIBE bool in BCIManager. If set to false, the application will attempt to connect to an already running Openvibe.


## Sending markers

Stimulations (EEG event markers) can be added to the signals by calling BCIManager.sendStim(int code) method in your code.


## Data retrieval (Openvibe -> Unity)

To receive data from Openvibe in real-time, a TCP Writer box must be present in your Openvibe scenario and RECEIVE_DATA in BCIManager set to true. With this setting, BCIManager creates another TCP connection after start of your application. To get a matrix with the signal data, call BCIManager.receiveData() after BCIManager.receiverReady() == true, the data are then provided in OpenvibeSignal class, present in OpenvibeReceiver.cs file.

OpenvibeSignal.signal is the matrix with data from Openvibe with the following structure: [number of sample, number of channel]. Number of channels corresponds to the signal stream in Openvibe scenario. Number of samples can be more than 1 in a signal matrix. See example files for simple data retrieval processing.


## Examples

ExampleScript.cs contains examples how to send stimulations and log events. ExampleScriptReceiver.cs contains an extra code for demonstration of Openvibe -> Unity data retrieval.

SampleScene.unity is a working demo with BCIController game object that has Logger, BCIManager, and ExampleScript attached.


## Data loss prevention

Although it is vital to double-check that the entire BCI pipeline executes properly and both GDF signals and logs are saved before starting the experiments, there are safety mechanisms preventing experimental data loss in BCIManager:

- The application will end itself if connection to AS cannot be made at the beginning of its run (this usually happens because of forgotten click to Play button in AS or EEG device disconnection for any reason).
- The application will also end under the condition that Designer could not be started. This is done by checking that openvibe-designer process is currently running. Please make sure that Openvibe Designer is not running before running your Unity application to prevent issues (unless in DO_NOT_START_OPENVIBE mode).
- Logger component writes the logs to hard drive every 2 seconds (the interval can be easily changed via LOG_FILE_SAVING_INTERVAL variable in Logger), saving experimental data in case of crashes of the Unity application.


## Troubleshooting

If the BCIManager does not connect to Openvibe AS, check that AS is running and in Play mode. Also check hostname and port, or firewall settings if running on another computer than Unity software (generally not recommended due to network delays).

If Openvibe Designer is not starting, check the paths to Openvibe installation and to the script for openvibe-record.cmd. Both of these paths are easily changable at the top of BCIManager class (OPENVIBE_RUNNER_PATH and OPENVIBE_INSTALLATION_PATH).

If you can see Openvibe starting (a console window shows up after starting your Unity app) but it exits immediately, there is either a problem with the openvibe-record.cmd starting script, or directly with your scenario (XML file). In general, you should first customize the scenarios for your needs by running Openvibe Designer manually by opening the scenario (XML file from OpenvibeScripts at your Unity project folder) and doing necessary edits and/or checking whether the scenario plays successfully or an error is shown in the bottom log pane in Designer window.
