using System;

using Log = SCANsat.SCAN_Platform.Logging.ConsoleLogger;


namespace SCANsat.SCAN_Platform
{
	public abstract class SCAN_ConfigNodeStorage : IPersistenceLoad, IPersistenceSave
	{

		Func<string> timeNow = () => string.Format("{0:ddMMyyyy-HHmmss}", DateTime.Now);

		public SCAN_ConfigNodeStorage() { } // FIXME: should be private?
		public SCAN_ConfigNodeStorage(string filePath) { FilePath = filePath; } // FIXME: should be private?

		private string _FilePath;
		public string FilePath
		{
			get { return _FilePath; }
			set { _FilePath = System.IO.Path.Combine(KSPUtil.ApplicationRootPath, "GameData/" + value + ".cfg").Replace("\\", "/"); }
		}

		private string topNodeName;
		public string TopNodeName
		{
			get { return topNodeName; }
			internal set { topNodeName = value; }
		}
		public string FileName { get { return System.IO.Path.GetFileName(FilePath); } }
		public bool FileExists { get { return System.IO.File.Exists(FilePath); } }

		void IPersistenceLoad.PersistenceLoad() { OnDecodeFromConfigNode(); }
		void IPersistenceSave.PersistenceSave() { OnEncodeToConfigNode(); }

		public virtual void OnDecodeFromConfigNode() { }
		public virtual void OnEncodeToConfigNode() { }
		public virtual void onSavePost() { }

		public bool Load() { return Load(FilePath); }
		public bool Load(string fileFullName)
		{
			try
			{
				Log.Debug("Loading ConfigNode");
				if (FileExists)
				{
					ConfigNode cnToLoad = GameDatabase.Instance.GetConfigNode(topNodeName);
					ConfigNode.LoadObjectFromConfig(this, cnToLoad);
					return true;
				}
				else
				{
					Log.Now("File could not be found to load({0})", fileFullName);
					return false;
				}
			}
			catch (Exception ex)
			{
				Log.Now("Failed to Load ConfigNode from file ({0}) - Error:{1}", fileFullName, ex);
				Log.Now("Storing old config - {0}", fileFullName + ".err-" + timeNow);
				System.IO.File.Copy(fileFullName, fileFullName + ".err-" + timeNow, true);
				return false;
			}
		}

		public bool LoadSavedCopy()
		{
			try
			{
				Log.Debug("Loading ConfigNode");
				if (FileExists)
				{
					ConfigNode cnToLoad = ConfigNode.Load(FilePath);
					ConfigNode cnUnwrapped = cnToLoad.GetNode(this.GetType().Name);
					ConfigNode.LoadObjectFromConfig(this, cnUnwrapped);
					return true;
				}
				else
				{
					Log.Now("File could not be found to load after saving new copy ({0})", FilePath);
					return false;
				}
			}
			catch (Exception ex)
			{
				Log.Now("Failed to Load ConfigNode from file after saving ({0}) - Error:{1}", FilePath, ex.Message);
				return false;
			}
		}

		public bool Save()
		{
			Log.Debug("Saving ConfigNode");
			return Save(FilePath);
		}
		public bool Save(string fileFullName)
		{
			try
			{
				ConfigNode cnToSave = AsConfigNode;
				ConfigNode cnSaveWrapper = new ConfigNode(GetType().Name);
				cnSaveWrapper.AddNode(cnToSave);
				cnSaveWrapper.Save(fileFullName);
				onSavePost();
				return true;
			}
			catch (Exception ex)
			{
				Log.Now("Failed to Save ConfigNode to file({0})-Error:{1}", fileFullName, ex.Message);
				return false;
			}
		}

		public ConfigNode AsConfigNode
		{
			get
			{
				try
				{
					ConfigNode cnTemp = new ConfigNode(GetType().Name);
					cnTemp = ConfigNode.CreateConfigFromObject(this, cnTemp);
					return cnTemp;
				}
				catch (Exception ex)
				{
					Log.Now("Failed to generate ConfigNode-Error;{0}", ex.Message);
					return new ConfigNode(GetType().Name);
				}
			}
		}

		internal static string _AssemblyName { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }
		internal static string _AssemblyLocation { get { return System.Reflection.Assembly.GetExecutingAssembly().Location; } }
		internal static string _AssemblyFolder { get { return System.IO.Path.GetDirectoryName(_AssemblyLocation); } }
	}
}
