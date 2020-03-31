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
	partial void OnPostParse();
	public override void Dispose()
	{1}
";
		if(string.IsNullOrEmpty(_namespace) == false)
		{
			file.AppendFormat("namespace {0}\n{1}\n", _namespace, "{");
		}
		file.AppendFormat(fun, parserItemName, "{", "}");

		//实现Dispose函数
		for(int i = 0; i < _headers.Length; i ++)
		{
			ExcelHeaderItem header = _headers[i];
			if (header.fieldtype == ExceFieldType.TEXT)
			{
				file.AppendFormat("\t\t_{0} = null;\n", buildName(header.fieldname));
			}
		}
		file.AppendLine("\t}\n");

		//成员变量和属性,以及Get回调方法

		fun = @"	{0} _{1} = {3};
	public {0} {1} {4} get {4} return _{1}; {5} {5}
	internal static System.Func<DataStoreItem, {0}> _Get{1} = delegate (DataStoreItem item) {4} return (({2})item).{1}; {5};

";
		string fun_string = @"	{0} _{1} = null;
	public {0} {1} {4} get {4} if(_{1} == null) {7}.BuildString(ref _{1}, this, {6}); return _{1}; {5} {5}

";
		for (int index = 0; index < _headers.Length; index++)
		{
			string typeName = "int";
			string defValue = "0";
			string format = fun;
			ExcelHeaderItem header = _headers[index];
			switch (header.fieldtype)
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
			file.AppendFormat(format, typeName, buildName(header.fieldname), parserItemName, defValue, "{", "}", 4 + index * 2, paserName);
		}

		//Parse函数
		file.AppendFormat("\tpublic override void Parse(int position, FlatbufferDataStore item)\n", structItemName);
		file.AppendLine("\t{\n");
		for (int index = 0; index < _headers.Length; index++)
		{
			string fieldNethod = null;
			ExcelHeaderItem header = _headers[index];
			string fieldName = buildName(header.fieldname);
			ExceFieldType excelType = header.fieldtype;
            switch(excelType)
            {
				case ExceFieldType.INTEGER:
					fieldNethod = "GetIntValue";
					break;

				case ExceFieldType.LONG:
					fieldNethod = "GetLongValue";
					break;

				case ExceFieldType.REAL:
					fieldNethod = "GetFloatValue";
					break;

			}
			if (string.IsNullOrEmpty(fieldNethod) == false)
			{
				file.AppendFormat("\t\t_{0} = item.{1}({2});\n", fieldName, fieldNethod, index);
			}
		}
		file.AppendLine("\t\tOnPostParse();\n\t}");
		file.AppendLine("\n}\n");
	}

	static string[] _fieldType = { "int", "long", "float"};
	static string[] _searchFun = { "__SearchInt", "__SearchLong", "__SearchSingle" };
	static string[] _compareFun = { "DataStoreHelper.__CompareInt", "DataStoreHelper.__CompareLong", "DataStoreHelper.__CompareSingle" };
	void BuildParser(StringBuilder file, string structItemName, string structListName, string parserItemName, string paserName)
	{
		string fun = @"#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public partial class {0} : DataStoreSet
{5}
	static {0} __Instance = null;
    static {0}()
    {5}
        __Instance = new {0}();
		__Instance.m_type = typeof({1});
        __Instance.m_fieldsCount = {4};
        __Instance.m_assetPath = ""{2}"";
        __Instance.m_creator = delegate(){5} return new {1}();{6};
        __Instance.__FieldNames = ____FieldNames;
        __Instance.__Comparsions = ____Comparsions;
        __Instance.__FieldAttributes = ____FieldAttributes;
#if UNITY_EDITOR
		DataItemPaser.PushDataItemParser(CreateDataItemPaser);
#endif
    {6}

    public static void LoadDatas()
    {5}
        __Instance.__Init();
    {6}

";
        file.AppendFormat(fun, paserName, parserItemName, _loaderName, structListName, _headers.Length, "{", "}");

        //fieldName
		file.Append("\tstatic string[] ____FieldNames = {");
        if(_headers.Length > 0)
        {
			for (int index = 0; index < _headers.Length; index++)
			{
				ExcelHeaderItem header = _headers[index];
				file.AppendFormat("\"_{0}\",", buildName(header.fieldname));
			}
			file.Remove(file.Length - 1, 1);
		}
		file.AppendLine("};\n");

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
		file.AppendLine("};\n");

		//Comparison数组，排序时用到
		file.AppendLine("\tstatic Comparison<DataStoreItem>[] ____Comparsions = \n\t{");
		fun = @"		delegate(DataStoreItem a, DataStoreItem b) {3} return {1}((({0})a).{2}, (({0})b).{2}); {4},
";
		for (int index = 0; index < _headers.Length; index++)
		{
			ExcelHeaderItem header = _headers[index];
			string typeName = "DataStoreHelper.__CompareInt";
			switch (header.fieldtype)
			{
				case ExceFieldType.LONG:
					{
						typeName = "DataStoreHelper.__CompareLong";
					}
					break;

				case ExceFieldType.REAL:
					{
						typeName = "DataStoreHelper.__CompareSingle";
					}
					break;

				case ExceFieldType.TEXT:
					{
						typeName = "DataStoreHelper.__CompareString";
					}
					break;
			}

			//0:parserItemName
			//1:funName
			//2:fieldname
			//3:{
			//4:}
			file.AppendFormat(fun, parserItemName, typeName, buildName(header.fieldname), "{", "}");
		}
		file.AppendLine("\t};\n");


		fun = @"	public static int count {5} get {5} return __Instance.__GetLength(); {6} {6}

    static System.Func<DataStoreItem, bool> ConvertFilter(System.Func<{1}, bool> filter)
    {5}
		if (filter != null)
		{5}
            return delegate (DataStoreItem item)
			{5}
				return filter(item as {1});
			{6};
		{6}
        return null;
    {6}

    public static void BuildString(ref string str, {1} item, int offset)
    {5}
        __Instance.__BuildString(ref str, item, offset);
    {6}

	public static Query<{1}> Query(System.Func<{1}, bool> filter = null)
	{5}
		return Query<{1}>.Create(__Instance.__Search(ConvertFilter(filter)));
	{6}

";
		//0:paserName
		//1:parserItemName
		//2:excelname;
		//3:structListName
		//4:fieldCount
		//5:{
		//6:}
		file.AppendFormat(fun, paserName, parserItemName, _loaderName, structListName, _headers.Length, "{", "}");

		//添加编辑器检查函数
		fun = @"#if UNITY_EDITOR
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
		fun = @"	public static {4} Max{0}(System.Func<{4}, bool> filter = null) {2} return __Instance.__FindMax({1}, ConvertFilter(filter)) as {4}; {3}
	public static void KeyFor{0}() {2} __Instance.__BuildKeyByField({1}); {3}
	public static Query<{4}, {5}> Query{0}({5} value, System.Func<{4}, bool> filter = null)
	{2}
		return Query<{4}, {5}>.Create(__Instance.{7}({1}, {6}, {4}._Get{0}, value, ConvertFilter(filter)));
	{3}

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
						string compareFun = _compareFun[(int)excelType - 1];
						string searchFun = _searchFun[(int)excelType - 1];
						file.AppendFormat(fun, buildName(header.fieldname), index, "{", "}", parserItemName, fieldType, compareFun, searchFun);
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