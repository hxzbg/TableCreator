using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class MySqlBuilder
{
	ExcelParser _excel = null;
	Dictionary<string, string> _dict = null;
	public MySqlBuilder(ExcelParser parser, Dictionary<string, string> dict)
	{
		_dict = dict;
		_excel = parser;
	}

	static string CheckString(string input)
	{
		if(input == null)
		{
			return "";
		}
		return input.Replace("\"", "\\\"").Replace("\'", "\\'").Replace("\n", "\\n");
	}

	public void Append(StringBuilder builder)
	{
		//清理数据
		builder.AppendFormat("-- {0}\nTRUNCATE {0};\n", _excel.FileName);
		for(int i = 0; i < _excel.RowCount; i ++)
		{
			builder.AppendFormat("INSERT INTO `{0}` VALUES(", _excel.FileName);
			for (int j = 0; j < _excel.FieldCount; j ++)
			{
				string str = _excel.GetString(i, j);
				switch (_excel.GetFieldType(j))
				{
					case ExceFieldType.INTEGER:
					case ExceFieldType.REAL:
						{
							if(string.IsNullOrEmpty(str))
							{
								builder.Append("null");
							}
							else
							{
								builder.AppendFormat("\'{0}\'", str);
							}
						}
						break;

					default:
						{
							FlatBuffersCreator.StringUnit unit = FlatBuffersCreator.SplitString(str, _excel.FileName, _dict);
							if (string.IsNullOrEmpty(unit._outkey))
							{
								builder.Append("null");
							}
							else
							{
								builder.AppendFormat("'{0}", unit._outkey);
								if (unit._out_pars != null && unit._out_pars.Length > 0)
								{
									for (int k = 0; k < unit._out_pars.Length; k++)
									{
										builder.AppendFormat(",{0}", unit._out_pars[k]);
									}
								}
								builder.Append("'");
							}
						}
						break;
				}

				if(j + 1 < _excel.FieldCount)
				{
					builder.Append(",");
				}
				else
				{
					builder.AppendLine(");");
				}
			}
		}
		builder.AppendLine();
	}

	public static void AppendDict(ExcelParser parser, StringBuilder builder)
	{
		//清理数据
		string fun = @"
-- Localization
DROP TABLE IF EXISTS `Localization`;
";
		builder.Append(fun);

		if(parser.FieldCount <= 0)
		{
			return;
		}

		int lastFieldIndex = parser.FieldCount - 1;
		builder.Append("CREATE TABLE `Localization` (\n");
		for(int i = 0; i < parser.FieldCount; i ++)
		{
			string str = CheckString(parser.GetFieldName(i));
			builder.AppendFormat("`{0}` TEXT{1}", str, i < lastFieldIndex ? ",\n" : "\n");
		}
		builder.AppendLine(");");

		for(int i = 0; i < parser.RowCount; i ++)
		{
			builder.Append("INSERT INTO `Localization` VALUES(");
			for (int j = 0; j < parser.FieldCount; j ++)
			{
				string str = CheckString(parser.GetString(i, j));
				builder.AppendFormat("'{0}'{1}", str, j < lastFieldIndex ? "," : "");
			}
			builder.AppendLine(");");
		}

		builder.AppendLine();
	}

	public static void Save(string output, StringBuilder file)
	{
		if (string.IsNullOrEmpty(output) == false && Directory.Exists(output) == false)
		{
			Directory.CreateDirectory(output);
		}
		output = Path.Combine(output, "mysql.sql");
		File.WriteAllText(output, file.ToString());
	}
}