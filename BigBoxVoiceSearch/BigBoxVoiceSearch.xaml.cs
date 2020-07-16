using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;
using System.Speech.Recognition;
using System.Linq;
using System.Windows.Media.Imaging;
using System;
using System.Collections.ObjectModel;

namespace BigBoxVoiceSearch
{
    public class VoiceSearchResult
    {
        public ObservableCollection<IGame> MatchingGames { get; set; }
        public string RecognizedPhrase { get; set; }
        public float Confidence { get; set; }
        
        public int MatchCount 
        {
            get 
            {
                if(MatchingGames == null)
                {
                    return (0);
                }

                return MatchingGames.Count(); 
            } 
        }

        public VoiceSearchResult()
        {
            MatchingGames = new ObservableCollection<IGame>();
        }
    }

    public class GameTitleGrammarBuilder
    {
        private static string[] SpaceSplitter = new string[1] { " " };

        public IGame Game { get; set; }
        public string Title { get; set; }        
        public string MainTitle { get; set; }        
        public string Subtitle { get; set; }        
        public List<string> TitleWords { get; set; }

        public GameTitleGrammarBuilder(IGame _game)
        {
            Game = _game;
            Title = Game.Title;
            TitleWords = new List<string>();

            // find the index of the colon which indicates separation between title and subtitle
            int indexOfTitleSplit = Title.IndexOf(':');

            // get rid of the colon
            if (indexOfTitleSplit >= 1)
            {
                Title = Title.Replace(':', ' ');
            }

            // split title into individual words
            string[] allTitleWords = Title.Split(SpaceSplitter, StringSplitOptions.RemoveEmptyEntries);                        
            foreach(string word in allTitleWords)
            {
                // filter out noise words
                if (!IsNoiseWord(word))
                {
                    // replace roman numerals
                    string wordRomanNumeralReplaced = GetRomanNumeralReplacement(word);
                    if(!string.Equals(word, wordRomanNumeralReplaced, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // replace the roman numeral in the title
                        Title = Title.Replace(word, wordRomanNumeralReplaced);
                    }
                    // add the word to the title words
                    TitleWords.Add(wordRomanNumeralReplaced);
                }
            }

            // set the main and subtitle if the index of : exists
            if(indexOfTitleSplit >= 1)
            {
                MainTitle = Title.Substring(0, indexOfTitleSplit).Trim();
                Subtitle = Title.Substring(indexOfTitleSplit + 1).Trim();
            }

            // funky hack to get rid of multiple spaces
            Title = Title.Replace("  ", " ");
        }

        public static bool IsNoiseWord(string wLower)
        {
            if (string.Equals(wLower, "the", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "a", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "of", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "at", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "as", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "and", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "to", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "n'", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "'n", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "b", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "in", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "on", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static string GetRomanNumeralReplacement(string str)
        {
            switch(str)
            {
                case "II":
                    return "2";
                case "III":
                    return "3";
                case "IV":
                    return "4";
                case "V":
                    return "5";
                case "VI":                
                    return "6";                    
                case "VII":
                    return "7";
                case "VIII":
                    return "8";
                case "IX":
                    return "9";
                case "X":
                    return "10";
                case "XI":
                    return "11";
                case "XII":
                    return "12";
                case "XIII":
                    return "13";
                case "XIV":
                    return "14";
                case "XV":
                    return "15";
                case "XVI":
                    return "16";
                case "XVII":
                    return "17";
                case "XVIII":
                    return "18";
                case "XIX":                
                    return "19";
                default:
                    return str;
            }
        }
    }

    /// <summary>
    /// Interaction logic for BigBoxVoiceSearch.xaml
    /// </summary>
    public partial class BigBoxVoiceSearch : UserControl, IBigBoxThemeElementPlugin
    {
        List<GameTitleGrammarBuilder> GameTitleGrammarBuilders = new List<GameTitleGrammarBuilder>();

        private bool RecognitionInProgress = false;

        // inidicate progress of setting up recognition
        private bool RecognizerInitialized = false;
        private int RecognizerSetupTotalGames = 0;
        private int RecognizerSetupCurrentGame = 0;
        
        private string _appPath;
        public static SpeechRecognitionEngine Recognizer = new SpeechRecognitionEngine();
        List<string> TitleElements = new List<string>();

        VoiceSearchResult selectedSearchResult;
        int? SearchResultsSelectedIndex;
        public static ObservableCollection<VoiceSearchResult> VoiceSearchResults = new ObservableCollection<VoiceSearchResult>();

        IGame selectedGame;
        int? SelectedIndex;
        public static ObservableCollection<IGame> MatchingTitles = new ObservableCollection<IGame>();
     
        IGame[] AllGames = PluginHelper.DataManager.GetAllGames();
        
        private bool PluginEnabled;     
        
        public BigBoxVoiceSearch()
        {
            InitializeComponent();

            this.Loaded += UserControl_Loaded;
            this.Visibility = Visibility.Hidden;
            this.PluginEnabled = false;
        }

        public static void Log(string logMessage)
        {
            using (StreamWriter w = File.AppendText("BigBoxVoiceSearchLog.txt"))
            {
                w.Write("\r\nLog Entry : ");
                w.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
                w.WriteLine("  :");
                w.WriteLine($"  :{logMessage}");
                w.WriteLine("-------------------------------");
            }
        }



        // activate the plug-in with page up/down
        public bool OnPageDown()
        {
            this.DoRecognize();
            return (true);
        }

        // activate the plug-in with page up/down
        public bool OnPageUp()
        {
            this.DoRecognize();
            return (true);
        }

        // if the plug-in is active then move upwards in the result list
        public bool OnUp(bool held)
        {
            if (!this.PluginEnabled)
            {
                return false;
            }

            this.shiftUp();
 
            return true;
        }

        // if the plug-in is active then move down in the result list
        public bool OnDown(bool held)
        {
            if(!this.PluginEnabled)
            {
                return false;
            }

            this.shiftDown();

            return true;
        }

        void shiftLeft()
        {
            if (SearchResultsSelectedIndex == 0)
            {
                SearchResultsSelectedIndex = VoiceSearchResults.Count - 1;
            }
            else
            {
                SearchResultsSelectedIndex -= 1;
            }

            BigBoxVoiceSearch.Log($"Shift left: {SearchResultsSelectedIndex}");

            this.selectedSearchResultChanged();
        }

        void shiftRight()
        {
            if (SearchResultsSelectedIndex == VoiceSearchResults.Count - 1)
            {
                SearchResultsSelectedIndex = 0;
            }
            else
            {
                SearchResultsSelectedIndex += 1;
            }

            BigBoxVoiceSearch.Log($"Shift right: {SearchResultsSelectedIndex}");

            this.selectedSearchResultChanged();
        }


        void shiftUp()
        {
            // todo: perform shifting in games list
            if (SelectedIndex == 0)
            {
                SelectedIndex = MatchingTitles.Count - 1;
            }
            else
            {
                SelectedIndex -= 1;
            }

            this.selectedGameChanged();
        }

        void shiftDown()
        {
            if (SelectedIndex == MatchingTitles.Count - 1)
            {
                SelectedIndex = 0;
            }
            else
            {
                SelectedIndex += 1;
            }
            this.selectedGameChanged();
        }

        public void selectedSearchResultChanged()
        {
            if (SearchResultsSelectedIndex == null)
                return;

            // get a handle on the selected voice search result 
            selectedSearchResult = VoiceSearchResults[SearchResultsSelectedIndex.GetValueOrDefault()];

            // set the selection in the list box and scroll it into view
            ListBox_RecognitionResults.SelectedIndex = SearchResultsSelectedIndex.GetValueOrDefault();
            ListBox_RecognitionResults.SelectedItem = ListBox_RecognitionResults.SelectedIndex;
            ListBox_RecognitionResults.ScrollIntoView(ListBox_RecognitionResults.SelectedItem);

            // update game results list box with games from selected recognition result
            MatchingTitles = selectedSearchResult.MatchingGames;
            ListBox_Results.ItemsSource = MatchingTitles;
            SelectedIndex = 0;
            selectedGameChanged();
        }

        // when the selected game changes, scroll the list into place and display the image for the current selected game
        public void selectedGameChanged()
        {
            if (SelectedIndex == null)
                return;

            // get a handle on the selected game
            selectedGame = MatchingTitles[SelectedIndex.GetValueOrDefault()];

            // set the selection in the list box and scroll it into view 
            ListBox_Results.SelectedIndex = SelectedIndex.GetValueOrDefault();
            ListBox_Results.SelectedItem = ListBox_Results.SelectedIndex;
            ListBox_Results.ScrollIntoView(ListBox_Results.SelectedItem);

            // set game details - title text, game front image, platform clear logo
            if (selectedGame != null)
            {
                TextBlock_CurrentTitle.Text = selectedGame.Title;

                if (selectedGame.FrontImagePath != null)
                {
                    Image_GameFront.Source = new BitmapImage(new Uri(selectedGame.FrontImagePath));
                }
                else
                {
                    // todo: set missing game art image 
                    Image_GameFront.Source = null;
                }

                if(selectedGame.PlatformClearLogoImagePath != null)
                {
                    Image_PlatformClearLogo.Source = new BitmapImage(new Uri(selectedGame.PlatformClearLogoImagePath));
                }
                else
                {
                    // todo: set missing platform clear logo image
                    Image_PlatformClearLogo.Source = null;
                }
            }
            else
            {
                // blank out game title, game art, and platform logo
                TextBlock_CurrentTitle.Text = null;
                Image_GameFront.Source = null;
                Image_PlatformClearLogo.Source = null;
            }
        }

        // on enter - show the selected game
        public bool OnEnter()
        {
            if(!PluginEnabled)
            {
                return (false);
            }

            if (SelectedIndex == null)
            {
                return (true);
            }

            IGame currentGame = MatchingTitles[SelectedIndex.GetValueOrDefault()] as IGame;
            if (currentGame == null)
            {
                return (true);
            }

            PluginHelper.BigBoxMainViewModel.ShowGame(currentGame, FilterType.None);

            return (true);
        }

        // on escape - hide the plug-in UI
        public bool OnEscape()
        {
            if (!this.PluginEnabled)
            {
                return (false);
            }

            this.Visibility = Visibility.Hidden;
            this.PluginEnabled = false;

            return (true);
        }

        // shift results left
        public bool OnLeft(bool held)
        {
            if (!this.PluginEnabled)
            {
                return false;
            }

            this.shiftLeft();

            return true;
        }

        // shift results right
        public bool OnRight(bool held)
        {
            if (!this.PluginEnabled)
            {
                return false;
            }

            this.shiftRight();

            return true;
        }

        // on voice search - reset everything and fire off a voice search
        public void DoRecognize()
        {
            if(!RecognizerInitialized)
            {
                ResetForInitializing();
                return;
            }

            if(RecognitionInProgress)
            {
                return;
            }

            RecognitionInProgress = true;

            ResetForNewSearch();

            // fire off the voice recognition
            Recognizer.RecognizeAsync(RecognizeMode.Single); 
        }

        public void ResetForInitializing()
        {
            // flag the plug-in UI as focused so we know it's active when moving around in other events
            this.PluginEnabled = true;

            // make sure the plug-in UI is visible 
            this.Visibility = Visibility.Visible;

            // clear the result list            
            TextBlock_Prompt.Text = $"Please wait while voice recognition processes games {RecognizerSetupCurrentGame} of {RecognizerSetupTotalGames}";

            // clear the collection of words from the voice recognition
            VoiceSearchResults.Clear();

            // clear the collection of titles that were previously matched
            MatchingTitles.Clear();

            // clear the box image if there is one
            TextBlock_CurrentTitle.Text = null;
            Image_GameFront.Source = null;
            Image_PlatformClearLogo.Source = null;
            ListBox_Results.ItemsSource = null;
            ListBox_RecognitionResults.ItemsSource = null;
        }

        public void ResetForNewSearch()
        {
            // flag the plug-in UI as focused so we know it's active when moving around in other events
            this.PluginEnabled = true;

            // make sure the plug-in UI is visible 
            this.Visibility = Visibility.Visible;

            // clear the result list            
            TextBlock_Prompt.Text = "Speak a game title";
            
            // clear the collection of words from the voice recognition
            VoiceSearchResults.Clear();

            // clear the collection of titles that were previously matched
            MatchingTitles.Clear();

            // clear the box image if there is one
            TextBlock_CurrentTitle.Text = null;
            Image_GameFront.Source = null;
            Image_PlatformClearLogo.Source = null;
            ListBox_Results.ItemsSource = null;
            ListBox_RecognitionResults.ItemsSource = null;
        }

        public void OnSelectionChanged(FilterType filterType, string filterValue, IPlatform platform, IPlatformCategory category, IPlaylist playlist, IGame game)
        {
        }

        // setup the voice recognition with a grammar consisting of all of the titles in the user's installation, split by word
        private void InitRecognizer()
        {
            RecognizerInitialized = false;
            RecognizerSetupTotalGames = AllGames.Count();
            RecognizerSetupCurrentGame = 0;

            // create the voice search grammar from installed games
            foreach (var game in AllGames)
            {
                // increase progress
                RecognizerSetupCurrentGame += 1;

                GameTitleGrammarBuilder gameTitleGrammarBuilder = new GameTitleGrammarBuilder(game);
                GameTitleGrammarBuilders.Add(gameTitleGrammarBuilder);

                if (!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.Title) 
                    && !TitleElements.Contains(gameTitleGrammarBuilder.Title))
                {
                    TitleElements.Add(gameTitleGrammarBuilder.Title);
                }

                if (!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.MainTitle) && !TitleElements.Contains(gameTitleGrammarBuilder.MainTitle))
                {
                    TitleElements.Add(gameTitleGrammarBuilder.MainTitle);
                }

                if (!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.Subtitle) && !TitleElements.Contains(gameTitleGrammarBuilder.Subtitle))
                {
                    TitleElements.Add(gameTitleGrammarBuilder.Subtitle);
                }

                foreach(string word in gameTitleGrammarBuilder.TitleWords)
                {
                    if (!string.IsNullOrWhiteSpace(word) && !TitleElements.Contains(word))
                    {
                        TitleElements.Add(word);
                    }
                }
            }


            // create the list of choices from the title elements
            Choices choices = new Choices();
            choices.Add(TitleElements.ToArray());

            // create a grammar builder with the choice list
            GrammarBuilder grammarBuilder = new GrammarBuilder();
            grammarBuilder.Append(choices);

            // create a grammar from the grammer builder
            Grammar grammar = new Grammar(grammarBuilder);
            grammar.Name = "Search list items";

            // setup the recognizer
            Recognizer = new SpeechRecognitionEngine();
            Recognizer.InitialSilenceTimeout = TimeSpan.FromSeconds(4.0);
            Recognizer.RecognizeCompleted += new EventHandler<RecognizeCompletedEventArgs>(RecognizeCompleted);
            Recognizer.LoadGrammarAsync(grammar);
            Recognizer.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(SpeechHypothesized);            
            Recognizer.SetInputToDefaultAudioDevice();
            Recognizer.RecognizeAsyncCancel();
            RecognizerInitialized = true;
        }

        private void UserControl_Loaded_1(object sender, RoutedEventArgs e)
        {            
            _appPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        }

        // if confidence is low, ignore it, if it's medium, save the word, if it's very high, eliminate everything and take the high confidence result
        void SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            if (!GameTitleGrammarBuilder.IsNoiseWord(e.Result.Text))
            {                
                if(!VoiceSearchResults.Any(r => r.RecognizedPhrase.Equals(e.Result.Text, StringComparison.InvariantCultureIgnoreCase)))
                {
                    // add if it doesn't exist already
                    VoiceSearchResults.Add(new VoiceSearchResult { RecognizedPhrase = e.Result.Text, Confidence = e.Result.Confidence });
                }
                else
                {
                    // update confidence if text already exists but confidence on new item is higher
                    var existingResult = VoiceSearchResults.First(r => r.RecognizedPhrase.Equals(e.Result.Text, StringComparison.InvariantCultureIgnoreCase));
                    if(existingResult.Confidence < e.Result.Confidence)
                    {
                        VoiceSearchResults.Remove(existingResult);
                        VoiceSearchResults.Add(new VoiceSearchResult { RecognizedPhrase = e.Result.Text, Confidence = e.Result.Confidence });
                    }
                }
            }
        }

