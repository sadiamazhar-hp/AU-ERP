using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Mvc;

namespace AU_ERP.Main_Controller
{
    public class purchaseorderController : Controller
    {
        // GET: purchaseorder
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Create() {   
        return View();
        }
    }
}