﻿using System;
using System.Net;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.WebControls;

namespace Nauplius.SP.UserSync.ADMIN.FoundationSync
{
    public partial class ProfileSettings : LayoutsPageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            LoadSettings();
        }

        protected void btnSave_OnClick(object sender, EventArgs e)
        {
            bool site, exchange = false;
            var farm = SPFarm.Local;

            if (!string.IsNullOrEmpty(tBox1.Text))
            {
                site = ValidateSiteCollection();
            }
            else
            {
                if (farm.Properties.ContainsKey("pictureStorageUrl"))
                {
                    farm.Properties.Remove("pictureStorageUrl");
                }
            }

            if (!string.IsNullOrEmpty(tBox2.Text))
            {
                exchange = ValidateExchangeConnection();
            }
            else
            {
                if (farm.Properties.ContainsKey("useExchange"))
                {
                    farm.Properties.Remove("useExchange");
                }

                if (farm.Properties.ContainsKey("ewsUrl"))
                {
                    farm.Properties.Remove("ewsUrl");
                }
            }
        }

        internal bool ValidateSiteCollection()
        {
            if (!Uri.IsWellFormedUriString(tBox1.Text, UriKind.Absolute)) return false;

            var uri = new UriBuilder(tBox1.Text + "/_api/lists/getbytitle('UserPhotos')");
            var request = (HttpWebRequest)WebRequest.Create(uri.Uri);
            request.Credentials = CredentialCache.DefaultNetworkCredentials;

            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var farm = SPFarm.Local;
                var url = tBox1.Text + "/UserPhotos";

                if (farm.Properties.ContainsKey("pictureStorageUrl"))
                {
                    farm.Properties["pictureStorageUrl"] = url;
                }
                else
                {
                    farm.Properties.Add("pictureStorageUrl", url);
                }

                farm.Update(true);
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception)
            {
                //not a valid location or access denied; add ULS logging
            }

            return false;
        }

        internal bool ValidateExchangeConnection()
        {
            if (!Uri.IsWellFormedUriString(tBox2.Text, UriKind.Absolute)) return false;

            var uri = new UriBuilder(tBox2.Text);
            var request = (HttpWebRequest)WebRequest.Create(uri.Uri);
            request.UseDefaultCredentials = true;

            SPSecurity.RunWithElevatedPrivileges(delegate
            {
                try
                {
                    var response = (HttpWebResponse)request.GetResponse();
                    var farm = SPFarm.Local;

                    if (farm.Properties.ContainsKey("useExchange"))
                    {
                        farm.Properties["useExchange"] = "True";
                    }
                    else
                    {
                        farm.Properties.Add("useExchange", "True");
                    }

                    if (farm.Properties.ContainsKey("ewsUrl"))
                    {
                        farm.Properties["ewsUrl"] = uri.Uri.ToString();
                    }
                    else
                    {
                        farm.Properties.Add("ewsUrl", uri.Uri.ToString());
                    }

                    farm.Update(true);
                }
                catch (Exception)
                {
                    //Log to ULS, unable to add property values
                }                
            });

            return false;
        }

        internal void LoadSettings()
        {
            var farm = SPFarm.Local;

            try
            {
                if (farm.Properties.ContainsKey("useExchange") && (string) farm.Properties["useExchange"] == "True")
                {
                    if (!string.IsNullOrEmpty(farm.Properties["ewsUrl"].ToString()))
                    {
                        tBox2.Text = farm.Properties["ewsUrl"].ToString();
                    }
                    else
                    {
                        farm.Properties["useExchange"] = "False";
                        farm.Update();
                    }   
                }
            }
            catch (Exception)
            {
                //add to ULS logs, log message as unable to get property useExchange/ewsUrl.
            }

            try
            {
                if (farm.Properties.ContainsKey("pictureStorageUrl") && !string.IsNullOrEmpty(farm.Properties["pictureStorageUrl"].ToString()))
                {
                    //validate URI

                    var url = farm.Properties["pictureStorageUrl"].ToString();
                    var index = url.LastIndexOf("/");

                    if (index > 0)
                        url = url.Substring(0, index);
                    tBox1.Text = url;
                }
            }
            catch (Exception)
            {
                //add to ULS logs, log message as unable to get property pictureStorageUrl
            }
        }
    }
}