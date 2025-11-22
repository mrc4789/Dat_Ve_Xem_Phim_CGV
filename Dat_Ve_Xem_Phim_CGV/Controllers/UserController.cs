using Dat_Ve_Xem_Phim_CGV.Models;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Dat_Ve_Xem_Phim_CGV.Controllers
{
    public class UserController : Controller
    {
        private QLDATVEEntities ql = new QLDATVEEntities();

        public ActionResult Index(string mode = "login")
        {
            ViewBag.Mode = mode;
            return View();
        }

        // Xử lý Đăng nhập
        [HttpPost]
        public ActionResult Login(string tk, string mk)
        {
            var user = ql.KHACHHANGs.FirstOrDefault(t => (t.SDT == tk || t.EMAIL == tk) && t.MATKHAU == mk);

            if (user != null)
            {
                Session["TaiKhoan"] = user;
                Session["HoTen"] = user.HOTEN;

                return RedirectToAction("ProFile", "User");
            }

            ViewBag.ErrorLogin = "Tài khoản hoặc mật khẩu không đúng!";
            ViewBag.Mode = "login";
            return View("Index");
        }
        private string MaKHTuDong()
        {
            var KHCuoiDataBase = ql.KHACHHANGs.OrderByDescending(t => t.MAKH).FirstOrDefault();
            string laySo = KHCuoiDataBase.MAKH.Substring(2);
            int soHienTai = int.Parse(laySo);
            int soMoi = soHienTai + 1;
            string maMoi = "KH" + soMoi.ToString("D3"); // D3 nghĩa là: số 9 -> "009"
            return maMoi;   
        }
        // Đăng ký
        [HttpPost]
        public ActionResult Register(KHACHHANG kh, string confirmMK)
        {
            if (ModelState.IsValid)
            {
                if (kh.MATKHAU != confirmMK)
                {
                    ViewBag.ErrorRegister = "Mật khẩu xác nhận không khớp!";
                    ViewBag.Mode = "register";
                    return View("Index");
                }
                var check = ql.KHACHHANGs.FirstOrDefault(s => s.SDT == kh.SDT || s.EMAIL == kh.EMAIL);

                if (check == null)
                {
                    try
                    {
                        kh.MAKH = MaKHTuDong();
                        kh.DIEMTICHLUY = 0;
                        kh.HANGTHANHVIEN = "New";
                        ql.KHACHHANGs.Add(kh);
                        ql.SaveChanges();
                        TempData["ThongBao"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                        return RedirectToAction("Index", new { mode = "login" });
                    }
                    catch (Exception ex)
                    {
                        ViewBag.ErrorRegister = "Lỗi lặt vặt " + ex.Message;
                    }
                }
                else
                {
                    ViewBag.ErrorRegister = "Số điện thoại hoặc Email đã được sử dụng!";
                }
            }
            ViewBag.Mode = "register";
            return View("Index");
        }
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "User");
        }

        public ActionResult ProFile()
        {
            if (Session["TaiKhoan"] == null)
            {
                return RedirectToAction("Index");
            }
            var userSS = Session["TaiKhoan"] as KHACHHANG;
            var user = ql.KHACHHANGs.Find(userSS.MAKH);

            double tongTienDouble = ql.HOADONs
                .Where(h => h.MAKH == user.MAKH && h.TRANGTHAI == "Đã thanh toán")
                .Sum(h => (double?)h.THANHTIEN) ?? 0;
            decimal tongChiTieu = (decimal)tongTienDouble;

            string hangMoi = "New";
            if (tongChiTieu >= 5000000) hangMoi = "Gold";
            else if (tongChiTieu >= 2000000) hangMoi = "Silver";
            else if (tongChiTieu > 0) hangMoi = "Member";

            if (user.HANGTHANHVIEN != hangMoi)
            {
                user.HANGTHANHVIEN = hangMoi;
                ql.SaveChanges();
                Session["TaiKhoan"] = user;
            }
            ViewBag.TongChiTieu = tongChiTieu;
            return View(user);
        }
        [HttpGet]
        public ActionResult EditProFile()
        {
            if (Session["TaiKhoan"] == null)
            {
                return RedirectToAction("Index");
            }
            var userSS = Session["TaiKhoan"] as KHACHHANG;
            var user = ql.KHACHHANGs.Find(userSS.MAKH);
            double tongTienTam = ql.HOADONs
                .Where(h => h.MAKH == user.MAKH && h.TRANGTHAI == "Đã thanh toán")
                .Sum(h => (double?)h.THANHTIEN) ?? 0;

           decimal tongChiTieu = (decimal)tongTienTam;
            ViewBag.TongChiTieu = tongChiTieu;
            return View(user);
        }
        [HttpPost]
        public ActionResult EditProFile(KHACHHANG duLieuKH, string ktDoiMK, string mkCu, string mkMoi, string confirmMK)
        {
            var user = ql.KHACHHANGs.Find(duLieuKH.MAKH);
            double tongTienDouble = ql.HOADONs.Where(h => h.MAKH == user.MAKH && h.TRANGTHAI == "Đã thanh toán").Sum(h => (double?)h.THANHTIEN) ?? 0;
            ViewBag.TongChiTieu = (decimal)tongTienDouble;
            if (user != null)
            {
                user.HOTEN = duLieuKH.HOTEN;
                user.SDT = duLieuKH.SDT;
                user.EMAIL = duLieuKH.EMAIL;
            }
            if (!string.IsNullOrEmpty(ktDoiMK))
            {
                if (user.MATKHAU == mkCu)
                {
                    if (mkMoi == confirmMK)
                    {
                        user.MATKHAU = mkMoi;
                    }
                    else
                    {
                        ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                        return View(user);
                    }
                }
                else
                {
                    ViewBag.Error = "Mật khẩu cũ không đúng!";
                    return View(user);
                }
            }
            ql.SaveChanges();
            Session["TaiKhoan"] = user;
            Session["HoTen"] = user.HOTEN;
            TempData["ThongBao"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("ProFile");
        }
        public ActionResult LichSuGiaoDich()
        {
            if (Session["TaiKhoan"] == null)
            {
                return RedirectToAction("Index");
            }
            var userSession = Session["TaiKhoan"] as KHACHHANG;
            var listHoaDon = ql.HOADONs.Where(h => h.MAKH == userSession.MAKH).OrderByDescending(h => h.NGAYGD).ToList();
            return View(listHoaDon);
        }
        public ActionResult HistoryDetail(string id)
        {
            if (Session["TaiKhoan"] == null) return RedirectToAction("Index");

            var hoadon = ql.HOADONs.Find(id);
            if (hoadon == null) return RedirectToAction("LichSuGiaoDich");

            // LẤY THÔNG TIN THANH TOÁN 
            var phieuTT = ql.PHIEUTHANHTOANs.FirstOrDefault(pt => pt.MAHD == id);
            string tenPT = "Chưa thanh toán";
            int diemDung = 0;
            decimal thucTra = 0;

            if (phieuTT != null)
            {
                var pt = ql.PHUONGTHUCTHANHTOANs.Find(phieuTT.MAPT);
                tenPT = pt != null ? pt.TENPT : "Khác";
                diemDung = phieuTT.DIEMSUDUNG ?? 0;
                thucTra = (decimal)(phieuTT.SOTIENTHANHTOAN ?? 0);
            }

            var model = new ChiTietGiaoDichViewModel
            {
                MaHD = hoadon.MAHD,
                NgayMua = hoadon.NGAYGD.Value,
                TongTienHD = (decimal)(hoadon.THANHTIEN ?? 0),
                TrangThai = hoadon.TRANGTHAI,
                PhuongThucThanhToan = tenPT,
                DiemDaDung = diemDung,
                SoTienThucTra = thucTra,
                DanhSachVe = new List<TicketInfo>(),
                DanhSachBapNuoc = new List<ComboInfo>()
            };

            // LẤY VÉ & TRUY VẤN RA PHIM, SUẤT CHIẾU 
            var listVe = ql.VEs.Where(v => v.MAHD == id).ToList();
            foreach (var ve in listVe)
            {
                var ctdv = ql.CHITIETDATVEs.FirstOrDefault(ct => ct.MAVE == ve.MAVE);
                string tenPhim = "";
                string poster = "";
                string thoiGianChieu = "";
                string tenRap = "";
                string tenPhong = "";
                string gheNgoi = "";
                string loaiGhe = "";
                if (ctdv != null)
                {
                    var ghe = ql.GHEs.Find(ctdv.MAGHE);
                    if (ghe != null)
                    {
                        gheNgoi = (ghe.DAYGHE /*+ ghe.SOGHE*/).Trim();
                        loaiGhe = ghe.LOAIGHE;
                        tenPhong = ghe.MAPHONG;
                        var suatChieu = ql.SUATCHIEUx.FirstOrDefault(s => s.MAPHONG == ghe.MAPHONG);
                        if (suatChieu != null)
                        {
                            if (suatChieu.NGAYCHIEU.HasValue && suatChieu.GIOCHIEU.HasValue)
                            {
                                DateTime ngay = suatChieu.NGAYCHIEU.Value;
                                TimeSpan gio = suatChieu.GIOCHIEU.Value;
                                thoiGianChieu = ngay.ToString("dd/MM/yyyy") + " - " + gio.ToString(@"hh\:mm");
                            }
                            var phim = ql.PHIMs.Find(suatChieu.MAPHIM);
                            if (phim != null)
                            {
                                tenPhim = phim.TENPHIM;
                                poster = phim.POSTER;
                            }
                            var rap = ql.RAPs.Find(suatChieu.MARAP);
                            if (rap != null) tenRap = rap.TENRAP;
                        }
                    }
                }
                model.DanhSachVe.Add(new TicketInfo
                {
                    TenPhim = tenPhim,
                    Poster = poster,
                    TenRap = tenRap,
                    TenPhong = tenPhong,
                    GheNgoi = gheNgoi,
                    LoaiGhe = loaiGhe,
                    ThoiGianChieu = thoiGianChieu,
                    GiaVe = (decimal)(ve.GIAVE ?? 0)
                });
            }
            var listDichVu = ql.CHITIETHOADONDICHVUs.Where(dv => dv.MAHD == id).ToList();
            foreach (var item in listDichVu)
            {
                var dv = ql.DICHVUs.Find(item.MADV);
                model.DanhSachBapNuoc.Add(new ComboInfo
                {
                    TenDichVu = dv != null ? dv.TENDV : "Dịch vụ",
                    SoLuong = item.SOLUONG ?? 0,
                    ThanhTien = (decimal)(item.THANHTIEN ?? 0)
                });
            }

            return View(model);
        }
    }
    public class ChiTietGiaoDichViewModel
    {
        public string MaHD { get; set; }
        public DateTime NgayMua { get; set; }
        public decimal TongTienHD { get; set; }
        public string TrangThai { get; set; } 
        // Thông tin thanh toán
        public string PhuongThucThanhToan { get; set; }
        public int DiemDaDung { get; set; }
        public decimal SoTienThucTra { get; set; }
        public List<TicketInfo> DanhSachVe { get; set; }
        public List<ComboInfo> DanhSachBapNuoc { get; set; }
    }

    public class TicketInfo
    {
        public string TenPhim { get; set; } 
        public string Poster { get; set; }
        public string TenRap { get; set; }
        public string TenPhong { get; set; }
        public string GheNgoi { get; set; }
        public string LoaiGhe { get; set; }
        public string ThoiGianChieu { get; set; }
        public decimal GiaVe { get; set; }
    }
    public class ComboInfo
    {
        public string TenDichVu { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien { get; set; }
    }
}