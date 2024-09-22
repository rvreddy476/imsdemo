using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin;
using Owin;
using System.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Web.Routing;
using Microsoft.Owin.Security.Notifications;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Net;
using System.IO;

[assembly: OwinStartup(typeof(IMS.Web.App_Start.Startup))]

namespace IMS.Web.App_Start
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
           
            Console.WriteLine();
           
            string ClientId = ConfigurationManager.AppSettings["onelogin:ClientId"];
            string ClientSecret = ConfigurationManager.AppSettings["onelogin:ClientSecret"];          
            string RedirectUri = ConfigurationManager.AppSettings["onelogin:RedirectUri"];

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                LoginPath = new PathString("/Authentication/Login")
            });

            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret,               
                Authority = String.Format("https://harbingergroup.onelogin.com/oidc/2"),
                RedirectUri = RedirectUri,
                ResponseType = OpenIdConnectResponseType.Code, //code               
                Scope = OpenIdConnectScope.OpenIdProfile, //openid              
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    RedirectToIdentityProvider = notification =>
                    {
                        if (notification.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication)
                        {
                            var redirectUri = notification.Options.RedirectUri;                           
                            notification.ProtocolMessage.RedirectUri = redirectUri;
                        }
                        return Task.CompletedTask;
                    },

                  AuthorizationCodeReceived = async notification => {
                      await OnAuthorizationCodeReceived(notification);
                  },

                    //AuthorizationCodeReceived = async notification => 
                    //{
                    //    var code = notification.Code;

                    //    var tokenEndpoint = "https://harbingergroup.onelogin.com/oidc/2/token";
                    //    var formData = "grant_type=authorization_code" +
                    //                   "&client_id=" + notification.Options.ClientId +
                    //                   "&client_secret=" + notification.Options.ClientSecret +
                    //                   "&code=" + code +
                    //                   "&redirect_uri=" + notification.Options.RedirectUri;

                    //    var request = (HttpWebRequest)WebRequest.Create(tokenEndpoint);
                    //    request.Method = "POST";
                    //    request.Headers.Add("ContentType", "application/x-www-form-urlencoded");
                    //    var data = Encoding.UTF8.GetBytes(formData);
                    //    request.ContentLength = data.Length;

                    //    using (var stream = request.GetRequestStream())
                    //    {
                    //        stream.Write(data, 0, data.Length);
                    //    }
                    //    using (var response = (HttpWebResponse)request.GetResponse())
                    //    using (var reader = new StreamReader(response.GetResponseStream()))
                    //    {
                    //        var tokenContent = reader.ReadToEnd();
                    //        // Process tokenContent as needed
                    //    }
                    //    // Get user information using the access token
                    //        var userInfoEndpoint = "https://harbingergroup.onelogin.com/oidc/2/me";
                    //        var userInfoClient = new HttpClient();
                    //        userInfoClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    //        var userInfoResponse = await userInfoClient.GetAsync(userInfoEndpoint);
                    //        var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
                    //        var userInfo = JObject.Parse(userInfoContent);
                    //        var userEmail = userInfo.Value<string>("email");

                    //},


                    SecurityTokenValidated = notification =>
                    {
                       var i =  notification.AuthenticationTicket.Identity;
                        // Handle token validation event
                        // Perform additional validation or claims processing
                        return Task.CompletedTask;
                    },

                    AuthenticationFailed = notification =>
                    {
                        // Handle authentication failure event
                        // Log or handle the failure
                        return Task.CompletedTask;
                    }
                }
                });

           
        }
        private async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedNotification notification)
        {
            var code = notification.Code;
       
            using (var tokenClient = new HttpClient())
            {
                var tokenEndpoint = "https://harbingergroup.onelogin.com/oidc/2/token";
       
                var formData = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("client_id", notification.Options.ClientId),
                new KeyValuePair<string, string>("client_secret", notification.Options.ClientSecret),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", notification.Options.RedirectUri),
                

            });
               formData.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");                 
               var tokenResponse = await tokenClient.PostAsync(tokenEndpoint, formData);
                var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                var tokenData = JObject.Parse(tokenContent);
                var accessToken = tokenData.Value<string>("access_token");

                // Get user information using the access token
                var userInfoEndpoint = "https://harbingergroup.onelogin.com/oidc/2/me";
                var userInfoClient = new HttpClient();
                userInfoClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var userInfoResponse = await userInfoClient.GetAsync(userInfoEndpoint);
                var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
                var userInfo = JObject.Parse(userInfoContent);
                var userEmail = userInfo.Value<string>("email");
            }
        }
    }
}
