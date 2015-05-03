﻿using System;
using System.Collections.Generic;
using agsXMPP;

namespace OSAE.Jabber
{
    public class Jabber : OSAEPluginBase
    {
        XmppClientConnection xmppCon = new XmppClientConnection();
        string gAppName;
        bool shuttingDown = false;
        Boolean gDebug = false;
        private OSAE.General.OSAELog Log = new General.OSAELog();
        private agsXMPP.protocol.client.Message oldMmsg;

        public override void RunInterface(string pluginName)
        {
            gAppName = pluginName;
            if (OSAEObjectManager.ObjectExists(gAppName))
                Log.Info("Found the Jabber plugin's Object (" + gAppName + ")");

            try
            {
                gDebug = Convert.ToBoolean(OSAEObjectPropertyManager.GetObjectPropertyValue(gAppName, "Debug").Value);
            }
            catch
            {
                Log.Info("The JABBER Object Type seems to be missing the Debug Property!");
            }
            Log.Info("Debug Mode Set to " + gDebug);

            OwnTypes();

            // Subscribe to Events
            xmppCon.OnLogin += new ObjectHandler(xmppCon_OnLogin);
            xmppCon.OnRosterStart += new ObjectHandler(xmppCon_OnRosterStart);
            xmppCon.OnRosterEnd += new ObjectHandler(xmppCon_OnRosterEnd);
            xmppCon.OnRosterItem += new XmppClientConnection.RosterHandler(xmppCon_OnRosterItem);
            xmppCon.OnPresence += new agsXMPP.protocol.client.PresenceHandler(xmppCon_OnPresence);
            xmppCon.OnAuthError += new XmppElementHandler(xmppCon_OnAuthError);
            xmppCon.OnError += new ErrorHandler(xmppCon_OnError);
            xmppCon.OnClose += new ObjectHandler(xmppCon_OnClose);
            xmppCon.OnMessage += new agsXMPP.protocol.client.MessageHandler(xmppCon_OnMessage);

            connect();
        }

        public void OwnTypes()
        {
            //Added the follow to automatically own Speech Base types that have no owner.
            OSAEObjectType oType = OSAEObjectTypeManager.ObjectTypeLoad("JABBER");

            if (oType.OwnedBy == "")
            {
                OSAEObjectTypeManager.ObjectTypeUpdate(oType.Name, oType.Name, oType.Description, gAppName, oType.BaseType, oType.Owner, oType.SysType, oType.Container, oType.HideRedundant);
                Log.Info("Jabber Plugin took ownership of the JABBER Object Type.");
            }
            else
            {
                Log.Info("Jabber Plugin correctly owns the JABBER Object Type.");
            }
        }

