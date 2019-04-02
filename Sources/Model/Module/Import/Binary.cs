using System.Collections;

namespace Armine.Model.Module.Import
{
	public class Binary : IImporter
	{
		#region Members
		protected static readonly string[] extensions = new string[] { Constants.binaryExtension.ToLower() };

		internal static CLARTE.Serialization.Binary serializer = new CLARTE.Serialization.Binary();
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

		#region IImporter implementation
		public string[] GetSupportedExtensions()
		{
			return extensions;
		}

		public IEnumerator ImportFromFile(string filename, ImporterReturnCallback return_callback, ProgressCallback progress_callback)
		{
			return serializer.Deserialize(filename, s => return_callback((Type.Scene) s), p => progress_callback(p));
		}

		public IEnumerator ImportFromBytes(string filename, byte[] data, ImporterReturnCallback return_callback, ProgressCallback progress_callback)
		{
			return serializer.Deserialize(data, s => return_callback((Type.Scene) s), p => progress_callback(p));
		}
		#endregion
	}
}
