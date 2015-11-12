﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OSAE.MediaCenter
{
    public class MCDevice
    {
        string pName;
        private string _type;
        private string _name;
        private string _ip;
        private int _CommandPort = 0;
        private int _StatusPort = 0;

        //OSAELog
        private static OSAE.General.OSAELog Log = new General.OSAELog();

        String mycommand;

        private StreamWriter commandStreamWriter = null;
        private StreamReader commandStreamReader = null;
        private TcpClient commandTcpClient = null;
        private NetworkStream commandSockStream = null;

        private StreamWriter statusStreamWriter = null;
        private StreamReader statusStreamReader = null;
        private TcpClient statusTcpClient = null;
        private NetworkStream statusSockStream = null;

        private List<Session> sessions = new List<Session>();

        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        public string IP
        {
            get { return _ip; }
            set { _ip = value; }
        }
        public int CommandPort
        {
            get { return _CommandPort; }
            set
            {
                _CommandPort = value;
                _StatusPort = value - 100;
            }
        }
        public int StatusPort
        {
            get { return _StatusPort; }
            set
            {
                _StatusPort = value;
                _CommandPort = value + 100;
            }
        }

        public MCDevice(string name, string pluginname)
        {
            _name = name;
            pName = pluginname;
        }


        public void EstablishInitialConnections()
        {
            EstablishCommandConnection(true);
            EstablishStatusConnection(true);
        }

        private void EstablishCommandConnection(bool initialconnection)
        {

            try
            {
                if (commandStreamReader != null)
                {
                    commandStreamReader.Close();
                }
                if (commandStreamWriter != null)
                {
                    commandStreamWriter.Close();
                }
                if (commandTcpClient != null)
                {
                    commandTcpClient.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error closing old command connections to MediaCenter device (" + _name + "): " + ex.Message);
            }

            try
            {
                if (_ip != "" && _CommandPort != 0)
                {
                    if (initialconnection) Log.Debug("Creating Command TCP Client (" + _name + "): ip-" + _ip + " commandport-" + _CommandPort);

                    commandTcpClient = new TcpClient(_ip, _CommandPort);

                    //get a network stream from server
                    commandSockStream = commandTcpClient.GetStream();

                    // create new writer and reader stream to send and receive
                    commandStreamWriter = new StreamWriter(commandSockStream);
                    commandStreamReader = new StreamReader(commandSockStream);

                    //Start listening
                    Thread commandlistenThread = new Thread(new ThreadStart(commandListen));
                    commandlistenThread.Start();

                    OSAEObjectStateManager.ObjectStateSet(_name, "ON", pName);
                }
                else
                {
                    if (initialconnection) Log.Info(_name + " - Properties not set correctly: ip-" + _ip + " networkport-" + _CommandPort + " statusport-" + _StatusPort);
                }
            }
            catch (Exception ex)
            {
                if (initialconnection) Log.Error("Error creating command connection to MediaCenter device (" + _name + "): " + ex.Message);
                OSAEObjectStateManager.ObjectStateSet(_name, "OFF", pName);
            }
        }

        private void EstablishStatusConnection(bool initialconnection)
        {

            try
            {
                if (statusStreamReader != null)
                {
                    statusStreamReader.Close();
                }
                if (statusStreamWriter != null)
                {
                    statusStreamWriter.Close();
                }
                if (statusTcpClient != null)
                {
                    statusTcpClient.Close();
                }

            }
            catch (Exception ex)
            {
                Log.Error("Error closing old status connections to MediaCenter device (" + _name + "): " + ex.Message);
            }

            try
            {
                if (_ip != "" && _StatusPort != 0)
                {
                    if (initialconnection) Log.Debug("Creating Status TCP Client (" + _name + "): ip-" + _ip + " statusport-" + _StatusPort);
                    statusTcpClient = new TcpClient(_ip, _StatusPort);

                    //get a network stream from server
                    statusSockStream = statusTcpClient.GetStream();

                    // create new writer and reader stream to send and receive
                    statusStreamWriter = new StreamWriter(statusSockStream);
                    statusStreamReader = new StreamReader(statusSockStream);

                    //Start listening
                    Thread statuslistenThread = new Thread(new ThreadStart(statusListen));
                    statuslistenThread.Start();

                    OSAEObjectStateManager.ObjectStateSet(_name, "ON", pName);
                }
                else
                {
                    if (initialconnection) Log.Info(_name + " - Properties not set correctly: ip-" + _ip + " networkport-" + _CommandPort + " statusport-" + _StatusPort);
                }
            }
            catch (Exception ex)
            {
                if (initialconnection) Log.Error("Error creating connection to MediaCenter device (" + _name + "): " + ex.Message);
                OSAEObjectStateManager.ObjectStateSet(_name, "OFF", pName);
            }
        }

        public Boolean CheckConnections()
        {
            Boolean connected = false;

            //try to re-establish any connections that are not connected
            if (statusTcpClient == null)
            {
                EstablishStatusConnection(false);
            }
            else
            {
                if (!statusTcpClient.Connected)
                {
                    EstablishStatusConnection(false);
                }
            }

            if (commandTcpClient == null)
            {
                EstablishCommandConnection(false);
            }
            else
            {
                if (!commandTcpClient.Connected)
                {
                    EstablishCommandConnection(false);
                }
            }

            // now return the status
            if (commandTcpClient == null || statusTcpClient == null)
            {
                connected = false;
            }
            else
            {
                if (commandTcpClient.Connected && statusTcpClient.Connected)
                {
                    connected = true;
                }
                else
                {
                    connected = false;
                }
            }

            return connected;
        }

        public void ProcessCommand(String method_name, String parameter_1, String parameter_2)
        {
            switch (method_name)
            {
                case "PLAY":
                    Log.Info("PLAY event triggered (" + _name + ")");
                    SendCommand_Network("playrate Play");
                    break;
                case "STOP":
                    Log.Info("STOP event triggered (" + _name + ")");
                    SendCommand_Network("playrate Stop");
                    break;
                case "PAUSE":
                    Log.Info("STOP event triggered (" + _name + ")");
                    SendCommand_Network("playrate Pause");
                    break;
                case "MUTE":
                    Log.Info("MUTE event triggered (" + _name + ")");
                    SendCommand_Network("volume Mute");
                    break;
                case "UNMUTE":
                    Log.Info("UNMUTE event triggered (" + _name + ")");
                    SendCommand_Network("volume UnMute");
                    break;
                case "SETVOLUME":
                    mycommand = "volume " + parameter_1;
                    Log.Info("SETVOLUME event triggered (" + _name + "), command to send=" + mycommand);
                    SendCommand_Network(mycommand);
                    break;
                case "NOTIFY":
                    mycommand = @"msgbox ""OSA"" """ + parameter_1 + @""" " + parameter_2;
                    Log.Info("NOTIFY event triggered (" + _name + "), command to send=" + mycommand);
                    SendCommand_Network(mycommand);
                    break;
                case "GOTO":
                    mycommand = "goto " + parameter_1;
                    Log.Info("GOTO event triggered (" + _name + "), command to send=" + mycommand);
                    SendCommand_Network(mycommand);
                    break;
                case "SEND_COMMAND":
                    mycommand = parameter_1;
                    Log.Info("SEND_COMMAND event triggered (" + _name + "), command to send=" + mycommand);
                    SendCommand_Network(mycommand);
                    break;
                case "TEST_REESTABLISHCONNECTIONS":
                    EstablishInitialConnections();                    
                    break;

            }

        }

        public void SendCommand_Network(String command)
        {

            try
            {
                int length = command.Length;
                length++;
                int total = length + 16;
                char code = (char)length;

                // build up packet header and rest of command - followed by <CR> (chr(13))
                //
                string line = command;

                if (commandTcpClient == null)
                {
                    EstablishCommandConnection(false);
                }

                if (!commandTcpClient.Connected)
                {
                    EstablishCommandConnection(false);
                }

                if (commandTcpClient.Connected)
                {
                    // send command to receiver
                    //
                    commandStreamWriter.WriteLine(line);
                    commandStreamWriter.Flush();
                    Log.Info("Sent command (" + _name + "): " + line);
                }

            }
            catch (Exception e)
            {
                Log.Error("Error sending command (" + _name + "): " + e.Message);
            }
        }

        private void commandListen()
        {
            while (commandTcpClient.Connected)
            {
                try
                {
                    string curLine = commandStreamReader.ReadLine();
                    Log.Info("Received command data from MCDevice(" + _name + "): " + curLine);

                }
                catch (Exception ex)
                {
                    Log.Error("Error reading from command stream (" + _name + "): " + ex.Message);
                }
            }
        }

        private int findSession(int sessionnum)
        {
            int index = -1;
            foreach (Session s in sessions)
            {
                index++;
                if (s.SessionNumber == sessionnum)
                    return index;
            }
            return -1;
        }


        private void statusListen()
        {
            int cursessionindex = -1;
            int currsessionnum = -1;
            DateTime sessiontime = DateTime.Now;
            System.TimeSpan sessioninfoduration = new System.TimeSpan(0, 0, 0, 15); //session info occurs within 15 seconds otherwise we can't identify what session it belongs to
            System.TimeSpan sessionkeepduration = new System.TimeSpan(0, 1, 0, 0); //keep session for 1 hour after last update

            while (statusTcpClient.Connected)
            {
                try
                {
                    string curLine = statusStreamReader.ReadLine();

                    if (!curLine.Equals(""))
                    {
                        if (curLine.IndexOf("TrackTime=") > -1)
                        {
                            int tracktime;
                            bool isNum = Int32.TryParse(curLine.Replace("TrackTime=", ""), out tracktime);
                            if (isNum)
                            {
                                if (tracktime % 30 == 0 || tracktime == 1)
                                {
                                    Log.Debug("Received status data from MCDevice(" + _name + "): " + curLine);
                                }

                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Current Position", curLine.Replace("TrackTime=", ""), pName);
                            }
                            else
                            {
                                Log.Error("Error parsing tracktime (" + _name + "): ");
                            }


                        }
                        else
                        {
                            Log.Debug("Received status data from MCDevice(" + _name + "): " + curLine);


                            if (curLine.IndexOf("StartSession=") > -1 || curLine.IndexOf("204 Connected (") > -1)
                            {
                                int sessionnum;
                                bool isNum = Int32.TryParse(curLine.Replace("StartSession=", ""), out sessionnum);
                                if (isNum)
                                {
                                    currsessionnum = sessionnum;
                                }
                                else
                                {
                                    currsessionnum = 0;
                                }

                                Session currsession = new Session(sessionnum);
                                sessiontime = DateTime.Now;
                                sessions.Add(currsession);
                            }

                            if (curLine.IndexOf("EndSession=") > -1)
                            {
                                int sessionnum;
                                bool isNum = Int32.TryParse(curLine.Replace("EndSession=", ""), out sessionnum);
                                if (isNum)
                                {
                                    int foundsession = findSession(sessionnum);
                                    if (foundsession != -1)
                                    {
                                        sessions[foundsession].active = false;
                                    }
                                    else // if we can't find that session then it could have been already in progress and got assigned session number 0 when we started.. so mark 0 and not active
                                    {
                                        foundsession = findSession(0);
                                        if (foundsession != -1)
                                        {
                                            sessions[foundsession].active = false;
                                        }
                                    }

                                }
                            }

                            // session items
                            if (DateTime.Now <= sessiontime.Add(sessioninfoduration))
                            {
                                cursessionindex = findSession(currsessionnum);
                                if (cursessionindex != -1)
                                {
                                    if (curLine.IndexOf("Play=True") > -1)
                                        sessions[cursessionindex].Play = true;
                                    if (curLine.IndexOf("Stop=True") > -1)
                                        sessions[cursessionindex].Stop = true;

                                    //session types
                                    if (curLine.IndexOf("Recording=True") > -1)
                                        sessions[cursessionindex].SessionType = "Recording";
                                    if (curLine.IndexOf("PVR=True") > -1)
                                        sessions[cursessionindex].SessionType = "PVR";
                                    if (curLine.IndexOf("StreamingContentVideo=True") > -1)
                                        sessions[cursessionindex].SessionType = "StreamingContentVideo";
                                    if (curLine.IndexOf("StreamingContentAudio=True") > -1)
                                        sessions[cursessionindex].SessionType = "StreamingContentAudio";
                                    if (curLine.IndexOf("TVTuner=True") > -1)
                                        sessions[cursessionindex].SessionType = "TVTuner";
                                    if (curLine.IndexOf("DVD=True") > -1)
                                        sessions[cursessionindex].SessionType = "DVD";
                                    if (curLine.IndexOf("CD=True") > -1)
                                        sessions[cursessionindex].SessionType = "CD";
                                    if (curLine.IndexOf("Radio=True") > -1)
                                        sessions[cursessionindex].SessionType = "Radio";

                                    if (curLine.IndexOf("MediaName=") > -1)
                                        sessions[cursessionindex].MediaName = curLine.Replace("MediaName=", "");
                                    if (curLine.IndexOf("MediaTime") > -1)
                                        sessions[cursessionindex].MediaTimeS = curLine.Replace("MediaTime=", "");
                                    if (curLine.IndexOf("TrackNumber") > -1)
                                        sessions[cursessionindex].TrackNumber = curLine.Replace("TrackNumber=", "");
                                    if (curLine.IndexOf("TrackDuration") > -1)
                                        sessions[cursessionindex].MediaTimeS = curLine.Replace("TrackDuration=", "");
                                }
                            }
                            else
                            {
                                // somehow we only need to update for non recording sessions (also if the curr session is a recording... find the last active non recording and update that)
                                cursessionindex = findSession(currsessionnum);
                                if (cursessionindex != -1)
                                {
                                    if (curLine.IndexOf("MediaName=") > -1)
                                        sessions[cursessionindex].MediaName = curLine.Replace("MediaName=", "");
                                    if (curLine.IndexOf("MediaTime") > -1)
                                        sessions[cursessionindex].MediaTimeS = curLine.Replace("MediaTime=", "");
                                    if (curLine.IndexOf("TrackNumber") > -1)
                                        sessions[cursessionindex].TrackNumber = curLine.Replace("TrackNumber=", "");

                                    if (curLine.IndexOf("TrackDuration") > -1)
                                        sessions[cursessionindex].MediaTimeS = curLine.Replace("TrackDuration=", "");

                                }
                            }


                            // Other Statuses (states, last screen, volume, last keypress, dialog visible, last dialog)

                            if (curLine.IndexOf("Play=True") > -1)
                            {
                                OSAEObjectStateManager.ObjectStateSet(_name, "Playing", pName);
                            }

                            if (curLine.IndexOf("Stop=True") > -1)
                            {
                                OSAEObjectStateManager.ObjectStateSet(_name, "Stopped", pName);
                            }

                            if (curLine.IndexOf("Pause=True") > -1)
                            {
                                OSAEObjectStateManager.ObjectStateSet(_name, "Paused", pName);
                            }

                            if (curLine.IndexOf("Pause=True") > -1)
                            {
                                OSAEObjectStateManager.ObjectStateSet(_name, "Paused", pName);
                            }

                            if (curLine.IndexOf("Mute=True") > -1)
                            {
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Mute", "TRUE", pName);
                            }
                            if (curLine.IndexOf("Mute=False") > -1)
                            {
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Mute", "FALSE", pName);
                            }

                            if (curLine.IndexOf("Volume=") > -1)
                            {
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Volume", curLine.Replace("Volume=", ""), pName);
                            }

                            if (curLine.IndexOf("FS_") > -1)
                            {
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Last Screen", curLine.Replace("=True", "").Replace("FS_", ""), pName);
                            }

                            if (curLine.IndexOf("KeyPress=") > -1)
                            {
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Last Key Press", curLine.Replace("KeyPress=", ""), pName);
                            }

                            if (curLine.IndexOf("DialogVisible=True") > -1)
                            {
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Dialog Visible", "TRUE", pName);
                            }
                            if (curLine.IndexOf("DialogVisible=False") > -1)
                            {
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Dialog Visible", "FALSE", pName);
                            }



                            // now set recording info, media info, media type based on active sessions
                            // also clean up sessions and time out any that appear to have been left open and orphaned

                            Boolean Recording = false;
                            String RecordingName = "";
                            String RecordingChannel = "";
                            int RecordingCount = 0;

                            String MediaName = "";
                            String MediaTime = "0";
                            String MediaType = "";

                            cursessionindex = -1;
                            foreach (Session s in sessions)
                            {
                                cursessionindex++;

                                Log.Debug("looping sessions for " + _name + " (" + sessions.Count().ToString() + ")-  active:" + s.active + ", sessionnum:" + s.SessionNumber + ", type:" + s.SessionType + ", lastupdate:" + s.lastupdate + ", medianame:" + s.MediaName + ", play:" + s.Play.ToString() + ", stop:" + s.Stop.ToString());

                                if (s.active)
                                {
                                    if (s.SessionType.Equals("Recording"))
                                    {
                                        Recording = true;
                                        RecordingChannel = RecordingChannel + s.TrackNumber + ", ";
                                        if (s.SessionNumber == 0) // fix issue when plugin first started it will give us the last media played and not the current recording name on start of the status connection
                                        {
                                            MediaName = s.MediaName;
                                        }
                                        else
                                        {
                                            RecordingName = RecordingName + s.MediaName + ", ";
                                        }
                                        RecordingCount++;
                                    }
                                    else
                                    {
                                        MediaName = s.MediaName;
                                        MediaTime = s.MediaTimeS;
                                        MediaType = s.SessionType;
                                    }
                                }
                                else
                                {
                                    if (DateTime.Now >= s.lastupdate.Add(sessionkeepduration) && cursessionindex != sessions.Count() - 1) // if it is not active and hasn't been updated in an hour and is not the last session then remove it
                                    {
                                        sessions.Remove(s);
                                    }
                                }
                            }

                            if (Recording)
                            {
                                RecordingName = RecordingName.TrimEnd(',', ' ');
                                RecordingChannel = RecordingChannel.TrimEnd(',', ' ');

                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Recording", "TRUE", pName);
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Recording Name", RecordingName, pName);
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Recording Channel", RecordingChannel, pName);
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Number of Recordings", RecordingCount.ToString(), pName);
                            }
                            else
                            {
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Recording", "FALSE", pName);
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Recording Name", "", pName);
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Recording Channel", "", pName);
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Number of Recordings", "0", pName);
                            }

                            if (!MediaName.Equals(""))
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Media Name", MediaName, pName);
                            if (!MediaTime.Equals("0"))
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Media Time", MediaTime, pName);
                            if (!MediaType.Equals(""))
                                OSAEObjectPropertyManager.ObjectPropertySet(_name, "Media Type", MediaType, pName);

                        }
                    }
                    else
                    {
                        Log.Debug("Current line is blank - curLine>" + curLine + "<");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error reading from status stream (" + _name + "): " + ex.Message);
                }
            }
        }

        public void CloseConnections()
        {
            commandStreamReader.Close();
            commandStreamWriter.Close();
            commandTcpClient.Close();

            statusStreamReader.Close();
            statusStreamWriter.Close();
            statusTcpClient.Close();
        }
    }
}
