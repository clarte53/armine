using System.Collections;

namespace Armine.Model.Module
{
	#region Delegates
	/// <summary>
	/// Callback prototype to receive scene when import is done.
	/// </summary>
	/// <param name='scene'>The result scene after import.</param>
	public delegate void ImporterReturnCallback(Type.Scene scene);
	#endregion

	/// <summary>
	/// Base class of all import modules.
	/// </summary>
	public interface IImporter : IModule
	{
		/// <summary>
		/// Method to import from a file.
		/// </summary>
		/// <param name='filename'>The name of the file to load, with path.</param>
		/// <param name='return_callback'>The callback to receive the loaded scene when the import is finished.</param>
		/// <param name='progress_callback'>The callback to receive progress notifications during loading.</param>
		/// <returns>An enumerator that can be used to execute this method in a coroutine.</returns>
		IEnumerator ImportFromFile(string filename, ImporterReturnCallback return_callback, ProgressCallback progress_callback);

		/// <summary>
		/// Method to import from a byte array.
		/// </summary>
		/// <param name='filename'>The name of the file, for error messages and getting the extension.</param>
		/// <param name='data'>The byte array containing the data to load.</param>
		/// <param name='return_callback'>The callback to receive the loaded scene when the import is finished.</param>
		/// <param name='progress_callback'>The callback to receive progress notifications during loading.</param>
		/// <returns>An enumerator that can be used to execute this method in a coroutine.</returns>
		IEnumerator ImportFromBytes(string filename, byte[] data, ImporterReturnCallback return_callback, ProgressCallback progress_callback);
	}
}
