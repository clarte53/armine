using System;

namespace Armine.Model.Module
{
	#region Delegates
	/// <summary>
	/// Callback prototype to receive regular progress notifications of completion percentage of the current task.
	/// </summary>
	/// <param name='percentage'>Current percentage of task completion.</param>
	public delegate void ProgressCallback(float percentage);
	#endregion

	/// <summary>
	/// Buse class of all modules.
	/// </summary>
	public interface IModule : IDisposable
	{
		/// <summary>
		/// Get the list of file extensions supported by this module. The extensions should be lower case, without prefixes such as '.'. 
		/// </summary>
		/// <returns>The list of supported extensions.</returns>
		string[] GetSupportedExtensions();
	}
}
