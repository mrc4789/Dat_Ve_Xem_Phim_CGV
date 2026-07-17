// Class để map kết quả từ stored procedure LayDanhSachGhe
using System;

namespace Dat_Ve_Xem_Phim_CGV.Models
{
    public class LayDanhSachGheResult
    {
        public string MAGHE { get; set; }
        public string MAPHONG { get; set; }
        public string LOAIGHE { get; set; }  
        public string DAYGHE { get; set; }  
        public string SOGHE { get; set; }    
        public int COT { get; set; }      
        public int HANG { get; set; }    
        public decimal PHUPHI { get; set; }
        public string TINHTRANG { get; set; } 
        public DateTime? THOIGIANDAT { get; set; }
        public int? ThoiGianConLai { get; set; } 
    }

}