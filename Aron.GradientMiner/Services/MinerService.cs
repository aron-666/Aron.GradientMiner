using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using SeleniumExtras.WaitHelpers;
using System.Net;
using System.Drawing;
using System.Text;
using Aron.GradientMiner.Models;
using Newtonsoft.Json;
using System.Diagnostics.Metrics;

namespace Aron.GradientMiner.Services
{
    public class MinerService : IMinerService
    {
        public ChromeDriver driver { get; set; }
        private readonly AppConfig _appConfig;
        private readonly MinerRecord _minerRecord;
        private readonly string extensionPath = "./Gradient.crx";
        private readonly string extensionId = "caacbgbklghmpodbdafajbgdnegacfmo";
        private bool Enabled { get; set; } = true;

        private Thread? thread;

        private DateTime BeforeRefresh = DateTime.MinValue;
        public MinerService(AppConfig appConfig, MinerRecord minerRecord)
        {
            _appConfig = appConfig;
            _minerRecord = minerRecord;
            // call https://ifconfig.me to get the public IP address
            try
            {
                _minerRecord.PublicIp = new WebClient().DownloadString("https://ifconfig.me");
            }
            catch (Exception ex)
            {
                _minerRecord.PublicIp = "Error to get your public ip.";
            }

            thread = new Thread(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (Enabled)
                        {
                            await Run();
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _minerRecord.Exception = ex.ToString();
                        _minerRecord.ExceptionTime = DateTime.Now;
                        _minerRecord.Status = MinerStatus.Error;
                    }
                    finally
                    {
                        await Task.Delay(10000);
                    }
                }

            })
            { IsBackground = true };

