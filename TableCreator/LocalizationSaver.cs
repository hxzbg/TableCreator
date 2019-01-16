using System.IO;
using global::System;
using global::FlatBuffers;
using System.Collections.Generic;

public static partial class LocalizationSaver
{
	static int CreateString(string str, Dictionary<string, int> dict, FlatBufferBuilder builder)
	{
		int offset = 0;
		str = string.IsNullOrEmpty(str) ? "" : str;
		if (dict.TryGetValue(str, out offset) == false)
		{
			offset = builder.CreateString(str.Replace("\\n", "\n")).Value;
			dict[str] = offset;
		}
		return offset;
	}

	public static void Save(string path, string[] knownLanguages, Dictionary<string, string[]> dictionary)
	{
		path = path != null ? path : "";
		if (string.IsNullOrEmpty(path) == false && Directory.Exists(path) == false)
		{
			Directory.CreateDirectory(path);
		}

		int knownLanguagesSize = knownLanguages.Length;
		FlatBufferBuilder builder = new FlatBufferBuilder(1);
		//提前收集所有字符串
		Dictionary<string, int> dict = new Dictionary<string, int>();
		Dictionary<string, string[]>.Enumerator em = dictionary.GetEnumerator();
		while(em.MoveNext())
		{
			KeyValuePair<string, string[]> pair = em.Current;
			CreateString(pair.Key, dict, builder);

			string[] vars = pair.Value;
			for (int i = 0; i < vars.Length; i ++)
			{
				CreateString(vars[i], dict, builder);
			}
		}
		CreateString("", dict, builder);

		int index = 0;
		int[] items = new int[dictionary.Count];
		em = dictionary.GetEnumerator();
		while (em.MoveNext())
		{
			KeyValuePair<string, string[]> pair = em.Current;
			builder.StartVector(4, knownLanguagesSize + 1, 4);
			string[] vars = pair.Value;
			for (int i = knownLanguagesSize - 1; i >= 0; i--)
			{
				string v = vars.Length > i ? vars[i] : "";
				builder.AddOffset(CreateString(v, dict, builder));
			}
			builder.AddOffset(CreateString(pair.Key, dict, builder));
			items[index] = builder.EndVector().Value;
			builder.StartObject(1);
			builder.AddOffset(0, items[index], 0);
			items[index] = builder.EndObject();
			index++;
		}

		int[] knownLanguagesIds = new int[knownLanguages.Length + 1];
		knownLanguagesIds[0] = builder.CreateString("").Value;
		for (index = 0; index < knownLanguages.Length; index ++)
		{
			knownLanguagesIds[index + 1] = builder.CreateString(knownLanguages[index]).Value;
		}
		builder.StartVector(4, knownLanguages.Length + 1, 4);
		for(int i = knownLanguagesIds.Length - 1; i >= 0; i --)
		{
			builder.AddOffset(knownLanguagesIds[i]);
		}
		index = dictionary.Count;
		int vector = builder.EndVector().Value;
		builder.StartObject(1);
		builder.AddOffset(0, vector, 0);
		vector = builder.EndObject();

		builder.StartVector(4, items.Length + 1, 4);
		for (int i = items.Length - 1; i >= 0; i --)
		{
			builder.AddOffset(items[i]);
		}
		builder.AddOffset(vector);
		vector = builder.EndVector().Value;

		builder.StartObject(1);
		builder.AddOffset(0, vector, 0);
		builder.Finish(builder.EndObject());

		MemoryStream ms = builder.DataBuffer.ToMemoryStream(builder.DataBuffer.Position, builder.Offset);
		File.WriteAllBytes(Path.Combine(path, "localization.bytes"), ms.ToArray());
	}
}