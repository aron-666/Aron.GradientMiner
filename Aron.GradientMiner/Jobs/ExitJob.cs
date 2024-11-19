using Aron.GradientMiner.Models;
using Aron.GradientMiner.Services;
using OpenQA.Selenium;
using Quartz;
using System.Drawing;
using System.Net;
using System.Xml.Linq;

namespace Aron.GradientMiner.Jobs
{
    public class ExitJob(MinerRecord _minerRecord, IMinerService minerService, IHostApplicationLifetime applicationLifetime) : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                if (File.Exists("shutdown.flag"))
                {

                    applicationLifetime.StopApplication();
                    File.Delete("shutdown.flag");

                }
            }
            catch (Exception e)
            {
            }
            return Task.CompletedTask;

        }




    }
}
