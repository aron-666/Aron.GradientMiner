using OpenQA.Selenium.Chrome;

namespace Aron.GradientMiner.Services
{
    public interface IMinerService
    {
        public ChromeDriver driver { get; set; }

        void Start();
        void Stop();
    }
}