using CLARTE.Serialization;

namespace Armine.Model.Type
{
	public partial class Material : IBinarySerializable
	{
		public partial class TextureParams : IBinarySerializable
		{
			public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
			{
				uint read = serializer.FromBytes(buffer, start, out index);
				read += serializer.FromBytes(buffer, start + read, out offset);
				read += serializer.FromBytes(buffer, start + read, out scale);

				return read;
			}

			public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
			{
				uint written = serializer.ToBytes(ref buffer, start, index);
				written += serializer.ToBytes(ref buffer, start + written, offset);
				written += serializer.ToBytes(ref buffer, start + written, scale);

				return written;
			}
		}

		public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
		{
			int hide_flags;
			int gi_flags;

			uint read = serializer.FromBytes(buffer, start, out name);
			read += serializer.FromBytes(buffer, start + read, out shader);
			read += serializer.FromBytes(buffer, start + read, out renderQueue);
			read += serializer.FromBytes(buffer, start + read, out hide_flags);
			read += serializer.FromBytes(buffer, start + read, out gi_flags);
			read += serializer.FromBytes(buffer, start + read, out keywords);
			read += serializer.FromBytes(buffer, start + read, out passes);
			read += serializer.FromBytes(buffer, start + read, out ints);
			read += serializer.FromBytes(buffer, start + read, out floats);
			read += serializer.FromBytes(buffer, start + read, out vectors);
			read += serializer.FromBytes(buffer, start + read, out colors);
			read += serializer.FromBytes(buffer, start + read, out textures);

			hideFlags = (UnityEngine.HideFlags) hide_flags;
			globalIlluminationFlags = (UnityEngine.MaterialGlobalIlluminationFlags) gi_flags;

			return read;
		}

		public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
		{
			uint written = serializer.ToBytes(ref buffer, start, name);
			written += serializer.ToBytes(ref buffer, start + written, shader);
			written += serializer.ToBytes(ref buffer, start + written, renderQueue);
			written += serializer.ToBytes(ref buffer, start + written, (int) hideFlags);
			written += serializer.ToBytes(ref buffer, start + written, (int) globalIlluminationFlags);
			written += serializer.ToBytes(ref buffer, start + written, keywords);
			written += serializer.ToBytes(ref buffer, start + written, passes);
			written += serializer.ToBytes(ref buffer, start + written, ints);
			written += serializer.ToBytes(ref buffer, start + written, floats);
			written += serializer.ToBytes(ref buffer, start + written, vectors);
			written += serializer.ToBytes(ref buffer, start + written, colors);
			written += serializer.ToBytes(ref buffer, start + written, textures);

			return written;
		}
	}
}
