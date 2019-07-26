using System;
using System.Collections;
using Armine.Model.Type;

namespace Armine.Model.Module.Export
{
	/// <summary>
	/// Binary exporter module.
	/// </summary>
	public class Binary : IExporter
	{
		#region Members
		protected static readonly string[] extensions = new string[] { Constants.binaryExtension.ToLower() };
		#endregion

		#region IDisposable Support
		private bool isDisposed = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if(!isDisposed)
			{
				if(disposing)
				{
					// TODO: delete managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and replace finalizer below.
				// TODO: set fields of large size with null value.

				isDisposed = true;
			}
		}

		// TODO: replace finalizer only if the above Dispose(bool disposing) function as code to free unmanaged resources.
		//~Binary()
		//{
		//	Dispose(false);
		//}

		/// <summary>
		/// Release the ressources used by the exporter.
		/// </summary>
		public void Dispose()
		{
			// Pass true in dispose method to clean managed resources too and say GC to skip finalize in next line.
			Dispose(true);

			// If dispose is called already then say GC to skip finalize on this instance.
			// TODO: uncomment next line if finalizer is replaced above.
			// GC.SuppressFinalize(this);
		}
		#endregion

		#region IExporter implementation
		/// <summary>
		/// Provide the list of file extensions supported by this exporter module.
		/// </summary>
		/// <returns>The list of supported extensions.</returns>
		public string[] GetSupportedExtensions()
		{
			return extensions;
		}

		/// <summary>
		/// Export data asynchronously to a destination file.
		/// </summary>
		/// <param name="scene">The scene representation to export.</param>
		/// <param name="filename">The file to export to.</param>
		/// <param name="return_callback">The calback used to notify the caller when the export is completed.</param>
		/// <param name="progress_callback">The callback to regularly notify the caller of the export progress.</param>
		/// <returns>An iterator to use inside a coroutine.</returns>
		public IEnumerator ExportToFile(Scene scene, string filename, ExporterSuccessCallback return_callback, ProgressCallback progress_callback)
		{
			return Import.Binary.serializer.Serialize(scene, filename, s => return_callback(s), p => progress_callback(p));
		}

		/// <summary>
		/// Export data asynchronously to a byte array.
		/// </summary>
		/// <param name="scene">The scene representation to export.</param>
		/// <param name="filename">The name of the file corresponding to the exported data. The extension is used to determine which codec use.</param>
		/// <param name="return_callback">The calback used to notify the caller when the export is completed.</param>
		/// <param name="progress_callback">The callback to regularly notify the caller of the export progress.</param>
		/// <returns>An iterator to use inside a coroutine.</returns>
		public IEnumerator ExportToBytes(Scene scene, string filename, ExporterReturnCallback return_callback, ProgressCallback progress_callback)
		{
			return Import.Binary.serializer.Serialize(scene, (data, written) => {
				byte[] result = new byte[written];

				Array.Copy(data, result, (int) written);

				return_callback(result);
			}, p => progress_callback(p));
		}
		#endregion
	}
}
