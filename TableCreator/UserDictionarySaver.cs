using System;
using System.IO;
using System.Text;
using OfficeOpenXml;
using System.Collections.Generic;

public class UserDictionarySaver
{
	static string GetValue(ExcelRange Cells, int Row, int Col)
	{
		object obj = Cells[Row, Col].Value;
		return obj != null ? obj.ToString() : "";
	}

	public static void Merge(string path, Dictionary<string, string> dict, int select)
	{
		if(dict == null)
		{
			return;
		}

		ExcelPackage package = null;
		using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read))
		{
			int rows = 0;
			package = new ExcelPackage(stream);
			ExcelWorkbook workbook = package.Workbook;
			ExcelWorksheet sheet = workbook.Worksheets[0];
			ExcelAddressBase Dimension = sheet.Dimension;
			var cells = sheet.Cells;
			if (Dimension != null)
			{
				rows = Dimension.Rows;
				for (int i = 1; i <= rows; i++)
				{
					string r = null;
					string k = GetValue(cells, i, 1);
					string v = GetValue(cells, i, 2);
					if (dict.TryGetValue(k, out r))
					{
						dict.Remove(k);
					}
				}
			}

			if(dict.Count <= 0)
			{
				return;
			}

			rows += 1;
			Dictionary<string, string>.Enumerator em = dict.GetEnumerator();
			while (em.MoveNext())
			{
				KeyValuePair<string, string> pair = em.Current;
				cells[rows, 1].Value = pair.Key;
				cells[rows, select + 1].Value = pair.Value;
				rows++;
			}

			stream.Close();
			stream.Dispose();
		}

		if(package != null)
		{
			File.Delete(path);
			using (Stream stream = new FileStream(path, FileMode.Create))
			{
				package.SaveAs(stream);
			}
			package.Dispose();
		}
	}
}