using System.Web;
using System.Web.Mvc;

namespace Dat_Ve_Xem_Phim_CGV
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
