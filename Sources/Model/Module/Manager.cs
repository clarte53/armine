using System;
using System.Collections.Generic;

namespace Armine.Model.Module
{
	/// <summary>
	/// Base class for classes managing a set of IModule.
	/// </summary>
	/// <typeparam name="T">The type of IModule to manage.</typeparam>
	public class Manager<T> : IDisposable where T : IModule
	{
		#region Members
		protected Dictionary<string, T> modules;
		protected Dictionary<string, T> extensionHandler;
		protected bool isDisposed;
		#endregion

		#region Constructors
		/// <summary>
		/// Constructor of manager base class.
		/// </summary>
		public Manager()
		{
			isDisposed = false;

			modules = new Dictionary<string, T>();
			extensionHandler = new Dictionary<string, T>();
		}
		#endregion

		#region IDisposable implementation
		private void Dispose(bool disposing)
		{
			lock(this)
			{
				if(!isDisposed)
				{
					if(disposing)
					{
						// TODO: delete managed state (managed objects).

						foreach(KeyValuePair<string, T> module in modules)
						{
							module.Value.Dispose();
						}

						modules.Clear();
						extensionHandler.Clear();
					}

					// TODO: free unmanaged resources (unmanaged objects) and replace finalizer below.
					// TODO: set fields of large size with null value.

					isDisposed = true;
				}
			}
		}

		// TODO: replace finalizer only if the above Dispose(bool disposing) function as code to free unmanaged resources.
		//~ModuleManager()
		//{
		//	Dispose(false);
		//}

		/// <summary>
		/// Dispose method to release allocated ressources.
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

		#region Getter / Setter
		/// <summary>
		/// Returns the list of file extensions supported by this instance, as a set of the supported extensions of the loaded modules.
		/// </summary>
		public string[] SupportedExtensions
		{
			get
			{
				string[] extensions = new string[extensionHandler.Count];

				uint index = 0;

				foreach(KeyValuePair<string, T> pair in extensionHandler)
				{
					extensions[index++] = pair.Key;
				}

				return extensions;
			}
		}
		#endregion

		#region Modules handling
		/// <summary>
		/// Add a new module.
		/// </summary>
		/// <param name="name">The name to reference the module with.</param>
		/// <param name="module">The module to add.</param>
		public void AddModule(string name, T module)
		{
			if(isDisposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			if(module != null && !string.IsNullOrEmpty(name))
			{
				modules.Add(name, module);

				string[] extensions = module.GetSupportedExtensions();

				if(extensions != null)
				{
					foreach(string ext in extensions)
					{
						extensionHandler[ext.ToLower()] = module;
					}
				}
			}
		}

		/// <summary>
		/// Get a registered module.
		/// </summary>
		/// <param name="name">The name the module is referenced with.</param>
		/// <returns>The given module, or null if the module does not exist.</returns>
		public T GetModule(string name)
		{
			if(isDisposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			T module;

			if(!modules.TryGetValue(name, out module))
			{
				module = default(T);
			}

			return module;
		}
		#endregion
	}
}