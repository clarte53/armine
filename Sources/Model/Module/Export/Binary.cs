using System;
using System.Collections;
using Armine.Model.Type;

namespace Armine.Model.Module.Export
{
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
		public string[] GetSupportedExtensions()
		{
			return extensions;
		}

		public IEnumerator ExportToFile(Scene scene, string filename, ExporterSuccessCallback return_callback, ProgressCallback progress_callback)
		{
			return Import.Binary.serializer.Serialize(scene, filename, s => return_callback(s), p => progress_callback(p));
		}

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
