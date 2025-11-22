using Dat_Ve_Xem_Phim_CGV.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

public class MuaVeController : Controller
{
    public QLDATVEEntities ql = new QLDATVEEntities();

    public ActionResult Index(string diaChi, string ngayChieu, string maLoai)
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

        ViewBag.DiaChiDuocChon = diaChi;
        ViewBag.NgayChieu = ngayChieu;
        ViewBag.LoaiMHChon = maLoai;
        ViewBag.DanhSachDiaChi = danhSachDiaChi;

        return View(danhSachRap); 
    }


    public ActionResult _SuatChieu(string ngayChieu, string diaChi, string maLoai)
    {
        if (!string.IsNullOrEmpty(diaChi)) diaChi = diaChi.Trim();
        if (!string.IsNullOrEmpty(maLoai)) maLoai = maLoai.Trim();

        DateTime dateParams = string.IsNullOrEmpty(ngayChieu) ? DateTime.Now : DateTime.Parse(ngayChieu);

        object diaChiParam = string.IsNullOrEmpty(diaChi) ? (object)DBNull.Value : diaChi;
        object maLoaiParam = string.IsNullOrEmpty(maLoai) ? (object)DBNull.Value : maLoai;

        var listSuatChieu = ql.Database.SqlQuery<LaySuatChieu>(
            "EXEC TraCuuLichChieu @NgayChieu, @MaLoai, @DiaChi",
            new System.Data.SqlClient.SqlParameter("@NgayChieu", dateParams),
            new System.Data.SqlClient.SqlParameter("@DiaChi", diaChiParam),
            new System.Data.SqlClient.SqlParameter("@MaLoai", maLoaiParam)
        ).ToList();

        return PartialView(listSuatChieu);
    }
    // Hiển thị chọn ngày chiếu
    public ActionResult _NgayChieu(string diaChi, string ngayChieu, string maLoai, string maSuatChieu)
    {
        ViewBag.DiaChiDuocChon = diaChi;
        ViewBag.NgayChieu = ngayChieu;
        ViewBag.LoaiMHChon = maLoai;
        ViewBag.MaSuatChieu = ql.SUATCHIEUx.FirstOrDefault(x => x.MASUATCHIEU == maSuatChieu);
        return PartialView();
    }

    // Hiển thị chọn loại màn hình
    public ActionResult _LoaiManHinh(string diaChi, string ngayChieu, string maLoai)
    {
        ViewBag.DiaChiDuocChon = diaChi;
        ViewBag.NgayChieu = ngayChieu;
        ViewBag.LoaiMHChon = maLoai;

        return PartialView(ql.LOAIMANHINHs.ToList());
    }

    // Hiển thị chọn địa chỉ
    public ActionResult _LocTheoDiaChi(string diaChi, string ngayChieu, string maLoai)
    {
        List<string> danhSachDiaChi = ql.RAPs.Select(x => x.DIACHI).ToList();

        ViewBag.DiaChiDuocChon = diaChi;
        ViewBag.NgayChieu = ngayChieu;
        ViewBag.LoaiMHChon = maLoai;
        return PartialView(danhSachDiaChi);
    }
}