        // once recognition is completed, match the voice recognition result against the games list
        void RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            RecognitionInProgress = false;

            MatchingTitles.Clear();

            if (e?.Error != null)
            {
                if (Recognizer != null)
                {
                    Recognizer.RecognizeAsyncCancel();
                }
                TextBlock_Prompt.Text = $"Error: {e.Error.Message}";
                return;
            }

            if (e?.InitialSilenceTimeout == true || e?.BabbleTimeout == true)
            {
                if (Recognizer != null)
                {
                    Recognizer.RecognizeAsyncCancel();
                }

                TextBlock_Prompt.Text = $"Voice recognition could not year anything, please try again";
                return;
            }

            // title match each voice search result
            if(VoiceSearchResults?.Count() > 0)
            {
                var orderedVoiceSearchResults = VoiceSearchResults.OrderByDescending(s => s.Confidence).ToList();
                VoiceSearchResults.Clear();

                foreach (var voiceSearchResult in orderedVoiceSearchResults)
                {
                    // get exact title matches
                    var fullTitleMatches = from gameTitleGrammarBuilder in GameTitleGrammarBuilders
                                        where string.Equals(voiceSearchResult.RecognizedPhrase, gameTitleGrammarBuilder.Title, StringComparison.InvariantCultureIgnoreCase)
                                        select new { gameTitleGrammarBuilder.Game, MatchLevel = 0 };

                    // get main title matches
                    var mainTitleMatches = from gameTitleGrammarBuilder in GameTitleGrammarBuilders
                                       where string.Equals(voiceSearchResult.RecognizedPhrase, gameTitleGrammarBuilder.MainTitle, StringComparison.InvariantCultureIgnoreCase)
                                       select new { gameTitleGrammarBuilder.Game, MatchLevel = 1 };

                    // get subtitle matches
                    var subTitleMatches = from gameTitleGrammarBuilder in GameTitleGrammarBuilders
                                          where string.Equals(voiceSearchResult.RecognizedPhrase, gameTitleGrammarBuilder.Subtitle, StringComparison.InvariantCultureIgnoreCase)
                                          select new { gameTitleGrammarBuilder.Game, MatchLevel = 1 };

                    // get matches where the title starts with the term
                    var fullTitleStartsWith = from gameTitleGrammarBuilder in GameTitleGrammarBuilders
                                            where gameTitleGrammarBuilder.Title.StartsWith(voiceSearchResult.RecognizedPhrase)
                                            select new { gameTitleGrammarBuilder.Game, MatchLevel = 2 };

                    // get matches where the title contains a term
                    var fullTitleContains = from gameTitleGrammarBuilder in GameTitleGrammarBuilders
                                            where gameTitleGrammarBuilder.Title.Contains(voiceSearchResult.RecognizedPhrase)
                                            select new { gameTitleGrammarBuilder.Game, MatchLevel = 3 };

                    // union them together, group by game, get the minimum (best) match level
                    var allMatches = fullTitleMatches
                        .Union(mainTitleMatches)
                        .Union(subTitleMatches)
                        .Union(fullTitleContains)
                        .GroupBy(s => s.Game)
                        .Select(s => new { Game = s.Key, MatchLevel = s.Min(m => m.MatchLevel) });

                    var allMatchesOrdered = allMatches.OrderBy(s => s.MatchLevel).ThenBy(s => s.Game.Title).ToList();

                    foreach(var game in allMatchesOrdered)
                    {
                        voiceSearchResult.MatchingGames.Add(game.Game);
                    }

                    VoiceSearchResults.Add(voiceSearchResult);

                }

                ListBox_RecognitionResults.ItemsSource = VoiceSearchResults;
                SearchResultsSelectedIndex = 0;
                selectedSearchResult = VoiceSearchResults[SearchResultsSelectedIndex.GetValueOrDefault()];
                selectedSearchResultChanged();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _appPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            
            /*
            Thread thread = new Thread(() =>
                this.InitRecognizer()
            );
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            */
            
            this.InitRecognizer();
        }
    }
}