            thread.Start();
        }

        public void Stop()
        {
            Enabled = false;
        }

        public void Start()
        {

            Enabled = true;

        }

        private async Task Run()
        {
            try
            {
                driver?.Close();
                driver?.Quit();
                driver?.Dispose();
                driver = null;
                _minerRecord.Status = MinerStatus.AppStart;
                _minerRecord.IsConnected = false;
                _minerRecord.LoginUserName = null;
                _minerRecord.ReconnectSeconds = 0;
                _minerRecord.ReconnectCounts = 0;
                _minerRecord.Exception = null;
                _minerRecord.ExceptionTime = null;
                _minerRecord.Points = "0";

                // get assembly version
                _minerRecord.AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();


                // 設定 Chrome 擴充功能路徑
                string chromedriverPath = "/usr/bin/chromedriver";

                // 建立 Chrome 選項
                ChromeOptions options = new ChromeOptions();
                if (!_appConfig.ShowChrome)
                    options.AddArgument("--headless=new");
                options.AddArgument("--no-sandbox");
                //options.AddArgument("--enable-javascript");
                options.AddArgument("--auto-close-quit-quit");
                options.AddArgument("disable-infobars");
                options.AddArgument("--window-size=1024,768");
                if ((_appConfig.ProxyEnable ?? "").ToLower() == "true"
                    && !string.IsNullOrEmpty(_appConfig.ProxyHost))
                {
                    options.AddArgument("--proxy-server=" + _appConfig.ProxyHost);
                    if (!string.IsNullOrEmpty(_appConfig.ProxyUser) && !string.IsNullOrEmpty(_appConfig.ProxyPass))
                    {
                        options.AddArgument($"--proxy-auth={_appConfig.ProxyUser}:{_appConfig.ProxyPass}");
                    }
                }
                options.AddExcludedArgument("enable-automation");
                options.AddUserProfilePreference("credentials_enable_service", false);
                options.AddUserProfilePreference("profile.password_manager_enabled", false);
                options.AddArgument("--disable-gpu"); // 禁用 GPU 加速，减少资源占用
                options.AddArgument("--disable-software-rasterizer"); // 禁用软件光栅化器
                options.AddArgument("--disable-dev-shm-usage"); // 禁用 /dev/shm 临时文件系统
                options.AddArgument("--disable-notifications");
                options.AddArgument("--disable-popup-blocking");
                options.AddArgument("--disable-infobars");
                options.AddArgument("--renderer-process-limit=3");
                //options.AddArgument("--force-dark-mode");
                options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36 Edg/121.0.0.0");
                string userDataDir = Path.Combine(Directory.GetCurrentDirectory(), "UserData");
                if (!Directory.Exists(userDataDir))
                    Directory.CreateDirectory(userDataDir);
                options.AddArgument("--user-data-dir=" + userDataDir);

                options.AddExtension(extensionPath);


                // 建立 Chrome 瀏覽器
                if (!File.Exists(chromedriverPath))
                {
                    chromedriverPath = "./chromedriver";
                    options.AddArgument("--chromedriver=" + chromedriverPath);
                    driver = new ChromeDriver(options);

                }
                else
                    driver = new ChromeDriver(chromedriverPath, options);
                try
                {

                    await Login();

                    // 關閉其他頁面
                    var originalWindow = driver.CurrentWindowHandle;
                    foreach (var handle in driver.WindowHandles)
                    {
                        if (handle != originalWindow)
                        {
                            driver.SwitchTo().Window(handle);
                            driver.Close();
                        }
                    }
                    driver.SwitchTo().Window(originalWindow);


                    // 前往擴充功能頁面
                    driver.Navigate().GoToUrl($"chrome-extension://{extensionId}/popup.html");
                    Console.WriteLine("Go to extension: " + driver.Url);



                    await Task.Delay(new Random().Next(2100, 5455));
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));

                    // 檢查是否出現I got it 按鈕
                    IWebElement? iGotItBtn = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[text()='I got it']")));
                    if (iGotItBtn != null)
                    {
                        iGotItBtn.Click();
                    }

                }
                catch (Exception ex)
                {
                    _minerRecord.Status = MinerStatus.LoginError;
                    _minerRecord.Exception = ex.ToString();
                    _minerRecord.ExceptionTime = DateTime.Now;
                    Console.WriteLine(ex);
                    return;
                }



                driver.Manage().Window.Size = new Size(1024, 768);

                _minerRecord.Status = MinerStatus.Disconnected;
                while (Enabled)
                {
                    try
                    {


                        if (!driver.PageSource.Contains("Disconnected") && driver.PageSource.Contains("Status"))
                        {
                            _minerRecord.Status = MinerStatus.Connected;
                            //$('img[alt="token"]')

                            //IWebElement? userNameElement = driver.FindElement(By.CssSelector("span[title='Username']"));
                            _minerRecord.IsConnected = true;
                        }
                        else
                        {

                            _minerRecord.Status = MinerStatus.Disconnected;
                            _minerRecord.IsConnected = false;
                            _minerRecord.ReconnectCounts++;

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        _minerRecord.Status = MinerStatus.Connected;
                    }
                    finally
                    {
                        int countdownSeconds = 30;

                        // 倒數計時
                        while (countdownSeconds > 0)
                        {
                            _minerRecord.ReconnectSeconds = countdownSeconds;

                            SpinWait.SpinUntil(() => false, 1000); // 等待 1 秒
                            if (!driver.PageSource.Contains("Disconnected") && driver.PageSource.Contains("Status"))
                                break;
                            countdownSeconds--;
                            if (!Enabled)
                            {
                                break;
                            }
                        }
                        // 20-35 分鐘後重新整理
                        if (Enabled && BeforeRefresh.AddMinutes(15 + new Random().Next(5, 20)) <= DateTime.Now)
                        {
                            BeforeRefresh = DateTime.Now;
                            //refresh
                            driver.Navigate().GoToUrl($"chrome-extension://{extensionId}/popup.html");
                            SpinWait.SpinUntil(() => !Enabled, 15000);
                        }
                        await Task.Delay(5000);
                    }
                }
                _minerRecord.Status = MinerStatus.Stop;
            }
            catch (Exception ex)
            {
                _minerRecord.Exception = ex.ToString();
                _minerRecord.ExceptionTime = DateTime.Now;
                _minerRecord.Status = MinerStatus.Error;
                Console.WriteLine(ex);
            }
            finally
            {
                driver?.Close();
                driver?.Quit();
                driver?.Dispose();
                driver = null;
            }
        }


        private async Task Login()
        {
            try
            {
                // 前往登入頁面
                var originalWindow = driver.CurrentWindowHandle;
                driver.SwitchTo().Window(originalWindow);


                _minerRecord.Status = MinerStatus.LoginPage;
                driver.Navigate().GoToUrl("https://app.gradient.network");
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

                Console.WriteLine("Go to app: " + driver.Url);

                // 等待 email 輸入框出現並填入 email
                try
                {
                    wait.Timeout = TimeSpan.FromSeconds(3);
                    bool isEmailInputExist = false;
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            IWebElement emailInput = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("input[placeholder='Enter Email']")));
                            emailInput.SendKeys(_appConfig.UserName);
                            isEmailInputExist = true;
                            break;
                        }
                        catch
                        {
                            if (driver.PageSource.Contains("dashboard"))
                            {
                                throw new Exception("Already login.");
                            }
                        }

                    }

                    if (!isEmailInputExist)
                        throw new Exception("Timed out after 30 seconds.");
                    wait.Timeout = TimeSpan.FromSeconds(30);


                }
                catch (Exception ex)
                {
                    if (driver.PageSource.Contains("dashboard"))
                    {
                        if (CheckUser())
                            return;
                        else
                        {
                            await Logout();
                            throw;
                        }
                    }
                    throw;
                }

                // 等待密碼輸入框出現並填入密碼
                IWebElement passwordInput = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("input[placeholder='Enter Password']")));
                passwordInput.SendKeys(_appConfig.Password);

                // 等待並點擊登入按鈕
                IWebElement loginButton = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("button.custom-flying-button")));
                loginButton.Click();

                // 等待登入成功
                wait.Until(ExpectedConditions.UrlContains("dashboard"));
                _ = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//div[text()='Copy Referral Link']")));
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task Logout()
        {
            try
            {
                await Task.Delay(3000);
                driver.Navigate().GoToUrl("https://app.gradient.network/dashboard/setting");
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                IWebElement? logoutBtn = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//div[text()='Log Out']")));
                logoutBtn.Click();
                // confirm
                IWebElement? confirmBtn = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//div[text()='Confirm']")));
                confirmBtn.Click();
            }
            catch (Exception ex)
            {
            }
        }

        private bool CheckUser()
        {
            try
            {
                driver.Navigate().GoToUrl("https://app.gradient.network/dashboard/setting");
                // wait for svg element exist
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                IWebElement? element = wait.Until(ExpectedConditions.ElementExists(By.XPath("//div[text()='Current Boost']")));
                if (element != null)
                {
                    if (driver.PageSource.Contains(_appConfig.UserName))
                        return true;
                }
            }
            catch (Exception ex)
            {

            }
            return false;

        }


    }

}