        public override void ProcessCommand(OSAEMethod method)
        {
            try
            {
                //basically just need to send parameter two to the contact in parameter one with sendMessage();
                //Process incomming command
                string to = "";
                if (gDebug) Log.Debug("Process command: " + method.MethodName);
                if (gDebug) Log.Debug("Message: " + method.Parameter2);
                OSAEObjectProperty prop = OSAEObjectPropertyManager.GetObjectPropertyValue(method.Parameter1, "JabberID");

                if(prop != null)
                    to = prop.Value;
                else
                    to = method.Parameter1;

                if (to == "") to = method.Parameter1;

                if (gDebug) Log.Debug("To: " + to);

                switch (method.MethodName)
                {
                    case "SEND MESSAGE":
                        sendMessage(Common.PatternParse(method.Parameter2), to);
                        break;

                    case "SEND FROM LIST":
                        //Speech List here should not be hard coded, but I understand we only have 2 parameters to work with...
                        string speechList = method.Parameter2.Substring(0,method.Parameter2.IndexOf(","));
                        string listItem = method.Parameter2.Substring(method.Parameter2.IndexOf(",") + 1, method.Parameter2.Length - (method.Parameter2.IndexOf(",")+ 1));
                        if (gDebug) Log.Debug("List = " + speechList + "   Item=" + listItem);
                        sendMessage(Common.PatternParse(OSAEObjectPropertyManager.ObjectPropertyArrayGetRandom(speechList, listItem)), to);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error processing command ", ex);
            }
        }

        public override void Shutdown()
        {
            shuttingDown = true;
            xmppCon.Close();
            Log.Info("Shutdown!");
        }

        void xmppCon_OnMessage(object sender, agsXMPP.protocol.client.Message msg)
        {
            // ignore empty messages (events)
            if (msg.Body == null) return;

            if (msg.Type == agsXMPP.protocol.client.MessageType.error) return;

            if (oldMmsg == msg) return;

            oldMmsg = msg;

            if (gDebug) Log.Debug(String.Format("OnMessage from: {0} type: {1}", msg.From.Bare, msg.Type.ToString()));
            if (gDebug) Log.Debug("INPUT: " + msg.Body);
            string pattern = Common.MatchPattern(msg.Body);
            if (pattern == string.Empty)
                if (gDebug) Log.Debug("INPUT: No Matching Pattern found!" );
        }

        void xmppCon_OnClose(object sender)
        {
            Log.Info("OnClose Connection Closed");
            if (!shuttingDown)
            {
                Log.Info("Connection Closed unexpectedly.  Attempting Reconnect...");
                connect();
            }
        }

        void xmppCon_OnError(object sender, Exception ex)
        {
            Log.Error("OnError");
        }

        void xmppCon_OnAuthError(object sender, agsXMPP.Xml.Dom.Element e)
        {
            Log.Error("OnAuthError");
        }

        void xmppCon_OnPresence(object sender, agsXMPP.protocol.client.Presence pres)
        {
            Log.Info(String.Format("Received Presence from: {0} show: {1} status: {2}", pres.From.ToString(), pres.Show.ToString(), pres.Status));

            OSAEObjectCollection objects = OSAEObjectManager.GetObjectsByType("PERSON");

            foreach (OSAEObject oObj in objects)
            {
                OSAEObject obj = OSAEObjectManager.GetObjectByName(oObj.Name);

                if (OSAEObjectPropertyManager.GetObjectPropertyValue(obj.Name, "JabberID").Value == pres.From.Bare)
                {
                    if (pres.Show.ToString() == "away")
                        OSAEObjectPropertyManager.ObjectPropertySet(obj.Name, "JabberStatus", "Idle", "Jabber");
                    else if (pres.Show.ToString() == "NONE")
                        OSAEObjectPropertyManager.ObjectPropertySet(obj.Name, "JabberStatus", "Online", "Jabber");
                    break;
                }
            }
        }

        void xmppCon_OnRosterItem(object sender, agsXMPP.protocol.iq.roster.RosterItem item)
        {
            bool found = false;
            Log.Info(String.Format("Received Contact {0}", item.Jid.Bare));

            OSAEObjectCollection objects = OSAEObjectManager.GetObjectsByType("PERSON");

            foreach (OSAEObject oObj in objects)
            {
                OSAEObject obj = OSAEObjectManager.GetObjectByName(oObj.Name);
                if (OSAEObjectPropertyManager.GetObjectPropertyValue(obj.Name, "JabberID").Value == item.Jid.Bare)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                OSAEObjectManager.ObjectAdd(item.Jid.Bare, "Discovered Jabber contact", "PERSON", "", "Unknown", true);
                OSAEObjectPropertyManager.ObjectPropertySet(item.Jid.Bare, "JabberID", item.Jid.Bare, "Jabber");
            }
        }

        void xmppCon_OnRosterEnd(object sender)
        {
            if (gDebug) Log.Debug("OnRosterEnd");

            // Send our own presence to teh server, so other epople send us online
            // and the server sends us the presences of our contacts when they are
            // available
            xmppCon.SendMyPresence();
        }

        void xmppCon_OnRosterStart(object sender)
        {
            if (gDebug) Log.Debug("OnRosterStart");
        }

        void xmppCon_OnLogin(object sender)
        {
            if (gDebug) Log.Debug("OnLogin");
        }

        private void connect()
        {
            Jid jidUser = new Jid(OSAEObjectPropertyManager.GetObjectPropertyValue(gAppName, "Username").Value);

            xmppCon.Username = jidUser.User;
            xmppCon.Server = jidUser.Server;
            xmppCon.Password = OSAEObjectPropertyManager.GetObjectPropertyValue(gAppName, "Password").Value;
            xmppCon.AutoResolveConnectServer = true;
            Log.Info("Connecting to: " + xmppCon.Server + " as user: " + xmppCon.Username);

            try
            {
                xmppCon.Open();
            }
            catch (Exception ex)
            {
                Log.Error("Error connecting: ", ex);
            }
        }

        private void sendMessage(string message, string contact)
        {
            if (gDebug) Log.Debug("OUTPUT: '" + message + "' to " + contact);
            // Send a message
            agsXMPP.protocol.client.Message msg = new agsXMPP.protocol.client.Message();
            msg.Type = agsXMPP.protocol.client.MessageType.chat;
            msg.To = new Jid(contact);
            msg.Body = message;

            xmppCon.Send(msg);
        }
    }
}
