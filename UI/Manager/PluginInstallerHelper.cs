﻿namespace Manager_WPF
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.ServiceModel;
    using System.Windows.Forms;
    using NetworkCommsDotNet;
    using OSAE;

    /// <summary>
    /// Helper class used to install new plugins
    /// </summary>
    public partial class PluginInstallerHelper
    {
        //OSAELog
        private OSAE.General.OSAELog Log = new OSAE.General.OSAELog();

        public  void InstallPlugin(string filepath)
        {
            try
            {
                string ErrorText = string.Empty;

                InstallPlugin pi = new InstallPlugin(filepath);
                pi.ShowDialog();

                if (pi.install)
                {
                    if (!InstallPlugin(filepath, ref ErrorText))
                    {
                        MessageBox.Show("Package was not successfully installed.");
                    }

                    else if (!string.IsNullOrEmpty(ErrorText))
                    {
                        MessageBox.Show("Package installed.");

                    }

                    else
                    {
                        MessageBox.Show("Package installed.");

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error installing plugin: " + ex.Message);
            }
        }

        public bool InstallPlugin(string PluginPackagePath, ref string ErrorText)
        {
            string exePath = Path.GetDirectoryName(Application.ExecutablePath);
            if (Directory.Exists(exePath + "/tempDir/"))
            {
                Directory.Delete(exePath + "/tempDir/", true);
            }

            PluginDescription desc = new PluginDescription();
            string tempfolder = exePath + "/tempDir/";
            string zipFileName = Path.GetFullPath(PluginPackagePath);
            string DescPath = null;

            bool NoError = true;

            ICSharpCode.SharpZipLib.Zip.FastZip fastZip = new ICSharpCode.SharpZipLib.Zip.FastZip();
            try
            {
                fastZip.ExtractZip(zipFileName, tempfolder, null);
                // find all included plugin descriptions and install the plugins
                List<string> osapdFiles = new List<string>();
                List<string> sqlFiles = new List<string>();

                string[] pluginFile = Directory.GetFiles(tempfolder, "*.osapd", SearchOption.TopDirectoryOnly);
                osapdFiles.AddRange(pluginFile);
                string[] sqlFile = Directory.GetFiles(tempfolder, "*.sql", SearchOption.TopDirectoryOnly);
                sqlFiles.AddRange(sqlFile);

                if (osapdFiles.Count == 0)
                {
                    MessageBox.Show("No plugin description files found.");
                    return false;
                }

                if (osapdFiles.Count > 1)
                {
                    MessageBox.Show("More than one plugin description file found.");
                    return false;
                }
                if (osapdFiles.Count == 1)
                {

                    DescPath = osapdFiles[0];
                }

                if (!string.IsNullOrEmpty(DescPath))
                {
                    desc.Deserialize(DescPath);

                    //NoError = desc.VerifyInstall(ref ErrorText);

                    //uninstall previous plugin and delete the folder
                    if (UninstallPlugin(desc))
                    {
                        MessageBox.Show("Starting 4.");
                        // get the plugin folder path
                        string pluginFolder = desc.Path;
                        if (!string.IsNullOrEmpty(pluginFolder))  //only extract valid plugins
                        {
                            string[] files = System.IO.Directory.GetFiles(tempfolder);

                            string ConnectionString = string.Format("Uid={0};Pwd={1};Server={2};Port={3};Database={4};allow user variables=true",
                                Common.DBUsername, Common.DBPassword, Common.DBConnection, Common.DBPort, Common.DBName);
                            MessageBox.Show("Connecting to DB using:  " + Common.DBUsername + "  " + Common.DBPassword + "  " + Common.DBConnection + "  " + Common.DBPort + "  " + Common.DBName);
                            MySql.Data.MySqlClient.MySqlConnection connection = new MySql.Data.MySqlClient.MySqlConnection(ConnectionString);
                            connection.Open();
                            foreach (string s in sqlFile)
                            {
                                try
                                {

                                    MySql.Data.MySqlClient.MySqlScript script = new MySql.Data.MySqlClient.MySqlScript(connection, File.ReadAllText(s));
                                    script.Execute();
                                }
                                catch (Exception ex)
                                {
                                    this.Log.Error("Error running sql script: " + s, ex);
                                }
                            }

                            System.IO.Directory.Move(tempfolder, exePath + "/Plugins/" + pluginFolder);

                            //Check if we are running a x64 bit architecture (This is a silly way to do it since I am not sure if every 64 bit machine has this directory...)
                            bool is64bit = Environment.Is64BitOperatingSystem;

                            //Do a check for any x64 assemblies, and prompt the user to install them if they are running a 64 bit machine
                            if (is64bit && (desc.x64Assemblies.Count > 0))
                            {
                                /* x64 assemblies generally have the same name as their x32 counterparts when referenced by the OSA app
                                 * however they are packaged as "filename.ext.x64" so we will replace the 32bit file which is installed by
                                 * default with the 64bit versioin with the same filename.ext
                                 */

                                if (MessageBox.Show(
                                    "You are running an x64 architecture and this plugin has specific assemblies built for 64bit machines." +
                                    " It is highly recommended that you install the 64bit versions to ensure proper compatibility",
                                    "Install 64bit Assemblies?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                {
                                    //Install the 64bit assemblies over the 32 bit ones...
                                    string[] x64files = System.IO.Directory.GetFiles(exePath + "/Plugins/" + pluginFolder, "*.x64");

                                    foreach (string str in x64files)
                                    {
                                        string destFile = System.IO.Path.Combine(exePath + "/Plugins/" + pluginFolder + "/", System.IO.Path.GetFileNameWithoutExtension(str));
                                        //Copy it to the new destination overwriting the old file if it exists
                                        System.IO.File.Copy(str, destFile, true);
                                    }
                                }
                            }

                            //Delete all the files with .x64 extensions since they aren't needed anymore
                            string[] delfiles = System.IO.Directory.GetFiles(exePath + "/Plugins/" + pluginFolder, "*.x64");
                            foreach (string str in delfiles)
                                System.IO.File.Delete(str);

                            this.Log.Info("Sending message to service to load plugin.");
                            
                        }
                    }
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("catch: " + ex.Message);
                return false;
            }
                if (Directory.Exists(exePath + "/tempDir/"))
                {
                    deleteFolder(exePath + "/tempDir/");
                }

                OSAEMethodManager.MethodQueueAdd("SERVICE-" + Common.ComputerName, "RELOAD PLUGINS", "", "", "Plugin Installer");
            return NoError;
        }

        /// <summary>
        /// Unistalls a plugin
        /// </summary>
        /// <param name="desc">The information about the plugin to be deleted</param>
        /// <returns>true if deleted false otherwise</returns>
        public  bool UninstallPlugin(PluginDescription desc)
        {
            bool returnValue = false;

            string ip = "localhost";
           
            if (ip == "localhost")
                ip = Common.LocalIPAddress();
            NetworkComms.SendObject("Plugin", ip, 10051, desc.Type + "|False");

            Thread.Sleep(2000);

            string exePath = Path.GetDirectoryName(Application.ExecutablePath);
            string pluginFolder = exePath + "/Plugins/" + desc.Path;
            if (Directory.Exists(pluginFolder))
            {
                if (deleteFolder(pluginFolder))
                    returnValue = true;
                else
                    returnValue = false;

                if (Directory.Exists(pluginFolder))
                    returnValue = false;
            }
            else
                returnValue = true;
            return returnValue;
        }

        /// <summary>
        /// Deletes a folder and its contents on disk
        /// </summary>
        /// <param name="FolderName">The folder to be deleted</param>
        public bool deleteFolder(string FolderName)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(FolderName);
                foreach (FileInfo fi in dir.GetFiles())
                {
                    fi.Delete();
                }

                foreach (DirectoryInfo di in dir.GetDirectories())
                {
                    deleteFolder(di.FullName);

                }

                dir.Delete(true);
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
