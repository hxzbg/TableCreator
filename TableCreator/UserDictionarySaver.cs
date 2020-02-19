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

		List<int> indexs = new List<int>();
		List<int> lengths = new List<int>();
		StringBuilder sb = new StringBuilder();
		System.Action<int, int> ParserAction = delegate (int start, int length)
		{
			indexs.Add(start);
			lengths.Add(length);
		};

		Dictionary<string, string>.Enumerator em;
		{
			Dictionary<string, string> newdict = new Dictionary<string, string>();
			em = dict.GetEnumerator();
			while (em.MoveNext())
			{
				KeyValuePair<string, string> pair = em.Current;

				sb.Remove(0, sb.Length);
				int start = 0;
				string line = pair.Value;
				FormatOperatorChecker.ParseFormaters(line, ParserAction);
				if (indexs.Count > 0)
				{
					int lastOffset = -1;
					for (int index = indexs.Count - 1; index >= 0; index--)
					{
						start = indexs[index];
						char c = line[start];
						switch (c)
						{
							case '{':
								{
									int offset = 0;
									start = start + 1;
									c = line[start];
									if (c <= 'Z')
									{
										offset = c - 'A';
									}
									else if (c <= 'z')
									{
										offset = c - 'a' + 26;
									}
									if (offset > lastOffset)
									{
										lastOffset = offset;
										lastOffset++;
									}
								}
								break;

							default:
								break;
						}
					}

					{
						start = 0;
						if (lastOffset <= 0)
						{
							lastOffset = 0;
						}
						for (int index = 0; index < indexs.Count; index++)
						{
							int pos = indexs[index];
							if (pos > start)
							{
								sb.Append(line.Substring(start, pos - start));
							}
							start = pos;
							char c = line[start];
							switch (c)
							{
								case '{':
									{
										int offset = 0;
										start = start + 1;
										c = line[start];
										if (c <= 'Z')
										{
											offset = c - 'A';
										}
										else if (c <= 'z')
										{
											offset = c - 'a' + 26;
										}
										sb.Append("{");
										sb.Append((char)('0' + offset));
										sb.Append('}');
									}
									break;

								case '%':
									{
										sb.Append("{");
										sb.Append((char)('0' + lastOffset));
										sb.Append('}');
										lastOffset++;
									}
									break;

								default:
									break;
							}
							start = start + lengths[index] + 1;
						}
					}
				}
				indexs.Clear();
				if (start < line.Length)
				{
					sb.Append(line.Substring(start));
				}

				newdict.Add(pair.Key, sb.ToString());
			}
			dict = newdict;
		}

		ExcelPackage package = null;
		using (FileStream stream = File.Open(path + ".xlsx", FileMode.Open, FileAccess.Read))
		{
			int rows = 0;
			package = new ExcelPackage(stream);
			ExcelWorkbook workbook = package.Workbook;
			ExcelWorksheet sheet = workbook.Worksheets[1];
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
			em = dict.GetEnumerator();
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
			File.Delete(path + ".xlsx");
			using (Stream stream = new FileStream(path + ".xlsx", FileMode.Create))
			{
				package.SaveAs(stream);
			}
			package.Dispose();
		}
	}
}