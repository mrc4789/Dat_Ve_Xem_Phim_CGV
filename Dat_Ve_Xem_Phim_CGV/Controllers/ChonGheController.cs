using Dat_Ve_Xem_Phim_CGV.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace Dat_Ve_Xem_Phim_CGV.Controllers
{
    public class ChonGheController : Controller
    {
        public QLDATVEEntities ql = new QLDATVEEntities();

        public ActionResult Index(string maSuatChieu)
        {
            if (!string.IsNullOrEmpty(maSuatChieu)) maSuatChieu = maSuatChieu.Trim();

            if (string.IsNullOrEmpty(maSuatChieu))
                return RedirectToAction("Index", "MuaVe");

            var suatChieu = ql.SUATCHIEUx.FirstOrDefault(x => (x.MASUATCHIEU ?? "").Trim() == maSuatChieu);
            if (suatChieu == null)
            {
                TempData["Error"] = "Không tìm thấy suất chiếu.";
                return RedirectToAction("Index", "MuaVe");
            }


            ViewBag.MaSuatChieu = maSuatChieu;
            ViewBag.TenPhim = suatChieu.PHIM.TENPHIM;
            ViewBag.Poster = suatChieu.PHIM.POSTER;
            ViewBag.TenRap = suatChieu.PHONGCHIEU.RAP.TENRAP;
            ViewBag.DiaChiRap = suatChieu.PHONGCHIEU.RAP.DIACHI;
            ViewBag.GioChieu = suatChieu.GIOCHIEU.Value.ToString(@"hh\:mm");
            ViewBag.NgayChieu = suatChieu.NGAYCHIEU.Value.ToString("dd/MM/yyyy");

            return View();
        }

        // Danh sách ghế theo suất chiếu
        public ActionResult _DanhSachGhe(string maSuatChieu)
        {
            if (string.IsNullOrWhiteSpace(maSuatChieu))
                return PartialView(new List<LayDanhSachGheResult>());

            maSuatChieu = maSuatChieu.Trim();

            var danhSachGhe = ql.Database.SqlQuery<LayDanhSachGheResult>(
                "EXEC LayDanhSachGhe @MaSuatChieu",
                new SqlParameter("@MaSuatChieu", maSuatChieu)
            ).ToList();

            ViewBag.SoGheTrong = danhSachGhe.Count(g => g.TINHTRANG == "Trống");
            ViewBag.TongSoGhe = danhSachGhe.Count;

            return PartialView(danhSachGhe);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DatGhe(string maSuatChieu, string seatIds)
        {
            if (!string.IsNullOrEmpty(maSuatChieu)) maSuatChieu = maSuatChieu.Trim();

            if (string.IsNullOrEmpty(seatIds))
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một ghế.";
                return RedirectToAction("Index", new { maSuatChieu });
            }

            var maGheList = seatIds.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            var ketQua = new List<string>();

            bool thanhCong = true;

            foreach (var maGhe in maGheList)
            {
                var result = ql.Database.SqlQuery<string>(
                    "EXEC KiemTraDatGhe @MaGhe, @MaSuatChieu",
                    new SqlParameter("@MaGhe", maGhe),
                    new SqlParameter("@MaSuatChieu", maSuatChieu)
                ).FirstOrDefault();

                ketQua.Add($"{maGhe}: {result}");

                if (result != null && result.Contains("đã có người đặt"))
                    thanhCong = false;
            }

            if (thanhCong)
            {
                Session["MaSuatChieu"] = maSuatChieu;
                Session["SelectedSeats"] = seatIds;
                Session["SoGheDat"] = maGheList.Count;
                Session["TongTien"] = maGheList.Count * 90000; 

                return RedirectToAction("ThanhToan", new {maSuatChieu});
            }
            else
            {
                TempData["Error"] = "Một số ghế đã được đặt bởi người khác!";
                TempData["ChiTiet"] = string.Join("<br/>", ketQua);
                return RedirectToAction("Index", new { maSuatChieu });
            }
        }


        public ActionResult ThanhToan(string maSuatChieu)
        {
            if (Session["MaSuatChieu"] == null || Session["SelectedSeats"] == null)
                return RedirectToAction("Index", "MuaVe");

            ViewBag.MaSuatChieu = Session["MaSuatChieu"];
            ViewBag.SeatIds = Session["SelectedSeats"];
            ViewBag.SoGhe = Session["SoGheDat"];
            ViewBag.TongTien = Session["TongTien"];
            var suatChieu = ql.SUATCHIEUx.FirstOrDefault(x => (x.MASUATCHIEU ?? "").Trim() == maSuatChieu);
            ViewBag.TenRap = suatChieu.PHONGCHIEU.RAP.TENRAP;
            ViewBag.DiaChiRap = suatChieu.PHONGCHIEU.RAP.DIACHI;
            ViewBag.GioChieu = suatChieu.GIOCHIEU.Value.ToString(@"hh\:mm");
            ViewBag.NgayChieu = suatChieu.NGAYCHIEU.Value.ToString("dd/MM/yyyy");
            return View();
        }


        [HttpPost]
        public JsonResult KiemTraGhe(string maGhe, string maSuatChieu)
        {
            if (!string.IsNullOrEmpty(maSuatChieu)) maSuatChieu = maSuatChieu.Trim();
            if (!string.IsNullOrEmpty(maGhe)) maGhe = maGhe.Trim();

            var status = ql.Database.SqlQuery<string>(
                "EXEC KiemTraGheTrong @MaGhe, @MaSuatChieu",
                new SqlParameter("@MaGhe", maGhe),
                new SqlParameter("@MaSuatChieu", maSuatChieu)
            ).FirstOrDefault();

            return Json(new { status = status ?? "Đã đặt" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ql.Dispose();
            base.Dispose(disposing);
        }
    }
}