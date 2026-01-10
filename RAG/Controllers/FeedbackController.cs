using Microsoft.AspNetCore.Mvc;

namespace RAG.Controllers
{
    public class FeedbackController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
