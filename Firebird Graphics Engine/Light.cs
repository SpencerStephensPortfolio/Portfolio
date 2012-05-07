using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace Firebird
{
	/// <summary>
	/// Determines the class of light: point or directional.
	/// </summary>
	public enum LightType
	{
		Point = 0,
		Directional = 1
	}

	/// <summary>
	/// Graphics Light data structure
	/// </summary>
	public class Light
	{
		/// <summary>
		/// Light type.
		/// </summary>
		public LightType Type;

		/// <summary>
		/// Light color.
		/// </summary>
		public Color Color;

		/// <summary>
		/// Light intensity. Determines range and power.
		/// </summary>
		public float Intensity;

		/// <summary>
		/// Location information. Position for point lights, normal for
		/// directional lights.
		/// </summary>
		public Vector3 Location;

		/// <summary>
		/// Creates a default light.
		/// </summary>
		public Light()
		{
			Type = LightType.Directional;
			Color = Color.White;
			Intensity = 3.0f;
		}

		/// <summary>
		/// Saves to a binary stream. Currently version 0.
		/// </summary>
		/// <param name="writer">Binary stream</param>
		public void Save(BinaryWriter writer)
		{
			const short version = 0;

			writer.Write(version);
			writer.Write((uint)Type);
			writer.Write(Color.R);
			writer.Write(Color.G);
			writer.Write(Color.B);
			writer.Write(Intensity);
		}

		/// <summary>
		/// Initializes a light from a binary stream.
		/// </summary>
		/// <param name="light">Light being initialized</param>
		/// <param name="reader">Binary stream</param>
		public static Light Read(BinaryReader reader)
		{
			short version = reader.ReadInt16();
			switch (version)
			{
				case 0:
					return ReadVer0(reader);
				default:
					throw new ArgumentException("Light version " + version + " not supported.");
			}
		}

		/// <summary>
		/// Initializes a light from a binary stream, version 0.
		/// </summary>
		/// <param name="light">Light being initialized</param>
		/// <param name="reader">Binary stream</param>
		private static Light ReadVer0(BinaryReader reader)
		{
			Light light = new Light();

			light.Type = (LightType)reader.ReadUInt32();
			light.Color.R = reader.ReadByte();
			light.Color.G = reader.ReadByte();
			light.Color.B = reader.ReadByte();
			light.Intensity = reader.ReadSingle();

			return light;
		}
	}
}
