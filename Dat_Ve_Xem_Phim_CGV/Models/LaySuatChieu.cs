using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dat_Ve_Xem_Phim_CGV.Models
{
    public class LaySuatChieu
    {
        string maSuatChieu;
        TimeSpan gioChieu; 
        string tenRap; 
        string dinhDang;
        string diaChi;
        public string MaSuatChieu { get => maSuatChieu; set => maSuatChieu = value; }
        public TimeSpan GioChieu { get => gioChieu; set => gioChieu = value; }
        public string TenRap { get => tenRap; set => tenRap = value; }
        public string DinhDang { get => dinhDang; set => dinhDang = value; }
        public string DiaChi { get => diaChi; set => diaChi = value; }

        public LaySuatChieu() { }
    }
}