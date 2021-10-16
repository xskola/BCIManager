using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* TrainingDriver - example script serving as skeleton for 2-class motor imagery training sequence management
 *
 * - training is started after Spacebar is pressed
 * - key p activates pause
 * - key i markes current trial as invalid in both log and recording
 * - key insert puts mark to EEG recording
 *
 * Text game objects that should be utilized for the training:
 * - text is the main display for simple instructions (eye fixation, left/right trial, ...)
 * - progressText is for showing progress of the user training in %
 * - finalText can show summary after training (such as total score)
 *
 * motivationAnimation and successAnimation can be used to provide training cues via prepared animations for started trials and for successful trials
 * Animation-related code is commented out.
 *
 * Various parameters in InitializeModeOfOperation() can be sent from a menu scene or set manually.
 * BCIManager.START_AUTOMATICALLY must be set to false so demo modes can work without initiating Openvibe connection
 * Demo mode must be turned off to actually use BCIManager!
*/

public class TrainingDriver : MonoBehaviour {
    // public motivationAnimation; //
    // public successAnimation;    // replace with animation references
    public bool demoMode = true;   // <-- must be changed to false to use BCIManager
    public Animator animator;
    public Text text;
    public Text progressText;
    public Text finalText;
    private static System.Random rng = new System.Random();
    string triallogfilename = @"log\log-trials-";
    List<String> triallog = new List<string>();		// for detailed log on each trial
    List<int> sides = new List<int>();
    public int trialOngoing;
    public int trialNum = -1;
    private int trialNumTimeouting;
    private double trialDelta;
    private double trialCorrectDelta;
    private double trialIncorrectDelta;
    private double trialNeutralDelta;
    private double trialEvalDelta;
    private double trialDecisionLTime;
    private double trialDecisionRTime;
    public double score;
    private double totalScore;
    double initialTicks = 0f;
    int totalNumberOfTrials = 40;
    public string extraLogLine;
    public string extraTrialLogLine;
    private bool paused = false;
    public bool timeouting = false;
    public bool invalidating = false;
    private bool firstRecording = false;
	private bool bciManagerConnected = false;

    private double class_LR;

    float max_anim_speed = 1f;
    float min_anim_speed = 0.2f;
    float anim_speed;

    const int TRIAL_LEFT = -1;
    const int TRIAL_RIGHT = 1;
    const int TRIAL_NONE = 0;

    public const int TL_PRE_TRAINING = 0;
    public const int TL_NONTRIAL = 1;
    public const int TL_INIT = 2;
    public const int TL_ONSET = 3;
    public const int TL_ONGOING_FEEDBACK = 4;
    public const int TL_ENDED = 5;
    public const int TL_ONGOING_FAIL = 6;
    public const int TL_POST_TRAINING = 10;

    float TIME_ONSET;
    float TIME_TIMEOUT;
    float TIME_REST_C;  // time for rest = TIME_REST_C + TIME_REST_L*{0..1}
    float TIME_REST_L;
    float TIME_ANIM_DURATION;
    float TIME_TOTAL;

    public int trainingState = TL_PRE_TRAINING;
	static BCIManager bci;
	
	double ProcessLDAData(OpenvibeSignal s)
    {
		if(s == null)
			return 0;
		
		// Extracts LDA classification results from received signal matrix
        if (s.samples != 2 || s.channels != 1)
            return 0;    
      
        double value1 = 0, value2 = 0;
        try
        {
            value1 = s.signal[0, 0];
            value2 = s.signal[1, 0];
        }
        catch
        {
            Debug.Log("Issue with the data coming from the TCP stream.");
        }

        return value2 - value1;
    }
	
    string TrainingStateDesc(int state = -1)
    {
        string desc = "";
        if (state == -1)
            return TrainingStateDesc(trainingState);

        switch(state)
        {
            case 0: desc = "TL_PRE_TRAINING";
                break;
            case 1: desc = "TL_NONTRIAL";
                break;
            case 2: desc = "TL_INIT";
                break;
            case 3: desc = "TL_ONSET";
                break;
            case 4: desc = "TL_ONGOING_DFB";
                break;
            case 5: desc = "TL_ONGOING_FEEDBACK";
                break;
            case 6: desc = "TL_ENDED";
                break;
            case 7: desc = "TL_ONGOING_FAIL";
                break;
            case 10: desc = "TL_POST_TRAINING";
                break;
        }
        return desc;
    }

