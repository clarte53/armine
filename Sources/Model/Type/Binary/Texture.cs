using CLARTE.Serialization;

namespace Armine.Model.Type
{
	public partial class Texture : IBinarySerializable
	{
		public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
		{
			int format_data;

			uint read = serializer.FromBytes(buffer, start, out filename);
			read += serializer.FromBytes(buffer, start + read, out width);
			read += serializer.FromBytes(buffer, start + read, out height);
			read += serializer.FromBytes(buffer, start + read, out format_data);
			read += serializer.FromBytes(buffer, start + read, out data);

			format = (UnityEngine.TextureFormat) format_data;

			return read;
		}

		public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
		{
			uint written = serializer.ToBytes(ref buffer, start, filename);
			written += serializer.ToBytes(ref buffer, start + written, width);
			written += serializer.ToBytes(ref buffer, start + written, height);
			written += serializer.ToBytes(ref buffer, start + written, (int) format);
			written += serializer.ToBytes(ref buffer, start + written, data);

			return written;
		}
	}
}
