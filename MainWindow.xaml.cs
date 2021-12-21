using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NAudio.Wave;
using System.Net;
using System.Windows.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;

namespace ShareAssist
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static Viewer viewer = new Viewer();
        static Previewer previewer = new Previewer();
        public static MainWindow controlPanel;

        Env env = new Env { ViewerHeight = 360 };

        static Settings settings = new Settings();


        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = env;
            controlPanel = this;
            viewer.Show();

            viewer.Left = 10;
            viewer.Top = System.Windows.SystemParameters.WorkArea.Bottom - viewer.Height - 30;
            this.Left = 10 + viewer.Width + 10;
            this.Top = System.Windows.SystemParameters.WorkArea.Bottom - viewer.Height - 30;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += timer_Tick;
            timer.Start();

            //config file
            if (File.Exists("ShareAssist.json"))
            {
                string jsonText = File.ReadAllText("ShareAssist.json");
                settings = JsonConvert.DeserializeObject<Settings>(jsonText);
                env.ViewerHeight = int.Parse(settings.Size);
                
            }
            else
            {
                File.Create("ShareAssist.json");
            }

        }

        class Settings
        {
            public string Size { get; set; }
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            viewer.Owner = this;
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            string output = JsonConvert.SerializeObject(settings);
            File.WriteAllText("ShareAssist.json", output);
            Application.Current.Shutdown();
        }



        public class Env : INotifyPropertyChanged
        {

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

            private int viewerHeightValue;
            /// Sets the height of the viewer window; also sets the width to make a 16:9 aspect ratio.
            public int ViewerHeight
            {
                get { return viewerHeightValue; }
                set
                {
                    viewerHeightValue = value;
                    viewer.Height = value;
                    settings.Size = value.ToString();
                    viewer.Width = ViewerWidth;
                    OnPropertyChanged();
                }
            }
            public int ViewerWidth
            {
                get
                {
                    switch (viewerHeightValue)
                    {
                        case 240: return 426;
                        case 360: return 640;
                        case 480: return 854;
                        case 720: return 1280;
                        default:
                            int td = Convert.ToInt32(Math.Round(viewerHeightValue * (16.0 / 9)));
                            return td;
                    }
                }
            }

        }



        public static Uri[] targetArray = new Uri[20];
        public static string[] titleArray = new string[20];
        public static int currentTargetId = 0;
        public static string[] typesArray = new string[20];
        public static bool armed = false;


        int tagGetter(object sender)
        {
            FrameworkElement senderButton = (FrameworkElement)((Button)sender);
            int senderButtonTag = int.Parse(senderButton.Tag.ToString());
            return senderButtonTag;
        }


        private void Button_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            int tag = tagGetter(sender);
            fileWiper(tag);
            
        }
        private void clearAllButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 20; i++)
            {
                fileWiper(i);
            }
        }

        void fileWiper(int tag)
        {
            targetArray[tag] = null;
            titleArray[tag] = "(no file)";
            Label label = (Label)FindName("Title" + tag.ToString());
            label.Content = titleArray[tag];
            StackPanel spanel = (StackPanel)label.Parent;
            Image icon = (Image)FindName("Icon" + tag.ToString());
            icon.Source = null;
            spanel.Background = Brushes.White;
            typesArray[tag] = "none";
        }

        private void singleLoader(object sender, RoutedEventArgs e)
        {
            int tag = tagGetter(sender);
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Media files|*.mp3;*.mp4;*.jpg;*.jpeg;*.jfif;*.png;*.gif";
            if (openFileDialog.ShowDialog() == true)
            {
                Uri path = new Uri(openFileDialog.FileName);
                setterUpper(path, tag);
            }
        }

        private void folderLoaderButton(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                for (int i = 0; i < 20; i++)
                {
                    fileWiper(i);
                }
                string[] files = Directory.GetFiles(folderDialog.SelectedPath);
                
                int y = 0;
                for (int x=0; x<Math.Min(files.Length, 20); x++)
                {
                    string ext = Path.GetExtension(files[x]).ToLower();
                    if (ext == ".mp3" || ext == ".mp4" || ext == ".jpg" || ext == ".jpeg" || ext == ".jfif" || ext == ".png" || ext == ".gif")
                    {
                        setterUpper(new Uri(files[x]), y);
                        y++;
                    }
                    
                    
                }
            }
        }
                
        void setterUpper(Uri path, int tag)
        {
            targetArray[tag] = path;
            
            TagLib.File tfile = TagLib.File.Create(path.LocalPath);
            string TagLibTitle = tfile.Tag.Title;
            if (TagLibTitle == null)
            {
                titleArray[tag] = Path.GetFileName(path.LocalPath);
            } else
            {
                titleArray[tag] = TagLibTitle;
            }

            
            Label label = (Label)FindName("Title" + tag.ToString());
            label.Content = titleArray[tag];
            
            StackPanel spanel = (StackPanel)label.Parent;
            Image icon = (Image)FindName("Icon" + tag.ToString());

            string ext = Path.GetExtension(targetArray[tag].ToString()).ToLower();
            switch (ext)
            {
                case ".mp4":
                    { setupVideo(); break; }
                case ".mp3":
                    { setupAudio(); break; }
                case ".jpg":
                case ".jpeg":
                case ".jfif":
                case ".png":
                case ".gif":
                    { setupImage(); break; }

                default: { MessageBox.Show("Can't use filetype " + ext + ", sorry!"); return; };
            }

            void setupVideo()
            {
                typesArray[tag] = "video";
                if (targetArray[tag].ToString().Contains("sjjm") || titleArray[tag].Contains("song") || titleArray[tag].Contains("Song"))
                {
                    spanel.Background = Brushes.DeepSkyBlue;
                    icon.Source = new BitmapImage(new Uri("/Icons/music-clef-treble.png", UriKind.Relative));
                }
                else 
                {
                    spanel.Background = Brushes.LightCyan;
                    icon.Source = new BitmapImage(new Uri("/Icons/video-vintage.png", UriKind.Relative));
                }
            }
            void setupImage()
            {
                typesArray[tag] = "image";
                spanel.Background = Brushes.PeachPuff;
                icon.Source = new BitmapImage(new Uri("/Icons/panorama-variant-outline.png", UriKind.Relative));
            }
            void setupAudio()
            {
                typesArray[tag] = "audio";
                spanel.Background = Brushes.PaleGreen;
                icon.Source = new BitmapImage(new Uri("/Icons/volume-high.png", UriKind.Relative));
            }
        }


        private void TargetButton(object sender, RoutedEventArgs e)
        {
            currentTargetId = tagGetter(sender);
            TargetUpdater();
        }

        private void NextButton(object sender, RoutedEventArgs e)
        {
            if (currentTargetId == 19) { currentTargetId = 0; } else { currentTargetId++; }
            TargetUpdater();
            Play();
        }
        private void PrevButton(object sender, RoutedEventArgs e)
        {
            if (currentTargetId == 0) { currentTargetId = 19; } else { currentTargetId--; }
            TargetUpdater();
            Play();
        }

        void TargetUpdater()
        {
            for (int i = 0; i < 20; i++)
            {
                Button button = (Button)FindName("Target" + i);
                if (i == currentTargetId) { button.Background = Brushes.Orange; }
                else { button.Background = Brushes.LightGray; }
            }
        }

        private void PlayButtonClick(object sender, RoutedEventArgs e) { Play(); }
        
        static public void Play()
        {
            switch (typesArray[currentTargetId])
            {
                case "video": { playVideo(); break; }
                case "image": { playImage(); break; }
                case "audio": { playAudio(); break; }
                default: return;
            }
            void playVideo()
            {
                viewer.ImagePlayer.Visibility = Visibility.Collapsed;
                viewer.Player.Visibility = Visibility.Visible;
                viewer.Player.Source = targetArray[currentTargetId];
                viewer.Player.LoadedBehavior = MediaState.Play;
            };

            void playImage()
            {
                viewer.ImagePlayer.Visibility = Visibility.Visible;
                viewer.Player.Visibility = Visibility.Collapsed;
                viewer.ImagePlayer.Source = new BitmapImage(targetArray[currentTargetId]);
                viewer.Player.LoadedBehavior = MediaState.Stop;

            };
            void playAudio()
            {
                viewer.ImagePlayer.Visibility = Visibility.Visible;
                viewer.Player.Visibility = Visibility.Collapsed;
                viewer.ImagePlayer.Source = new BitmapImage(new Uri("/Images/audio-graphic.png", UriKind.Relative));
                viewer.Player.Source = targetArray[currentTargetId];
                viewer.Player.LoadedBehavior = MediaState.Play;
            }

        }

        private void ArmButtonClick(object sender, RoutedEventArgs e)
        {
            switch (armed)
            {
                case false:
                    {
                        ArmButton.Background = Brushes.OrangeRed;
                        armed = true;
                        break;
                    }
                case true:
                    {
                        ArmButton.Background = Brushes.LightGray;
                        armed = false;
                        break;
                    }
            }
        }


        private void StopButton(object sender, RoutedEventArgs e)
        {
            viewer.Player.LoadedBehavior = MediaState.Close;
            viewer.ImagePlayer.Visibility = Visibility.Hidden;

        }

        private void PauseButton(object sender, RoutedEventArgs e)
        {
            if (viewer.ImagePlayer.Visibility == Visibility.Visible) { return; }
            MediaState state = viewer.Player.LoadedBehavior;
            switch (state)
            {
                case MediaState.Play: { viewer.Player.LoadedBehavior = MediaState.Pause; break; }
                default: { viewer.Player.LoadedBehavior = MediaState.Play; break; }
            }
        }

        private void HandleDrag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }


    #region Background Music

        public List<Mp3Item> mp3items = new List<Mp3Item> { };
        public TimeSpan maxSpan = new TimeSpan(0);
        public TimeSpan minSpan = new TimeSpan(1);

        public class Mp3Item
        {
            public string Path { get; set; }
            public TimeSpan Length { get; set; }
        }

        Uri path = new Uri(@"C:/");
        string[] PathsList;
        private void selectFolderButton(object sender, RoutedEventArgs e)
        {
            string previousText = folderButtonText.Content.ToString();
            folderButtonText.Content = "∞ Loading...";
            controlsBG.Opacity = 0.3;

            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.ShowDialog();

            if (folderDialog.SelectedPath == "")
            {
                folderButtonText.Content = previousText;
                controlsBG.Opacity = 1;
                return;
            }

            path = new Uri(folderDialog.SelectedPath); 

            PathsList = Directory.GetFiles(folderDialog.SelectedPath, "*.mp3");
            mp3items.Clear();
            if (PathsList.Length == 0) { folderButtonText.Content = "No .mp3 files in this folder.";  return; }

            for (int i = 0; i < PathsList.Length; i++)
            {
                Mp3Item item2add  = new Mp3Item() { Path = PathsList[i], Length = new AudioFileReader(PathsList[i]).TotalTime };
                mp3items.Add(item2add);
            }

            mp3items = mp3items.OrderBy(item => item.Length).ToList();

            maxSpan = mp3items.Max(a => a.Length);
            minSpan = mp3items.Min(a => a.Length);

            controlsBG.Opacity = 1;
            folderButtonText.Content = "Current: .../" + WebUtility.UrlDecode(path.Segments[path.Segments.Length - 2]) + WebUtility.UrlDecode(path.Segments[path.Segments.Length - 1] + ", Total files: " + PathsList.Length.ToString());
        }

        public TimeSpan ttg;
        void timer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            DateTime target = new DateTime(now.Year, now.Month, now.Day, int.Parse(hourBox.Text), int.Parse(minBox.Text), 0);
            ttg = target.Subtract(now);
            if (ttg.TotalSeconds <= 0) { ttgLabel.Content = "00:00:00"; }
            else
            {
                ttgLabel.Content = ttg.ToString(@"hh\:mm\:ss");
            }
            if (mediaBG.NaturalDuration == Duration.Automatic) { songRemainLabel.Content = "00:00"; }
            else
            {
                songRemainLabel.Content = (mediaBG.NaturalDuration.TimeSpan - mediaBG.Position).ToString(@"mm\:ss");
            }
        }


        private void PlayNextBG(object sender, RoutedEventArgs e)
        {
            if (maxSpan < ttg) { playRandom(); return; }
            if (minSpan > ttg) { return; }

            for (int i = mp3items.Count - 1; i != 0; i--)
            {
                if (mp3items[i].Length < ttg)
                {
                    mediaBG.LoadedBehavior = MediaState.Play;
                    mediaBG.Source = new Uri(mp3items[i].Path);
                    return;
                }
            }

        }



        private void StopBG(object sender, RoutedEventArgs e)
        {
            mediaBG.LoadedBehavior = MediaState.Close;
        }

        public void playRandom()
        {
            Random rnd = new Random();
            int next = rnd.Next(0, mp3items.Count);
            mediaBG.LoadedBehavior = MediaState.Play;
            try { mediaBG.Source = new Uri(mp3items[next].Path); }
            catch { return; }
        }

        public double volume = 0.1;

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaBG.Volume = Math.Pow(volumeSlider.Value, 3.0);
        }

        private void addChecklistItem(object sender, RoutedEventArgs e)
        {
            TextBox newItem = new TextBox()
            {
                Width = 200,
                Foreground = Brushes.Red,
                FontWeight = FontWeights.ExtraBlack,
                Margin = new Thickness(5),
                Text = "[enter text]"
            };
            newItem.TextChanged += new TextChangedEventHandler(reset);

            static void reset(object sender, TextChangedEventArgs e)
            {
                TextBox textbox = sender as TextBox;
                textbox.Foreground = Brushes.Red;
                textbox.FontWeight = FontWeights.ExtraBlack;
            }

            StackPanel panel = new StackPanel()
            {
                Orientation = Orientation.Horizontal
            };
            Button button = new Button()
            {
                Content = "⭕",
                Margin = new Thickness(5),
                Cursor = Cursors.Hand,
            };
            button.Click += new RoutedEventHandler(checkUncheck);

            panel.Children.Add(newItem);
            panel.Children.Add(button);
            checkItemPanel.Children.Add(panel);

        }

        private void checkUncheck(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            StackPanel panel = button.Parent as StackPanel;
            TextBox textbox = panel.Children[0] as TextBox;

            textbox.Foreground = Brushes.DarkGreen;
            textbox.FontWeight = FontWeights.Normal;
        }

        private void HelpOpen(object sender, MouseButtonEventArgs e)
        {
            HelpLicense helpLicense = new HelpLicense();
            helpLicense.Show();
        }

        private void PreviewStart(object sender, MouseEventArgs e)
        {
            int tag = tagGetter(sender);
            if(typesArray[tag] != "image") { return; }

            previewer.image.Source = new BitmapImage(targetArray[tag]);
            previewer.Show();
            previewer.Left = this.Left;
            previewer.Top = this.Top;
        }
        private void PreviewEnd(object sender, MouseEventArgs e)
        {
            previewer.Hide();
            previewer.image.Source = null;
        }
    }
    #endregion
}