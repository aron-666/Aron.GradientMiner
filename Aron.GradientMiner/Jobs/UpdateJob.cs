using Aron.GradientMiner.Models;
using System.Net;
using System.Xml.Linq;

namespace Aron.GradientMiner.Jobs
{
    public class UpdateJob(MinerRecord _minerRecord) : IHostedService, IDisposable
    {
        private Timer _timer;
        public int Interval { get; } = 10 * 60 * 1000;

        public void Execute(object state)
        {
            try
            {
                // call https://ifconfig.me to get the public IP address
                try
                {
                    _minerRecord.PublicIp = new WebClient().DownloadString("https://ifconfig.me");
                }
                catch (Exception ex)
                {
                    _minerRecord.PublicIp = "Error to get your public ip.";
                }

                // call https://raw.githubusercontent.com/aron-666/Aron.GradientMiner/master/Aron.GradientMiner/Aron.GradientMiner.csproj to get the latest version
                var latestVersion = new WebClient().DownloadString("https://raw.githubusercontent.com/aron-666/Aron.GradientMiner/master/Aron.GradientMiner/Aron.GradientMiner.csproj");

                _minerRecord.LastAppVersion = parseVersion(latestVersion);
            }
            catch (Exception e)
            {
            }
            return;

        }

        private string parseVersion(string xml)
        {
            try
            {
                // 載入 XML 檔案
                XDocument doc = XDocument.Parse(xml);

                // 找到 PropertyGroup 元素
                XElement propertyGroup = doc.Descendants("PropertyGroup").FirstOrDefault();

                if (propertyGroup != null)
                {
                    // 找到 AssemblyVersion 元素
                    XElement assemblyVersionElement = propertyGroup.Element("AssemblyVersion");

                    if (assemblyVersionElement != null)
                    {
                        // 取得 AssemblyVersion 的值
                        string assemblyVersion = assemblyVersionElement.Value;
                        return assemblyVersion;
                    }
                    else
                    {
                    }
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
            return null;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(Execute, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(Interval));
            return Task.CompletedTask;
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

    }
}