    void InitializeModeOfOperation()
    {
        int trials = 20;
        
        totalNumberOfTrials = trials*2;
		// demoMode = true;
		// firstRecording = false;

        max_anim_speed = 1f;	// set to higher for progressive animation speed-up

		TIME_ONSET = 0.5f;							//
		TIME_TOTAL = 1.2f;							//
		TIME_TIMEOUT = 1.5f + (TIME_TOTAL * 2);		//
		TIME_REST_C = 1f;							//
		TIME_REST_L = 2f;							// set according to animation timing
		
		Debug.Log("Starting a session with " + trials + " trials.");
    }

    void InitializeRandomSides()
    {
        for (int i = 1; i <= totalNumberOfTrials / 2; i++)
            sides.Add(TRIAL_LEFT);
        for (int i = 1; i <= totalNumberOfTrials / 2; i++)
            sides.Add(TRIAL_RIGHT);
        Extensions.Shuffle(sides);
    }

    IEnumerator InitializeMIRecording()
    {
        trainingState = TL_INIT;
        trialNum = 1;
        yield return new WaitForSeconds(0.5f);
        if (!demoMode)
            BCIManager.SendStim(OpenvibeStimCodes.OVTK_StimulationId_ExperimentStart);
        yield return new WaitForSeconds(0.5f);        
    }

    IEnumerator Trial(bool right = false)
    {
        if (!demoMode)
        {
            BCIManager.SendStim(OpenvibeStimCodes.OVTK_GDF_Start_Of_Trial);
            BCIManager.SendStim(OpenvibeStimCodes.OVTK_GDF_Cross_On_Screen);
        }
        text.text = "+";
        
		yield return new WaitForSeconds(0.5f);	// the delays, if needed, should be tuned to animations
        // motivationAnimation.Start(right); <- trigger animation motivating MI trial (parameter true if right side trial)
        // yield return new WaitForSeconds(0.75f);

        switch (right)
        {
            case true:
                
                if (!demoMode)
                    BCIManager.SendStim(OpenvibeStimCodes.OVTK_GDF_Right);
                text.text = ">";    
                trainingState = TL_ONSET;
                trialOngoing = TRIAL_RIGHT;
                break;

            case false:
                if (!demoMode)
                    BCIManager.SendStim(OpenvibeStimCodes.OVTK_GDF_Left);
                trainingState = TL_ONSET;
                text.text = "<";
                trialOngoing = TRIAL_LEFT;
                break;
        }
        
        StartCoroutine(TrialTimeouter(trialNum));
    }

    public void TrialFin()
    {
        if (!(trainingState == TL_ONGOING_FEEDBACK || trainingState == TL_ONGOING_FAIL))
            return;

        UpdateProgress();

        if (invalidating)
            sides.Add(trialOngoing);	// repeat invalidated trial side

        int lastTrialSide = trialOngoing;
        trialOngoing = TRIAL_NONE;
        trainingState = TL_ENDED;
        StartCoroutine(TrialFinisher());

        Debug.Log(Time.time + " in TrialFin() for side " + lastTrialSide + " and trial num " + trialNum);

        if (!demoMode)
            BCIManager.SendStim(OpenvibeStimCodes.OVTK_GDF_End_Of_Trial);

        if (!invalidating && !firstRecording)
            ShowScore();

        anim_speed = 1f;
        animator.speed = anim_speed;

        string logline = ((double)((DateTime.Now.Ticks - initialTicks) / 10000000f)).ToString("################.################") + "; " +
            trialNum + "; " + lastTrialSide + "; " +
            trialDelta + "; " + trialCorrectDelta + "; " + trialIncorrectDelta + "; " + score + "; " + extraTrialLogLine;
        triallog.Add(logline);
        extraTrialLogLine = "";

        Debug.Log("trialDelta: " + trialDelta.ToString() + " --- trialCorrectDelta: " + trialCorrectDelta + " --- trialEvalDelta: " + trialEvalDelta);
        Debug.Log("trialIncorrectDelta: " + trialIncorrectDelta + " --- trialNeutralDelta: " + trialNeutralDelta);

        trialNum++;
        
        if(!invalidating)
            totalScore += score;

        trialDelta = 0;
        trialCorrectDelta = 0;
        trialEvalDelta = 0;
        trialIncorrectDelta = 0;
        trialNeutralDelta = 0;
        score = 0;
        timeouting = false;
        invalidating = false;
    }

    IEnumerator TrialTimeouter(int trialNumTimeouting)
    {
        // end of trial by timeout
        yield return new WaitForSeconds(TIME_TIMEOUT);
        if ((trainingState == TL_ONGOING_FEEDBACK || trainingState == TL_ONGOING_FAIL) && trialNumTimeouting == trialNum)
        {
            text.text = "";
            // if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idling"))
            // {
            //     successAnimation.triggerLever(trialOngoing);	// successAnimation trigger by timeout
            // }
            trainingState = TL_ONGOING_FAIL;
            timeouting = true;
            anim_speed = max_anim_speed;
            animator.speed = anim_speed;
            extraTrialLogLine += "timeout ";
        }
    }

