using Dat_Ve_Xem_Phim_CGV.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace WebRapPhim.Controllers
{
    public class QLPhimController : Controller
    {
        public QLDATVEEntities ql = new QLDATVEEntities();

        // GET: QLPhim
        public ActionResult Index()
        {
            return View(ql.PHIMs.ToList());
        }

        // GET: QLPhim/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PHIM pHIM = ql.PHIMs.Find(id);
            if (pHIM == null)
            {
                return HttpNotFound();
            }
            return View(pHIM);
        }

        // GET: QLPhim/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: QLPhim/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MAPHIM,TENPHIM,NHASANXUAT,THELOAI,THOILUONG,NGONNGU,DINHDANG,NGAYPH,TRANGTHAI,MOTA,POSTER,TRAILER,DOTUOI,DIENVIEN")] PHIM pHIM)
        {
            if (ModelState.IsValid)
            {
                ql.PHIMs.Add(pHIM);
                ql.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(pHIM);
        }

        // GET: QLPhim/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PHIM pHIM = ql.PHIMs.Find(id);
            if (pHIM == null)
            {
                return HttpNotFound();
            }
            return View(pHIM);
        }

        // POST: QLPhim/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MAPHIM,TENPHIM,NHASANXUAT,THELOAI,THOILUONG,NGONNGU,DINHDANG,NGAYPH,TRANGTHAI,MOTA,POSTER,TRAILER,DOTUOI,DIENVIEN")] PHIM pHIM)
        {
            if (ModelState.IsValid)
            {
                ql.Entry(pHIM).State = EntityState.Modified;
                ql.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(pHIM);
        }

        // GET: QLPhim/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PHIM pHIM = ql.PHIMs.Find(id);
            if (pHIM == null)
            {
                return HttpNotFound();
            }
            return View(pHIM);
        }

        // POST: QLPhim/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            PHIM pHIM = ql.PHIMs.Find(id);
            ql.PHIMs.Remove(pHIM);
            ql.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: /Phim/DanhSach?loai=dangchieu&page=1
        public ActionResult DanhSach(
    string loai = "tatca",
    string search = "",
    string theloai = "tatca",
    int page = 1)
        {
            const int pageSize = 12;
            page = page < 1 ? 1 : page;

            IQueryable<PHIM> query = ql.PHIMs.AsNoTracking();

            // Lọc theo loại (trạng thái)
            switch (loai.ToLower())
            {
                case "dangchieu": query = query.Where(p => p.TRANGTHAI == "Đang chiếu"); ViewBag.Title = "Phim Đang Chiếu"; break;
                case "sapchieu": query = query.Where(p => p.TRANGTHAI == "Sắp chiếu"); ViewBag.Title = "Phim Sắp Chiếu"; break;
                case "ngungchieu": query = query.Where(p => p.TRANGTHAI == "Ngưng chiếu"); ViewBag.Title = "Phim Ngưng Chiếu"; break;
                default: ViewBag.Title = "Tất Cả Phim"; break;
            }

            // Tìm kiếm theo tên
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.TENPHIM.Contains(search));
            }

            // Lọc theo thể loại (giả sử có trường THELOAI trong PHIM)
            if (theloai != "tatca")
            {
                query = query.Where(p => p.THELOAI == theloai);
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var phimList = query
                .OrderBy(p => p.NGAYPH)
                .ThenBy(p => p.TENPHIM)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Truyền dữ liệu
            ViewBag.Loai = loai;
            ViewBag.Search = search;
            ViewBag.TheLoai = theloai; // <-- THÊM DÒNG NÀY
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(phimList);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ql.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
