using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Windows.Media.Control;
using Genius;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using Genius.Models.Response;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Windows.Shapes;
using Windows.Media.Protection.PlayReady;
using Genius.Models.Song;
using System.Xml.Linq;

namespace LyricFinder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _lyricsSource;
        private string _lyricsTranslated;
        private Finder _finder;
        private bool handleSelectionChanged = true;
        public MainWindow()
        {
            InitializeComponent();
            _finder = new Finder();
            translate_Button.IsEnabled = false;
            back_Button.IsEnabled = false;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            songs_ComboBox.Items.Clear();
            _lyricsSource = string.Empty;
            _lyricsTranslated = string.Empty;
            translate_Button.IsEnabled = false;
            back_Button.IsEnabled = false;

            var songs = await _finder.GetSongs();

            if (songs.Count == 0) return;

            var lyrics = await _finder.GetLyrics(songs.FirstOrDefault().Result.Id);

            if (lyrics == "Song is not found")
            {
                OutputBlock.Text = "Песня не найдена";
                return;
            }

            string[] lines = lyrics.Split(@"\n");

            OutputBlock.Inlines.Clear();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                    OutputBlock.Inlines.Add(new LineBreak());
                OutputBlock.Inlines.Add(new Run(lines[i]));
            }

            translate_Button.IsEnabled = true;

            for (int i = 0; i < songs.Count; i++)
            {
                if (i == 0) SetSelectedItem(songs[i].Result);

                var song = songs[i].Result;
                songs_ComboBox.Items.Add(song);
            }

        }
        private async void translate_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_lyricsTranslated != string.Empty)
            {
                OutputBlock.Text = _lyricsTranslated;
                translate_Button.IsEnabled = false;
                back_Button.IsEnabled = true;
                return;
            }

            _lyricsSource = OutputBlock.Text;

            OutputBlock.Inlines.Clear();

            using (var client = new HttpClient())
            {
                string fromLanguage = "auto";
                string toLanguage = "ru";

                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={Uri.EscapeUriString(_lyricsSource)}";

                HttpResponseMessage response = await client.GetAsync(url);

                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                var translations = JArray.Parse(responseBody)[0];

                foreach (var t in translations.Children())
                {
                    var translatedText = t.First?.ToString();
                    if (!string.IsNullOrEmpty(translatedText))
                    {
                        OutputBlock.Inlines.Add(new Run(translatedText));
                    }
                }
                _lyricsTranslated = OutputBlock.Text;
                translate_Button.IsEnabled = false;
                back_Button.IsEnabled = true;
            }
        }
        private void Back_Button_Click(object sender, RoutedEventArgs e)
        {
            OutputBlock.Text = _lyricsSource;
            translate_Button.IsEnabled = true;
            back_Button.IsEnabled = false;
        }
        private async void manualInput_Button_Click(object sender, RoutedEventArgs e)
        {
            songs_ComboBox.Items.Clear();
            Manualnput manualnput = new Manualnput();

            if (manualnput.ShowDialog() == false) return;
            if (manualnput.inputBox.Text == string.Empty) return;

            _lyricsSource = string.Empty;
            _lyricsTranslated = string.Empty;
            translate_Button.IsEnabled = false;
            back_Button.IsEnabled = false;

            string query = manualnput.inputBox.Text;

            var songs = _finder.GetSongs(query).Result;

            var firstSong = songs.FirstOrDefault().Result;

            var lyrics = await _finder.GetLyrics(firstSong.Id);

            string[] lines = lyrics.Split(@"\n");

            OutputBlock.Inlines.Clear();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                    OutputBlock.Inlines.Add(new LineBreak());
                OutputBlock.Inlines.Add(new Run(lines[i]));
            }

            translate_Button.IsEnabled = true;

            for (int i = 0; i < songs.Count; i++)
            {
                if (i == 0) SetSelectedItem(songs[i].Result);

                var song = songs[i].Result;
                songs_ComboBox.Items.Add(song);
            }

        }

        private async void songs_ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!handleSelectionChanged) return;
            if (songs_ComboBox.SelectedItem == null) return;

            var selectedSong = songs_ComboBox.SelectedItem as Song;

            _lyricsSource = string.Empty;
            _lyricsTranslated = string.Empty;
            translate_Button.IsEnabled = false;
            back_Button.IsEnabled = false;

            var lyrics = await _finder.GetLyrics(selectedSong.Id);

            string[] lines = lyrics.Split(@"\n");

            OutputBlock.Inlines.Clear();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                    OutputBlock.Inlines.Add(new LineBreak());
                OutputBlock.Inlines.Add(new Run(lines[i]));
            }

            translate_Button.IsEnabled = true;
        }

        private async void SetSelectedItem(Song item)
        {
            handleSelectionChanged = false;
            songs_ComboBox.SelectedItem = item;
            await Task.Delay(100);
            handleSelectionChanged = true;
        }
    }
}