    IEnumerator TrialFinisher()
    {
        float delay = TIME_REST_C + TIME_REST_L * (float)rng.NextDouble();
        delay += (int)(trialDelta / 3);
        yield return new WaitForSeconds(delay);
        trainingState = TL_NONTRIAL;
    }

    public void ShowScore()
    {
        if (score > 1)
            text.text = ((int)score).ToString();
        else if (score > 999)
            text.text = "999";
        else
            text.text = "";
    }

    private void GetClassifierResult()
    {
        // Obtain the classificiation decisions from Openvibe TCP channels
        // and log them in the descriptive log

        if (demoMode || firstRecording)
        {
            float shift;
            if (trialOngoing == TRIAL_LEFT)
                shift = -0.75f;
            else
                shift = -0.25f;
            
            class_LR = (float)rng.NextDouble() + shift;
            // ~75% of hitting the "right" class
            return;
        }
        else
        {
			if (bciManagerConnected && BCIManager.ReceiverReady())
				class_LR = ProcessLDAData(BCIManager.ReceiveData());
        }

        double timeToLog;
        if (initialTicks == 0)
            timeToLog = -1f;
        else
            timeToLog = (double)((DateTime.Now.Ticks - initialTicks) / 10000000f);

        string logline = trialNum + "; " + trialOngoing + "; " + TrainingStateDesc(trainingState) + "; " +
            trialDelta + "; " + class_LR + "; " + trialCorrectDelta + "; " + trialIncorrectDelta + "; " + anim_speed;

        if (extraLogLine != "")
        {
            logline += "; ";
            logline += extraLogLine;
            extraLogLine = "";
        }
        Logger.LogEvent(logline);
    }

    void FeedbackContinuous(double status)
    {
        if(trialOngoing == TRIAL_LEFT)
            status = -status;

        if (status == 0)
            return;

        // if (status > 0)
        // {
        //    if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idling"))
        //        successAnimation.triggerLever(trialOngoing);
        // }

        anim_speed = animator.speed;

        
        if (status > 0)
            anim_speed += (anim_speed / 4);
        else if (status < 0)
            anim_speed -= (anim_speed / 4);

        if (anim_speed < min_anim_speed)
            anim_speed = min_anim_speed;
        else if (anim_speed > max_anim_speed)
            anim_speed = max_anim_speed;
        
        animator.speed = anim_speed;
    }

    public void UpdateProgress()
    {
        float trialsToGo = sides.ToArray().Length;
        int remainingPerc = 100 - ((int)((trialsToGo / totalNumberOfTrials) * 100));
        if (remainingPerc < 0)
            remainingPerc = 0;
        if (remainingPerc == 100)
            progressText.text = "✓";
        else
            progressText.text = remainingPerc.ToString() + "%";
    }

