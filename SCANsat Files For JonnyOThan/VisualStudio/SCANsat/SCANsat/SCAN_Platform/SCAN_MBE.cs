#region license
/* 
 *  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANsat - MonoBehaviour Extended
 * 
 * A modified form of TriggerAu's MonoBehaviour Extended class:
 * http://forum.kerbalspaceprogram.com/threads/66503-KSP-Plugin-Framework
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 *
 */
#endregion
using System;

using UnityEngine;

using Log = SCANsat.SCAN_Platform.Logging.ConsoleLogger;


namespace SCANsat.SCAN_Platform
{
	public abstract class SCAN_MBE : MonoBehaviour
	{
		//#region Constructor
		////private MonoBehaviourExtended()
		////{
		////}
		////internal static MonoBehaviourExtended CreateComponent(GameObject AttachTo)
		////{
		////    MonoBehaviourExtended monoReturn;
		////    monoReturn = AttachTo.AddComponent<MonoBehaviourExtended>();
		////    return monoReturn;
		////}
		//static SCAN_MBE()
		//{
		//	UnityEngine.Random.seed = (int)(DateTime.Now - DateTime.Now.Date).TotalSeconds;
		//	Log.Now("Seed Generated...");
		//}
		//#endregion
		internal T AddComponent<T>() where T : UnityEngine.Component { return gameObject.AddComponent<T>(); }

		private static bool _seedGenerated;
		private bool _RepeatRunning;
		private bool _OnGUI_FirstRun;
		private float _RepeatInitialWait;
		private float _RepeatSecs;
		private double LooperUTLastStart { get; set; }
		private double LooperUTStart { get; set; }
		internal TimeSpan LooperDuration { get; private set; }
		internal double LooperUTPeriod { get; private set; }
		internal bool LooperRunning { get { return _RepeatRunning; } }

		internal float LooperRate
		{
			get { return _RepeatSecs; }
			private set
			{
				Log.Debug("Setting RepeatSecs to {0}", value);
				_RepeatSecs = value;
				if (LooperRunning) RestartLooper();
			}
		}

		internal float SetRepeatTimesPerSecond(Int32 dxdt) { return (float)(1 / (float)dxdt); }
		internal float SetRepeatTimesPerSecond(float dxdt) { return (float)(1 / dxdt); }
		internal float SetRepeatRate(float NewSeconds) { return NewSeconds; }
		internal float LooperInitialWait
		{
			get { return _RepeatInitialWait; }
			set { _RepeatInitialWait = value; }
		}

		protected void RestartLooper() { StopLooper(); StartLooper(); }

		protected bool StartLooper(Int32 TimesPerSec)
		{
			Log.Debug("Starting the repeating function");
			StopLooper(); 							// stop it if its running
			SetRepeatTimesPerSecond(TimesPerSec); 	// set the new value
			return StartLooper(); 					// start it and return the result
		}
		protected bool StartLooper()
		{
			try
			{
				Log.Debug("Invoking the repeating function");
				this.InvokeRepeating("LooperWrapper", _RepeatInitialWait, LooperRate);
				_RepeatRunning = true;
			}
			catch (Exception) { Log.Now("Unable to invoke the repeating function"); }

			return _RepeatRunning;
		}
		protected bool StopLooper()
		{
			try
			{
				Log.Debug("Cancelling the repeating function");
				this.CancelInvoke("LooperWrapper");
				_RepeatRunning = false;
			}
			catch (Exception) { Log.Now("Unable to cancel the repeating function"); }
			return _RepeatRunning;
		}

		protected virtual void Looper()
		{
			//Log.Debug("WorkerBase"); 
		}

		private void LooperWrapper()
		{
			DateTime Duration = DateTime.Now; 					// record the start date
			LooperUTLastStart = LooperUTStart;					// ... and last invocation
			LooperUTStart = Planetarium.GetUniversalTime();	// ... and this too
			LooperUTPeriod = LooperUTStart - LooperUTLastStart;// and diff!

			Looper();	// do the work (overload this)

			LooperDuration = (DateTime.Now - Duration);
		}

		//See this for info on order of execuction
		//  http://docs.unity3d.com/Documentation/Manual/ExecutionOrder.html
		protected virtual void Awake()
		{
			Log.Debug("MBE Awakened");

			if (!_seedGenerated)
			{
				UnityEngine.Random.InitState((int)(DateTime.Now - DateTime.Now.Date).TotalSeconds);

				_seedGenerated = true;
			}
		}		// 1.
		//internal virtual void OnEnable()		{ }								// 2.
		protected virtual void Start() { Log.Debug("MBE Started"); }		// 3.
		protected virtual void FixedUpdate() { }								// 4a. called (>1) per frame
		protected virtual void Update() { }								// 4b.   ""   (=1) per frame
		// 4c.        <coroutines run>
		protected virtual void LateUpdate() { }								// 4d.   ""   (=1) per frame
		//internal virtual void OnGUI()			{ }								// 5.    ""   (>1) per frame
		// 5a. (layout and repaint)
		// 5a. (layout and input) (1 per input)
		protected virtual void OnGUIEvery() { }
		protected virtual void OnGUI_FirstRun() { Log.Debug("Running OnGUI OnceOnly Code"); }
		//internal virtual void OnDisable()		{ }								// 6.
		protected virtual void OnDestroy() { Log.Debug("MBE Destroy-ing"); }	// 7.

		private void OnGUI()
		{
			if (!_OnGUI_FirstRun)
			{
				_OnGUI_FirstRun = true; 					// set the flag so this only runs once
				//if (!SCAN_SkinsLibrary._Initialized)
					//SCAN_SkinsLibrary.InitSkinList();		// set up the skins library
				OnGUI_FirstRun();							// then actually *do* the firstrun stuff
			}
			OnGUIEvery();
		}

	}
}
