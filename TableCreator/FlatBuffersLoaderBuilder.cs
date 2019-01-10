﻿using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class FlatBuffersLoaderBuilder
{
	ExcelParser _excel = null;
	public FlatBuffersLoaderBuilder(ExcelParser parser)
	{
		_excel = parser;
	}

	string buildName(string input, string addtion = null)
	{
		StringBuilder sb = new StringBuilder();
		string[] parts = input.Split(new char[] { '_', '-', ' '});
		for(int index = 0; index < parts.Length; index ++)
		{
			string part = parts[index];
			sb.Append(part[0].ToString().ToUpper());
			if(part.Length > 0)
			{
				sb.Append(part, 1, part.Length - 1);
			}
		}

		if(string.IsNullOrEmpty(addtion) == false)
		{
			sb.Append(addtion);
		}

		return sb.ToString();
	}

	void BuildStructItem(StringBuilder file, string itemName, string listName)
	{
		file.AppendFormat("struct {0} : IFlatbufferObject\n", itemName);
		file.AppendLine("{");

		//变量和函数
		//0:itemName
		//1:listName
		//2:{
		//3:}
		string fun = @"	private Table __p;
	public ByteBuffer ByteBuffer {1} get {1} return __p.bb; {2} {2}
	public void __init(int _i, ByteBuffer _bb) {1} __p.bb_pos = _i; __p.bb = _bb; {2}
	public {0} __assign(int _i, ByteBuffer _bb) {1} __init(_i, _bb); return this; {2}

";
		file.AppendFormat(fun, itemName, "{", "}");

		//Item属性
		for (int index = 0; index < _excel.FieldCount; index++)
		{
			string typeName = "int";
			string funName = "bb.GetInt";
			string defValue = "0";
			string fieldName = buildName(_excel.GetFieldName(index));
			switch (_excel.GetFieldType(index))
			{
				case ExceFieldType.REAL:
					{
						typeName = "float";
						funName = "bb.GetFloat";
						defValue = "0.0f";
					}
					break;

				case ExceFieldType.TEXT:
					{
						typeName = "string";
						funName = "__string";
						defValue = "null";
					}
					break;
			}

			//0:typeName
			//1:funName
			//2:defValue;
			//3:{
			//4:}
			//5:id
			//6:fieldName
			file.AppendFormat("\tpublic {0} {6} {3} get {3} int o = __p.__offset({5}); return o != 0 ? __p.{1}(o + __p.bb_pos) : {2}; {4} {4}\n", typeName, funName, defValue, "{", "}", 4 + index * 2, fieldName);
		}

		file.AppendLine("}\n");
	}

	void BuildStructList(StringBuilder file, string itemName, string listName)
	{
		//List类类名
		file.AppendFormat("struct {0} : IFlatbufferObject\n", listName);
		file.AppendLine("{");

		//变量和函数
		//0:itemName
		//1:listName
		//2:{
		//3:}
		string fun = @"	private Table __p;
	public ByteBuffer ByteBuffer {1} get {1} return __p.bb; {2} {2}
	public static {0} GetRootAs{0}(ByteBuffer _bb) {1} return GetRootAs{0}(_bb, new {0}()); {2}
	public static {0} GetRootAs{0}(ByteBuffer _bb, {0} obj) {1} return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); {2}
	public void __init(int _i, ByteBuffer _bb) {1} __p.bb_pos = _i; __p.bb = _bb; {2}
	public {0} __assign(int _i, ByteBuffer _bb) {1} __init(_i, _bb); return this; {2}
	
	public {3}? List(int j) {1} int o = __p.__offset(4); return o != 0 ? ({3}?)(new {3}()).__assign(__p.__indirect(__p.__vector(o) + j * 4), __p.bb) : null; {2}
	public int ListLength {1} get {1} int o = __p.__offset(4); return o != 0 ? __p.__vector_len(o) : 0; {2} {2}
";
		file.AppendFormat(fun, listName, "{", "}", itemName);
		file.AppendLine("}");
	}

	void BuildItemForParser(StringBuilder file, string structItemName, string structListName, string parserItemName, string paserName)
	{
		string fun = @"public partial class {0} : DataItemReadOnly
{1}
	partial void OnPostParse();
	public void Dispose()
	{1}
";
		file.AppendFormat(fun, parserItemName, "{");

		//实现Dispose函数
		for(int i = 0; i < _excel.FieldCount; i ++)
		{
			if(_excel.GetFieldType(i) == ExceFieldType.TEXT)
			{
				file.AppendFormat("\t\t_{0} = null;\n", buildName(_excel.GetFieldName(i)));
			}
		}
		file.AppendLine("\t}\n");

		//成员变量和属性,以及Get回调方法
		fun = @"	{0} _{1} = {3};
	public {0} {1} {4} get {4} return _{1}; {5} {5}
	internal static System.Func<{2}, {0}> _Get{1} = delegate ({2} item) {4} return item._{1}; {5};

";
		for (int index = 0; index < _excel.FieldCount; index++)
		{
			string typeName = "int";
			string defValue = "0";
			switch (_excel.GetFieldType(index))
			{
				case ExceFieldType.REAL:
					{
						typeName = "float";
						defValue = "0.0f";
					}
					break;

				case ExceFieldType.TEXT:
					{
						typeName = "string";
						defValue = "null";
					}
					break;
			}

			//0:typeName
			//1:funName
			//2:classname
			//3:defValue;
			//4:{
			//5:}
			file.AppendFormat(fun, typeName, buildName(_excel.GetFieldName(index)), parserItemName, defValue, "{", "}");
		}

		//Comparison数组，排序时用到
		file.AppendFormat("\tinternal static Comparison<{0}>[] _Comparison = \n", parserItemName);
		file.AppendLine("\t{");
		fun = @"		delegate({0} a, {0} b) {3} return {1}(a._{2}, b._{2}); {4},
";
		for (int index = 0; index < _excel.FieldCount; index++)
		{
			string typeName = "DataItemReadOnly.CompareInt";
			switch (_excel.GetFieldType(index))
			{
				case ExceFieldType.REAL:
					{
						typeName = "DataItemReadOnly.CompareSingle";
					}
					break;

				case ExceFieldType.TEXT:
					{
						typeName = "DataItemReadOnly.CompareString";
					}
					break;
			}

			//0:parserItemName
			//1:funName
			//2:fieldname
			//3:{
			//4:}
			file.AppendFormat(fun, parserItemName, typeName, buildName(_excel.GetFieldName(index)), "{", "}");
		}
		file.AppendLine("\t};\n");

		//Parse函数
		file.AppendFormat("\tinternal void Parse(FlatBuffersData.{0} item)\n", structItemName);
		file.AppendLine("\t{");
		for (int index = 0; index < _excel.FieldCount; index++)
		{
			fun = buildName(_excel.GetFieldName(index));
			file.AppendFormat("\t\t_{0} = item.{0};\n", fun);
		}
		file.AppendLine("\t\tParseDone();\n\t}");

		file.AppendLine("}\n");
	}

	void BuildParser(StringBuilder file, string structItemName, string structListName, string parserItemName, string paserName)
	{
		string fun = @"public static partial class {0}
{5}
	static List<{1}>[] _listArray = null;

	public static void LoadDatas()
	{5}
		if (_listArray != null)
		{5}
			return;
		{6}

		ByteBuffer data = DataItemReadOnly.Load(""{2}"");
		if (data == null)
		{5}
			return;
		{6}

		_listArray = new List<{1}>[{4}];
		FlatBuffersData.{3} structList = FlatBuffersData.{3}.GetRootAs{3}(data);
		List<{1}> list = new List<{1}>(structList.ListLength);
		for (int index = 0; index < structList.ListLength; index++)
		{5}
			{1} item = new {1}();
			item.Parse(structList.List(index).Value);
			list.Add(item);
		{6}
		list.Sort({1}._Comparison[0]);
		_listArray[0] = list;
	{6}

	public static void Dispose()
	{5}
		if(_listArray != null)
		{5}
			List<{1}> list = _listArray[0];
			for(int index = 0; index < list.Count; index ++)
			{5}
				{1} item = list[index];
				item.Dispose();
			{6}

			for (int index = 1; index < _listArray.Length; index ++)
			{5}
				list = _listArray[index];
				if(list != null)
				{5}
					list.Clear();
				{6}
				_listArray[index] = null;
			{6}
		{6}
	{6}

	static void BuildKeyByIndex(int index)
	{5}
		if (_listArray[index] == null)
		{5}
			List<{1}> list = new List<{1}>(_listArray[0].Count);
			list.AddRange(_listArray[0]);
			_listArray[index] = list;

			list.Sort({1}._Comparison[index]);
		{6}
	{6}

	static void FindItemByField<T, TV>(List<T>[] listArray, int field, TV value, System.Func<TV, TV, int> comparison, System.Func<T, TV> get_value, System.Func<T, bool> receiver) where T : {1}
	{5}
		if(receiver == null)
		{5}
			return;
		{6}

		BuildKeyByIndex(field);
		List<T> list = listArray[field];
		int id = DataItemReadOnly.BinarySearch<T, TV>(list, comparison, get_value, value);
		if(id >= 0)
		{5}
			for(int index = id; index < list.Count; index ++)
			{5}
				T item = list[index];
				if(comparison(get_value(item), value) != 0 || receiver(item) == false)
				{5}
					return;
				{6}
			{6}
		{6}
	{6}
";
		//0:paserName
		//1:parserItemName
		//2:excelname;
		//3:structListName
		//4:fieldCount
		//5:{
		//6:}
		file.AppendFormat(fun, paserName, parserItemName, _excel.FileName, structListName, _excel.FieldCount, "{", "}");

		//添加BuildMainKeyXX函数
		//0:fieldname;
		//1:index
		//2:{
		//3:}
		//4:parserItemName
		//5:fieldtype
		//6:compare fun
		fun = @"	public static void KeyFor{0}() {2} BuildKeyByIndex({1}); {3}
	public static void Find{0}({5} value, System.Func<{4}, bool> receiver)
	{2}
		FindItemByField<{4}, {5}>(_listArray, {1}, value, {6}, {4}._Get{0}, receiver);
	{3}

";
		for (int index = 0; index < _excel.FieldCount; index++)
		{
			string fieldType = "int";
			string compareFun = "DataItemReadOnly.CompareInt";
			switch (_excel.GetFieldType(index))
			{
				case ExceFieldType.REAL:
					{
						fieldType = "float";
						compareFun = "DataItemReadOnly.CompareSingle";
					}
					break;

				case ExceFieldType.TEXT:
					{
						fieldType = "string";
						compareFun = "DataItemReadOnly.CompareString";
					}
					break;
			}
			file.AppendFormat(fun, buildName(_excel.GetFieldName(index)), index, "{", "}", parserItemName, fieldType, compareFun);
		}

		file.AppendLine("}");
	}

	public string Build(string output)
	{
		if(_excel == null || _excel.RowCount <= 0)
		{
			return null;
		}

		StringBuilder file = new StringBuilder();
		string structItemName = buildName(_excel.FileName, "StructItem");
		string structListName = buildName(_excel.FileName, "StructList");
		string parserItemName = buildName(_excel.FileName, "Item");
		string paserName = buildName(_excel.FileName);

		//CS文件标题
		file.Append(@"// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using global::System;
using global::FlatBuffers;
using System.Collections.Generic;

namespace FlatBuffersData
{
");
		BuildStructItem(file, structItemName, structListName);
		BuildStructList(file, structItemName, structListName);
		file.AppendLine("}\n");
		BuildItemForParser(file, structItemName, structListName, parserItemName, paserName);
		BuildParser(file, structItemName, structListName, parserItemName, paserName);

		if(string.IsNullOrEmpty(output) == false && Directory.Exists(output) == false)
		{
			Directory.CreateDirectory(output);
		}
		output = Path.Combine(output, buildName(_excel.FileName) + ".cs");
		File.WriteAllText(output, file.ToString());
		return output;
	}
}