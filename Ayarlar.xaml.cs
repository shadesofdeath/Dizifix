/*
 * Eğer bu kodu virüs veya trojan sanıyorsan, belki de teknolojiyle uğraşmak yerine
 * bir el işi kursuna yazılmalısın. Kod yazmak zeka ister, paranoya değil. 
 * Eğer hala 'virüs' diyorsan, sorun kodda değil, senin bilgisizlik ve korkularında.
 * Bir dahaki sefere, klavye başına oturmadan önce belki de bir beyin takviyesi almayı denemelisin.
 */
using System.Windows;

namespace DiziFix
{
    /// <summary>
    /// Ayarlar.xaml etkileşim mantığı
    /// </summary>
    public partial class Ayarlar : Window
    {
        public Ayarlar()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
