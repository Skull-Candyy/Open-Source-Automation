﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using OSAE;

public partial class analytics : System.Web.UI.Page
{

    private int restPort = 8732;

    public void RaisePostBackEvent(string eventArgument)
    {
        string[] args = eventArgument.Split('_');

        if (args[0] == "gvProperties")
        {
            hdnSelectedRow.Text = args[1];
            hdnSelectedPropID.Text = gvProperties.DataKeys[Int32.Parse(hdnSelectedRow.Text)]["prop_id"].ToString();
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["Username"] == null) Response.Redirect("~/Default.aspx");
        int objSet = OSAEAdminManager.GetAdminSettingsByName("AnalyticsTrust");
        int tLevel = Convert.ToInt32(Session["TrustLevel"].ToString());
        if (tLevel < objSet)
        {
            Response.Redirect("~/permissionError.aspx");
        }
        loadProperties();
        loadStates();
        getRestPort();
    }

    protected void Page_PreRender(object sender, EventArgs e)
    {
        if (hdnSelectedRow.Text != "")
        {
            gvProperties.Rows[Int32.Parse(hdnSelectedRow.Text)].Attributes.Remove("onmouseout");
            gvProperties.Rows[Int32.Parse(hdnSelectedRow.Text)].Style.Add("background", "lightblue");
        }
    }
    
    private void loadProperties()
    {
        gvProperties.DataSource = OSAESql.RunSQL("SELECT DISTINCT CONCAT(object_name,' - ',property_name) as prop_name, object_name, property_name, LEFT(property_datatype, 1) AS property_datatype FROM osae_v_object_property_history WHERE property_datatype IN ('Integer', 'Float', 'Boolean') ORDER BY prop_name");
        gvProperties.DataBind();
    }

    private void loadStates()
    {
        gvStates.DataSource = OSAESql.RunSQL("SELECT DISTINCT object_name FROM osae_v_object_state_change_history ORDER BY object_name");
        gvStates.DataBind();
    }
    private void getRestPort()
    {
        
        if (!OSAEObjectPropertyManager.GetObjectPropertyValue("Rest", "REST Port").Id.Equals(String.Empty))
        {
            try
            {
                restPort = int.Parse(OSAEObjectPropertyManager.GetObjectPropertyValue("Rest", "REST Port").Value);
            }
            catch (FormatException)
            {
                // do nothing and move on
            }
            catch (OverflowException)
            {
                // do nothing and move on
            }
            catch (ArgumentNullException)
            {
                // do nothing and move on
            }
        }

        hdnRestPort.Value = restPort.ToString();
    }
}
