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
using System.Security.Policy;
using Genius.Models;
using System.Collections.Generic;

namespace LyricFinder
{
    public class Finder
    {
        private readonly string APIKEY = "PUT_YOUR_API_KEY_HERE";
        public Finder()
        {
        }
        public async Task<List<SearchHit>> GetSongs()
        {
            var gsmtcsm = await GetSystemMediaTransportControlsSessionManager();
            var mediaProperties = await GetMediaProperties(gsmtcsm.GetCurrentSession());

            var songName = mediaProperties.Title;
            var artistName = mediaProperties.Artist;

            string pattern = @"(\[.*?\])|(\(.*?\))";
            songName = Regex.Replace(songName, pattern, "").Trim();

            var client = new GeniusClient(APIKEY);

            SearchResponse searchResults = null;

            if (songName.Contains("-") || songName.Contains("—"))
            {
                searchResults = await client.SearchClient.Search($"{songName}");
            }
            else
            {
                searchResults = await client.SearchClient.Search($"{artistName} {songName}");
            }

            var songs = searchResults.Response.Hits;

            return songs;
        }
        public async Task<List<SearchHit>> GetSongs(string query)
        {

            var client = new GeniusClient(APIKEY);

            SearchResponse searchResults = null;

            searchResults = await client.SearchClient.Search(query);

            var songs = searchResults.Response.Hits;

            return songs;
        }
        public async Task<string> GetLyrics(ulong songId)
        {
            var client = new GeniusClient(APIKEY);

            try
            {
                var song = await client.SongClient.GetSong(songId);

                var web = new HtmlWeb();
                var doc = web.Load(song.Response.Song.Url);
                var lyricsJson = doc.DocumentNode.SelectSingleNode("//script[contains(text(), 'window.__PRELOADED_STATE__')]").InnerText;

                var start = lyricsJson.IndexOf(@"<p>");
                var end = lyricsJson.IndexOf(@",\""c") - 1;

                var preloadedState = lyricsJson[start..end];
                string lyricsHtml = Regex.Unescape(preloadedState);

                MessageBox.Show("Найдена песня " + song.Response.Song.FullTitle, "Успех !", MessageBoxButton.OK, MessageBoxImage.None);

                var lyrics = ConvertHtmlToPlainText(lyricsHtml);

                return lyrics;


            }
            catch { return "Song is not found"; }
        }
        
        private static string ConvertHtmlToPlainText(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            return ConvertContentTo(document.DocumentNode);
        }

        private static string ConvertContentTo(HtmlNode node)
        {
            var innerText = new StringBuilder();

            foreach (var subnode in node.ChildNodes)
            {
                switch (subnode.NodeType)
                {
                    case HtmlNodeType.Text:
                        innerText.Append(subnode.InnerText);
                        break;
                    case HtmlNodeType.Element:
                        innerText.Append(ConvertContentTo(subnode));
                        break;
                }
            }

            return innerText.ToString();
        }
        private static async Task<GlobalSystemMediaTransportControlsSessionManager> GetSystemMediaTransportControlsSessionManager()
=> await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

        private static async Task<GlobalSystemMediaTransportControlsSessionMediaProperties> GetMediaProperties(GlobalSystemMediaTransportControlsSession session)
            => await session.TryGetMediaPropertiesAsync();
    }
}
