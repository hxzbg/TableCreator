using System;
using System.IO;
using System.Xml;
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
				string sharp_out = "";
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
				string configPath = Path.Combine(exeRoot, "TableCreatorOutput.xml");
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
				doc.Save(configPath);

				if(string.IsNullOrEmpty(Path.GetPathRoot(bin_out)))
				{
					bin_out = Path.Combine(exeRoot, bin_out);
				}

				if (string.IsNullOrEmpty(Path.GetPathRoot(sharp_out)))
				{
					sharp_out = Path.Combine(exeRoot, sharp_out);
				}

				for (int i = 0; i < list.Count; i++)
				{
					string file = list[i];
					Console.WriteLine(string.Format("{0} / {1} : {2}", i + 1, list.Count, file));
					ExcelParser parser = new ExcelParser(file);

					FlatBuffersCreator creator = new FlatBuffersCreator(parser);
					Console.WriteLine(string.Format("write bin to {0}", creator.SaveFlatBuffer(creator.CreateFlatBufferBuilder(), bin_out)));

					FlatBuffersLoaderBuilder builder = new FlatBuffersLoaderBuilder(parser);
					Console.WriteLine(string.Format("write code to {0}", builder.Build(sharp_out)));
					parser.Dispose();

					Console.WriteLine();
				}
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
