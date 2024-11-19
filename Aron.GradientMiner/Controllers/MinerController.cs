using Aron.GradientMiner.Models;
using Aron.GradientMiner.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aron.GradientMiner.Controllers
{
    [Authorize]
    public class MinerController : Controller
    {
        private readonly MinerRecord _minerRecord;
        private readonly IMinerService _minerService;

        public MinerController(MinerRecord minerRecord, IMinerService minerService)
        {
            _minerRecord = minerRecord;
            _minerService = minerService;
        }
        public IActionResult Index()
        {

            return View(_minerRecord);
        }

        public IActionResult Stop()
        {
            _minerService?.Stop();
            return RedirectToAction("");
        }

        public IActionResult Start()
        {
            _minerService?.Start();
            return RedirectToAction("");
        }


        public MinerRecord GetMinerRecord()
        {
            return _minerRecord;
        }
    }
}
