using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Reflection;
using Markdig;

namespace ShareAssist
{
    /// <summary>
    /// Interaction logic for HelpLicense.xaml
    /// </summary>
    public partial class HelpLicense : Window
    {
        public HelpLicense()
        {
            InitializeComponent();
        }

        private void TextBlock_Initialized(object sender, EventArgs e)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "ShareAssist.README.md";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))

            using (StreamReader reader = new StreamReader(stream))
            {
                
                string mdText = reader.ReadToEnd();
                string result = Markdown.ToHtml(mdText);

                licenseBox.NavigateToString(result);
            }

        }

    }
}
