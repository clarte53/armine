#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

using System;
using System.Reflection;
using UnityEngine;

namespace Armine.Utils
{
	public sealed class License
	{
		internal enum Feature
		{
			ARMINE_IMPORT = 0,
			ARMINE_EXPORT,
			ARMINE_TOOLS
		}

		#region Check license
		public static bool IsLicensed()
		{
			return InvokeMethod<bool>("Exist", true, null);
		}

		public static string DeviceID()
		{
			return InvokeMethod<string>("ID", "", null);
		}

		public static string Date()
		{
			return InvokeMethod<string>("Max", "", null);
		}

		public static string GetLicense()
		{
			return InvokeMethod<string>("Get", "", null);
		}

		public static bool SetLicense(string license)
		{
			return InvokeMethod<bool>("Set", false, new object[] {license});
		}

		public static bool ImportIsPermitted()
		{
			return IsPermitted(Feature.ARMINE_IMPORT);
		}
		
		public static bool ExportIsPermitted()
		{
			return IsPermitted(Feature.ARMINE_EXPORT);
		}
		
		public static bool ToolScriptsArePermited()
		{
			return IsPermitted(Feature.ARMINE_TOOLS);
		}
		#endregion
		
		#region decrement count
		public static void DecrementImportCount()
		{
			DecrementCount(Feature.ARMINE_IMPORT);
		}
		
		public static void DecrementExportCount()
		{
			DecrementCount(Feature.ARMINE_EXPORT);
		}
		
		public static void DecrementToolCount()
		{
			DecrementCount(Feature.ARMINE_TOOLS);
		}
		#endregion

		#region Internal methods
		private static bool IsEditor()
		{
			return Application.isEditor && ! Application.isPlaying;
		}

		private static bool IsPermitted(Feature feature)
		{
			return InvokeMethod<bool>("IsOK", true, new object[] {(int) feature, IsEditor()});
		}
		
		private static void DecrementCount(Feature feature)
		{
			InvokeMethod<int>("Less", 0, new object[] {(int) feature});
		}

		private static T InvokeMethod<T>(string method, T default_result, object[] parameters)
		{
			T result = default_result;
			
			Type type = Type.GetType("_");
			
			if(type != null)
			{
				MethodInfo call = type.GetMethod(method, BindingFlags.InvokeMethod |  BindingFlags.NonPublic | BindingFlags.Static);

				if(call != null)
				{
					try
					{
						result = (T) call.Invoke(null, parameters);
					}
					catch(Exception e)
					{
						Debug.LogError(e.ToString());

						result = default_result;
					}
				}
			}
			
			return result;
		}
		#endregion
	}
}

#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
