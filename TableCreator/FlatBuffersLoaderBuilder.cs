using System;
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

	static string[] _typeName = { "int", "long", "float" };
	static string[] _funName = { "bb.GetInt", "bb.GetLong", "bb.GetFloat" };
	static string[] _defValue = { "0", "0", "0.0f" };
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
	public Table table {1} get {1} return __p; {2} {2}
	public ByteBuffer ByteBuffer {1} get {1} return __p.bb; {2} {2}
	public void __init(int _i, ByteBuffer _bb) {1} __p.bb_pos = _i; __p.bb = _bb; {2}
	public {0} __assign(int _i, ByteBuffer _bb) {1} __init(_i, _bb); return this; {2}

";
		file.AppendFormat(fun, itemName, "{", "}");

		//Item属性
		for (int index = 0; index < _excel.FieldCount; index++)
		{
			string fieldName = buildName(_excel.GetFieldName(index));
			ExceFieldType excelType = _excel.GetFieldType(index);
			switch (excelType)
			{
				case ExceFieldType.INTEGER:
				case ExceFieldType.LONG:
				case ExceFieldType.REAL:
					{
						string typeName = _typeName[(int)excelType - 1];
						string funName = _funName[(int)excelType - 1];
						string defValue = _defValue[(int)excelType - 1];

						//0:typeName
						//1:funName
						//2:defValue;
						//3:{
						//4:}
						//5:id
						//6:fieldName
						file.AppendFormat("\tpublic {0} {6} {3} get {3} int o = __p.__offset({5}); return o != 0 ? __p.{1}(o + __p.bb_pos) : {2}; {4} {4}\n", typeName, funName, defValue, "{", "}", 4 + index * 2, fieldName);
					}
					break;
			}
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
		string fun = @"public partial class {0} : DataItemBase
{1}
	partial void OnPostParse();
	public void Dispose()
	{1}
";
		file.AppendFormat(fun, parserItemName, "{", "}");

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

		file.AppendLine("\tTable __p;\n");

		fun = @"	{0} _{1} = {3};
	public {0} {1} {4} get {4} return _{1}; {5} {5}
	internal static System.Func<{2}, {0}> _Get{1} = delegate ({2} item) {4} return item.{1}; {5};

";
		string fun_string = @"	{0} _{1} = null;
	public {0} {1} {4} get {4} if(_{1} == null) DataItemBase.__BuildString(ref _{1}, __p, {6}); return _{1}; {5} {5}

";
		for (int index = 0; index < _excel.FieldCount; index++)
		{
			string typeName = "int";
			string defValue = "0";
			string format = fun;
			switch (_excel.GetFieldType(index))
			{
				case ExceFieldType.LONG:
					{
						typeName = "long";
						defValue = "0";
					}
					break;

				case ExceFieldType.REAL:
					{
						typeName = "float";
						defValue = "0.0f";
					}
					break;

				case ExceFieldType.TEXT:
					{
						format = fun_string;
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
			file.AppendFormat(format, typeName, buildName(_excel.GetFieldName(index)), parserItemName, defValue, "{", "}", 4 + index * 2);
		}

		//Comparison数组，排序时用到
		file.AppendFormat("\tinternal static Comparison<{0}>[] _Comparison = \n", parserItemName);
		file.AppendLine("\t{");
		fun = @"		delegate({0} a, {0} b) {3} return {1}(a._{2}, b._{2}); {4},
";
		for (int index = 0; index < _excel.FieldCount; index++)
		{
			string typeName = "DataItemBase.CompareInt";
			switch (_excel.GetFieldType(index))
			{
				case ExceFieldType.LONG:
					{
						typeName = "DataItemBase.CompareLong";
					}
					break;

				case ExceFieldType.REAL:
					{
						typeName = "DataItemBase.CompareSingle";
					}
					break;

				case ExceFieldType.TEXT:
					{
						typeName = "DataItemBase.CompareString";
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
		file.AppendLine("\t{\n\t\t__p=item.table;");
		for (int index = 0; index < _excel.FieldCount; index++)
		{
			string fieldName = buildName(_excel.GetFieldName(index));
			ExceFieldType excelType = _excel.GetFieldType(index);
			if(excelType == ExceFieldType.INTEGER || excelType == ExceFieldType.LONG || excelType == ExceFieldType.REAL)
			{
				file.AppendFormat("\t\t_{0} = item.{0};\n", fieldName);
			}
		}
		file.AppendLine("\t\tOnPostParse();\n\t}");
		file.AppendLine("}\n");
	}

	static string[] _fieldType = { "int", "long", "float"};
	static string[] _compareFun = { "DataItemBase.CompareInt", "DataItemBase.CompareLong", "DataItemBase.CompareSingle" };
	void BuildParser(StringBuilder file, string structItemName, string structListName, string parserItemName, string paserName)
	{
		string fun = @"public static partial class {0}
{5}
	static List<{1}> _list = null;
	static List<{1}>[] _mainKey = null;
	static ByteBuffer _data = null;
	public static void LoadDatas()
	{5}
		if (_mainKey != null)
		{5}
			return;
		{6}

		if (_data != null)
		{5}
			_data.Dispose();
			_data = null;
		{6}

		_data = DataItemBase.Load(""{2}"");
		if (_data == null)
		{5}
			return;
		{6}

		_mainKey = new List<{1}>[{4}];
		FlatBuffersData.{3} structList = FlatBuffersData.{3}.GetRootAs{3}(_data);
		_list = new List<{1}>(structList.ListLength);
		for (int index = 0; index < structList.ListLength; index++)
		{5}
			{1} item = new {1}();
			item.Parse(structList.List(index).Value);
			_list.Add(item);
		{6}
		DataItemBase.OnPostLoaded({0}.Dispose);
	{6}

	public static void Dispose()
	{5}
		if(_data != null)
		{5}
			_data.Dispose();
			_data = null;
		{6}

		if(_list != null)
		{5}
			for(int index = 0; index < _list.Count; index ++)
			{5}
				{1} item = _list[index];
				item.Dispose();
			{6}
			_list = null;
		{6}

		if(_mainKey != null)
		{5}
			for (int index = 0; index < _mainKey.Length; index ++)
			{5}
				List<{1}> list = _mainKey[index];
				if(list != null)
				{5}
					list.Clear();
				{6}
				_mainKey[index] = null;
			{6}
			_mainKey = null;
		{6}
	{6}

	public static int count {5} get {5} LoadDatas(); return _list != null ? _list.Count : 0; {6} {6}

	public static Query<{1}> Query()
	{5}
		LoadDatas();
		return Query<{1}>.Create(_list);
	{6}

	static void BuildKeyByIndex(int index)
	{5}
		LoadDatas();
		if (_list != null && _mainKey[index] == null)
		{5}
			List<{1}> list = new List<{1}>(_list.Count);
			list.AddRange(_list);
			_mainKey[index] = list;

			list.Sort({1}._Comparison[index]);
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
		fun = @"	public static {4} Max{0}(System.Func<{4}, bool> comparete = null) {2} BuildKeyByIndex({1}); return DataItemBase.FindMax<{4}>(_mainKey[{1}], comparete); {3}
	public static void KeyFor{0}() {2} BuildKeyByIndex({1}); {3}
	public static Query<{4}, {5}> Query{0}({5} value)
	{2}
		BuildKeyByIndex({1}); return Query<{4}, {5}>.Create(_mainKey[{1}], value, {6}, {4}._Get{0});
	{3}

";
		for (int index = 0; index < _excel.FieldCount; index++)
		{
			ExceFieldType excelType = _excel.GetFieldType(index);
			switch (excelType)
			{
				case ExceFieldType.INTEGER:
				case ExceFieldType.LONG:
				case ExceFieldType.REAL:
					{
						string fieldType = _fieldType[(int)excelType - 1];
						string compareFun = _compareFun[(int)excelType - 1];
						file.AppendFormat(fun, buildName(_excel.GetFieldName(index)), index, "{", "}", parserItemName, fieldType, compareFun);
					}
					break;

				default:
					break;
			}
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