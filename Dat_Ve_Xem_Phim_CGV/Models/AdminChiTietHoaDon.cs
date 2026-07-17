using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dat_Ve_Xem_Phim_CGV.Models
{
    public class AdminChiTietHoaDon
    {
        public string MaHD { get; set; }
        public string MaGiaoDich { get; set; }
        public DateTime? NgayDat { get; set; }
        public decimal TongTien { get; set; }
        public string TenPhim { get; set; }
        public string DoTuoi { get; set; }
        public string ThoiLuongHienThi { get; set; }
        public string DinhDang { get; set; }
        public string Poster { get; set; }
        public DateTime? NgayChieu { get; set; }
        public TimeSpan? GioChieu { get; set; }
        public string MaRapLabel { get; set; }
        public string TenRap { get; set; }
        public string DiaChiRap { get; set; }
        public string DanhSachGhe { get; set; }
        public int SoLuongCombo { get; set; }
        public string MaVe { get; set; }
        public string MaKH { get; set; }
    }
}