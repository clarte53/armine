using System.Collections.Generic;
using CLARTE.Serialization;

namespace Armine.Model.Type
{
	public partial class Metadata : IBinarySerializable
	{
		public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
		{
			return serializer.FromBytes(buffer, start, out data);
		}

		public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
		{
			return serializer.ToBytes(ref buffer, start, data);
		}
	}
}
