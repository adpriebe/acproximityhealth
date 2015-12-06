using System;
using System.IO;
using System.Xml;

namespace ProximityHealth {
	public static class Util {
        private static string SETTINGS_FILE = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\Asheron's Call\" + "pxhsettings.xml";
        private static string LOG_DIR = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\Asheron's Call\" + Globals.PluginName + "_error.txt";
        public static XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();

        public static void LogError(Exception ex) {
			try	{
				using (StreamWriter writer = new StreamWriter(ProximityHealth.Util.LOG_DIR, true))
				{
					writer.WriteLine(DateTime.Now.ToString());
					writer.WriteLine("Error: " + ex.Message);
					writer.WriteLine("Source: " + ex.Source);
					writer.WriteLine("Stack: " + ex.StackTrace);
					if (ex.InnerException != null)
                    {
						writer.WriteLine("Inner: " + ex.InnerException.Message);
						writer.WriteLine("Inner Stack: " + ex.InnerException.StackTrace);
					}
					writer.WriteLine("---\n\n");
					writer.Close();
				}
			}
			catch {
                
			}
		}

        private static void logMessage(string msg)
        {
            try
            {
                Globals.Host.Actions.AddChatText("[" + Globals.PluginName + "] " + msg, 5);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        public static void log(LogChannels ch, string msg)
        {
            switch (ch)
            {
                case LogChannels.CH_LOG:
                    logMessage(msg);
                    break;
                case LogChannels.CH_DEBUG:
                    if (PluginCore.debug)
                        logMessage(msg);
                    break;
                case LogChannels.CH_NET:
                    if (PluginCore.netDebug)
                        logMessage(msg);
                    break;
                case LogChannels.CH_UI:
                    if (PluginCore.uiDebug)
                        logMessage(msg);
                    break;
                case LogChannels.CH_TARGET:
                    if (PluginCore.targetDebug)
                        logMessage(msg);
                    break;
            }
        }

        public static void writeToXml()
        {
            try
            {
                using (XmlWriter writer = XmlWriter.Create(SETTINGS_FILE, xmlWriterSettings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("settings");
                    writer.WriteElementString("enabled", PluginCore.pluginEnabled.ToString().ToLower());
                    writer.WriteElementString("range", PluginCore.acquireRange.ToString());
                    writer.WriteElementString("targets", PluginCore.maxTargets.ToString());
                    writer.WriteElementString("updates", PluginCore.updateFreq.ToString());
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Flush();
                    writer.Close();
                    Util.log(LogChannels.CH_UI, "Wrote settings to file.");
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        public static Boolean loadFromXml()
        {
            try
            {
                if (File.Exists(SETTINGS_FILE))
                {
                    using (XmlReader reader = XmlReader.Create(SETTINGS_FILE))
                    {
                        reader.ReadToFollowing("enabled");
                        PluginCore.pluginEnabled = reader.ReadElementContentAsBoolean();
                        reader.ReadToFollowing("range");
                        PluginCore.acquireRange = reader.ReadElementContentAsDouble();
                        reader.ReadToFollowing("targets");
                        PluginCore.maxTargets = reader.ReadElementContentAsInt();
                        reader.ReadToFollowing("updates");
                        PluginCore.updateFreq = reader.ReadElementContentAsInt();

                        reader.Close();
                    }
                    Util.log(LogChannels.CH_UI, "Loaded settings from file.");
                    return true;
                }
                return false;
            }
            catch (Exception ex) { Util.LogError(ex); return false; }
        }
	}
}
