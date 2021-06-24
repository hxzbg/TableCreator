using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TableCreator
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				//bool rebuiild = false;
				string bin_out = "";
				string mysql_out = "";
				string sharp_out = "";
				string dict_path = "";
				string name_space = "";
				List<string> list = new List<string>();
				for (int index = 0; index < args.Length; index++)
				{
					string arg = args[index];
					Console.WriteLine(arg);
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

							case "-n":
								name_space = args[++index];
								break;

								/*
							case "-rebuild":
								{
									rebuiild = true;
								}
								break;
								*/
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
						else
						{
							list.Add(arg);
						}
					}
				}

				string workPath = null;
				XmlElement config_node = null;
				XmlDocument doc = new XmlDocument();
				string exeRoot = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
				string[] searchPaths = new string[] {Directory.GetCurrentDirectory(), exeRoot};
				string configPath = Path.Combine(exeRoot, "TableCreatorConfig.xml");
				if (string.IsNullOrEmpty(bin_out) || string.IsNullOrEmpty(sharp_out))
				{
					for(int i = 0; i < searchPaths.Length; i ++)
					{
						string searchPath = Path.Combine(searchPaths[i], "TableCreatorConfig.xml");
						if (File.Exists(searchPath))
						{
							doc.Load(searchPath);
							config_node = doc.SelectSingleNode("/config") as XmlElement;
							if (config_node != null)
							{
								configPath = searchPath;
								workPath = searchPaths[i];
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

								if (config_node.HasAttribute("namespace"))
								{
									name_space = config_node.GetAttribute("namespace");
								}
								break;
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
					bin_out = Path.Combine(workPath, bin_out);
				}

				if (string.IsNullOrEmpty(sharp_out) == false && string.IsNullOrEmpty(Path.GetPathRoot(sharp_out)))
				{
					sharp_out = Path.Combine(workPath, sharp_out);
				}

				if (string.IsNullOrEmpty(mysql_out) == false && string.IsNullOrEmpty(Path.GetPathRoot(mysql_out)))
				{
					mysql_out = Path.Combine(workPath, mysql_out);
				}

				/*
				string fbspath = Path.Combine(workPath, "FBS.txt");
				Dictionary<string, ExcelHeaderItem[]> excelheaders = ExcelParser.ParseExcelHeaderFromFbs(fbspath);
				if (rebuiild)
				{
					Dictionary<string, ExcelHeaderItem[]>.Enumerator em = excelheaders.GetEnumerator();
					while(em.MoveNext())
					{
						KeyValuePair<string, ExcelHeaderItem[]> pair = em.Current;
						ExcelHeaderItem[] headers = pair.Value;
						FlatBuffersLoaderBuilder builder = new FlatBuffersLoaderBuilder(pair.Key, pair.Value, name_space);
						string sharppath = builder.Build(sharp_out);
						if (string.IsNullOrEmpty(sharppath))
						{
							Console.WriteLine("生成代码失败。");
						}
						else
						{
							Console.WriteLine(string.Format("write code to {0}", sharppath));
						}
					}
					return;
				}
				*/

				ExcelParser dict_parser = null;
				Dictionary<string, string> user_dict = new Dictionary<string, string>();
				if (string.IsNullOrEmpty(dict_path) == false && string.IsNullOrEmpty(Path.GetPathRoot(dict_path)))
				{
					dict_path = Path.Combine(workPath, dict_path);
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

				dict_parser = ExcelParser.Create(dict_path);
				if(dict_parser.FieldCount <= 0)
				{
					throw new System.Exception("没有找到任何字典数据，必须配置字典文件");
				}

				int select = 1;
				if(dict_parser.FieldCount < 2)
				{
					Console.WriteLine("语言文件不合法，不包含任何语言信息");
					return;
				}
				else if (dict_parser.FieldCount > 2)
				{
					int maxl = dict_parser.FieldCount - 2;
					Console.WriteLine("按序号选择匹配的语言:");

					string msg = "";
					for(int i = 1; i < dict_parser.FieldCount; i++)
					{
						msg += string.Format("{0}.{1}\n", i, dict_parser.GetFieldName(i));
					}

					select = 0;
					while (select < 1 || select > maxl)
					{
						Console.Write(msg);
						string input = Console.ReadLine();
						int.TryParse(input, out select);
					}
				}
				Console.WriteLine(string.Format("\n-------------------读取语言 {0} 并解析数据库文件-------------------", dict_parser.GetFieldName(select)));

				FormatOperatorChecker checker = new FormatOperatorChecker(dict_parser);
				string logfile = dict_path + ".log";
				if (File.Exists(logfile))
				{
					File.Delete(logfile);
				}

				string errors = checker.Run();
				if (string.IsNullOrEmpty(errors) == false)
				{
					File.WriteAllText(logfile, errors);
					Console.WriteLine("多国语文件存在错误，详情请查看{0}。", logfile);
				}

				MatchEvaluator evaluator = delegate (Match match)
				{
					return match.Value.ToUpper();
				};

				for (int i = 0; i < dict_parser.RowCount; i++)
				{
					string k = dict_parser.GetString(i, 0);
					if(string.IsNullOrEmpty(k) || k.StartsWith("__") == false)
					{
						continue;
					}
					string v = dict_parser.GetString(i, select);
					v = Regex.Replace(v, "\\[[0-9a-fA-F]{6}\\]", evaluator);
					if(user_dict.ContainsKey(k))
					{
						Console.WriteLine(string.Format("忽略 {0}:{1}", k, user_dict[k]));
					}
					user_dict[k] = v;
				}

				Dictionary<string, ExcelHeaderItem[]> excelheaders = new Dictionary<string, ExcelHeaderItem[]>();
				StringBuilder mysql = new StringBuilder();
				for (int i = 0; i < list.Count; i++)
				{
					string file = list[i];
					Console.WriteLine(string.Format("{0} / {1} : {2}", i + 1, list.Count, file));
					ExcelParser parser = ExcelParser.Create(file);

					if (string.IsNullOrEmpty(bin_out) == false)
					{
						FlatBuffersCreator creator = new FlatBuffersCreator(parser, user_dict);
						string binpath = creator.SaveFlatBuffer(creator.CreateFlatBufferBuilder(), bin_out);
						if (string.IsNullOrEmpty(binpath))
						{
							Console.WriteLine("生成二进制文件失败。");
						}
						else
						{
							Console.WriteLine(string.Format("write bin to {0}", binpath));
						}						
					}

					if (string.IsNullOrEmpty(sharp_out) == false)
					{
						string loaderName = parser.FileName;
						ExcelHeaderItem[] headers = parser.ExcelHeader;
						FlatBuffersLoaderBuilder builder = new FlatBuffersLoaderBuilder(loaderName, headers, name_space);
						string sharppath = builder.Build(sharp_out);
						if(string.IsNullOrEmpty(sharppath))
						{
							Console.WriteLine("生成代码失败。");
						}
						else
						{
							Console.WriteLine(string.Format("write code to {0}", sharppath));
						}
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
				//ExcelParser.SaveExcelHeaderToFbs(fbspath, excelheaders);

				UserDictionarySaver.Merge(dict_path, user_dict, select);

				dict_parser.Dispose();
				dict_parser = ExcelParser.Create(dict_path);

				if (mysql.Length > 0)
				{
					MySqlBuilder.AppendDict(dict_parser, mysql);
					MySqlBuilder.Save(mysql_out, mysql);
				}

				string[] knownLanguages = new string[dict_parser.FieldCount - 1];
				for(int index = 1; index < dict_parser.FieldCount; index ++)
				{
					knownLanguages[index - 1] = dict_parser.GetFieldName(index);
				}

				Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>();
				for (int i = 0; i < dict_parser.RowCount; i++)
				{
					string key = dict_parser.GetString(i, 0).Trim();
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
