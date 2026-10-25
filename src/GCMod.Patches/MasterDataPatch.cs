using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Hook;
using DMM.OLG.Unity.Engine;
using Gc;
using Gc.Master;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Reflection;
using MonoMod.RuntimeDetour;

namespace GCMod.Patches;

[HarmonyPatch]
public static class MasterDataPatch
{
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void LoadCallbackFn(IntPtr self, IntPtr asset, IntPtr methodInfo);

	private static readonly LoadCallbackFn _loadHook = HookLoad;

	private static LoadCallbackFn _loadOrig;

	private static LoadCallbackFn _unhookedOrig;

	private static INativeDetour _loadDetour;

	private static Harmony _harmony;

	private static readonly object Lifecycle = new object();

	private static int _activeLoads;

	[ThreadStatic]
	private static IntPtr _pendingLoader;

	public static Func<MasterAttribute, string, string> JsonRewriter { get; set; }

	public static void Install()
	{
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected O, but got Unknown
		lock (Lifecycle)
		{
			if (_loadDetour != null)
			{
				return;
			}
			MethodInfo val = FindLoadCallback();
			if (val == (MethodInfo)null)
			{
				return;
			}
			IntPtr intPtr = Marshal.ReadIntPtr(IL2CPP.il2cpp_method_get_from_reflection(((Il2CppObjectBase)val).Pointer));
			if (intPtr == IntPtr.Zero)
			{
				throw new InvalidOperationException(((MemberInfo)val).Name + " has no native method pointer");
			}
			_unhookedOrig = Marshal.GetDelegateForFunctionPointer<LoadCallbackFn>(intPtr);
			INativeDetour val2 = INativeDetour.Create<LoadCallbackFn>(intPtr, _loadHook);
			Harmony val3 = null;
			try
			{
				_loadOrig = ((IDetour)val2).GenerateTrampoline<LoadCallbackFn>();
				val3 = Harmony.CreateAndPatchAll(typeof(MasterDataPatch), (string)null);
				((IDetour)val2).Apply();
				_loadDetour = val2;
				_harmony = val3;
			}
			catch
			{
				((IDisposable)val2).Dispose();
				if (val3 != null)
				{
					val3.UnpatchSelf();
				}
				throw;
			}
			Plugin.Log.LogInfo($"MasterDataPatch installed: {((MemberInfo)val).Name} @ 0x{intPtr.ToInt64():X}");
		}
	}

	public static bool TryUninstall()
	{
		lock (Lifecycle)
		{
			if (_activeLoads != 0)
			{
				return false;
			}
			if (_loadDetour == null)
			{
				return true;
			}
			((IDetour)_loadDetour).Undo();
			Harmony harmony = _harmony;
			if (harmony != null)
			{
				harmony.UnpatchSelf();
			}
			_loadOrig = _unhookedOrig;
			((IDisposable)_loadDetour).Dispose();
			_loadDetour = null;
			_harmony = null;
			_pendingLoader = IntPtr.Zero;
			return true;
		}
	}

	private static MethodInfo FindLoadCallback()
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		MethodInfo[] array = ((IEnumerable<MethodInfo>)Il2CppType.Of<MasterLoader<ItemMaster>>().GetMethods((BindingFlags)38)).Where(delegate(MethodInfo method)
		{
			if (((MethodBase)method).IsPrivate && !((MethodBase)method).IsGenericMethod && ((MemberInfo)method).Name.StartsWith("<Load>b__", StringComparison.Ordinal) && method.ReturnType.FullName == "System.Void")
			{
				Il2CppReferenceArray<ParameterInfo> parameters = ((MethodBase)method).GetParameters();
				if (parameters != null && ((Il2CppArrayBase<ParameterInfo>)(object)parameters).Length == 1)
				{
					return ((Il2CppArrayBase<ParameterInfo>)(object)parameters)[0].ParameterType.FullName == "System.Object";
				}
			}
			return false;
		}).ToArray();
		if (array.Length == 1)
		{
			return array[0];
		}
		Plugin.Log.LogError($"[MasterDataPatch] Expected one Load callback, found {array.Length}: {string.Join(", ", array.Select(m => ((MemberInfo)m).Name))}; patch disabled");
		return null;
	}

	private static void HookLoad(IntPtr self, IntPtr asset, IntPtr methodInfo)
	{
		LoadCallbackFn loadOrig;
		lock (Lifecycle)
		{
			_activeLoads++;
			loadOrig = _loadOrig;
		}
		IntPtr pendingLoader = _pendingLoader;
		_pendingLoader = self;
		try
		{
			loadOrig(self, asset, methodInfo);
		}
		finally
		{
			_pendingLoader = pendingLoader;
			lock (Lifecycle)
			{
				_activeLoads--;
			}
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(TextCompressUtil), "Decompress", new Type[] { typeof(string) })]
	private static void OnDecompressed(ref string __result)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		IntPtr pendingLoader = _pendingLoader;
		if (pendingLoader == IntPtr.Zero)
		{
			return;
		}
		_pendingLoader = IntPtr.Zero;
		try
		{
			IntPtr klass = IL2CPP.il2cpp_object_get_class(pendingLoader);
			IntPtr intPtr = ReadFieldPointer(pendingLoader, klass, "attribute");
			MasterAttribute arg = ((intPtr == IntPtr.Zero) ? ((MasterAttribute)null) : new MasterAttribute(intPtr));
			if (arg != null && !string.IsNullOrEmpty(arg.Object))
			{
				try
				{
					string dumpDir = @"c:\修复\raw_master_dump";
					string dumpFile = System.IO.Path.Combine(dumpDir, arg.Object + ".json");
					if (!System.IO.File.Exists(dumpFile))
					{
						System.IO.Directory.CreateDirectory(dumpDir);
						System.IO.File.WriteAllText(dumpFile, __result);
						Plugin.Log.LogInfo($"[MasterDumper] Dumped master table: {arg.Object}");
					}
				}
				catch {}
			}
			__result = JsonRewriter?.Invoke(arg, __result) ?? __result;
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError($"[MasterDataPatch] JSON rewrite failed; keeping original JSON: {ex}");
		}
	}

	private static IntPtr ReadFieldPointer(IntPtr instance, IntPtr klass, string fieldName)
	{
		IntPtr il2CppField = IL2CPP.GetIl2CppField(klass, fieldName);
		if (il2CppField == IntPtr.Zero)
		{
			throw new MissingFieldException("MasterLoader", fieldName);
		}
		int num = checked((int)IL2CPP.il2cpp_field_get_offset(il2CppField));
		return Marshal.ReadIntPtr(instance + num);
	}
}
