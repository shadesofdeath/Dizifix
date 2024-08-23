/*
 * Eğer bu kodu virüs veya trojan sanıyorsan, belki de teknolojiyle uğraşmak yerine
 * bir el işi kursuna yazılmalısın. Kod yazmak zeka ister, paranoya değil. 
 * Eğer hala 'virüs' diyorsan, sorun kodda değil, senin bilgisizlik ve korkularında.
 * Bir dahaki sefere, klavye başına oturmadan önce belki de bir beyin takviyesi almayı denemelisin.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Flurl.Http;
using Newtonsoft.Json;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using Path = System.IO.Path;

namespace DiziFix
{
    public partial class Download : Window, INotifyPropertyChanged
    {
        private List<DownloadDizi> diziler;
        private DownloadDizi secilenDizi;
        private DownloadSezon secilenSezon;
        private List<DownloadBolum> secilenBolumler = new List<DownloadBolum>();
        private string secilenDil = "Turkce"; 
        private string secilenAltyazi = "Yok"; 
        private string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe");
        private string indirmeYolu = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public Download()
        {
            InitializeComponent();
            YukleDizileri();

            DilCmbx.Items.Add("Türkçe");
            DilCmbx.Items.Add("Orjinal");
            DilCmbx.SelectedIndex = 0;
            DilCmbx.SelectionChanged += DilSecildi;

            AltyaziCmbx.Items.Add("Yok");
            AltyaziCmbx.Items.Add("Türkçe");
            AltyaziCmbx.Items.Add("İngilizce");
            AltyaziCmbx.SelectedIndex = 0; 
            AltyaziCmbx.SelectionChanged += AltyaziSecildi;

            DataContext = this;
        }

        private void YukleDizileri()
        {
            string json = GetEmbeddedResourceContent("DiziFix.diziler.json");
            if (!string.IsNullOrEmpty(json))
            {
                diziler = JsonConvert.DeserializeObject<List<DownloadDizi>>(json);
                DiziAramaCmbx.ItemsSource = diziler;
                DiziAramaCmbx.DisplayMemberPath = "DiziAdi";
                DiziAramaCmbx.SelectionChanged += DiziSecildi;
            }
            else
            {
                MessageBox.Show("Dizi verileri çekilemedi!", "Hata");
            }
        }

        private string GetEmbeddedResourceContent(string resourceName)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private void DiziSecildi(object sender, SelectionChangedEventArgs e)
        {
            secilenDizi = (DownloadDizi)DiziAramaCmbx.SelectedItem;
            SezonCmbx.ItemsSource = secilenDizi.Sezonlar;
            SezonCmbx.DisplayMemberPath = "SezonNumarasi";
            SezonCmbx.SelectionChanged += SezonSecildi;
        }

        private void SezonSecildi(object sender, SelectionChangedEventArgs e)
        {
            secilenSezon = (DownloadSezon)SezonCmbx.SelectedItem;
            BolumCmbx.ItemsSource = secilenSezon.Bolumler;
            BolumCmbx.DisplayMemberPath = "BolumNumarasi";
            BolumCmbx.SelectionChanged += BolumSecildi;
        }

        private void BolumSecildi(object sender, SelectionChangedEventArgs e)
        {
            secilenBolumler.Clear();
            if (BolumCmbx.SelectedItem != null)
            {
                secilenBolumler.Add((DownloadBolum)BolumCmbx.SelectedItem);
            }
        }

        private async void IndirButton_Click(object sender, RoutedEventArgs e)
        {
            if (secilenDizi == null)
            {
                MessageBox.Show("Lütfen bir dizi seçin.");
                return;
            }

            var dosyaDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dosyaDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                indirmeYolu = dosyaDialog.SelectedPath;
            }
            else
            {
                return; 
            }

            if ((TumSezonlarIndirButton.IsChecked == true || SecilenSezonIndirButton.IsChecked == true) &&
                MessageBox.Show("İlk bölüm indirildikten sonra diğerleri sırayla otomatik inecek. Onaylıyor musunuz?", "Onay", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (TumSezonlarIndirButton.IsChecked == true)
                {
                    await IndirSezonlari(secilenDizi.Sezonlar);
                }
                else if (SecilenSezonIndirButton.IsChecked == true && secilenSezon != null)
                {
                    await IndirSezonlari(new List<DownloadSezon> { secilenSezon });
                }
                else if (TekBolumIndirButton.IsChecked == true && secilenBolumler.Count > 0)
                {
                    await IndirBolumleri(secilenBolumler);
                }
                else
                {
                    MessageBox.Show("Lütfen indirmek istediğiniz seçeneği belirleyin.");
                    return;
                }
            }
            else if (TekBolumIndirButton.IsChecked == true && secilenBolumler.Count > 0)
            {
                await IndirBolumleri(secilenBolumler);
            }
            else
            {
                MessageBox.Show("Lütfen indirmek istediğiniz seçeneği belirleyin.");
                return;
            }
        }

        private async Task IndirSezonlari(List<DownloadSezon> sezonlar)
        {
            foreach (var sezon in sezonlar)
            {
                foreach (var bolum in sezon.Bolumler)
                {
                    await IndirBolumu(bolum, sezon, secilenDizi.DiziAdi);
                }
            }
        }

        private async Task IndirBolumleri(List<DownloadBolum> bolumler)
        {
            foreach (var bolum in bolumler)
            {
                await IndirBolumu(bolum, secilenSezon, secilenDizi.DiziAdi);
            }
        }
        private async Task IndirBolumu(DownloadBolum bolum, DownloadSezon sezon, string diziAdi)
        {
            string bolumKey = $"{diziAdi}-S{sezon.SezonNumarasi}E{bolum.BolumNumarasi}";
            string m3u8Url = secilenDil == "Türkçe" && !string.IsNullOrEmpty(bolum.TurkceM3u8Url)
                ? bolum.TurkceM3u8Url
                : bolum.OrjinalM3u8Url ?? bolum.TurkceM3u8Url;

            var fileName = $"{diziAdi} - Sezon {sezon.SezonNumarasi} - Bölüm {bolum.BolumNumarasi}";
            var filePath = Path.Combine(indirmeYolu, fileName + ".mp4");

            if (bolum.IndirmeDurumu == "İndiriliyor..." || bolum.IndirmeDurumu == "Tamamlandı")
            {
                MessageBox.Show("Bu bölüm zaten indiriliyor veya indirilmiş.");
                return;
            }

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                var arguments = $"-i \"{m3u8Url}\" -bsf:a aac_adtstoasc -vcodec copy -c copy -crf 50 \"{filePath}\"";

                var psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    CreateNoWindow = true, 
                    UseShellExecute = true, 
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                var process = new Process { StartInfo = psi };
                process.Start();

                bolum.IndirmeDurumu = "İndiriliyor...";

                process.WaitForExit();

                Dispatcher.Invoke(() =>
                {
                    if (process.ExitCode == 0)
                    {
                        bolum.IndirmeDurumu = "Tamamlandı";
                    }
                    else
                    {
                        bolum.IndirmeDurumu = "Hata";
                    }

                    if (bolum.IndirmeDurumu == "Tamamlandı")
                    {
                        if (sezon.Bolumler.IndexOf(bolum) < sezon.Bolumler.Count - 1)
                        {
                            int sonrakiBolumIndex = sezon.Bolumler.IndexOf(bolum) + 1;
                            IndirBolumu(sezon.Bolumler[sonrakiBolumIndex], sezon, diziAdi).ConfigureAwait(false);
                        }
                        else if (secilenDizi.Sezonlar.IndexOf(sezon) < secilenDizi.Sezonlar.Count - 1)
                        {
                            int sonrakiSezonIndex = secilenDizi.Sezonlar.IndexOf(sezon) + 1;
                            IndirSezonlari(new List<DownloadSezon> { secilenDizi.Sezonlar[sonrakiSezonIndex] }).ConfigureAwait(false);
                        }
                        else
                        {
                            this.Close();
                        }
                    }
                });

                if (secilenAltyazi != "Yok")
                {
                    string altyaziUrl = secilenAltyazi == "Türkçe" && !string.IsNullOrEmpty(bolum.TrAltyazi)
                        ? bolum.TrAltyazi
                        : bolum.EnAltyazi;

                    if (!string.IsNullOrEmpty(altyaziUrl))
                    {
                        var altyaziFileName = $"{fileName} - {secilenAltyazi} Altyazı.vtt";
                        await IndirAltyazi(altyaziUrl, Path.Combine(indirmeYolu, altyaziFileName));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("İndirme işlemi sırasında bir hata oluştu: " + ex.Message);
                bolum.IndirmeDurumu = "Hata";
            }
        }

        private async Task IndirAltyazi(string altyaziUrl, string fileName)
        {
            try
            {
                var altyaziBytes = await altyaziUrl.GetBytesAsync();
                var altyaziPath = Path.Combine(indirmeYolu, fileName + ".vtt");
                File.WriteAllBytes(altyaziPath, altyaziBytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Alt Yazı İndirirken bir hata oluştu: " + ex.Message);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DilSecildi(object sender, SelectionChangedEventArgs e)
        {
            if (DilCmbx.SelectedItem != null)
            {
                secilenDil = (string)DilCmbx.SelectedItem;
            }
        }

        private void AltyaziSecildi(object sender, SelectionChangedEventArgs e)
        {
            if (AltyaziCmbx.SelectedItem != null)
            {
                secilenAltyazi = (string)AltyaziCmbx.SelectedItem;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DownloadDizi
    {
        public string DiziAdi
        {
            get; set;
        }
        public string KapakResmiUrl
        {
            get; set;
        }
        public double ImdbPuani
        {
            get; set;
        }
        public List<DownloadSezon> Sezonlar
        {
            get; set;
        }
    }

    public class DownloadSezon
    {
        public int SezonNumarasi
        {
            get; set;
        }
        public string SezonKapakResmi
        {
            get; set;
        }
        public List<DownloadBolum> Bolumler
        {
            get; set;
        }
    }

    public class DownloadBolum
    {
        public int BolumNumarasi
        {
            get; set;
        }
        public string BolumKapakResmi
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

        public string IndirmeDurumu { get; set; } = "Bekleniyor";
    }
}