using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections.Generic;

namespace TableCreator
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				string bin_out = "";
				string mysql_out = "";
				string sharp_out = "";
				string dict_path = "";
				List<string> list = new List<string>();
				for (int index = 0; index < args.Length; index++)
				{
					string arg = args[index];
					if (arg.StartsWith("-"))
					{
						switch (arg)
						{
							case "-b":
								bin_out = args[++index];
								break;

							case "-c":
								sharp_out = args[++index];
								break;

							case "-d":
								dict_path = args[++index];
								break;

							case "-m":
								mysql_out = args[++index];
								break;
						}
					}
					else
					{
						FileInfo info = new FileInfo(arg);
						FileAttributes attributes = info.Attributes;
						if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
						{
							string[] files = Directory.GetFiles(arg, "*.*");
							for (int i = 0; i < files.Length; i++)
							{
								list.Add(files[i]);
							}
						}
						else if ((attributes & FileAttributes.Archive) == FileAttributes.Archive)
						{
							list.Add(arg);
						}
					}
				}

				XmlElement config_node = null;
				XmlDocument doc = new XmlDocument();
				string exeRoot = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
				string configPath = Path.Combine(exeRoot, "TableCreatorConfig.xml");
				if (string.IsNullOrEmpty(bin_out) || string.IsNullOrEmpty(sharp_out))
				{
					if (File.Exists(configPath))
					{
						doc.Load(configPath);
						config_node = doc.SelectSingleNode("/config") as XmlElement;
						if (config_node != null)
						{
							if (config_node.HasAttribute("bytes_output"))
							{
								bin_out = config_node.GetAttribute("bytes_output");
							}

							if (config_node.HasAttribute("sharp_output"))
							{
								sharp_out = config_node.GetAttribute("sharp_output");
							}

							if (config_node.HasAttribute("mysql_output"))
							{
								mysql_out = config_node.GetAttribute("mysql_output");
							}

							if (config_node.HasAttribute("dictionary"))
							{
								dict_path = config_node.GetAttribute("dictionary");
							}
						}
					}
				}

				if (config_node == null)
				{
					config_node = doc.CreateElement("config");
					doc.AppendChild(config_node);
				}
				config_node.SetAttribute("bytes_output", bin_out);
				config_node.SetAttribute("sharp_output", sharp_out);
				config_node.SetAttribute("mysql_output", mysql_out);
				config_node.SetAttribute("dictionary", dict_path);
				doc.Save(configPath);

				if(string.IsNullOrEmpty(bin_out) == false && string.IsNullOrEmpty(Path.GetPathRoot(bin_out)))
				{
					bin_out = Path.Combine(exeRoot, bin_out);
				}

				if (string.IsNullOrEmpty(sharp_out) == false && string.IsNullOrEmpty(Path.GetPathRoot(sharp_out)))
				{
					sharp_out = Path.Combine(exeRoot, sharp_out);
				}

				if (string.IsNullOrEmpty(mysql_out) == false && string.IsNullOrEmpty(Path.GetPathRoot(mysql_out)))
				{
					mysql_out = Path.Combine(exeRoot, mysql_out);
				}

				string fbspath = Path.Combine(exeRoot, "FBS.txt");
				Dictionary<string, ExcelHeaderItem[]> excelheaders = ExcelParser.ParseExcelHeaderFromFbs(fbspath);

				ExcelParser dict_parser = null;
				Dictionary<string, string> user_dict = new Dictionary<string, string>();
				if (string.IsNullOrEmpty(dict_path) == false && string.IsNullOrEmpty(Path.GetPathRoot(dict_path)))
				{
					dict_path = Path.Combine(exeRoot, dict_path);
				}

				Console.WriteLine(string.Format("解析字典文件 : {0}", dict_path));
				{
					FileAttributes attributes = File.GetAttributes(dict_path);
					if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
					{
						Console.WriteLine(string.Format("{0}只读，强制执行会造成本地化文本丢失的问题，继续请按'y'键。", dict_path));
						if (Console.ReadKey().KeyChar != 'y')
						{
							return;
						}
						Console.WriteLine();
					}
				}

				dict_parser = new ExcelParser(dict_path, null, FileAccess.Read);
				if(dict_parser.FieldCount <= 0)
				{
					throw new System.Exception("没有找到任何字典数据，必须配置字典文件");
				}

				for (int i = 0; i < dict_parser.RowCount; i++)
				{
					string k = dict_parser.GetString(i, 0);
					string v = dict_parser.GetString(i, 1);
					user_dict[k] = v;
				}

				StringBuilder mysql = new StringBuilder();
				for (int i = 0; i < list.Count; i++)
				{
					string file = list[i];
					Console.WriteLine(string.Format("{0} / {1} : {2}", i + 1, list.Count, file));
					ExcelParser parser = new ExcelParser(file, excelheaders);

					if (string.IsNullOrEmpty(bin_out) == false)
					{
						FlatBuffersCreator creator = new FlatBuffersCreator(parser, user_dict);
						Console.WriteLine(string.Format("write bin to {0}", creator.SaveFlatBuffer(creator.CreateFlatBufferBuilder(), bin_out)));
					}

					if (string.IsNullOrEmpty(sharp_out) == false)
					{
						FlatBuffersLoaderBuilder builder = new FlatBuffersLoaderBuilder(parser);
						Console.WriteLine(string.Format("write code to {0}", builder.Build(sharp_out)));
					}

					if (string.IsNullOrEmpty(mysql_out) == false)
					{
						MySqlBuilder builder = new MySqlBuilder(parser, user_dict);
						builder.Append(mysql);
						Console.WriteLine(string.Format("build sql code for {0}", parser.FileName));
					}

					parser.Dispose();

					Console.WriteLine();
				}

				UserDictionarySaver.Merge(dict_path, user_dict);

				dict_parser.Dispose();
				dict_parser = new ExcelParser(dict_path, null);

				if (mysql.Length > 0)
				{
					MySqlBuilder.AppendDict(dict_parser, mysql);
					MySqlBuilder.Save(mysql_out, mysql);
				}

				for (int i = 0; i < dict_parser.RowCount; i++)
				{
					string k = dict_parser.GetString(i, 0);
					string v = dict_parser.GetString(i, 1);
					user_dict[k] = v;
				}

				string[] knownLanguages = new string[dict_parser.FieldCount - 1];
				for(int index = 1; index < dict_parser.FieldCount; index ++)
				{
					knownLanguages[index - 1] = dict_parser.GetFieldName(index);
				}

				Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>();
				for (int i = 0; i < dict_parser.RowCount; i++)
				{
					string key = dict_parser.GetString(i, 0);
					if (string.IsNullOrEmpty(key) || key.StartsWith("//"))
					{
						continue;
					}

					string[] contents = new string[knownLanguages.Length];
					for (int j = 1; j < dict_parser.FieldCount; j++)
					{
						contents[j - 1] = dict_parser.GetString(i, j);
					}
					dictionary[key] = contents;
				}

				if(string.IsNullOrEmpty(bin_out) == false)
				{
					LocalizationSaver.Save(bin_out, knownLanguages, dictionary);
				}
				dict_parser.Dispose();

				ExcelParser.SaveExcelHeaderToFbs(fbspath, excelheaders);
			}
			catch(System.Exception e)
			{
				Console.WriteLine(e.Message);
			}
			finally
			{
				Console.WriteLine("done.");
				Console.ReadLine();
			}
		}
	}
}
