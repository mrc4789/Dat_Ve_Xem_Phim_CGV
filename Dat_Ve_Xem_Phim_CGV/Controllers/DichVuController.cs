using System.Linq;
using System.Web.Mvc;
using Dat_Ve_Xem_Phim_CGV.Models;

namespace Dat_Ve_Xem_Phim_CGV.Controllers
{
    public class DichVuController : Controller
    {
        private QLDATVEEntities ql = new QLDATVEEntities();


        public ActionResult Index()
        {
            var listDV = ql.DICHVUs.Where(d => d.TRANGTHAI != "Hết hàng").ToList();
            return View(listDV);
        }

        // PartialView nhúng vào trang thanh toán
        public ActionResult _DanhSachCombo()
        {
            var listDV = ql.DICHVUs.Where(d => d.TRANGTHAI != "Hết hàng").ToList();
            return PartialView(listDV);
        }
    }
}