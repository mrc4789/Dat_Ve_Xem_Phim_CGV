using System.Linq;
using System.Web.Mvc;
using Dat_Ve_Xem_Phim_CGV.Models;

namespace Dat_Ve_Xem_Phim_CGV.Controllers
{
    public class AdminController : Controller
    {
        private QLDATVEEntities ql = new QLDATVEEntities();

        public ActionResult ThongKeDoanhThu()
        {
            // Join Hóa đơn -> Vé -> Suất Chiếu -> Phim để lấy tên phim
            // Vì bảng Vé không có mã suất, phải đi đường vòng: Vé -> Hóa đơn (khó phân loại phim)
            // HOẶC: Vé -> Chi tiết đặt vé -> Ghế -> Phòng -> Suất -> Phim (Cách này chuẩn theo DB hiện tại)

            var stats = (from hd in ql.HOADONs
                         join v in ql.VEs on hd.MAHD equals v.MAHD
                         join ctdv in ql.CHITIETDATVEs on v.MAVE equals ctdv.MAVE
                         join ghe in ql.GHEs on ctdv.MAGHE equals ghe.MAGHE
                         join suat in ql.SUATCHIEUx on ghe.MAPHONG equals suat.MAPHONG
                         join phim in ql.PHIMs on suat.MAPHIM equals phim.MAPHIM

                         // Điều kiện quan trọng: Ngày hóa đơn trùng ngày chiếu để map đúng suất
                         where hd.TRANGTHAI == "Đã thanh toán"
                               && hd.NGAYGD == suat.NGAYCHIEU

                         group v by new { phim.TENPHIM } into g
                         select new ThongKeViewModel
                         {
                             TenPhim = g.Key.TENPHIM,
                             SoVeBan = g.Count(),
                             DoanhThu = g.Sum(x => x.GIAVE) ?? 0
                         }).ToList();

            return View(stats);
        }
    }
}