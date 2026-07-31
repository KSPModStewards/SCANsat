using System;
using Log = KSPCommunityLib.Logging.Log;


// TODO: remove this file and fixup references to call KSPBuildTools.Log directly
namespace SCANsat.SCAN_Platform.Logging
{
	public class ConsoleLogger
	{
		[System.Diagnostics.Conditional("DEBUG")]
		public static void Debug(string message, params object[] strParams)
		{
			Log.Debug(string.Format(message, strParams));
		}

		public static void Now(string message, params object[] strParams)
		{
			Log.Message(string.Format(message, strParams));
		}

		public static void Main()
		{
		}
	}
}