    IEnumerator FinishRecording()
    {
        if (!demoMode) {
			SendFinalizationStims();
		}
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator SendInvalidationSeq()
    {
        for (int i = 1; i <= 5; i++)
        {
            BCIManager.SendStim(OpenvibeStimCodes.OVTK_GDF_Artifact_Movement);
            yield return new WaitForSeconds(0.1f);
        }
    }

    void FailTrial()
    {
        text.text = "×××";
        extraLogLine = "invalidating";
        extraTrialLogLine += "invalid ";
        invalidating = true;
        trainingState = TL_ONGOING_FAIL;
        anim_speed = max_anim_speed;
        animator.speed = anim_speed;
        if(!demoMode)
            StartCoroutine(SendInvalidationSeq());
    }
	
	public void SendFinalizationStims()
	{
		BCIManager.SendStim(OpenvibeStimCodes.OVTK_GDF_End_Of_Session);
		BCIManager.SendStim(OpenvibeStimCodes.OVTK_StimulationId_Train);
		BCIManager.SendStim(OpenvibeStimCodes.OVTK_StimulationId_ExperimentStop);
	}
    
    void Update() {
        GetClassifierResult();

        switch (trainingState)
        {
            case TL_NONTRIAL:
                if (!paused)
                    trainingState = TL_INIT;
                else
                    text.text = "...";
                break;

            case TL_INIT:
                if (trialOngoing != TRIAL_NONE)
                    break;

                int nextSide = -42; // invalid
                try { nextSide = Extensions.PopAt(sides, 0); }
                catch (ArgumentOutOfRangeException)
                {
                    trainingState = TL_POST_TRAINING;
                    StartCoroutine(FinishRecording());
                    finalText.text += " " + ((int)(totalScore)).ToString();
                    finalText.gameObject.SetActive(true);
                    break;
                }

                if (nextSide == TRIAL_LEFT)
                {
                    Debug.Log(Time.time + " beginning left trial num " + trialNum);
                    trialOngoing = TRIAL_LEFT;
                    StartCoroutine(Trial(false));
                }
                else if (nextSide == TRIAL_RIGHT)
                {
                    Debug.Log(Time.time + " beginning right trial num " + trialNum);
                    trialOngoing = TRIAL_RIGHT;
                    StartCoroutine(Trial(true));
                }
                break;

            case TL_ONSET:
                trialDelta += Time.deltaTime;
                if (trialDelta >= TIME_ONSET)
                {
                    trialDelta = 0;
                    // after onset has passed, delta is the actual user effort time
					trainingState = TL_ONGOING_FEEDBACK;
					if (!demoMode)
						BCIManager.SendStim(OpenvibeStimCodes.OVTK_GDF_Feedback_Continuous);
                }
                break;

            case TL_ONGOING_FEEDBACK:
                score = (trialCorrectDelta * 100) / (trialCorrectDelta + trialIncorrectDelta);
                if (class_LR * trialOngoing < 0)
                    trialIncorrectDelta += Time.deltaTime;                    
                else if (class_LR * trialOngoing > 0)
                    trialCorrectDelta += Time.deltaTime;
                else if (class_LR == 0)
                    trialNeutralDelta += Time.deltaTime;
                trialDelta += Time.deltaTime;
                FeedbackContinuous(class_LR);
                break;

        }

        if (Input.GetKeyDown("space"))
        {
            if (trainingState == TL_PRE_TRAINING)
            {
                initialTicks = DateTime.Now.Ticks;
                StartCoroutine(InitializeMIRecording());
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!demoMode)
            {
                SendFinalizationStims();
                KillApps();
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/menu");
        }

        if (Input.GetKeyDown("b"))
        {
            BCIManager.SendStim(OpenvibeStimCodes.OVTK_StimulationId_BaselineStart);
        }

        if (Input.GetKeyDown("n"))
        {
            BCIManager.SendStim(OpenvibeStimCodes.OVTK_StimulationId_BaselineStop);
        }

        if(Input.GetKeyDown("p"))
        {
            paused = !paused;
            extraLogLine = "toggling pause to " + paused.ToString();
        }

        if (Input.GetKeyDown("i"))
            FailTrial();

        if (Input.GetKeyDown(KeyCode.Insert))
        {
            BCIManager.SendStim(OpenvibeStimCodes.OVTK_StimulationId_Label_00);
            extraLogLine = "mark";
        }
    }

    void Start()
    {
        InitializeModeOfOperation();
        InitializeRandomSides();
        UpdateProgress();

        if (demoMode)
        {
            BCIManager.connectionEnabled = false;
			BCIManager.DO_NOT_START_OPENVIBE = true;
        }        
		
		bci.Initialize();
		bciManagerConnected = true;
		trialOngoing = TRIAL_NONE;
		
        // logging
		string postfix = System.DateTime.Now.ToString("MM-dd-HH-mm-ss", System.Globalization.CultureInfo.InvariantCulture);
        string trialLogHeader = "Time; TrialNum; Side; TrialTotalTime; TrialCorrectTime; TrialIncorrectTime; Accuracy; Other";
        triallogfilename += postfix + ".csv";
        Logger.LogHeader("Time; TrialNum; Side; TrainingState; TrialTotalTime; LRClassification; MIClassification; TrialCorrectTime; TrialIncorrectTime; anim_speed");
        string settingsInfo = "demo mode: " + demoMode.ToString() + "\nnumber of trials per class: " + (totalNumberOfTrials/2).ToString() + "\nmin_anim_speed: " + min_anim_speed.ToString() + "\nmax_anim_speed: " + max_anim_speed.ToString() + "\nTIME_ONSET: " + TIME_ONSET.ToString() + "\nTIME_TOTAL: " + TIME_TOTAL.ToString() + "\nTIME_TIMEOUT: " + TIME_TIMEOUT.ToString() + "\nTIME_REST_C: " + TIME_REST_C.ToString() + "\nTIME_REST_L: " + TIME_REST_L.ToString() +  "\n";
        triallog.Add(settingsInfo);
        triallog.Add(trialLogHeader);

        // If using animation accompanying trials, set following references
		// motivationAnimation = ...;	// for animation accompanying training task (left/right hand MI)
        // successAnimation = ...;      // for animation accompanying successful task completition (e.g., > 50% time in correct MI state)
        anim_speed = 1f;
        animator.speed = anim_speed;
    }


    void StartOVScenario()
    {

    }

    void ConnectOpenvibe()
    {

    }

    void KillApps()
    {

    }

    void OnApplicationQuit()
    {

    }
}
