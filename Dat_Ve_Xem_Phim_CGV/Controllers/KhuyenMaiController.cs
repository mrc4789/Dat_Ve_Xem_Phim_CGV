using System;
using System.Linq;
using System.Web.Mvc;
using Dat_Ve_Xem_Phim_CGV.Models;

namespace Dat_Ve_Xem_Phim_CGV.Controllers
{
    public class KhuyenMaiController : Controller
    {
        private QLDATVEEntities ql = new QLDATVEEntities();

        public ActionResult Index()
        {
            // Lấy khuyến mãi còn hạn
            var listKM = ql.KHUYENMAIs
                           .Where(x => x.NGAYKT >= DateTime.Now)
                           .OrderByDescending(x => x.NGAYBD)
                           .ToList();
            return View(listKM);
        }

        public ActionResult Details(string id)
        {
            if (id == null) return RedirectToAction("Index");
            var km = ql.KHUYENMAIs.Find(id);
            return View(km);
        }
    }
}