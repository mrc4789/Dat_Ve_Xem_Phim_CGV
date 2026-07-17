using Dat_Ve_Xem_Phim_CGV.Models;
using System;
using System.Collections.Generic;
using System.Data;
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
            if (!string.IsNullOrEmpty(maSuatChieu))
                maSuatChieu = maSuatChieu.Trim();

            if (string.IsNullOrEmpty(maSuatChieu))
                return RedirectToAction("Index", "MuaVe");

            var suatChieu = ql.SUATCHIEUx
                .FirstOrDefault(x => (x.MASUATCHIEU ?? "").Trim() == maSuatChieu);

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

        /// <summary>
        /// Load danh sách ghế theo suất chiếu
        /// </summary>
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

        /// <summary>
        /// Giữ ghế tạm thời (10 phút)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GiuGhe(string maSuatChieu, string seatIds)
        {
            // ===== 1. Validate input =====
            if (!string.IsNullOrWhiteSpace(maSuatChieu))
                maSuatChieu = maSuatChieu.Trim();

            if (string.IsNullOrWhiteSpace(maSuatChieu))
            {
                TempData["Error"] = "Suất chiếu không hợp lệ.";
                return RedirectToAction("Index", "MuaVe");
            }

            if (string.IsNullOrWhiteSpace(seatIds))
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một ghế.";
                return RedirectToAction("Index", new { maSuatChieu });
            }

            // ===== 2. Lấy thông tin người dùng =====
            string maKH = Session["MaKH"]?.ToString();
            if (string.IsNullOrEmpty(maKH))
                maKH = "KH001"; // Demo – sau này thay bằng user đăng nhập

            string maGiuGhe = "HOLD_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            try
            {
                // ===== 3. Khai báo parameter CHUẨN =====
                var parameters = new[]
                {
            new SqlParameter("@DanhSachMaGhe", SqlDbType.NVarChar)
            {
                Value = seatIds
            },
            new SqlParameter("@MaSuatChieu", SqlDbType.Char, 5)
            {
                Value = maSuatChieu
            },
            new SqlParameter("@MAKH", SqlDbType.VarChar, 10)
            {
                Value = maKH
            },
            new SqlParameter("@MaGiuGhe", SqlDbType.VarChar, 50)
            {
                Value = maGiuGhe
            }
        };

                // ===== 4. GỌI PROC (UPDATE → ExecuteSqlCommand) =====
                ql.Database.ExecuteSqlCommand(
                    "EXEC GiuGheTamThoi @DanhSachMaGhe, @MaSuatChieu, @MAKH, @MaGiuGhe",
                    parameters
                );

                // ===== 5. Nếu tới đây → coi như giữ ghế THÀNH CÔNG =====
                Session["MaSuatChieu"] = maSuatChieu;
                Session["SelectedSeats"] = seatIds;
                Session["MaGiuGhe"] = maGiuGhe;
                Session["MaKH"] = maKH;

                // ===== 6. Tính tiền =====
                decimal tongTien = TinhTongTien(seatIds, maSuatChieu);
                Session["TongTien"] = tongTien;
                Session["SoGheDat"] = seatIds.Split(',').Length;

                return RedirectToAction("ChonComboVaKhuyenMai");
            }
            catch (SqlException ex)
            {
                // Lỗi SQL (deadlock, khóa ghế, dữ liệu)
                TempData["Error"] = "Không thể giữ ghế: " + ex.Message;
                return RedirectToAction("Index", new { maSuatChieu });
            }
            catch (Exception ex)
            {
                // Lỗi hệ thống
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
                return RedirectToAction("Index", new { maSuatChieu });
            }
        }
        public ActionResult ChonComboVaKhuyenMai()
        {
            // Kiểm tra session giữ ghế
            if (Session["MaGiuGhe"] == null || Session["SelectedSeats"] == null)
            {
                TempData["Error"] = "Phiên đặt vé đã hết hạn!";
                return RedirectToAction("Index", "MuaVe");
            }

            string maSuatChieu = Session["MaSuatChieu"].ToString();
            var suatChieu = ql.SUATCHIEUx.FirstOrDefault(x => x.MASUATCHIEU.Trim() == maSuatChieu);

            if (suatChieu == null)
                return RedirectToAction("Index", "MuaVe");

            // Lấy danh sách dịch vụ (combo) và khuyến mãi
            var danhSachDichVu = ql.DICHVUs
                .Where(d => d.TRANGTHAI == "Còn hàng")  // hoặc true nếu là bit
                .ToList();

            var danhSachKhuyenMai = ql.KHUYENMAIs
                .Where(k => k.NGAYBD <= DateTime.Today && k.NGAYKT >= DateTime.Today)
                .ToList();

            // Thông tin tóm tắt vé để hiển thị
            ViewBag.TenPhim = suatChieu.PHIM.TENPHIM;
            ViewBag.NgayChieu = suatChieu.NGAYCHIEU.Value.ToString("dd/MM/yyyy");
            ViewBag.GioChieu = suatChieu.GIOCHIEU.Value.ToString(@"hh\:mm");
            ViewBag.TenRap = suatChieu.PHONGCHIEU.RAP.TENRAP;
            ViewBag.SeatIds = Session["SelectedSeats"];
            ViewBag.SoGhe = Session["SoGheDat"];
            ViewBag.TongTienVe = Session["TongTien"]; 

            // === QUAN TRỌNG: Truyền danh sách cho partial ===
            ViewBag.DanhSachDichVu = danhSachDichVu;
            ViewBag.DanhSachKhuyenMai = danhSachKhuyenMai;

            return View();
        }

        /// <summary>
        /// Tính tổng tiền dựa trên danh sách ghế
        /// </summary>
        private decimal TinhTongTien(string seatIds, string maSuatChieu)
        {
            if (string.IsNullOrWhiteSpace(seatIds))
                return 0;

            var listSeatIds = seatIds
                .Split(',')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList();

            if (!listSeatIds.Any())
                return 0;

            var suatChieu = ql.SUATCHIEUx
                .FirstOrDefault(x => (x.MASUATCHIEU ?? "").Trim() == maSuatChieu);

            decimal giaVeCoBan = suatChieu?.GIACOBAN ?? 90000;

            decimal tongTien = 0;

            foreach (var maGhe in listSeatIds)
            {
                var ghe = ql.GHEs.FirstOrDefault(x => x.MAGHE == maGhe);
                if (ghe == null) continue;

                decimal giaGhe;

                if (ghe.LOAIGHE.Contains("Đôi") || ghe.LOAIGHE.Contains("Sweet"))
                {
                    giaGhe = 120000; // GIÁ RIÊNG SWEETBOX
                }
                else
                {
                    giaGhe = giaVeCoBan; // thường / VIP
                }

                decimal phuPhi = ghe.PHUPHI ?? 0;
                tongTien += giaGhe + phuPhi;
            }


            return tongTien;
        }




        /// <summary>
        /// Trang thanh toán
        /// </summary>
        public ActionResult ThanhToan()
        {
            if (Session["MaSuatChieu"] == null || Session["SelectedSeats"] == null)
                return RedirectToAction("Index", "MuaVe");

            string maSuatChieu = Session["MaSuatChieu"].ToString();

            var suatChieu = ql.SUATCHIEUx
                .FirstOrDefault(x => x.MASUATCHIEU.Trim() == maSuatChieu);

            if (suatChieu == null)
                return RedirectToAction("Index", "MuaVe");

            ViewBag.MaSuatChieu = maSuatChieu;
            ViewBag.TenPhim = suatChieu.PHIM.TENPHIM;
            ViewBag.SeatIds = Session["SelectedSeats"];
            ViewBag.SoGhe = Session["SoGheDat"];
            ViewBag.TongTien = Session["TongTien"];
            ViewBag.TenRap = suatChieu.PHONGCHIEU.RAP.TENRAP;
            ViewBag.DiaChiRap = suatChieu.PHONGCHIEU.RAP.DIACHI;
            ViewBag.GioChieu = suatChieu.GIOCHIEU.Value.ToString(@"hh\:mm");
            ViewBag.NgayChieu = suatChieu.NGAYCHIEU.Value.ToString("dd/MM/yyyy");

            return View();
        }

        /// <summary>
        /// Xử lý thanh toán
        /// </summary>
        /// <summary>
        /// Xử lý thanh toán
        /// </summary>
        [HttpPost]
        public ActionResult XuLyThanhToan(string phuongThucThanhToan)
        {
            if (Session["MaGiuGhe"] == null)
            {
                TempData["Error"] = "Phiên làm việc đã hết hạn!";
                return RedirectToAction("Index", "MuaVe");
            }

            string maGiuGhe = Session["MaGiuGhe"].ToString();
            string maKH = Session["MaKH"].ToString();
            string maSuatChieu = Session["MaSuatChieu"].ToString();
            decimal tongTien = Convert.ToDecimal(Session["TongTien"]);

            try
            {
                var parameters = new[]
                {
            new SqlParameter("@MaGiuGhe", maGiuGhe),
            new SqlParameter("@MaKhachHang", maKH),
            new SqlParameter("@MaSuatChieu", maSuatChieu),
            new SqlParameter("@TongTien", tongTien)
        };

                var result = ql.Database.SqlQuery<ThanhToanResult>(
                    "EXEC ThanhToan @MaGiuGhe, @MaKhachHang, @MaSuatChieu, @TongTien",
                    parameters
                ).FirstOrDefault();

                if (result != null && result.Success == 1)
                {
                    // Lấy thông tin chi tiết để hiển thị đẹp hơn
                    var listVe = ql.VEs.Where(v => v.MAHD == result.MaHoaDon).ToList();

                    string danhSachGhe = "---";
                    string maVeHienThi = result.MaVe?.Trim() ?? "---";

                    if (listVe.Any())
                    {
                        danhSachGhe = string.Join(", ", listVe.Select(v => v.MAGHE.Trim()));
                        maVeHienThi = string.Join(", ", listVe.Select(v => v.MAVE.Trim()));
                    }

                    var veDau = listVe.FirstOrDefault();
                    var suatChieu = veDau != null ? ql.SUATCHIEUx.FirstOrDefault(s => s.MASUATCHIEU.Trim() == veDau.MASUATCHIEU.Trim()) : null;
                    var phim = suatChieu?.PHIM;
                    var rap = suatChieu?.PHONGCHIEU?.RAP;

                    // Tính thời lượng hiển thị
                    string thoiLuongHienThi = "";
                    if (phim?.THOILUONG != null)
                    {
                        int tongPhut = phim.THOILUONG.Value;
                        int gio = tongPhut / 60;
                        int phut = tongPhut % 60;
                        thoiLuongHienThi = gio > 0 ? $"{gio} giờ {phut} phút" : $"{phut} phút";
                    }

                    // Lưu tất cả thông tin cần thiết vào TempData để view dùng
                    TempData["MaHD"] = result.MaHoaDon?.Trim();
                    TempData["MaGiaoDich"] = result.MaHoaDon?.Trim();
                    TempData["TongTien"] = tongTien;
                    TempData["TenPhim"] = phim?.TENPHIM ?? suatChieu?.PHIM.TENPHIM;
                    TempData["DoTuoi"] = phim?.DOTUOI ?? "P";
                    TempData["ThoiLuongHienThi"] = thoiLuongHienThi;
                    TempData["DinhDang"] = phim?.DINHDANG ?? "";
                    TempData["NgayChieu"] = suatChieu?.NGAYCHIEU;
                    TempData["GioChieu"] = suatChieu?.GIOCHIEU;
                    TempData["MaRapLabel"] = rap?.MARAP?.Trim() ?? "";
                    TempData["TenRap"] = rap?.TENRAP ?? suatChieu?.PHONGCHIEU?.RAP.TENRAP;
                    TempData["DiaChiRap"] = rap?.DIACHI ?? suatChieu?.PHONGCHIEU?.RAP.DIACHI;
                    TempData["DanhSachGhe"] = danhSachGhe;
                    TempData["SoLuongCombo"] = 0; // Hiện tại chưa có combo
                    TempData["MaVe"] = maVeHienThi;

                    // Xóa session giữ ghế
                    Session.Remove("MaSuatChieu");
                    Session.Remove("SelectedSeats");
                    Session.Remove("MaGiuGhe");
                    Session.Remove("TongTien");
                    Session.Remove("SoGheDat");
                    Session.Remove("MaKH");

                    return RedirectToAction("ThanhToanThanhCong");
                }
                else
                {
                    TempData["Error"] = result?.Message ?? "Thanh toán thất bại!";
                    return RedirectToAction("ThanhToan");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
                return RedirectToAction("ThanhToan");
            }
        }

        /// <summary>
        /// Trang thanh toán thành công - hiển thị vé đẹp như admin
        /// </summary>
        public ActionResult ThanhToanThanhCong()
        {
            if (TempData["MaHD"] == null)
                return RedirectToAction("Index", "MuaVe");

            // Không cần ViewBag nữa, tất cả đã lưu trong TempData
            return View();
        }

       

        /// <summary>
        /// Hủy giữ ghế (quay lại)
        /// </summary>
        [HttpPost]
        public ActionResult HuyGiuGhe()
        {
            if (Session["MaGiuGhe"] != null && Session["MaKH"] != null)
            {
                string maGiuGhe = Session["MaGiuGhe"].ToString();
                string maKH = Session["MaKH"].ToString();

                try
                {
                    ql.Database.ExecuteSqlCommand(
                        "EXEC HuyGiuGhe @MaGiuGhe, @MAKH",
                        new SqlParameter("@MaGiuGhe", maGiuGhe),
                        new SqlParameter("@MAKH", maKH)
                    );
                }
                catch { }
            }

            Session.Remove("MaGiuGhe");
            Session.Remove("SelectedSeats");
            Session.Remove("MaSuatChieu");

            return RedirectToAction("Index", "MuaVe");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ql.Dispose();
            base.Dispose(disposing);
        }
    }

    // ===== HELPER CLASSES =====
    public class GiuGheResult
    {
        public int Success { get; set; }
        public string Message { get; set; }
    }

    public class ThongTinGheResult
    {
        public string MAGHE { get; set; }
        public string LOAIGHE { get; set; }
        public string DAYGHE { get; set; }
        public decimal? PHUPHI { get; set; }
        public string MAPHONG { get; set; }
    }

    public class ThanhToanResult
    {
        public int Success { get; set; }
        public string Message { get; set; }
        public string MaHoaDon { get; set; }
        public string MaVe { get; set; }
        public decimal TongTien { get; set; }
        public int SoGheDaDat { get; set; }
    }
}