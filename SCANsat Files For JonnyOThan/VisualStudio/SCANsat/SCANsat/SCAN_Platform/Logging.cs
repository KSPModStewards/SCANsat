using System;
namespace SCANsat.SCAN_Platform.Logging
{
	public class ConsoleLogger
	{

		internal static string _AssemblyName { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }
		internal string _ClassName { get { return this.GetType().Name; } }

		[System.Diagnostics.Conditional("DEBUG")]
		public static void Debug(string message, params object[] strParams) { Now("DEBUG: " + message, strParams); }

		public static void Now(string message, params object[] strParams)
		{
			message = string.Format(message, strParams);															// This fills the params into the message
			string strMessageLine = string.Format("[{1}],{0}", message, _AssemblyName);	// This adds our standardised wrapper to each line
			UnityEngine.Debug.Log(strMessageLine);                        				// And this puts it in the log
		}

		public static void Main()
		{
		}
	}
}