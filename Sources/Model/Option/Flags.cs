#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

namespace Armine.Model.Option
{
	internal class Flags
	{
		internal static int Toogle(int flags, int flag, bool state)
		{
			return (state ? (flags | flag) : (flags & ~flag));
		}

		internal static bool IsSet(int flags, int flag)
		{
			return System.Convert.ToBoolean(flags & flag);	
		}
	}
}

#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
