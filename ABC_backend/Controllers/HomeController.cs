using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ABC_backend.Controllers
{
    public class HomeController : Controller
    {
        //______________________Returns the index home view to display all navigation options_____________________________
        public ActionResult Index()
        {
            return View();
        }
        //______________________Returns the Feedback view to provide user with closure, accepts feedback information and redirect information_____________________________
        public ActionResult Feedback(String message, bool isSuccess, String redirectControl, String redirectAction)
        {
            if (redirectControl != null && redirectAction != null)
            {
                ViewBag.RedirectControl = redirectControl;
                ViewBag.RedirectAction = redirectAction;
            }

            ViewBag.Title = "Success";
            if (!isSuccess)
            {
                ViewBag.Title = "Failed";
            }
            ViewBag.Message = message;
            return View();
        }

    }


}