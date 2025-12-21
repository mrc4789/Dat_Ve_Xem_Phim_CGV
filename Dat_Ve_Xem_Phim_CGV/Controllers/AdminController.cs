using Dat_Ve_Xem_Phim_CGV.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using WebGrease.Css.Extensions;

namespace Dat_Ve_Xem_Phim_CGV.Controllers
{
    public class AdminController : Controller
    {
        QLDATVEEntities db = new QLDATVEEntities();
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var user = Session["TaiKhoan"] as KHACHHANG;
            if (user == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "User", action = "Index" }));
                return;
            }
            if (user.MACV != 1)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "User", action = "ProFile" }));
                return;
            }
            base.OnActionExecuting(filterContext);
        }
        public ActionResult Dashboard()
        {
            ViewBag.PageTitle = "Tổng quan";
            DateTime today = DateTime.Today;

            ViewBag.TongPhim = db.PHIMs.Count();
            ViewBag.TongKhach = db.KHACHHANGs.Count();
            ViewBag.VeHomNay = db.VEs.Count(v => DbFunctions.TruncateTime(v.NGAYDATVE) == today);
            ViewBag.DoanhThuNgay = db.HOADONs
                .Where(h => DbFunctions.TruncateTime(h.NGAYGD) == today && h.TRANGTHAI == "Đã thanh toán")
                .Sum(h => (double?)h.THANHTIEN) ?? 0;

            var last7Days = Enumerable.Range(0, 7)
                .Select(i => today.AddDays(-i))
                .OrderBy(d => d).ToList();

            var doanhThuRaw = db.HOADONs
                .Where(h => h.TRANGTHAI == "Đã thanh toán" && DbFunctions.TruncateTime(h.NGAYGD) >= last7Days.FirstOrDefault())
                .GroupBy(h => DbFunctions.TruncateTime(h.NGAYGD))
                .Select(g => new { Ngay = g.Key, Tong = g.Sum(h => h.THANHTIEN) })
                .ToList();

            ViewBag.RevenueLabels = last7Days.Select(d => d.ToString("dd/MM")).ToList();
            ViewBag.RevenueValues = last7Days.Select(d => doanhThuRaw.FirstOrDefault(r => r.Ngay == d)?.Tong ?? 0).ToList();

            var tatcaHD = db.HOADONs.ToList();
            var tongSo = tatcaHD.Count();
            if (tongSo > 0)
            {
                ViewBag.PctThanhToan = (tatcaHD.Count(h => h.TRANGTHAI == "Đã thanh toán") * 100.0 / tongSo);
                ViewBag.PctChuaThanhToan = (tatcaHD.Count(h => h.TRANGTHAI == "Chưa thanh toán") * 100.0 / tongSo);
                ViewBag.PctHuy = (tatcaHD.Count(h => h.TRANGTHAI == "Đã hủy") * 100.0 / tongSo);
                ViewBag.TongSoHD = tongSo;
            }

            var topMovies = db.VEs
                .GroupBy(v => v.SUATCHIEU.PHIM.TENPHIM)
                .Select(g => new {
                    TenPhim = g.Key,
                    DoanhThuRaw = g.Sum(v => v.GIAVE) 
                })
                .ToList()
                .Select(x => new ThongKePhimViewModel
                {
                    TenPhim = x.TenPhim,
                    DoanhThu = (decimal)(x.DoanhThuRaw ?? 0) 
                })
                .OrderByDescending(x => x.DoanhThu)
                .Take(5)
                .ToList();


            ViewBag.RecentInvoices = db.HOADONs.OrderByDescending(h => h.NGAYGD).Take(5).ToList();

            return View(topMovies);
        }

        private void CapNhatTrangThaiPhimDuaTrenSuatChieu(string maPhim)
        {
            var phim = db.PHIMs.Find(maPhim);
            if (phim == null) return;

            DateTime homNay = DateTime.Today;

            var suatChieuCuoi = db.SUATCHIEUx
                .Where(s => s.MAPHIM == maPhim)
                .OrderByDescending(s => s.NGAYCHIEU)
                .ThenByDescending(s => s.GIOCHIEU)
                .FirstOrDefault();

            if (suatChieuCuoi != null)
            {
                DateTime ngayChieuCuoi = suatChieuCuoi.NGAYCHIEU.Value;

                if (ngayChieuCuoi > homNay)
                {
                    phim.TRANGTHAI = (phim.NGAYPH > homNay) ? "Sắp chiếu" : "Đang chiếu";
                }
                else if (ngayChieuCuoi == homNay)
                {
                    phim.TRANGTHAI = "Đang chiếu";
                }
                else
                {
                    phim.TRANGTHAI = "Ngưng chiếu";
                }
            }
            else
            {
                phim.TRANGTHAI = (phim.NGAYPH > homNay) ? "Sắp chiếu" : "Ngưng chiếu";
            }

            db.Entry(phim).State = EntityState.Modified;
            db.SaveChanges();
        }

        public ActionResult QuanLyPhim()
        {
            var dsPhim = db.PHIMs.ToList();
            foreach (var p in dsPhim)
            {
                CapNhatTrangThaiPhimDuaTrenSuatChieu(p.MAPHIM);
            }

            ViewBag.PageTitle = "Quản lý phim";
            return View(db.PHIMs.ToList());
        }

        private string TaoMaPhim()
        {
            var lastPhim = db.PHIMs
                .OrderByDescending(p => p.MAPHIM)
                .FirstOrDefault();

            if (lastPhim == null)
                return "PH001";

            int so = int.Parse(lastPhim.MAPHIM.Substring(2)) + 1;
            return "PH" + so.ToString("D3");
        }

        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.PageTitle = "Thêm phim mới";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PHIM phim, HttpPostedFileBase posterFile)
        {
            if (posterFile == null || posterFile.ContentLength == 0)
            {
                ModelState.AddModelError("POSTER", "Vui lòng tải lên poster phim");
            }

            if (ModelState.IsValid)
            {
                phim.MAPHIM = TaoMaPhim();
                phim.TRANGTHAI = "Sắp chiếu";

                DateTime homNay = DateTime.Today;
                if (phim.NGAYPH <= homNay)
                {
                    phim.TRANGTHAI = "Đang chiếu";
                }
                else
                {
                    phim.TRANGTHAI = "Sắp chiếu";
                }

                if (posterFile != null)
                {
                    string fileName = Path.GetFileName(posterFile.FileName);
                    string path = Path.Combine(Server.MapPath("/Img"), fileName);
                    posterFile.SaveAs(path);
                    phim.POSTER = fileName;
                }

                db.PHIMs.Add(phim);
                db.SaveChanges();
                CapNhatTrangThaiPhimDuaTrenSuatChieu(phim.MAPHIM);
                TempData["Success"] = "Thêm phim thành công!";
                return RedirectToAction("QuanLyPhim");
            }

            ViewBag.PageTitle = "Thêm phim mới";
            return View(phim);
        }

        // GET: Admin/Edit/
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            PHIM phim = db.PHIMs.Find(id);

            if (phim == null)
            {
                return HttpNotFound();
            }

            ViewBag.PageTitle = "Chỉnh sửa phim";
            return View(phim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(PHIM phim, HttpPostedFileBase posterFile)
        {
            if (ModelState.IsValid)
            {
                if (posterFile != null && posterFile.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(posterFile.FileName);
                    string path = Path.Combine(Server.MapPath("/Img"), fileName);
                    posterFile.SaveAs(path);
                    phim.POSTER = fileName;
                }

                db.Entry(phim).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("QuanLyPhim");
            }

            return View(phim);
        }

        public ActionResult XoaPhim(string id)
        {
            var p = db.PHIMs.Find(id);
            if (p == null) return HttpNotFound();

            if (p.TRANGTHAI != "Ngưng chiếu")
            {
                TempData["Error"] = "Không thể xóa! Chỉ được phép xóa những phim có trạng thái 'Ngưng chiếu'.";
                return RedirectToAction("QuanLyPhim");
            }

            bool daCoSuatChieu = db.SUATCHIEUx.Any(s => s.MAPHIM == id);
            if (daCoSuatChieu)
            {
                TempData["Error"] = "Không thể xóa! Phim này đã có dữ liệu suất chiếu.";
                return RedirectToAction("QuanLyPhim");
            }

            db.PHIMs.Remove(p);
            db.SaveChanges();
            TempData["Success"] = "Đã xóa phim thành công!";
            return RedirectToAction("QuanLyPhim");
        }

        // GET
        public ActionResult QuanLySuatChieu()
        {
            var suatChieus = db.SUATCHIEUx.Include(s => s.PHIM).Include(s => s.RAP).Include(s => s.PHONGCHIEU).ToList();

            ViewBag.MAPHIM = new SelectList(db.PHIMs, "MAPHIM", "TENPHIM");
            ViewBag.MARAP = new SelectList(db.RAPs, "MARAP", "TENRAP");
            ViewBag.MAPHONG = new SelectList(db.PHONGCHIEUx, "MAPHONG", "MAPHONG");

            ViewBag.PageTitle = "Quản lý suất chiếu";
            ViewBag.MARAP = new SelectList(db.RAPs.ToList().Select(r => new {
                MARAP = r.MARAP.Trim(),
                TENRAP = r.TENRAP
            }), "MARAP", "TENRAP");

            ViewBag.MAPHONG = new SelectList(db.PHONGCHIEUx.ToList().Select(p => new {
                MAPHONG = p.MAPHONG.Trim()
            }), "MAPHONG", "MAPHONG");
            return View(suatChieus);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateSuatChieu(SUATCHIEU sc)
        {
            if (ModelState.IsValid)
            {
                var phim = db.PHIMs.Find(sc.MAPHIM);
                if (sc.NGAYCHIEU > phim.NGAYPH.Value.AddMonths(2))
                {
                    TempData["Error"] = "Ngày chiếu vượt quá giới hạn 2 tháng của phim!";
                    return RedirectToAction("QuanLySuatChieu");
                }

                if (sc.NGAYCHIEU.Value.Add(sc.GIOCHIEU.Value) < DateTime.Now)
                {
                    TempData["Error"] = "Không thể thêm suất chiếu ở quá khứ!";
                    return RedirectToAction("QuanLySuatChieu");
                }

                var phimHienTai = db.PHIMs.Find(sc.MAPHIM);
                if (phimHienTai == null || phimHienTai.THOILUONG == null)
                {
                    TempData["Error"] = "Lỗi: Không tìm thấy thông tin phim hoặc phim chưa có thời lượng!";
                    return RedirectToAction("QuanLySuatChieu");
                }

                if (sc.NGAYCHIEU.HasValue && phimHienTai.NGAYPH.HasValue)
                {
                    DateTime ngayHetHan = phimHienTai.NGAYPH.Value.AddMonths(2);
                    if (sc.NGAYCHIEU.Value > ngayHetHan)
                    {
                        TempData["Error"] = $"Không thể thêm suất chiếu! Phim này đã hết hạn chiếu vào ngày {ngayHetHan:dd/MM/yyyy} (quá 2 tháng kể từ ngày phát hành).";
                        return RedirectToAction("QuanLySuatChieu");
                    }

                    if (sc.NGAYCHIEU.Value < phimHienTai.NGAYPH.Value)
                    {
                        TempData["Error"] = "Không thể thêm suất chiếu trước ngày phát hành phim!";
                        return RedirectToAction("QuanLySuatChieu");
                    }
                }

                DateTime startNew = sc.NGAYCHIEU.Value.Add(sc.GIOCHIEU.Value);
                DateTime endNew = startNew.AddMinutes((double)phimHienTai.THOILUONG);

                var dsSuatChieuTrongNgay = db.SUATCHIEUx
                    .Where(s => s.MAPHONG == sc.MAPHONG && s.NGAYCHIEU == sc.NGAYCHIEU)
                    .ToList();

                foreach (var item in dsSuatChieuTrongNgay)
                {
                    var phimCu = db.PHIMs.Find(item.MAPHIM);

                    DateTime startOld = item.NGAYCHIEU.Value.Add(item.GIOCHIEU.Value);
                    DateTime endOld = startOld.AddMinutes((double)phimCu.THOILUONG);

                    bool isOverlap = !(startNew >= endOld.AddMinutes(30) || endNew.AddMinutes(30) <= startOld);

                    if (isOverlap)
                    {
                        TempData["Error"] = $"Trùng lịch! Phòng này đã có phim '{phimCu.TENPHIM}' chiếu từ {startOld:HH:mm} đến {endOld:HH:mm}. Vui lòng chọn giờ khác cách ít nhất 30p.";
                        return RedirectToAction("QuanLySuatChieu");
                    }
                }

                var last = db.SUATCHIEUx.OrderByDescending(x => x.MASUATCHIEU).FirstOrDefault();
                int num = (last == null) ? 1 : int.Parse(last.MASUATCHIEU.Substring(2)) + 1;
                sc.MASUATCHIEU = "SC" + num.ToString("D3");
                sc.TRANGTHAI = "Sắp chiếu";

                db.SUATCHIEUx.Add(sc);
                db.SaveChanges();

                CapNhatTrangThaiPhimDuaTrenSuatChieu(sc.MAPHIM);

                TempData["Success"] = "Thêm suất chiếu thành công!";
                return RedirectToAction("QuanLySuatChieu");
            }
            return RedirectToAction("QuanLySuatChieu");
        }

        [HttpGet]
        public JsonResult GetSuatChieu(string id)
        {
            var sc = db.SUATCHIEUx.Where(x => x.MASUATCHIEU == id).Select(x => new {
                MASUATCHIEU = x.MASUATCHIEU,
                MAPHIM = x.MAPHIM,
                TENPHIM = x.PHIM.TENPHIM,
                MARAP = x.MARAP.Trim(),
                MAPHONG = x.MAPHONG.Trim(),
                NGAYCHIEU = x.NGAYCHIEU.ToString(),
                GIOCHIEU = x.GIOCHIEU.ToString(),
                GIACOBAN = x.GIACOBAN
            }).FirstOrDefault();

            return Json(sc, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditSuatChieu(SUATCHIEU sc)
        {
            if (ModelState.IsValid)
            {
                DateTime thoiGianMoi = sc.NGAYCHIEU.Value.Add(sc.GIOCHIEU.Value);
                if (thoiGianMoi < DateTime.Now)
                {
                    TempData["Error"] = "Thời gian chỉnh sửa phải lớn hơn thời gian hiện tại!";
                    return RedirectToAction("QuanLySuatChieu");
                }

                var currentSC = db.SUATCHIEUx.Find(sc.MASUATCHIEU);
                if (currentSC == null) return HttpNotFound();

                var phimHienTai = db.PHIMs.Find(sc.MAPHIM);
                DateTime startNew = sc.NGAYCHIEU.Value.Add(sc.GIOCHIEU.Value);
                DateTime endNew = startNew.AddMinutes((double)phimHienTai.THOILUONG);

                var dsSuatChieuKhac = db.SUATCHIEUx
                    .Where(s => s.MAPHONG == sc.MAPHONG
                             && s.NGAYCHIEU == sc.NGAYCHIEU
                             && s.MASUATCHIEU != sc.MASUATCHIEU)
                    .ToList();

                foreach (var item in dsSuatChieuKhac)
                {
                    var phimKhac = db.PHIMs.Find(item.MAPHIM);
                    DateTime startOld = item.NGAYCHIEU.Value.Add(item.GIOCHIEU.Value);
                    DateTime endOld = startOld.AddMinutes((double)phimKhac.THOILUONG);

                    bool isOverlap = !(startNew >= endOld.AddMinutes(30) || endNew.AddMinutes(30) <= startOld);

                    if (isOverlap)
                    {
                        TempData["Error"] = $"Không thể đổi sang phòng {sc.MAPHONG}! Đã có phim '{phimKhac.TENPHIM}' chiếu tại đây ({startOld:HH:mm} - {endOld:HH:mm}).";
                        return RedirectToAction("QuanLySuatChieu");
                    }
                }

                currentSC.MAPHONG = sc.MAPHONG;
                currentSC.MARAP = sc.MARAP;
                currentSC.NGAYCHIEU = sc.NGAYCHIEU;
                currentSC.GIOCHIEU = sc.GIOCHIEU;
                currentSC.GIACOBAN = sc.GIACOBAN;

                db.Entry(currentSC).State = EntityState.Modified;
                db.SaveChanges();
                CapNhatTrangThaiPhimDuaTrenSuatChieu(sc.MAPHIM);
                TempData["Success"] = "Cập nhật thông tin và phòng chiếu thành công!";
            }
            return RedirectToAction("QuanLySuatChieu");
        }

        public ActionResult DeleteSuatChieu(string id)
        {
            try
            {
                bool daCoNguoiDat = db.VEs.Any(v => v.MASUATCHIEU == id);

                if (daCoNguoiDat)
                {
                    TempData["Error"] = "Không thể xóa! Suất chiếu này đã có khách hàng đặt vé.";
                    return RedirectToAction("QuanLySuatChieu");
                }

                var sc = db.SUATCHIEUx.Find(id);
                if (sc != null)
                {
                    var tinhTrangGhes = db.TINHTRANGGHEs.Where(t => t.MASUATCHIEU == id).ToList();
                    if (tinhTrangGhes.Any())
                    {
                        db.TINHTRANGGHEs.RemoveRange(tinhTrangGhes);
                    }

                    string maPhimXoa = sc.MAPHIM; 
                    db.SUATCHIEUx.Remove(sc);
                    db.SaveChanges();

                    CapNhatTrangThaiPhimDuaTrenSuatChieu(maPhimXoa);
                    TempData["Success"] = "Đã xóa suất chiếu và các dữ liệu liên quan thành công!";
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.InnerException?.Message ?? ex.Message;
                TempData["Error"] = "Lỗi hệ thống: " + innerMsg;
            }

            return RedirectToAction("QuanLySuatChieu");
        }

        public ActionResult QuanLyVe()
        {
            // Sử dụng Include để lấy kèm dữ liệu Khách hàng
            var dsHoaDon = db.HOADONs
                             .Include(h => h.KHACHHANG)
                             .OrderByDescending(h => h.NGAYGD)
                             .ToList();

            // Kiểm tra nếu Model rỗng thì debug tại đây
            if (dsHoaDon == null) dsHoaDon = new List<HOADON>();

            ViewBag.PageTitle = "Quản lý đặt vé";
            return View(dsHoaDon);
        }

        [HttpGet]
        public JsonResult GetDetailVe(string maHD)
        {
            var chiTiet = db.VEs.Where(v => v.MAHD == maHD).Select(v => new {
                MaVe = v.MAVE,
                TenGhe = v.MAGHE,
                GiaVe = v.GIAVE,
                TenPhim = v.SUATCHIEU.PHIM.TENPHIM,
                NgayChieu = v.SUATCHIEU.NGAYCHIEU.ToString(),
                GioChieu = v.SUATCHIEU.GIOCHIEU.ToString()
            }).ToList();
            return Json(chiTiet, JsonRequestBehavior.AllowGet);
        }

        public ActionResult HuyHoaDon(string id)
        {
            try
            {
                var hd = db.HOADONs.Find(id);
                if (hd != null)
                {
                    var ves = db.VEs.Where(v => v.MAHD == id).ToList();

                    foreach (var ve in ves)
                    {
                        // Tìm tình trạng ghế tương ứng để xóa/cập nhật
                        var tinhTrang = db.TINHTRANGGHEs.FirstOrDefault(t =>
                            t.MASUATCHIEU == ve.MASUATCHIEU && t.MAGHE == ve.MAGHE);

                        if (tinhTrang != null)
                        {
                            db.TINHTRANGGHEs.Remove(tinhTrang);
                        }
                    }

                    db.VEs.RemoveRange(ves);
                    db.HOADONs.Remove(hd);
                    db.SaveChanges();

                    TempData["Success"] = "Đã hủy hóa đơn và giải phóng ghế thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi hủy hóa đơn: " + ex.Message;
            }
            return RedirectToAction("QuanLyVe");
        }

        public ActionResult ThongKe()
        {
            ViewBag.PageTitle = "Thống kê & Báo cáo";

            // 1. Các con số tổng quát
            ViewBag.TongDoanhThu = db.HOADONs.Sum(h => h.THANHTIEN) ?? 0;
            ViewBag.TongVe = db.VEs.Count();
            ViewBag.TongKhachHang = db.KHACHHANGs.Count();
            ViewBag.TongPhim = db.PHIMs.Count();

            // 2. Dữ liệu biểu đồ doanh thu 7 ngày gần nhất
            var sevenDaysAgo = DateTime.Now.Date.AddDays(-7);
            var today = DateTime.Now.Date;

            var revenueData = db.HOADONs
                .Where(h => h.NGAYGD >= sevenDaysAgo && h.NGAYGD <= today)
                .GroupBy(h => DbFunctions.TruncateTime(h.NGAYGD)) // Cắt bỏ giờ phút, chỉ giữ lại Ngày/Tháng/Năm
                .Select(g => new {
                    Ngay = g.Key,
                    Tien = g.Sum(h => h.THANHTIEN)
                })
                .OrderBy(g => g.Ngay)
                .ToList();

            // Đảm bảo không bị null và map dữ liệu chuẩn
            ViewBag.RevenueLabels = revenueData.Select(x => x.Ngay.Value.ToString("dd/MM")).ToList();
            ViewBag.RevenueValues = revenueData.Select(x => x.Tien ?? 0).ToList();

            int currentYear = DateTime.Now.Year;
            var monthlyData = db.HOADONs
                .Where(h => h.NGAYGD.Value.Year == currentYear)
                .GroupBy(h => h.NGAYGD.Value.Month)
                .Select(g => new {
                    Thang = g.Key,
                    Tien = g.Sum(h => h.THANHTIEN)
                })
                .OrderBy(g => g.Thang)
                .ToList();

            // Tạo mảng 12 tháng để đảm bảo tháng nào không có tiền vẫn hiện số 0
            var monthsLabels = new string[12];
            var monthsValues = new decimal[12];
            for (int i = 1; i <= 12; i++)
            {
                monthsLabels[i - 1] = "Thg " + i;
                var data = monthlyData.FirstOrDefault(x => x.Thang == i);
                monthsValues[i - 1] = data != null ? (decimal)data.Tien : 0;
            }
            ViewBag.MonthlyLabels = monthsLabels;
            ViewBag.MonthlyValues = monthsValues;

            // 2. Thống kê Doanh thu theo các Năm (5 năm gần nhất)
            int startYear = currentYear - 4;
            var yearlyData = db.HOADONs
                .Where(h => h.NGAYGD.Value.Year >= startYear)
                .GroupBy(h => h.NGAYGD.Value.Year)
                .Select(g => new {
                    Nam = g.Key,
                    Tien = g.Sum(h => h.THANHTIEN)
                })
                .OrderBy(g => g.Nam)
                .ToList();

            ViewBag.YearlyLabels = yearlyData.Select(x => x.Nam.ToString()).ToList();
            ViewBag.YearlyValues = yearlyData.Select(x => x.Tien).ToList();

            // 3. Top 5 phim doanh thu cao nhất
            var topMovies = (from ve in db.VEs
                             join sc in db.SUATCHIEUx on ve.MASUATCHIEU equals sc.MASUATCHIEU
                             join p in db.PHIMs on sc.MAPHIM equals p.MAPHIM
                             group ve by p.TENPHIM into g
                             select new
                             {
                                 TenPhim = g.Key,
                                 DoanhThuRaw = g.Sum(v => v.GIAVE), // Tính tổng với kiểu gốc trong DB
                                 SoVe = g.Count()
                             })
                 .OrderByDescending(x => x.DoanhThuRaw)
                 .Take(5)
                 .ToList() // Chuyển dữ liệu về bộ nhớ (Memory)
                 .Select(x => new ThongKePhimViewModel
                 {
                     TenPhim = x.TenPhim,
                     DoanhThu = Convert.ToDecimal(x.DoanhThuRaw), // Ép kiểu an toàn tại đây
                     SoVe = x.SoVe
                 }).ToList();

            return View(topMovies);
        }

        /////Hoài Nam/////
        ///DSKH.../////
        public ActionResult QuanLyKhachHang(string TimKH, int? page)
        {
            ViewBag.PageTitle = "Quản lý khách hàng";
            var khachHangs = db.KHACHHANGs.AsQueryable();
            if (!string.IsNullOrEmpty(TimKH))
            {
                khachHangs = khachHangs.Where(k => k.HOTEN.Contains(TimKH) || k.SDT.Contains(TimKH));
            }
            int pageSize = 5;
            int pageNumber = (page ?? 1);
            return View(khachHangs.OrderBy(k => k.MAKH).ToPagedList(pageNumber, pageSize));
        }

        ///Chi Tiet Kh và lịch sử giao dịch///
        public ActionResult ChiTietKhachHang(string id)
        {
            ViewBag.PageTitle = "Chi tiết khách hàng";
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var khachHang = db.KHACHHANGs.Find(id);
            if (khachHang == null) return HttpNotFound();
            ViewBag.ListHoaDon = db.HOADONs.Where(h => h.MAKH == id).OrderByDescending(h => h.NGAYGD).ToList();
            return View(khachHang);
        }

        ////Chi tiết hóa đơn////
        public ActionResult ChiTietHoaDon(string id)
        {
            ViewBag.PageTitle = "Chi tiết hóa đơn";
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var hoadon = db.HOADONs.Find(id);
            if (hoadon == null) return HttpNotFound();

            var listVe = db.VEs.Where(v => v.MAHD == id).ToList();
            var listDichVu = db.CHITIETHOADONDICHVUs.Where(dv => dv.MAHD == id).ToList();

            var model = new AdminChiTietHoaDon();

            // 1. Map thông tin cơ bản
            model.MaHD = hoadon.MAHD.Trim();
            model.MaGiaoDich = hoadon.MAHD.Trim();
            model.NgayDat = hoadon.NGAYGD;
            model.TongTien = (decimal)(hoadon.THANHTIEN ?? 0);
            model.SoLuongCombo = listDichVu.Sum(x => x.SOLUONG ?? 0);

            // --- QUAN TRỌNG: Lấy Mã Khách Hàng ---
            model.MaKH = hoadon.MAKH;

            // 2. Xử lý danh sách vé & ghế
            if (listVe.Count > 0)
            {
                model.MaVe = string.Join(", ", listVe.Select(v => v.MAVE.Trim()));
                model.DanhSachGhe = string.Join(", ", listVe.Select(v => v.MAGHE.Trim()));
            }
            else
            {
                model.MaVe = "---";
                model.DanhSachGhe = "---";
            }

            // 3. Xử lý thông tin Phim - Rạp - Suất chiếu (Lấy từ vé đầu tiên)
            if (listVe.Count > 0)
            {
                var veDau = listVe.First();
                var suatChieu = db.SUATCHIEUx.Find(veDau.MASUATCHIEU);

                if (suatChieu != null)
                {
                    model.NgayChieu = suatChieu.NGAYCHIEU;
                    model.GioChieu = suatChieu.GIOCHIEU;

                    // Thông tin Phim
                    var phim = db.PHIMs.Find(suatChieu.MAPHIM);
                    if (phim != null)
                    {
                        model.TenPhim = phim.TENPHIM;
                        model.DoTuoi = phim.DOTUOI;
                        model.DinhDang = phim.DINHDANG;
                        model.Poster = phim.POSTER;

                        // Tính thời lượng (Phút -> Giờ Phút)
                        int tongPhut = phim.THOILUONG ?? 0;
                        int gio = tongPhut / 60;
                        int phut = tongPhut % 60;

                        if (gio > 0)
                            model.ThoiLuongHienThi = $"{gio} giờ {phut} phút";
                        else
                            model.ThoiLuongHienThi = $"{phut} phút";
                    }

                    // Thông tin Rạp
                    var rap = db.RAPs.Find(suatChieu.MARAP);
                    if (rap != null)
                    {
                        model.MaRapLabel = rap.MARAP.Trim();
                        model.TenRap = rap.TENRAP;
                        model.DiaChiRap = rap.DIACHI;
                    }
                }
            }

            return View(model);
        }

        //QUẢN LÝ KHUYẾN MÃI
        public ActionResult QuanLyKhuyenMai(string TimKM, int? page)
        {
            ViewBag.PageTitle = "Quản lý khuyến mãi";
            var km = db.KHUYENMAIs.AsQueryable();
            if (!string.IsNullOrEmpty(TimKM))
            {
                km = km.Where(k => k.TENKM.Contains(TimKM) || k.MAKM.Contains(TimKM));
            }
            int pageSize = 5;
            int pageNumber = (page ?? 1);
            return View(km.OrderByDescending(k => k.NGAYBD).ToPagedList(pageNumber, pageSize));
        }
        public ActionResult ThemKhuyenMai()
        {
            ViewBag.PageTitle = "Thêm khuyến mãi";
            var model = new KHUYENMAI
            {
                NGAYBD = DateTime.Today
            };
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemKhuyenMai(KHUYENMAI model)
        {
            DateTime today = DateTime.Today;
            if (model.NGAYBD < today)
            {
                ModelState.AddModelError("NGAYBD", "Ngày bắt đầu không được nhỏ hơn hôm nay!");
            }
            if (model.NGAYKT < today)
            {
                ModelState.AddModelError("NGAYKT", "Ngày kết thúc phải lớn hơn hoặc bằng hôm nay!");
            }
            if (model.NGAYKT < model.NGAYBD)
            {
                ModelState.AddModelError("NGAYKT", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu!");
            }
            if (db.KHUYENMAIs.Any(k => k.MAKM == model.MAKM))
            {
                ModelState.AddModelError("MAKM", "Mã khuyến mãi đã tồn tại!");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            db.KHUYENMAIs.Add(model);
            db.SaveChanges();
            TempData["Success"] = "Thêm khuyến mãi thành công!";
            return RedirectToAction("QuanLyKhuyenMai");
        }
        public ActionResult SuaKhuyenMai(string id)
        {
            ViewBag.PageTitle = "Sửa khuyến mãi";
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var km = db.KHUYENMAIs.Find(id);
            if (km == null) return HttpNotFound();
            return View(km);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaKhuyenMai(KHUYENMAI model)
        {
            if (model.NGAYKT < model.NGAYBD)
            {
                ModelState.AddModelError("NGAYKT", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu!");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var km = db.KHUYENMAIs.Find(model.MAKM);
            if (km == null)
            {
                return HttpNotFound();
            }
            km.TENKM = model.TENKM;
            km.LOAIKM = model.LOAIKM;
            km.NGAYBD = model.NGAYBD;
            km.NGAYKT = model.NGAYKT;
            km.DIEUKIEN = model.DIEUKIEN;
            db.SaveChanges();
            TempData["Success"] = "Cập nhật khuyến mãi thành công!";
            return RedirectToAction("QuanLyKhuyenMai");
        }
        public ActionResult XoaKhuyenMai(string id)
        {
            var km = db.KHUYENMAIs.Find(id);
            if (km != null)
            {
                var daDung = db.CHITIETKHUYENMAIs.Any(ct => ct.MAKM == id);
                if (daDung)
                {
                    TempData["Error"] = "Không thể xóa khuyến mãi này vì đã có hóa đơn sử dụng!";
                }
                else
                {
                    db.KHUYENMAIs.Remove(km);
                    db.SaveChanges();
                    TempData["Success"] = "Xóa khuyến mãi thành công!";
                }
            }
            return RedirectToAction("QuanLyKhuyenMai");
        }

        // --- 1. GET: Hiển thị giao diện ---
        [HttpGet]
        public ActionResult ThemPhong()
        {
            // Lấy danh sách Rạp
            ViewBag.MaRap = new SelectList(db.RAPs, "MARAP", "TENRAP");

            // Lấy danh sách Loại màn hình (Mới thêm)
            // Value = MALOAI (Lưu vào DB), Text = DINHDANG (Hiển thị cho người dùng chọn)
            ViewBag.MaLoai = new SelectList(db.LOAIMANHINHs, "MALOAI", "DINHDANG");

            return View();
        }

        // --- 2. POST: Xử lý lưu ---
        [HttpPost]
        public ActionResult ThemPhong(PHONGCHIEU phong)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    phong.MAPHONG = TaoMaPhongTuDong();
                    db.PHONGCHIEUx.Add(phong);
                    db.SaveChanges();

                    var maPhongParam = new SqlParameter("@MaPhong", phong.MAPHONG);
                    db.Database.ExecuteSqlCommand("EXEC SinhGheChoPhong @MaPhong", maPhongParam);

                    TempData["ThongBao"] = "Thêm phòng và sinh ghế thành công!";
                    return RedirectToAction("DanhSachPhong");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi: " + ex.Message;
                }
            }

            // Nếu lỗi, phải load lại cả 2 danh sách để không bị null
            ViewBag.MaRap = new SelectList(db.RAPs, "MARAP", "TENRAP", phong.MARAP);
            ViewBag.MaLoai = new SelectList(db.LOAIMANHINHs, "MALOAI", "DINHDANG", phong.MALOAI); // (Mới thêm)

            return View(phong);
        }
        private string TaoMaPhongTuDong()
        {
            // Lấy mã phòng cuối cùng trong DB
            var lastRoom = db.PHONGCHIEUx.OrderByDescending(p => p.MAPHONG).FirstOrDefault();

            if (lastRoom == null)
            {
                return "P001";
            }

            // Tách số: P010 -> lấy 010 -> int 10
            string numberPart = lastRoom.MAPHONG.Substring(1);
            int number = int.Parse(numberPart);
            number++;

            // Format lại: P + 011
            return "P" + number.ToString("D3");
        }

    }

}