﻿using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using OSAE;

public partial class screens : System.Web.UI.Page
{
    private int restPort = 8732;
    public string gScreen;
    private OSAEImageManager imgMgr = new OSAEImageManager();
    private string currentuser;
    protected HiddenField authkey2 = new HiddenField();

    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["Username"] == null) Response.Redirect("~/Default.aspx");
        currentuser = Session["Username"].ToString();
        int objSet = OSAEAdminManager.GetAdminSettingsByName("Screen Trust");
        int tLevel = Convert.ToInt32(Session["TrustLevel"].ToString());
        if (tLevel < objSet) Response.Redirect("~/permissionError.aspx");
        SetSessionTimeout();
        getRestPort();
        hdnUserTrust.Value = Session["TrustLevel"].ToString();
        debuglabel.Text = Session["TrustLevel"].ToString();
        authkey2.ID = "authkey2";
        authkey2.ClientIDMode = ClientIDMode.Static;
        authkey2.Value = OSAESecurity.generateCurrentAuthKey(currentuser);
        UpdatePlaceholder.Controls.Add(authkey2);
        gScreen = Request.QueryString["id"];
        
        try
        {
            OSAEObject screen = OSAEObjectManager.GetObjectByName(gScreen);
            //List<OSAEScreenControl> controls = OSAEScreenControlManager.GetScreenControls(gScreen);
            OSAEObjectCollection screenObjects = OSAEObjectManager.GetObjectsByContainer(gScreen);
            string sImg = OSAEObjectPropertyManager.GetObjectPropertyValue(gScreen, "Background Image").Value.ToString();
            OSAEImage img = imgMgr.GetImage(sImg);
            imgBackground.ImageUrl = "~/ImageHandler.ashx?id=" + img.ID;
            foreach (OSAEObject obj in screenObjects)
                LoadControl(obj);
        }
        catch
        { return; }
    }

    private void LoadControl(OSAEObject obj)
    {
        #region State Image
        if (obj.Type == "CONTROL STATE IMAGE")
        {
            // Create instance of the UserControl SimpleControl
            ASP.ctrlStateImage ctrl = (ASP.ctrlStateImage)LoadControl("~/controls/ctrlStateImage.ascx");
            // Set the Public Properties
            ctrl.screenObject = OSAEObjectManager.GetObjectByName(obj.Name);
            UpdatePlaceholder.Controls.Add(ctrl);
            //stateImages.Add(ctrl);
        }
        #endregion

        #region Click Image
        else if (obj.Type == "CONTROL CLICK IMAGE")
        {
            // Create instance of the UserControl SimpleControl
            ASP.ctrlClickImage ctrl = (ASP.ctrlClickImage)LoadControl("~/controls/ctrlClickImage.ascx");
            // Set the Public Properties
            ctrl.screenObject = OSAEObjectManager.GetObjectByName(obj.Name);
            StaticPlaceholder.Controls.Add(ctrl);
        }
        #endregion

        #region Navigation Image
        else if (obj.Type == "CONTROL NAVIGATION IMAGE")
        {
            // Create instance of the UserControl SimpleControl
            ASP.ctrlNavigationImage ctrl = (ASP.ctrlNavigationImage)LoadControl("~/controls/ctrlNavigationImage.ascx");
            // Set the Public Properties
            ctrl.screenObject = OSAEObjectManager.GetObjectByName(obj.Name);
            StaticPlaceholder.Controls.Add(ctrl);
        }
        #endregion

        #region Property Label
        else if (obj.Type == "CONTROL PROPERTY LABEL")
        {
            // Create instance of the UserControl SimpleControl
            ASP.ctrlPropertyLabel ctrl = (ASP.ctrlPropertyLabel)LoadControl("~/controls/ctrlPropertyLabel.ascx");
            // Set the Public Properties
            ctrl.screenObject = OSAEObjectManager.GetObjectByName(obj.Name);
            UpdatePlaceholder.Controls.Add(ctrl);
        }
        #endregion

        #region Timer Label
        else if (obj.Type == "CONTROL TIMER LABEL")
        {
            // Create instance of the UserControl SimpleControl
            ASP.ctrlTimerLabel ctrl = (ASP.ctrlTimerLabel)LoadControl("~/controls/ctrlTimerLabel.ascx");
            // Set the Public Properties
            ctrl.screenObject = OSAEObjectManager.GetObjectByName(obj.Name);
            UpdatePlaceholder.Controls.Add(ctrl);
        }
        #endregion

        #region Camera Viewer
        else if (obj.Type == "CONTROL CAMERA VIEWER")
        {
            // Create instance of the UserControl SimpleControl
            ASP.VideoStreamViewer ctrl = (ASP.VideoStreamViewer)LoadControl("~/controls/VideoStreamViewer.ascx");
            // Set the Public Properties
            ctrl.screenObject = OSAEObjectManager.GetObjectByName(obj.Name);
            StaticPlaceholder.Controls.Add(ctrl);
        }
        #endregion

        #region User Control
        else if (obj.BaseType == "USER CONTROL")
        {
            string sUCType = obj.Property("Control Type").Value;
            string ucName = sUCType.Replace("USER CONTROL ", "");
            // Create instance of the UserControl SimpleControl
            dynamic ctrl = LoadControl("~/controls/usercontrols/" + ucName + "/ctrlUserControl.ascx") as System.Web.UI.UserControl;
            // Set the Public Properties
            ctrl.screenObject = obj;
            ctrl.initialize();
            UpdatePlaceholder.Controls.Add(ctrl);
        }
        #endregion

        #region Browser Control
        else if (obj.Type == "CONTROL BROWSER")
        {
            // Create instance of the UserControl SimpleControl
            ASP.ctrlBrowser ctrl = (ASP.ctrlBrowser)LoadControl("~/controls/ctrlBrowser.ascx");
            // Set the Public Properties
            ctrl.screenObject = OSAEObjectManager.GetObjectByName(obj.Name);
            StaticPlaceholder.Controls.Add(ctrl);
        }
        #endregion
    }

    protected void UpdateButton_Click(object sender, EventArgs e)
    {
        //foreach (var control in Page.Controls.OfType<ASP.ctrlStateImage>())
        //{
        //    if (OSAEObjectStateManager.GetObjectStateValue(control.ObjectName).Value != control.CurState)
        //    {
        //        control.Update();
        //    }
        //}
        authkey2.Value = OSAESecurity.generateCurrentAuthKey(currentuser);
    }

    private void getRestPort()
    {
        if (!OSAEObjectPropertyManager.GetObjectPropertyValue("Rest", "REST Port").Id.Equals(String.Empty))
        {
            try
            { restPort = int.Parse(OSAEObjectPropertyManager.GetObjectPropertyValue("Rest", "REST Port").Value); }
            catch (FormatException) { }
            catch (OverflowException) { }
            catch (ArgumentNullException) { }
        }
        hdnRestPort.Value = restPort.ToString();
    }

    private void SetSessionTimeout()
    {
        try
        {
            int timeout = 0;
            if (int.TryParse(OSAE.OSAEObjectPropertyManager.GetObjectPropertyValue("Web Server", "Timeout").Value, out timeout))
                Session.Timeout = timeout;
            else Session.Timeout = 60;
        }
        catch (Exception ex)
        {
            Master.Log.Error("Error setting session timeout", ex);
            Response.Redirect("~/error.aspx");
        }
    }
}