using System.Collections.Generic;

namespace Armine.Model.Type
{
	public sealed partial class Metadata
	{
		#region Members
		private Dictionary<string, object> data = null;
		#endregion

		#region Constructors
		public void Initialize()
		{
			data = new Dictionary<string, object>();
		}
		#endregion
	}
}
