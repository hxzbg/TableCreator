using System;
using System.IO;
using System.Text;
using ExcelDataReader;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class XlsParser : ExcelParser
{
	static string GetString(IExcelDataReader excelReader, int index)
	{
		if (index >= 0)
		{
			object obj = excelReader.GetValue(index);
			if (obj == null || obj.GetType() == typeof(System.DBNull))
			{
				return string.Empty;
			}
			return obj.ToString();
		}
		return "";
	}

	public XlsParser(string excelPath, FileAccess access = FileAccess.Read)
	{
		string extension = Path.GetExtension(excelPath);
		if (string.IsNullOrEmpty(extension) == false)
		{
			extension = extension.ToLower();
		}

		if (extension != ".xls" && extension != ".xlsx")
		{
			return;
		}

		_filename = Path.GetFileNameWithoutExtension(excelPath);
		using (FileStream excelFile = File.Open(excelPath, FileMode.Open, access, FileShare.Read))
		{
			IExcelDataReader excelReader = extension == ".xls" ? ExcelReaderFactory.CreateBinaryReader(excelFile) : ExcelReaderFactory.CreateOpenXmlReader(excelFile);

			//读标题
			if (excelReader.Read())
			{
				List<int> fieldids = new List<int>();
				List<string> fieldlist = new List<string>();
				for (int i = 0; i < excelReader.FieldCount; i++)
				{
					string fieldname = GetString(excelReader, i).Trim();
					fieldlist.Add(fieldname);
					fieldids.Add(i);
				}

                if (excelReader.Read())
                {
					for (int i = fieldlist.Count - 1; i >= 0; i--)
                    {
                        if (string.IsNullOrEmpty(fieldlist[i]) || ParseFieldType(GetString(excelReader, i)) == ExceFieldType.None)
                        {
                            fieldlist.RemoveAt(i);
                            fieldids.RemoveAt(i);
                        }
                    }
                }

				int fieldCount = fieldlist.Count;
				_excelheader = new ExcelHeaderItem[fieldCount];
				for (int i = 0; i < fieldCount; i++)
				{
					ExcelHeaderItem item = new ExcelHeaderItem();
					item.fieldid = i;
					item.fieldname = fieldlist[i];
					item.fieldtype = ParseFieldType(GetString(excelReader, fieldids[i]));
					_excelheader[i] = item;
				}


				//跳过注释
				excelReader.Read();

				//读取数据
				while(excelReader.Read())
                {
                    string[] contents = new string[fieldCount];
                    for (int i = 0; i < fieldCount; i++)
                    {
                        contents[i] = GetString(excelReader, fieldids[i]);
                    }
                    _contentList.Add(contents);
                }
			}
			excelFile.Dispose();
			excelReader.Dispose();
		}
	}
}
