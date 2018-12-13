using System.Collections;

namespace Armine.Model.Module
{
	#region Delegates
	/// <summary>
	/// Callback prototype to receive success status when export is done.
	/// </summary>
	/// <param name='success'>True if the data was exported without errors, false otherwise.</param>
	public delegate void ExporterSuccessCallback(bool success);

	/// <summary>
	/// Callback prototype to receive byte array when export is done.
	/// </summary>
	/// <param name='data'>The result byte array after export.</param>
	public delegate void ExporterReturnCallback(byte[] data);
	#endregion

	public interface IExporter : IModule
	{
		/// <summary>
		/// Method to export to a file.
		/// </summary>
		/// <param name='scene'>The scene to export.</param>
		/// <param name='filename'>The name of the file to export to, with path.</param>
		/// <param name='return_callback'>The callback to be notified of the export success or failure.</param>
		/// <param name='progress_callback'>The callback to receive progress notifications during export.</param>
		/// <returns>An enumerator that can be used to execute this method in a coroutine.</returns>
		IEnumerator ExportToFile(Type.Scene scene, string filename, ExporterSuccessCallback return_callback, ProgressCallback progress_callback);

		/// <summary>
		/// Method to export to a byte array.
		/// </summary>
		/// <param name='scene'>The scene to export.</param>
		/// <param name='filename'>The name of the file, for error messages and getting the extension.</param>
		/// <param name='return_callback'>The callback to receive the exported scene data when the export is finished.</param>
		/// <param name='progress_callback'>The callback to receive progress notifications during loading.</param>
		/// <returns>An enumerator that can be used to execute this method in a coroutine.</returns>
		IEnumerator ExportToBytes(Type.Scene scene, string filename, ExporterReturnCallback return_callback, ProgressCallback progress_callback);
	}
}
