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
            if (km == null) return HttpNotFound();

            // Lấy 4 khuyến mãi khác (trừ cái đang xem) để hiện ở dưới
            ViewBag.KhuyenMaiKhac = ql.KHUYENMAIs
                                      .Where(x => x.MAKM != id && x.NGAYKT >= DateTime.Now)
                                      .OrderByDescending(x => x.NGAYBD)
                                      .Take(4)
                                      .ToList();

            return View(km);
        }

        public ActionResult _ChonKhuyenMai()
        {
            // Logic: Lấy danh sách khuyến mãi còn hạn
            var listKM = ql.KHUYENMAIs
                           .Where(x => x.NGAYKT >= DateTime.Now)
                           .OrderByDescending(x => x.NGAYBD)
                           .ToList();

            return PartialView(listKM);
        }



    }
}