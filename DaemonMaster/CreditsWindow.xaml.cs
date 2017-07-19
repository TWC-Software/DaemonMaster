using System.Windows;

namespace DaemonMaster
{
    /// <summary>
    /// Interaktionslogik für CreditsWindow.xaml
    /// </summary>
    public partial class CreditsWindow : Window
    {
        public CreditsWindow()
        {
            InitializeComponent();

            textBoxCredits.Text =
                "The program \"DeamonMaster\" was originally created by MCPC10 (Main Developer) and Stuffi3000 \n\n GUI: Stuffi3000, MCPC10 \n Code: MCPC10 \n Icon: Stuffi3000 \n\n" +
                "Used Librarys: \n" +
                "=> Newtonsoft.Json - James Newton - King - MIT License \n" +
                "=> NLog - Jaroslaw Kowalski, Kim Christensen, Julian Verdurmen - BSD 3 clause \"New\" or \"Revised\" License \n" +
                "=> AutoUpdater.NET - RBSoft - MIT License \n" +
                "=> Active Directory Object Picker - Tulpep - MS-PL License \n\n" +
                "Thanks to: \n Pinvoke.net \n stackoverflow.com (for help from the users) \n entwickler-ecke.de (for help from the users)";
        }
    }
}
