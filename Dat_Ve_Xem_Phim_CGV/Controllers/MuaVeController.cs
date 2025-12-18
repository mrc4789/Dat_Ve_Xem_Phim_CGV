using Dat_Ve_Xem_Phim_CGV.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

public class MuaVeController : Controller
{
    public QLDATVEEntities ql = new QLDATVEEntities();

    // Action Index - thêm tham số maPhim
    public ActionResult Index(string maPhim, string diaChi, string ngayChieu, string maLoai)
    {
        List<RAP> danhSachRap = ql.RAPs.ToList();
        List<string> danhSachDiaChi = danhSachRap.Select(x => x.DIACHI).Distinct().OrderBy(x => x).ToList();

        if (string.IsNullOrEmpty(diaChi) && danhSachDiaChi.Any())
        {
            diaChi = danhSachDiaChi.First();
        }

        if (string.IsNullOrEmpty(ngayChieu))
        {
            ngayChieu = DateTime.Now.ToString("yyyy-MM-dd");
        }

        // Lưu maPhim vào ViewBag
        ViewBag.MaPhim = maPhim;
        ViewBag.DiaChiDuocChon = diaChi;
        ViewBag.NgayChieu = ngayChieu;
        ViewBag.LoaiMHChon = maLoai;
        ViewBag.DanhSachDiaChi = danhSachDiaChi;

        return View(danhSachRap);
    }

    // Action _SuatChieu - lọc theo maPhim với model LaySuatChieu đầy đủ
    public ActionResult _SuatChieu(string maPhim, string ngayChieu, string diaChi, string maLoai)
    {
        // Trim các tham số để tránh khoảng trắng thừa
        if (!string.IsNullOrEmpty(diaChi)) diaChi = diaChi.Trim();
        if (!string.IsNullOrEmpty(maLoai)) maLoai = maLoai.Trim();
        if (!string.IsNullOrEmpty(maPhim)) maPhim = maPhim.Trim();

        // Parse ngày chiếu
        DateTime dateParams = string.IsNullOrEmpty(ngayChieu) ? DateTime.Now : DateTime.Parse(ngayChieu);

        // Gọi Stored Procedure với đầy đủ tham số
        var listSuatChieu = ql.Database.SqlQuery<LaySuatChieu>(
            "EXEC TraCuuLichChieu @NgayChieu, @MaLoai, @DiaChi, @MaPhim",
            new SqlParameter("@NgayChieu", dateParams),
            new SqlParameter("@MaLoai", string.IsNullOrEmpty(maLoai) ? (object)DBNull.Value : maLoai),
            new SqlParameter("@DiaChi", string.IsNullOrEmpty(diaChi) ? (object)DBNull.Value : diaChi),
            new SqlParameter("@MaPhim", string.IsNullOrEmpty(maPhim) ? (object)DBNull.Value : maPhim)
        ).ToList();

        return PartialView(listSuatChieu);
    }

    // Hiển thị chọn ngày chiếu - thêm tham số maPhim
    public ActionResult _NgayChieu(string maPhim, string diaChi, string ngayChieu, string maLoai, string maSuatChieu)
    {
        ViewBag.MaPhim = maPhim;
        ViewBag.DiaChiDuocChon = diaChi;
        ViewBag.NgayChieu = ngayChieu;
        ViewBag.LoaiMHChon = maLoai;
        ViewBag.MaSuatChieu = ql.SUATCHIEUx.FirstOrDefault(x => x.MASUATCHIEU == maSuatChieu);

        return PartialView();
    }

    // Hiển thị chọn loại màn hình - thêm tham số maPhim
    public ActionResult _LoaiManHinh(string maPhim, string diaChi, string ngayChieu, string maLoai)
    {
        ViewBag.MaPhim = maPhim;
        ViewBag.DiaChiDuocChon = diaChi;
        ViewBag.NgayChieu = ngayChieu;
        ViewBag.LoaiMHChon = maLoai;

        return PartialView(ql.LOAIMANHINHs.ToList());
    }

    // Hiển thị chọn địa chỉ - thêm tham số maPhim
    public ActionResult _LocTheoDiaChi(string maPhim, string diaChi, string ngayChieu, string maLoai)
    {
        List<string> danhSachDiaChi = ql.RAPs.Select(x => x.DIACHI).Distinct().OrderBy(x => x).ToList();

        ViewBag.MaPhim = maPhim;
        ViewBag.DiaChiDuocChon = diaChi;
        ViewBag.NgayChieu = ngayChieu;
        ViewBag.LoaiMHChon = maLoai;

        return PartialView(danhSachDiaChi);
    }
}