/*
 * Eğer bu kodu virüs veya trojan sanıyorsan, belki de teknolojiyle uğraşmak yerine
 * bir el işi kursuna yazılmalısın. Kod yazmak zeka ister, paranoya değil. 
 * Eğer hala 'virüs' diyorsan, sorun kodda değil, senin bilgisizlik ve korkularında.
 * Bir dahaki sefere, klavye başına oturmadan önce belki de bir beyin takviyesi almayı denemelisin.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using Newtonsoft.Json;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace DiziFix
{
    public partial class MainWindow : Window
    {
        private List<Dizi> diziler = new List<Dizi>();
        private Dizi seciliDizi;
        public MainWindow()
        {
            InitializeComponent();

            YukleDizileri();
            DiziCombo.ItemsSource = diziler;
            DiziCombo.DisplayMemberPath = "DiziAdi";

            if (diziler.Count > 0)
            {
                DiziCombo.SelectedIndex = 0;
                seciliDizi = diziler[0];

                DiziSezon.ItemsSource = seciliDizi.Sezonlar;
                DiziSezon.DisplayMemberPath = "SezonNumarasi";
                DiziSezon.SelectedIndex = 0;

                if (seciliDizi.Sezonlar.Count > 0)
                {
                    Sezon seciliSezon = seciliDizi.Sezonlar[0];
                    DiziBolum.ItemsSource = seciliSezon.Bolumler;
                    DiziBolum.DisplayMemberPath = "BolumNumarasi";
                    DiziBolum.SelectedIndex = 0;

                    if (seciliSezon.Bolumler.Count > 0)
                    {
                        Bolum seciliBolum = seciliSezon.Bolumler[0];

                        DiziDublaj.Items.Clear();
                        if (!string.IsNullOrEmpty(seciliBolum.TurkceM3u8Url) && !string.IsNullOrEmpty(seciliBolum.OrjinalM3u8Url))
                        {
                            DiziDublaj.Items.Add("Orjinal");
                            DiziDublaj.Items.Add("Türkçe");
                        }
                        else if (!string.IsNullOrEmpty(seciliBolum.TurkceM3u8Url))
                        {
                            DiziDublaj.Items.Add("Türkçe");
                        }
                        DiziDublaj.SelectedIndex = 0;

                        DiziAltYazi.Items.Clear();

                        if (!string.IsNullOrEmpty(seciliBolum.TrAltyazi))
                        {
                            DiziAltYazi.Items.Add("Türkçe Altyazı");
                        }
                        if (!string.IsNullOrEmpty(seciliBolum.EnAltyazi))
                        {
                            DiziAltYazi.Items.Add("İngilizce Altyazı");
                        }

                        DiziAltYazi.Items.Add("Alt Yazı Yok");

                        if (DiziAltYazi.Items.Count == 1 ||
                            (string.IsNullOrEmpty(seciliBolum.TrAltyazi) && string.IsNullOrEmpty(seciliBolum.EnAltyazi)))
                        {
                            DiziAltYazi.SelectedItem = "Alt Yazı Yok";
                        }
                        else
                        {
                            DiziAltYazi.SelectedIndex = 0;
                        }
                    }
                }
            }

            DiziCombo.SelectionChanged += DiziCombo_SelectionChanged;
            DiziSezon.SelectionChanged += DiziSezon_SelectionChanged;
            DiziBolum.SelectionChanged += DiziBolum_SelectionChanged;
            DiziAra.TextChanged += DiziAra_TextChanged;
            IzleButton.Click += IzleButton_Click;
        }


        private void YukleDizileri()
        {
            string json = GetEmbeddedResourceContent("DiziFix.diziler.json");
            if (!string.IsNullOrEmpty(json))
            {
                diziler = JsonConvert.DeserializeObject<List<Dizi>>(json);
                DurumStatusbar.Items[0] = new StatusBarItem { Content = $"  Dizi sayısı : {diziler.Count}" };
            }
            else
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Dizi verileri çeklemedi!", "Hata");
            }
        }

        private string GetEmbeddedResourceContent(string resourceName)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            return null;
        }

        private void DiziAra_TextChanged(object sender, TextChangedEventArgs e)
        {
            string aramaMetni = DiziAra.Text.ToLower().Trim();

            if (string.IsNullOrEmpty(aramaMetni))
            {
                DiziCombo.ItemsSource = diziler;
                DiziCombo.SelectedIndex = -1;
                SifirlaComboBoxlar();
                return;
            }

            var bulunanDiziler = diziler.Where(d => d.DiziAdi.ToLower().Contains(aramaMetni)).ToList();

            if (bulunanDiziler.Any())
            {
                DiziCombo.ItemsSource = bulunanDiziler;
                DiziCombo.SelectedIndex = 0;
                seciliDizi = bulunanDiziler[0];
                GuncelleDiziSezon();
            }
            else
            {
                DiziCombo.ItemsSource = null;
                SifirlaComboBoxlar();
            }
        }

        private void SifirlaComboBoxlar()
        {
            seciliDizi = null;
            DiziSezon.ItemsSource = null;
            DiziBolum.ItemsSource = null;
            DiziDublaj.Items.Clear();
            DiziAltYazi.Items.Clear();
        }

        private void GuncelleDiziSezon()
        {
            if (seciliDizi != null && seciliDizi.Sezonlar != null && seciliDizi.Sezonlar.Count > 0)
            {
                DiziSezon.ItemsSource = seciliDizi.Sezonlar;
                DiziSezon.DisplayMemberPath = "SezonNumarasi";
                DiziSezon.SelectedIndex = 0;
                GuncelleDiziBolum();
            }
            else
            {
                SifirlaComboBoxlar();
            }
        }

        private void GuncelleDiziBolum()
        {
            Sezon seciliSezon = DiziSezon.SelectedItem as Sezon;
            if (seciliSezon != null && seciliSezon.Bolumler != null && seciliSezon.Bolumler.Count > 0)
            {
                DiziBolum.ItemsSource = seciliSezon.Bolumler;
                DiziBolum.DisplayMemberPath = "BolumNumarasi";
                DiziBolum.SelectedIndex = 0;
                GuncelleDublajVeAltyazi();
            }
            else
            {
                DiziBolum.ItemsSource = null;
                DiziDublaj.Items.Clear();
                DiziAltYazi.Items.Clear();
            }
        }

        private void GuncelleDublajVeAltyazi()
        {
            Bolum seciliBolum = DiziBolum.SelectedItem as Bolum;
            if (seciliBolum != null)
            {
                DiziDublaj.Items.Clear();
                if (!string.IsNullOrEmpty(seciliBolum.TurkceM3u8Url) && !string.IsNullOrEmpty(seciliBolum.OrjinalM3u8Url))
                {
                    DiziDublaj.Items.Add("Orjinal");
                    DiziDublaj.Items.Add("Türkçe");
                }
                else if (!string.IsNullOrEmpty(seciliBolum.TurkceM3u8Url))
                {
                    DiziDublaj.Items.Add("Türkçe");
                }
                else if (!string.IsNullOrEmpty(seciliBolum.OrjinalM3u8Url))
                {
                    DiziDublaj.Items.Add("Orjinal");
                }
                DiziDublaj.SelectedIndex = 0;

                DiziAltYazi.Items.Clear();
                if (!string.IsNullOrEmpty(seciliBolum.TrAltyazi))
                {
                    DiziAltYazi.Items.Add("Türkçe Altyazı");
                }
                if (!string.IsNullOrEmpty(seciliBolum.EnAltyazi))
                {
                    DiziAltYazi.Items.Add("İngilizce Altyazı");
                }
                DiziAltYazi.Items.Add("Alt Yazı Yok");
                DiziAltYazi.SelectedIndex = 0;
            }
        }
        private void DiziCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            seciliDizi = DiziCombo.SelectedItem as Dizi;
            if (seciliDizi != null)
            {
                if (seciliDizi.Sezonlar != null && seciliDizi.Sezonlar.Count > 0)
                {
                    DiziSezon.ItemsSource = seciliDizi.Sezonlar;
                    DiziSezon.DisplayMemberPath = "SezonNumarasi";
                    DiziSezon.SelectedIndex = 0;
                }
                else
                {
                    DiziSezon.ItemsSource = null;
                    DiziBolum.ItemsSource = null;
                    DiziDublaj.Items.Clear();
                    DiziAltYazi.Items.Clear();
                }
            }
        }

        private void DiziSezon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (seciliDizi != null)
            {
                Sezon seciliSezon = DiziSezon.SelectedItem as Sezon;
                if (seciliSezon != null && seciliSezon.Bolumler.Count > 0)
                {
                    DiziBolum.ItemsSource = seciliSezon.Bolumler;
                    DiziBolum.DisplayMemberPath = "BolumNumarasi";
                    DiziBolum.SelectedIndex = 0; 
                }
                else
                {
                    DiziBolum.ItemsSource = null;
                    DiziDublaj.Items.Clear();
                    DiziAltYazi.Items.Clear();
                }
            }
        }
        private void DiziBolum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (seciliDizi != null)
            {
                Bolum seciliBolum = DiziBolum.SelectedItem as Bolum;
                if (seciliBolum != null)
                {
                    DiziDublaj.Items.Clear();
                    if (!string.IsNullOrEmpty(seciliBolum.TurkceM3u8Url) && !string.IsNullOrEmpty(seciliBolum.OrjinalM3u8Url))
                    {
                        DiziDublaj.Items.Add("Orjinal");
                        DiziDublaj.Items.Add("Türkçe");
                    }
                    else if (!string.IsNullOrEmpty(seciliBolum.TurkceM3u8Url))
                    {
                        DiziDublaj.Items.Add("Orjinal");
                    }
                    DiziDublaj.SelectedIndex = 0;

                    DiziAltYazi.Items.Clear();
                    if (!string.IsNullOrEmpty(seciliBolum.TrAltyazi))
                    {
                        DiziAltYazi.Items.Add("Türkçe Altyazı");
                    }
                    if (!string.IsNullOrEmpty(seciliBolum.EnAltyazi))
                    {
                        DiziAltYazi.Items.Add("İngilizce Altyazı");
                    }
                    DiziAltYazi.Items.Add("Alt Yazı Yok");
                    DiziAltYazi.SelectedIndex = 0;
                }
            }
        }

        private void IzleButton_Click(object sender, RoutedEventArgs e)
        {
            if (seciliDizi != null)
            {
                string registryKey = @"HKEY_CURRENT_USER\Software\DiziFix";
                string lastWatchTimeValue = "LastWebViewTime";
                DateTime lastWatchTime;

                object registryValue = Registry.GetValue(registryKey, lastWatchTimeValue, null);

                if (registryValue != null && DateTime.TryParse(registryValue.ToString(), out lastWatchTime))
                {
                    if (DateTime.Now - lastWatchTime < TimeSpan.FromHours(24))
                    {
                        OpenMPV();
                        return;
                    }
                }

                if (!IsWebView2Available())
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("WebView2 bulunamadı. MPV başlatılmayacak.", "Hata");
                    return;
                }

                bool webViewStartedSuccessfully = false;
                try
                {
                    OpenWebView("http://bc.vc/PIpU6Ql");
                    webViewStartedSuccessfully = true;
                }
                catch (Exception ex)
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show($"WebView işlemi sırasında hata oluştu: {ex.Message}", "Hata");
                }

                if (webViewStartedSuccessfully)
                {
                    Registry.SetValue(registryKey, lastWatchTimeValue, DateTime.Now.ToString());
                    OpenMPV();
                }
                else
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("WebView2 başlatılamadı. Lütfen sisteminize WebView2 Runtime kurulu olduğundan emin olun.", "Hata");
                }
            }
        }

        private bool IsWebView2Available()
        {
            try
            {
                string baseDirectory = @"C:\Program Files (x86)\Microsoft\EdgeWebView\Application";

                string[] directories = Directory.GetDirectories(baseDirectory);
                foreach (string dir in directories)
                {
                    string webView2Path = Path.Combine(dir, "msedgewebview2.exe");
                    if (File.Exists(webView2Path))
                    {
                        return true; 
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }


        private void OpenMPV()
        {
            Sezon seciliSezon = DiziSezon.SelectedItem as Sezon;
            Bolum seciliBolum = DiziBolum.SelectedItem as Bolum;
            string dublaj = DiziDublaj.SelectedItem?.ToString();
            string altyazi = DiziAltYazi.SelectedItem?.ToString();
            string url;
            string altyaziDosyasi = null;

            if (dublaj == "Türkçe" && !string.IsNullOrEmpty(seciliBolum.TurkceM3u8Url))
            {
                url = seciliBolum.TurkceM3u8Url;
            }
            else if (dublaj == "Orjinal" && !string.IsNullOrEmpty(seciliBolum.OrjinalM3u8Url))
            {
                url = seciliBolum.OrjinalM3u8Url;
            }
            else
            {
                url = seciliBolum.TurkceM3u8Url;
            }

            if (altyazi == "Türkçe Altyazı" && !string.IsNullOrEmpty(seciliBolum.TrAltyazi))
            {
                altyaziDosyasi = seciliBolum.TrAltyazi;
            }
            else if (altyazi == "İngilizce Altyazı" && !string.IsNullOrEmpty(seciliBolum.EnAltyazi))
            {
                altyaziDosyasi = seciliBolum.EnAltyazi;
            }

            string mpvYolu = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mpv", "mpv.exe");

            try
            {
                var diziAdi = seciliDizi.DiziAdi;
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = mpvYolu,
                    Arguments = $"\"{url}\" " +
                                $"{(altyaziDosyasi != null ? $"--sub-file=\"{altyaziDosyasi}\" " : "")}" +
                                "--osc " + 
                                "--force-media-title=\"DiziFix\" " + 
                                $"--osd-playing-msg=\"{diziAdi}\"", 
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                System.Diagnostics.Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show($"MPV başlatılamadı: {ex.Message}", "Hata");
            }
        }
        private async void OpenWebView(string url)
        {
            var webViewWindow = new Window
            {
                Title = "DiziFix",
                Width = 0,
                Height = 0,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Opacity = 0
            };

            var webView = new WebView2();
            webViewWindow.Content = webView;

            webViewWindow.Show();

            await webView.EnsureCoreWebView2Async(null);

            webView.CoreWebView2.NewWindowRequested += (s, e) =>
            {
                e.Handled = true;
            };

            webView.NavigationCompleted += async (s, e) =>
            {
                if (webView.Source.ToString() == "https://www.blank.org/")
                {
                    await Task.Delay(3000);
                    webViewWindow.Close();
                    return;
                }
                await ClickButtonWhenAvailable(webView);
            };

            webView.CoreWebView2.Navigate(url);
        }

        private async Task ClickButtonWhenAvailable(WebView2 webView)
        {
            const int maxAttempts = 60; 
            const int delayBetweenAttempts = 500;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                string jsCode = @"
            (function() {
                var button = document.evaluate('//*[@id=""getLink""]', document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                if (button && button.style.display !== 'none' && !button.disabled) {
                    button.click();
                    return 'clicked';
                }
                return 'waiting';
            })()
        ";

                string result = await webView.CoreWebView2.ExecuteScriptAsync(jsCode);

                if (result.Trim('"') == "clicked")
                {
                    Console.WriteLine("Button clicked successfully.");
                    return;
                }

                await Task.Delay(delayBetweenAttempts);
            }

            Console.WriteLine("Button not found or not clickable after maximum attempts.");
        }
        private void AyarlarButton_Click(object sender, RoutedEventArgs e)
        {
            Ayarlar ayarlar = new Ayarlar();
            ayarlar.Show();
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            Download download = new Download();
            download.Show();
        }

        private void ChangelogButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Değişim günlüğü geelcek güncellemelerde aktif edilecek!");
        }

        private void GithubButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://github.com/shadesofdeath/Dizifix");
            }
            catch { }
        }
    }

    public class Dizi
    {
        public string DiziAdi
        {
            get; set;
        }
        public List<Sezon> Sezonlar
        {
            get; set;
        }
    }

    public class Sezon
    {
        public int SezonNumarasi
        {
            get; set;
        }
        public List<Bolum> Bolumler
        {
            get; set;
        }
    }

    public class Bolum
    {
        public int BolumNumarasi
        {
            get; set;
        }
        public string TurkceM3u8Url
        {
            get; set;
        }
        public string OrjinalM3u8Url
        {
            get; set;
        }
        public string TrAltyazi
        {
            get; set;
        }
        public string EnAltyazi
        {
            get; set;
        }
    }
}