﻿namespace OSAE
{
    using System;
    using System.Data;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Security;
    using System.Security.Policy;
    using MySql.Data.MySqlClient;
    using OSAE.General;

    /// <summary>
    /// Common helper class for common functionality
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Gets the name of the computer running the code
        /// </summary>
        public static string ComputerName
        {
            get
            {
                return Dns.GetHostName();
            }
        }

        /// <summary>
        /// Gets the installation directory of OSAE
        /// </summary>
        public static string ApiPath
        {
            get
            {
                ModifyRegistry registry = new ModifyRegistry();
                registry.SubKey = "SOFTWARE\\OSAE\\DBSETTINGS";
                return registry.Read("INSTALLDIR");
            }
        }

        public static string DBName
        {
            get
            {
                ModifyRegistry registry = new ModifyRegistry();
                registry.SubKey = "SOFTWARE\\OSAE\\DBSETTINGS";
                return registry.Read("DBNAME");
            }
        }

        public static string DBPort
        {
            get
            {
                ModifyRegistry registry = new ModifyRegistry();
                registry.SubKey = "SOFTWARE\\OSAE\\DBSETTINGS";
                return registry.Read("DBPORT");
            }
        }

        public static string DBPassword
        {
            get
            {
                ModifyRegistry registry = new ModifyRegistry();
                registry.SubKey = "SOFTWARE\\OSAE\\DBSETTINGS";
                return registry.Read("DBPASSWORD");
            }
        }

        public static string DBUsername
        {
            get
            {
                ModifyRegistry registry = new ModifyRegistry();
                registry.SubKey = "SOFTWARE\\OSAE\\DBSETTINGS";
                return registry.Read("DBUSERNAME");
            }
        }

        /// <summary>
        /// The address of the WCF server
        /// </summary>
        public static string WcfServer
        {
            get
            {
                ModifyRegistry registry = new ModifyRegistry();
                registry.SubKey = "SOFTWARE\\OSAE\\";

                return registry.Read("WCFSERVER");
            }
        }
             
        /// <summary>
        /// Gets the connection string used to connect to the OSA DB
        /// </summary>
        /// <remarks>reads from the registry each time so that if the settings change the
        /// service doesn't need to be restarted. If this creates performance issues then it
        /// can be changed over to  a static string that reads once.</remarks>
        public static string ConnectionString
        {
            get
            {
                string connectionString = string.Empty;
                
                ModifyRegistry registry = new ModifyRegistry();
                registry.SubKey = "SOFTWARE\\OSAE\\DBSETTINGS";

                connectionString = "SERVER=" + registry.Read("DBCONNECTION") + ";" +
                    "DATABASE=" + registry.Read("DBNAME") + ";" +
                    "PORT=" + registry.Read("DBPORT") + ";" +
                    "UID=" + registry.Read("DBUSERNAME") + ";" +
                    "PASSWORD=" + registry.Read("DBPASSWORD") + ";";

                return connectionString;
            }
        }

        public static string DBConnection
        {
            get
            {
                string databaseConnection = string.Empty;

                ModifyRegistry registry = new ModifyRegistry();
                registry.SubKey = "SOFTWARE\\OSAE\\DBSETTINGS";

                databaseConnection = registry.Read("DBCONNECTION");

                return databaseConnection;
            }
        }

        /// <summary>
        /// Test to see if we can get a successful connection to the DB
        /// </summary>
        /// <returns>True if connect success false otherwise</returns>
        public static DBConnectionStatus TestConnection()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(Common.ConnectionString))
                {
                    connection.Open();                    
                }
            }
            catch (Exception ex)
            {
                return new DBConnectionStatus(false, ex);
            }

            return new DBConnectionStatus(true, null);
        }

        /// <summary>
        /// CALL osae_sp_pattern_parse(pattern) and returns result
        /// This is parsing OUTPUT and not the OSA Patterns, it should be renamed...
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static string PatternParse(string pattern)
        {
            try
            {
                using (MySqlCommand command = new MySqlCommand())
                {
                    DataSet dataset = new DataSet();
                    command.CommandText = "CALL osae_sp_pattern_parse(@Pattern)";
                    command.Parameters.AddWithValue("@Pattern", pattern);
                    dataset = OSAESql.RunQuery(command);

                    if (dataset.Tables[0].Rows.Count > 0)
                    {
                        return dataset.Tables[0].Rows[0]["vInput"].ToString();
                    }
                    else
                    {
                        return pattern;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.GetLogger().AddToLog("API - PatternParse error: " + ex.Message, true);
                return string.Empty;
            }
        }

        public static string MatchPattern(string str, string sUser)
        {
            string ScriptParameter = "";
            sUser = sUser.ToUpper();
            try
            {
                str = str.ToUpper();
                str = str.TrimEnd('?', '.', '!') + " ";
                str = str.Replace(" 'S", "'S");
                str = str.Replace("YOUR ", "SYSTEM'S ");
                str = str.Replace("YOU ARE ", "SYSTEM IS ");
                str = str.Replace("ARE YOU ", "IS SYSTEM ");
                str = str.Replace("MY ", sUser + "'S ");
                str = str.Replace(" ME ", " " + sUser + " ");
                str = str.Replace("AM I ", "IS " + sUser + " ");
                str = str.Replace("I AM ",sUser +  " IS ");

                DataSet dataset = new DataSet();
                //command.CommandText = "SELECT pattern FROM osae_v_pattern WHERE `match`=@Name";
                //command.Parameters.AddWithValue("@Name", str);
                dataset = OSAESql.RunSQL("SELECT pattern FROM osae_v_pattern_match WHERE UPPER(`match`)='" + str.Replace("'", "''") + "'");

                if (dataset.Tables[0].Rows.Count > 0)
                {

                    //Since we have a match, lets execute the scripts
                    OSAEScriptManager.RunPatternScript(dataset.Tables[0].Rows[0]["pattern"].ToString(), "", "SYSTEM");
                    return dataset.Tables[0].Rows[0]["pattern"].ToString();
                }
                else
                {
                    //Replace Words with place holders and retry the pattern match
                    //example  "Please turn the main light on" becomes "Please turn the [OBJECT] [STATE]"

                    //Step 1: Break the Input into an Array to Query the Words for DB matches
                    

                    string[] words = str.Split(' ');

                    DataSet dsObjects = new DataSet();
                    foreach (String word in words)
                    {
                        dsObjects = OSAE.Common.ObjectNamesStartingWith(word.Replace("'S",""));
                        foreach (DataRow dr in dsObjects.Tables[0].Rows)
                        {
                            if (str.IndexOf(dr["object_name"].ToString().ToUpper()) > -1)
                            //return "Found " + dr["object_name"].ToString();
                            {
                                str = str.Replace(dr["object_name"].ToString().ToUpper(), "[OBJECT]");
                                if (ScriptParameter.Length > 1)
                                {
                                    ScriptParameter = ScriptParameter + ",";
                                }
                                ScriptParameter += dr["object_name"].ToString();
                                //Determine if the Object is Possessive, which would be followed by a Property
                                if (str.ToUpper().IndexOf("[OBJECT]'S") > -1)
                                {
                                    //Here We have found our Possessive Object, so we need to look for an appropriate property afterwards
                                    //So we are going to retrieve a property list and compare it to the start of theremainder of the string

                                    DataSet dsProperties = new DataSet();
                                    dsProperties = OSAEObjectPropertyManager.ObjectPropertyListGet(dr["object_name"].ToString());
                                    foreach (DataRow drProperty in dsProperties.Tables[0].Rows)
                                    {
                                        //Here we need to break the string into words to avoid partial matches
                                        int objectStartLoc = str.ToUpper().IndexOf("[OBJECT]'S");
                                        string strNewSearch = str.Substring(objectStartLoc + 11);
                                        if (strNewSearch.ToUpper().IndexOf(drProperty["property_name"].ToString().ToUpper()) > -1)
                                        {
                                            str = str.Replace("[OBJECT]'S " + drProperty["property_name"].ToString().ToUpper(), "[OBJECT]'S [PROPERTY]");
                                            //str = str.Replace(drState["state_label"].ToString().ToUpper(), "[STATE]");
                                            ScriptParameter += "," + drProperty["property_name"].ToString();
                                        }
                                    }
                                }

                                //Here We have found our Object, so we need to look for an appropriate state afterwards
                                //So we are going to retrieve a state list and compare it to the remainder of the string
                                DataSet dsStates = new DataSet();
                                dsStates = OSAEObjectStateManager.ObjectStateListGet(dr["object_name"].ToString());
                                foreach (DataRow drState in dsStates.Tables[0].Rows)
                                {
                                    //Here we need to break the string into words to avoid partial matches
                                    string replacementString = "";
                                    string[] wordArray = str.Split(new Char[] { ' ' });
                                    foreach (string w in wordArray)
                                    {
                                        if (replacementString.Length > 1)
                                        {
                                            replacementString = replacementString + " ";
                                        }
                                        if (drState["state_label"].ToString().ToUpper() == w || drState["state_name"].ToString().ToUpper() == w)
                                        {
                                            replacementString = replacementString + "[STATE]";
                                            //str = str.Replace(drState["state_label"].ToString().ToUpper(), "[STATE]");
                                            ScriptParameter += "," + drState["state_name"].ToString();
                                        }
                                        else
                                        {
                                            replacementString = replacementString + w;
                                        }
                                    }
                                    //Now that we have replaced the Object and State, Lets check for a match again
                                    //DataSet dataset = new DataSet();
                                    //command.CommandText = "SELECT pattern FROM osae_v_pattern WHERE `match`=@Name";
                                    //command.Parameters.AddWithValue("@Name", str);
                                    //dataset = OSAESql.RunQuery(command);
                                    replacementString = replacementString.Replace(" 'S", "'S");
                                    dataset = OSAESql.RunSQL("SELECT pattern FROM osae_v_pattern_match WHERE `match`='" + replacementString.Replace("'", "''") + "'");
                                    if (dataset.Tables[0].Rows.Count > 0)
                                    {
                                        //return dataset.Tables[0].Rows[0]["pattern"].ToString();
                                        //Since we have a match, lets execute the scripts
                                        OSAEScriptManager.RunPatternScript(dataset.Tables[0].Rows[0]["pattern"].ToString(), ScriptParameter, "Jabber");
                                        return dataset.Tables[0].Rows[0]["pattern"].ToString();
                                    }
                                    //break;
                                }
                                //break;
                            }
                        }
                    }
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logging.GetLogger().AddToLog("API - MatchPattern error: " + ex.Message, true);
                return string.Empty;
            }

        }



        /// <summary>
        /// Get all object names that start with a single word
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static DataSet ObjectNamesStartingWith(string pattern)
        {
                using (MySqlCommand command = new MySqlCommand())
                {
                   // DataSet dataset = new DataSet();
                   // command.CommandText = "SELECT object_name FROM osae_object WHERE UPPER(object_name) LIKE '@Pattern%' ORDER BY Length(object_name) DESC";
                  //  command.Parameters.AddWithValue("@Pattern", pattern.ToUpper());
                  //  dataset = OSAESql.RunQuery(command);
                  //  return dataset;
                    DataSet dataset = new DataSet();
                    //command.CommandText = "SELECT object_name FROM osae_object WHERE UPPER(object_name) LIKE '@Pattern%' ORDER BY Length(object_name) DESC";
                    //command.Parameters.AddWithValue("@Pattern", pattern.ToUpper());
                    dataset = OSAESql.RunSQL("SELECT object_name FROM osae_object WHERE UPPER(object_name) LIKE '" + pattern.Replace("'", "''") + "%' ORDER BY Length(object_name) DESC");
                    return dataset;
                }
        }



        public static void InitialiseLogFolder()
        {
            try
            {
                FileInfo file = new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\OSAE\Logs\");
                file.Directory.Create();
                if (OSAEObjectPropertyManager.GetObjectPropertyValue("SYSTEM", "Prune Logs").Value == "TRUE")
                {
                    string[] files = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\OSAE\Logs\");
                    foreach (string f in files)
                        File.Delete(f);
                }
            }
            catch (Exception ex)
            {
                // Exception handling should be handled inside calling application
                throw new Exception("Error getting registry settings and/or deleting logs: " + ex.Message, ex);
            }
        }

        public static string GetComputerIP()
        {
            IPHostEntry ipEntry = Dns.GetHostByName(Common.ComputerName);
            IPAddress[] addr = ipEntry.AddressList;
            return addr[0].ToString();
        }

        public static void CreateComputerObject(string sourceName)
        {
            Logging.GetLogger().AddToLog("Creating Computer object", true);
            string computerIp = Common.GetComputerIP();

            if (OSAEObjectManager.GetObjectByName(Common.ComputerName) == null)
            {                                  
                OSAEObject obj = OSAEObjectManager.GetObjectByAddress(computerIp);
                if (obj == null)
                {
                    OSAEObjectManager.ObjectAdd(Common.ComputerName, Common.ComputerName, "COMPUTER", computerIp, string.Empty, true);
                    OSAEObjectPropertyManager.ObjectPropertySet(Common.ComputerName, "Host Name", Common.ComputerName, sourceName);
                }
                else if (obj.Type == "COMPUTER")
                {
                    OSAEObjectManager.ObjectUpdate(obj.Name, Common.ComputerName, obj.Description, "COMPUTER", computerIp, obj.Container, obj.Enabled);
                    OSAEObjectPropertyManager.ObjectPropertySet(Common.ComputerName, "Host Name", Common.ComputerName, sourceName);
                }
                else
                {
                    OSAEObjectManager.ObjectAdd(Common.ComputerName + "." + computerIp, Common.ComputerName, "COMPUTER", computerIp, string.Empty, true);
                    OSAEObjectPropertyManager.ObjectPropertySet(Common.ComputerName + "." + computerIp, "Host Name", Common.ComputerName, sourceName);
                }
            }
            else
            {
                OSAEObject obj = OSAEObjectManager.GetObjectByName(Common.ComputerName);
                OSAEObjectManager.ObjectUpdate(obj.Name, obj.Name, obj.Description, "COMPUTER", computerIp, obj.Container, obj.Enabled);
                OSAEObjectPropertyManager.ObjectPropertySet(obj.Name, "Host Name", Common.ComputerName, sourceName);
            }
        }

        public static AppDomain CreateSandboxDomain(string name, string path, SecurityZone zone, Type item)
        {
            var setup = new AppDomainSetup { ApplicationBase = Common.ApiPath, PrivateBinPath = Path.GetFullPath(path) };

            var evidence = new Evidence();
            evidence.AddHostEvidence(new Zone(zone));
            var permissions = SecurityManager.GetStandardSandbox(evidence);

            StrongName strongName = item.Assembly.Evidence.GetHostEvidence<StrongName>();

            return AppDomain.CreateDomain(name, null, setup);
        }

        public static long GetJavascriptTimestamp(System.DateTime input)
        {
            return (long)input.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        public static string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }
    }
}
