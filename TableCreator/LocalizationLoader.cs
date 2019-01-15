using global::System;
using global::FlatBuffers;
using System.Collections.Generic;

namespace FlatBuffersData
{
struct LocalizationStructItem : IFlatbufferObject
{
	private Table __p;
	public ByteBuffer ByteBuffer { get { return __p.bb; } }
	public void __init(int _i, ByteBuffer _bb) { __p.bb_pos = _i; __p.bb = _bb; }
	public LocalizationStructItem __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

	public string Key { get { return DataItemBase.__GetString(__p, 4, 0); } }
	public string[] Values { get { return DataItemBase.__GetStringArgs(__p, 4, 1); } }
}

struct LocalizationStructList : IFlatbufferObject
{
	private Table __p;
	public ByteBuffer ByteBuffer { get { return __p.bb; } }
	public static LocalizationStructList GetRootAsLocalizationStructList(ByteBuffer _bb) { return GetRootAsLocalizationStructList(_bb, new LocalizationStructList()); }
	public static LocalizationStructList GetRootAsLocalizationStructList(ByteBuffer _bb, LocalizationStructList obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
	public void __init(int _i, ByteBuffer _bb) { __p.bb_pos = _i; __p.bb = _bb; }
	public LocalizationStructList __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }
	
	public LocalizationStructItem? List(int j) { int o = __p.__offset(4); return o != 0 ? (LocalizationStructItem?)(new LocalizationStructItem()).__assign(__p.__indirect(__p.__vector(o) + j * 4), __p.bb) : null; }
	public int ListLength { get { int o = __p.__offset(4); return o != 0 ? __p.__vector_len(o) : 0; } }
}
}

public static partial class LocalizationLoader
{
	static string[] _knownLanguages = null;
	public static string[] knownLanguages
	{
		get
		{
			if(_knownLanguages == null)
			{
				LoadDatas();
			}
			return _knownLanguages;
		}
	}

	static Dictionary<string, string[]> _dictionary = null;
	public static Dictionary<string, string[]> dictionary
	{
		get
		{
			if(_dictionary == null)
			{
				LoadDatas();
			}
			return _dictionary;
		}
	}

	public static void LoadDatas()
	{
		ByteBuffer data = DataItemBase.Load("localization");
		if (data == null)
		{
			return;
		}

		_knownLanguages = new string[0];
		_dictionary = new Dictionary<string, string[]>();
		FlatBuffersData.LocalizationStructList structList = FlatBuffersData.LocalizationStructList.GetRootAsLocalizationStructList(data);
		if(structList.ListLength <= 1)
		{
			return;
		}

		FlatBuffersData.LocalizationStructItem item = structList.List(0).Value;
		_knownLanguages = item.Values;

		for (int index = 1; index < structList.ListLength; index++)
		{
			item = structList.List(index).Value;
			string key = item.Key;
			string[] values = item.Values;
			_dictionary[key] = values;
		}
		data.Dispose();
	}
}