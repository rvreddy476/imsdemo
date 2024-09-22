using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Configuration;
using IMS.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Web.Security;
using Microsoft.Owin.Security;

namespace IMS.Web.Controllers
{
    [AllowAnonymous]
    public class AuthenticationController : Controller
    {

        private OidcOptions options = new OidcOptions
        {
            ClientId = "e6049dc0-d944-013c-a135-1604c08a032c195891",
            ClientSecret = "5fc92f9a9ecdf5429a42f7b73ebd58c53a4500a0e8c454c3764ee80cb6206caa",          
        };


         //GET: Authentication
       //  Redirect to OneLogin login page


        public ActionResult RedirectToLogin()
        {
            //if(HttpContext.User.Identity == null)
            //{
            //
            //}
            //else if (!HttpContext.User.Identity.IsAuthenticated)
            // {
            //     HttpContext.GetOwinContext().Authentication.Challenge(
            //         "OpenIdConnect");
            //     return new HttpUnauthorizedResult();
            // }
            return RedirectToAction("Login", "Login");
           // return View();
        }

        //public async Task<ActionResult> RedirectToLogin()
        //{
        //    string clientID = ConfigurationManager.AppSettings["onelogin:ClientId"];
        //    string clientsecret = ConfigurationManager.AppSettings["onelogin:ClientSecret"];
        //    string redirectUri = ConfigurationManager.AppSettings["onelogin:RedirectUri"];

        //    string url = $"https://harbingergroup.onelogin.com/oidc/2/auth?client_id={clientID}&redirect_uri={redirectUri}&response_type=code&scope=openid";


        //    using (HttpClient client = new HttpClient())
        //    {
        //        try
        //        {

        //            HttpResponseMessage response = await client.GetAsync(url);

        //            // Check if the request was successful (status code 200)
        //            if (response.IsSuccessStatusCode)
        //            {

        //                string responseBody = await response.Content.ReadAsStringAsync();
        //                Console.WriteLine(responseBody);
        //                return Redirect(url);
        //            }
        //            else
        //            {

        //                Console.WriteLine("Request failed with status code: " + response.StatusCode);
        //            }
        //        }
        //        catch (HttpRequestException e)
        //        {

        //            Console.WriteLine("Request failed: " + e.Message);
        //        }
        //    }
        //    return View();

        //}

        public ActionResult Login()
        {
            return View();
        }

        //[HttpGet]
        //[AllowAnonymous]
        //public async Task<ActionResult> Login(string returnUrl = "")
        //{
        //    if(HttpContext.User.Identity.IsAuthenticated == true)
        //    {

        //    }

        //    // Clear existing authentication cookie
        //         ClearAuthenticationCookie();          

        //        string clientID = ConfigurationManager.AppSettings["onelogin:ClientId"];
        //        string clientsecret = ConfigurationManager.AppSettings["onelogin:ClientSecret"];
        //        string redirectUri = ConfigurationManager.AppSettings["onelogin:RedirectUri"];

        //        string url = $"https://harbingergroup.onelogin.com/oidc/2/auth?client_id={clientID}&redirect_uri={redirectUri}&response_type=code&scope=openid";


        //        using (HttpClient client = new HttpClient())
        //        {
        //            try
        //            {

        //                HttpResponseMessage response = await client.GetAsync(url);

        //                // Check if the request was successful (status code 200)
        //                if (response.IsSuccessStatusCode)
        //                {

        //                    string responseBody = await response.Content.ReadAsStringAsync();
        //                    Console.WriteLine(responseBody);
        //                    return Redirect(url);
        //                }
        //                else
        //                {

        //                    Console.WriteLine("Request failed with status code: " + response.StatusCode);
        //                }
        //            }
        //            catch (HttpRequestException e)
        //            {

        //                Console.WriteLine("Request failed: " + e.Message);
        //            }
        //        }
        //        return Redirect(url);



        //}

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel loginModel, string returnUrl = "/Login/WelcomeUser")
        {
            ViewData["ReturnUrl"] = returnUrl;

            var token = await LoginUser(loginModel.userEmail, loginModel.userPassword);

            if (!String.IsNullOrEmpty(token.AccessToken))
            {
                // We need to call OIDC again to get the user claims
                var user = await GetUserInfo(token.AccessToken);

                // Store user claims in cookies
                StoreUserClaimsInCookies(user, token.AccessToken);

                // Redirect to the dashboard after successful login
                return Redirect(returnUrl);
            }

            ModelState.AddModelError(string.Empty, "Login Failed");
            return View(loginModel);
        }

        //[HttpGet]
        //public async Task<ActionResult> Logout()
        //{
        //    // Clear user claims cookies
        //    ClearUserClaimsCookies();
        //    return RedirectToAction("Index", "Home");
        //}

        private async Task<OidcOptions> LoginUser(string useremail, string userpassword)
        {
            using (var client = new HttpClient())
            {
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("username", useremail),
                    new KeyValuePair<string, string>("password", userpassword),
                    new KeyValuePair<string, string>("client_id", options.ClientId),
                    new KeyValuePair<string, string>("client_secret", options.ClientSecret),
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("scope", "openid profile email")
                });

                var uri = String.Format("https://harbingergroup.onelogin.com/oidc/2/token", options.Region);

                var res = await client.PostAsync(uri, formData);

                var json = await res.Content.ReadAsStringAsync();

                var tokenReponse = JsonConvert.DeserializeObject<OidcOptions>(json);

                return tokenReponse;
            }
        }
        private async Task<LoginViewModel> GetUserInfo(string accessToken)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var uri = String.Format("https://harbingergroup.onelogin.com/oidc/me", options.Region);

                var res = await client.GetAsync(uri);

                var json = await res.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<LoginViewModel>(json);
            }
        }

        private void StoreUserClaimsInCookies(LoginViewModel user, string accessToken)
        {
            // Store user claims in cookies
            Response.Cookies["UserId"].Value = user.Id.ToString();
            Response.Cookies["UserEmail"].Value = user.userEmail;
            Response.Cookies["AccessToken"].Value = accessToken;
        }

        private void ClearUserClaimsCookies()
        {
            // Clear user claims cookies
            Response.Cookies["UserId"].Expires = DateTime.Now.AddDays(-1);
            Response.Cookies["Username"].Expires = DateTime.Now.AddDays(-1);
            Response.Cookies["AccessToken"].Expires = DateTime.Now.AddDays(-1);
        }

        private void ClearAuthenticationCookie()
        {
            // Clear authentication cookie
            Response.Cookies[FormsAuthentication.FormsCookieName].Expires = DateTime.Now.AddDays(-1);
        }

    }
}