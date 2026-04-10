using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Mvc;

namespace AU_ERP.Main_Controller
{
    public class BPController : Controller
    {
        // GET: BP
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Create()
        {
            return View();
        }
        public IActionResult BPgroup()
        {
            return View();
        }

    }
}