using Amazon.S3;
using deneme;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Xml;
using Telegram.Bot;

namespace vizyoner {
    internal class Program {
        static async Task Main(string[] args) {

            string awsAccessKey = "";
            string awsSecretKey = "";
            string token = "";
            long chatId = 0;
            var url = "https://vizyonergenc.com/ilanlar";
            var bot = new TelegramBotClient(token);

            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            ChromeOptions chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless");
            chromeOptions.AddArgument("--disable-extensions");
            chromeOptions.AddArgument("--disable-dev-shm-usage");
            chromeOptions.AddArgument("--no-sandbox");
            chromeOptions.AddArgument("--disable-gpu");
            chromeOptions.AddArgument("--allow-running-insecure-content");
            chromeOptions.AddExcludedArgument("enable-logging");
            chromeOptions.AddArgument("--silent");

            Console.WriteLine("Chrome açılıyor..");
            var driver = new ChromeDriver(service, chromeOptions);
            Console.WriteLine("Adrese gidiliyor..");
            driver.Navigate().GoToUrl(url);
            Thread.Sleep(2000);
            Console.WriteLine("Veriler alınıyor..");
            scrollEnd(driver);
            Thread.Sleep(2000);
            Console.WriteLine("Veriler alındı, chrome kapatılıyor..");
            var source = driver.PageSource;
            driver.Quit();

            Console.WriteLine("İlanlar ayıklanıyor..");
            var vizyonerDataList = getVizyonerData(source); // vizyoner ilan listesi
            Console.WriteLine("Amazona bağlanılıyor..");
            S3FileSystem s3 = new S3FileSystem(awsAccessKey, awsSecretKey);
            AmazonS3Client s3Client = s3.getS3Client("files3-sa", "vizyoner.txt");
            Console.WriteLine("Amazon verileri alınıyor..");
            List<string> s3List = await s3.getTitleList(); // veri tabanındaki listeler
            Console.WriteLine("Veriler karşılaştırılıyor..");
            List<Model> containsList = await getContainsListAsync(vizyonerDataList, s3List); // vizyonerde olup veri tabanında olmayan listeler

            if (containsList.Count > 0) {
                await sendAnnouncementToTheBotAsync(containsList, bot, chatId);
                await s3.sendData(vizyonerDataList);
            } else {
                Console.WriteLine("Yeni veri bulunamadı..");
            }

            Console.WriteLine("Program sonlandırılıyor..");
            Thread.Sleep(20000);


        }

        private static async Task sendAnnouncementToTheBotAsync(List<Model> containsList, TelegramBotClient bot, long chatId) {
            if (containsList.Count > 0) {
                foreach (var item in containsList) {
                    string message = $"{item.title}\n\nŞirket: {item.company}\t\nKonum: {item.location}\t\nBaşvuru Tarihleri: {item.dateRange}\t\nİlan Son Güncellemesi: {item.lastUpdateDate}\t\n{item.numberOfApplication}\t\nİlan Linki: {item.link}";

                    Console.WriteLine(message);
                    await bot.SendTextMessageAsync(chatId, message);
                    Console.WriteLine("\nDuyuru gönderildi..");
                }
            } else {
                Console.WriteLine("Gönderilecek duyuru yok..");
            }
        }

        private static async Task<List<Model>> getContainsListAsync(List<Model> announcementList, List<string> s3List) {
            List<Model> list = new List<Model>();

            foreach (var item in announcementList) {
                if (s3List.Contains(item.title) == false) {
                    list.Add(item);
                }
            }
            return list;
        }

        private static List<Model> getVizyonerData(string source) {
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(source);
            var postings = htmlDocument.DocumentNode.SelectNodes("//*[@id=\"job-list-paginate\"]/a");
            Model model;
            var models = new List<Model>();
            foreach (var posting in postings) {
                model = new Model();
                model.title = posting.SelectSingleNode(".//h3[@class='job-listing-title']").InnerText.Trim().Replace("\r\n", "").Replace("                        ", "");
                model.link = posting.SelectSingleNode("//a[@class='job-listing']").GetAttributeValue("href", "");

                var lis = posting.SelectNodes(".//li");


                for (var i = 0; i < 5; i++) {
                    if (i == 0) {
                        model.company = lis[i].InnerText.Trim();
                    } else if (i == 1) {
                        model.location = lis[i].InnerText.Trim();
                    } else if (i == 2) {
                        var first = lis[i].SelectSingleNode(".//span[@title='İlan Başlangıç Tarihi']");
                        var second = lis[i].SelectSingleNode(".//span[@title='İlan Bitiş Tarihi']");
                        model.dateRange = first.InnerText.Trim() + " - " + second.InnerText;
                    } else if (i == 3) {
                        model.lastUpdateDate = lis[i].InnerText.Trim();
                    } else {
                        model.numberOfApplication = lis[i].InnerText.Trim();
                    }
                }

                models.Add(model);
            }

            return models;
        }
        private static void scrollEnd(IWebDriver driver) {
            // JavaScriptExecutor oluşturma
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

            // Sayfanın sonuna kadar kaydırma
            long lastHeight = (long)js.ExecuteScript("return document.documentElement.scrollHeight");
            while (true) {
                // Sayfanın sonuna kadar kaydırma
                js.ExecuteScript("window.scrollTo(0, document.documentElement.scrollHeight);");

                // Sayfanın yüklenmesini beklemek için kısa bir süre bekleyin
                System.Threading.Thread.Sleep(2000); // Örnek olarak 2 saniye bekleyelim

                // Yeni yüksekliği al
                long newHeight = (long)js.ExecuteScript("return document.documentElement.scrollHeight");

                // Eğer sona ulaşıldıysa döngüden çık
                if (newHeight == lastHeight) {
                    break;
                }
                lastHeight = newHeight;
            }
        }
    }

}
