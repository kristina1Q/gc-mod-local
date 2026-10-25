namespace GCMod;

public static class Logger
{
	public static void Info(string msg)
	{
		Plugin.Log.LogInfo((object)msg);
	}

	public static void Warn(string msg)
	{
		Plugin.Log.LogWarning((object)msg);
	}

	public static void Error(string msg)
	{
		Plugin.Log.LogError((object)msg);
	}
}
