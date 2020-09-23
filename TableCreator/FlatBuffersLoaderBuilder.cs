using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class FlatBuffersLoaderBuilder
{
	string _namespace = null;
	string _loaderName = null;
	ExcelHeaderItem[] _headers = null;
	public FlatBuffersLoaderBuilder(string loaderName, ExcelHeaderItem[] headrs, string name_space = null)
	{
		_headers = headrs;
		_namespace = name_space;
		_loaderName = loaderName;
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

	void BuildItemForParser(StringBuilder file, string structItemName, string structListName, string parserItemName, string paserName)
	{
		string fun = @"public partial class {0} : DataStoreItem
{1}
";
		if(string.IsNullOrEmpty(_namespace) == false)
		{
			file.AppendFormat("namespace {0}\n{1}\n", _namespace, "{");
		}
		file.AppendFormat(fun, parserItemName, "{", "}");

		//成员变量和属性,以及Get回调方法

		fun = @"	public {2} {3} {0} get {0} return {4}((int){5}.Field.{3}); {1} {1}
";
		for (int index = 0; index < _headers.Length; index++)
		{
			string typeName = "int";
			string funName = "GetInt32";
			ExcelHeaderItem header = _headers[index];
			switch (header.fieldtype)
			{
				case ExceFieldType.LONG:
					{
						typeName = "long";
						funName = "GetInt64";
					}
					break;

				case ExceFieldType.REAL:
					{
						typeName = "float";
						funName = "GetSingle";
					}
					break;

				case ExceFieldType.TEXT:
					{
						typeName = "string";
						funName = "GetString";
					}
					break;
			}

			//0:"{"
			//1:"}"
			//2:value type
			//3:value name;
			//4:function name
			//5:field id
			file.AppendFormat(fun, "{", "}", typeName, buildName(header.fieldname), funName, paserName);
		}
		file.AppendLine("}\n");
	}

	static string[] _fieldType = { "int", "long", "float"};
	static string[] _queryFun = { "QueryInt32", "QueryInt64", "QuerySingle" };
	void BuildParser(StringBuilder file, string structItemName, string structListName, string parserItemName, string paserName)
	{
		string fun = @"
#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public partial class {1} : DataStoreSet
{0}
	static {1} __Instance = null;
";
        file.AppendFormat(fun, "{", paserName);

		file.Append(@"	public enum Field
	{
");
		if (_headers.Length > 0)
		{
			for (int index = 0; index < _headers.Length; index++)
			{
				ExcelHeaderItem header = _headers[index];
				file.AppendFormat("		{0}={1},\n", buildName(header.fieldname), index);
			}
			file.Remove(file.Length - 2, 2);
		}
		file.Append("\n	}\n");

		//fieldAttributes
		file.Append("\tstatic int[] ____FieldAttributes = {");
		if (_headers.Length > 0)
		{
			for (int index = 0; index < _headers.Length; index++)
			{
				ExcelHeaderItem header = _headers[index];
				file.AppendFormat("{0},", (int)header.fieldtype);
			}
			file.Remove(file.Length - 1, 1);
		}
		file.Append("};\n");

		fun = @"
    static {2}()
    {0}
        __Instance = new {2}();
		__Instance.__TypeForItem = typeof({3});
        __Instance.__AssetPath = ""{4}"";
        __Instance.__FieldAttributes = ____FieldAttributes;
#if UNITY_EDITOR
		DataItemPaser.PushDataItemParser(CreateDataItemPaser);
#endif
    {1}

    public static void LoadDatas()
    {0}
        __Instance.__Init();
    {1}

    static System.Func<DataStoreItem, bool> ConvertFilter(System.Func<{3}, bool> filter)
    {0}
		if (filter != null)
		{0}
            return delegate (DataStoreItem item)
			{0}
				return filter(item as {3});
			{1};
		{1}
        return null;
    {1}

	public static int Count {0} get {0} return __Instance.__GetLength(); {1} {1}
	public static {3} Max(Field field, System.Func<{3}, bool> filter = null) {0} return ({3})__Instance.__FindMax((int)field, ConvertFilter(filter)); {1}
	public static Query<{3}> Query(System.Func<{3}, bool> filter = null)
	{0}
		return Query<{3}>.Create(__Instance.__Search(ConvertFilter(filter)));
	{1}
	/*
	public static QueryDataStore Query(System.Func<DataStoreItem, bool> filter = null)
	{0}
		return __Instance.__Search(filter);
	{1}
	*/
	public static void BuildKeyByField(Field field) {0} __Instance.__BuildKeyByField((int)field); {1}
	public static QueryDataStoreInt32 QueryInt32(Field field, int value, System.Func<DataStoreItem, bool> filter = null)
	{0}
		return __Instance.__SearchInt32((int)field, value, filter);
	{1}
	public static QueryDataStoreSingle QuerySingle(Field field, float value, System.Func<DataStoreItem, bool> filter = null)
	{0}
		return __Instance.__SearchSingle((int)field, value, filter);
	{1}
	public static QueryDataStoreInt64 QueryInt64(Field field, long value, System.Func<DataStoreItem, bool> filter = null)
	{0}
		return __Instance.__SearchInt64((int)field, value, filter);
	{1}
";
		file.AppendFormat(fun, "{", "}", paserName, parserItemName, _loaderName);

		//添加编辑器检查函数
		fun = @"
#if UNITY_EDITOR
	static DataItemPaser CreateDataItemPaser()
	{1}
		__Instance.__Init();
		return new DataItemPaser(";
		file.AppendFormat(fun, paserName, "{", "}");
		file.AppendFormat("\"{0}\", new string[]", _loaderName);
		file.Append("{");
		for(int i = 0; i < _headers.Length; i ++)
		{
			file.AppendFormat("\"{0}\",", _headers[i].fieldname);
		}
		file.Remove(file.Length - 1, 1);
		file.Append("},");
		file.Append("new string[]{");
		for (int i = 0; i < _headers.Length; i++)
		{
			file.AppendFormat("\"{0}\",", _headers[i].fieldtype);
		}
		file.Remove(file.Length - 1, 1);
		file.Append("},");
		file.Append("__Instance.__GetLength(),");

		file.AppendFormat(@"delegate (int index)
		{0}
            {3} item = __Instance.__GetItem(index) as {3};
            if(item == null)
            {0}
                return null;
            {1}
			string[] array = new string[{2}];
", "{", "}", _headers.Length, parserItemName);
		for(int i = 0; i < _headers.Length; i ++)
		{
			ExcelHeaderItem header = _headers[i];
			if (header.fieldtype == ExceFieldType.TEXT)
			{
				file.AppendFormat("			array[{0}] = item.{1};\n", i, buildName(header.fieldname));
			}
			else
			{
				file.AppendFormat("			array[{0}] = item.{1}.ToString();\n", i, buildName(header.fieldname));
			}
		}
		file.Append(@"			return array;
		});
	}
#endif
");

		//添加BuildMainKeyXX函数
		//0:fieldname;
		//1:index
		//2:{
		//3:}
		//4:parserItemName
		//5:fieldtype
		//6:compare fun
		fun = @"
	public static Query<{2}, {3}> Query{5}({3} value, System.Func<{2}, bool> filter = null)
	{0}
		return Query<{2}, {3}>.Create({4}(Field.{5}, value, ConvertFilter(filter)));
	{1}

";
		for (int index = 0; index < _headers.Length; index++)
		{
			ExcelHeaderItem header = _headers[index];
			ExceFieldType excelType = header.fieldtype;
			switch (excelType)
			{
				case ExceFieldType.INTEGER:
				case ExceFieldType.LONG:
				case ExceFieldType.REAL:
					{
						string fieldType = _fieldType[(int)excelType - 1];
						string queryFun = _queryFun[(int)excelType - 1];
						file.AppendFormat(fun, "{", "}", parserItemName, fieldType, queryFun, buildName(header.fieldname));
					}
					break;

				default:
					break;
			}
		}


		file.AppendLine("}");

		if (string.IsNullOrEmpty(_namespace) == false)
		{
			file.AppendLine("}");
		}
	}

	public string Build(string output)
	{
		if(_headers == null || _headers.Length <= 0)
		{
			return null;
		}

		StringBuilder file = new StringBuilder();
		string structItemName = buildName(_loaderName, "StructItem");
		string structListName = buildName(_loaderName, "StructList");
		string parserItemName = buildName(_loaderName, "Item");
		string paserName = buildName(_loaderName);

		//CS文件标题
		file.Append(@"// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using global::System;
using global::FlatBuffers;
using System.Collections.Generic;

");
		BuildItemForParser(file, structItemName, structListName, parserItemName, paserName);
		BuildParser(file, structItemName, structListName, parserItemName, paserName);

		if(string.IsNullOrEmpty(output) == false && Directory.Exists(output) == false)
		{
			Directory.CreateDirectory(output);
		}
		output = Path.Combine(output, buildName(_loaderName) + ".cs");
		File.WriteAllText(output, file.ToString());
		return output;
	}
}