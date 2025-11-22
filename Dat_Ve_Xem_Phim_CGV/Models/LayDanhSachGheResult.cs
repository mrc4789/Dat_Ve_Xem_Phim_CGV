// Class để map kết quả từ stored procedure LayDanhSachGhe
namespace Dat_Ve_Xem_Phim_CGV.Models
{
    public class LayDanhSachGheResult
    {
        public string MAGHE { get; set; }
        public string MAPHONG { get; set; }
        public string LOAIGHE { get; set; }
        public string DAYGHE { get; set; }
        public string SOGHE { get; set; }
        public int? COT { get; set; }
        public double? PHUPHI { get; set; }
        public string TINHTRANG { get; set; }
    }
}