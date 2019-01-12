using System;
using System.IO;
using FlatBuffers;
using System.Collections.Generic;

class FlatBuffersCreator
{
	ExcelParser _excel = null;
	public FlatBuffersCreator(ExcelParser parser)
	{
		_excel = parser;
	}

	public FlatBufferBuilder CreateFlatBufferBuilder()
	{
		if(_excel == null || _excel.RowCount <= 0)
		{
			return null;
		}

		FlatBufferBuilder builder = new FlatBufferBuilder(1);

		//提前收集所有字符串
		Dictionary<string, int> dict = new Dictionary<string, int>();
		for(int index = 0; index < _excel.FieldCount; index ++)
		{
			if(_excel.GetFieldType(index) == ExceFieldType.TEXT)
			{
				for(int i = 0; i < _excel.RowCount; i ++)
				{
					string key = _excel.GetString(i, index);
					if(dict.ContainsKey(key) == false)
					{
						dict[key] = builder.CreateString(key).Value;
					}
				}
			}
		}

		//压栈数据
		int[] items = new int[_excel.RowCount];
		for(int i = 0; i < _excel.RowCount; i ++)
		{
			builder.StartObject(_excel.FieldCount);
			for(int j = 0; j < _excel.FieldCount; j ++)
			{
				switch(_excel.GetFieldType(j))
				{
					case ExceFieldType.INTEGER:
						{
							builder.AddInt(j, _excel.GetInt(i, j), 0);
						}
						break;

					case ExceFieldType.REAL:
						{
							builder.AddFloat(j, _excel.GetSingle(i, j), 0.0f);
						}
						break;

					case ExceFieldType.TEXT:
						{
							string var = _excel.GetString(i, j);
							builder.AddOffset(j, dict[var], 0);
						}
						break;
				}
			}
			items[i] = builder.EndObject();
		}

		//打包数据
		builder.StartVector(4, items.Length, 4);
		for (int index = items.Length - 1; index >= 0; index--)
		{
			builder.AddOffset(items[index]);
		}
		int vector = builder.EndVector().Value;

		builder.StartObject(1);
		builder.AddOffset(0, vector, 0);
		builder.Finish(builder.EndObject());
		
		return builder;
	}

	public string SaveFlatBuffer(FlatBufferBuilder builder, string path)
	{
		if(builder == null)
		{
			return "";
		}

		if(string.IsNullOrEmpty(path) == false && Directory.Exists(path) == false)
		{
			Directory.CreateDirectory(path);
		}
		path = Path.Combine(path, _excel.FileName + ".bytes");
		MemoryStream ms = builder.DataBuffer.ToMemoryStream(builder.DataBuffer.Position, builder.Offset);
		File.WriteAllBytes(path, ms.ToArray());
		return path;
	}
}
