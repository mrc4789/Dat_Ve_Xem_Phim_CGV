using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dat_Ve_Xem_Phim_CGV.Models
{
    public class LaySuatChieu
    {
        public string MaPhim { get; set; }
        public string TenPhim { get; set; }
        public string MaLoai { get; set; }
        public string DinhDang { get; set; }
        public string MaRap { get; set; }
        public string TenRap { get; set; }
        public string DiaChi { get; set; }     
        public DateTime NgayChieu { get; set; }
        public TimeSpan GioChieu { get; set; }
        public double GiaCoban { get; set; }
        public string MaSuatChieu { get; set; }
    }
}