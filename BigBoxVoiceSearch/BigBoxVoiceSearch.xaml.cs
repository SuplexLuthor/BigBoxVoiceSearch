using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.IO;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;
using System.Speech.Recognition;
using System.Linq;
using System.Windows.Media.Imaging;
using System;
using System.Security.Policy;
using System.Text;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Data;

namespace BigBoxVoiceSearch
{
    public class VoiceSearchResult
    {
        public List<IGame> MatchingGames { get; set; }
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
    }

    public class GameTitleGrammarBuilder
    {
        public IGame Game { get; set; }
        public string Title { get; set; }
        public string TitleClean { get; set; }
        public string MainTitle { get; set; }
        public string MainTitleClean { get; set; }
        public string Subtitle { get; set; }
        public string SubtitleClean { get; set; }
        public List<string> TitleWords { get; set; }

        public GameTitleGrammarBuilder(IGame _game)
        {
            Game = _game;
            Title = Game.Title;
            TitleClean = CleanUpString(Title);
            SetupMainTitle();
            MainTitleClean = CleanUpString(MainTitle);
            SubtitleClean = CleanUpString(Subtitle);
            SetupTitleWords();
        }

        private static string CleanUpString(string str)
        {
            if(string.IsNullOrWhiteSpace(str))
            {
                return "";
            }

            return ReplaceRomanNumerals(RemoveSpecialCharacters(str));
        }

        private void SetupMainTitle()
        {
            int splitIndex = Title.IndexOf(':');

            if (splitIndex>=0)
            {
                MainTitle = Title.Substring(0, splitIndex);
                Subtitle = Title.Substring(splitIndex+1).Trim();
            }
        }

        private void SetupTitleWords()
        {
            TitleWords = new List<string>();

            // if no space - just add the word and get out
            if(!TitleClean.Contains(" "))
            {
                TitleWords.Add(TitleClean);
                return;
            }

            // split on space
            string[] splitter = new string[1];
            splitter[0] = " ";
            string[] cleanTitleWords = TitleClean.Split(splitter, StringSplitOptions.RemoveEmptyEntries);

            if(cleanTitleWords == null)
            {
                return;
            }

            foreach (string word in cleanTitleWords)
            {
                if (!IsNoiseWord(word))
                {
                    TitleWords.Add(word);
                }
            }
        }

        public static string RemoveSpecialCharacters(string str)
        {
            if(string.IsNullOrWhiteSpace(str))
            {
                return ("");
            }

            string result = str.Replace(":", " ");
            result = result.Replace("-", " ");
            return (result);
        }

        public static bool IsNoiseWord(string wLower)
        {
            if (string.Equals(wLower, "the", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "a", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "of", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "at", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "as", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "and", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "ii", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "to", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "n'", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "'n", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "a", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "b", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "x", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "in", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "on", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static string ReplaceRomanNumerals(string str)
        {
            string result = str;
            string[] wordsInString = result.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in wordsInString)
            {
                switch (word)
                {
                    case "II":
                        result = result.Replace("II", "2");
                        break;
                    case "III":
                        result = result.Replace("III", "3");
                        break;
                    case "IV":
                        result = result.Replace("IV", "4");
                        break;
                    case "V":
                        result = result.Replace("V", "5");
                        break;
                    case "VI":
                        result = result.Replace("VI", "6");
                        break;
                    case "VII":
                        result = result.Replace("VII", "7");
                        break;
                    case "VIII":
                        result = result.Replace("VIII", "8");
                        break;
                    case "IX":
                        result = result.Replace("IX", "9");
                        break;
                    case "X":
                        result = result.Replace("X", "10");
                        break;
                    case "XI":
                        result = result.Replace("XI", "11");
                        break;
                    case "XII":
                        result = result.Replace("XII", "12");
                        break;
                    case "XIII":
                        result = result.Replace("XIII", "13");
                        break;
                    case "XIV":
                        result = result.Replace("XIV", "14");
                        break;
                    case "XV":
                        result = result.Replace("XV", "15");
                        break;
                    case "XVI":
                        result = result.Replace("XVI", "16");
                        break;
                    case "XVII":
                        result = result.Replace("XVII", "17");
                        break;
                    case "XVIII":
                        result = result.Replace("XVIII", "18");
                        break;
                    case "XIX":
                        result = result.Replace("XIX", "19");
                        break;
                }
            }
            BigBoxVoiceSearch.Log($"Replace roman numbers from ({str}) to ({result})");

            return (result);
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

        IGame selectedGame;
        private string _appPath;
        public static SpeechRecognitionEngine Recognizer = new SpeechRecognitionEngine();
        List<string> TitleElements = new List<string>();

        VoiceSearchResult selectedSearchResult;
        int? SearchResultsSelectedIndex;
        public static ObservableCollection<VoiceSearchResult> SearchResults = new ObservableCollection<VoiceSearchResult>();


        int? SelectedIndex;
        public static ObservableCollection<IGame> MatchingTitles = new ObservableCollection<IGame>();
     
        IGame[] AllGames = PluginHelper.DataManager.GetAllGames();
        private bool focused;     
        
        public BigBoxVoiceSearch()
        {
            InitializeComponent();

            this.Loaded += UserControl_Loaded;
            this.Visibility = Visibility.Hidden;
            this.focused = false;
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
            if (!this.focused)
            {
                return false;
            }

            this.shiftLeft();
 
            return true;
        }

        // if the plug-in is active then move down in the result list
        public bool OnDown(bool held)
        {
            if(!this.focused)
            {
                return false;
            }

            this.shiftRight();

            return true;
        }

        void shiftLeft()
        {
            if (SearchResultsSelectedIndex == 0)
            {
                SearchResultsSelectedIndex = SearchResults.Count - 1;
            }
            else
            {
                SearchResultsSelectedIndex -= 1;
            }

            this.selectedSearchResultChanged();
        }

        void shiftRight()
        {
            if (SearchResultsSelectedIndex == SearchResults.Count - 1)
            {
                SearchResultsSelectedIndex = 0;
            }
            else
            {
                SearchResultsSelectedIndex += 1;
            }
            this.selectedSearchResultChanged();
        }

        public void selectedSearchResultChanged()
        {
            if (SearchResultsSelectedIndex == null)
                return;

            selectedSearchResult = SearchResults[SearchResultsSelectedIndex.GetValueOrDefault()];
            ListBox_RecognitionResults.SelectedIndex = SearchResultsSelectedIndex.GetValueOrDefault();
            ListBox_RecognitionResults.SelectedItem = ListBox_RecognitionResults.SelectedIndex;
            ListBox_RecognitionResults.ScrollIntoView(ListBox_RecognitionResults.SelectedItem);

            // todo: update game results list box with games from selected recognition result
        }

        // when the selected game changes, scroll the list into place and display the image for the current selected game
        public void selectedGameChanged()
        {
            if (SelectedIndex == null)
                return;

            selectedGame = MatchingTitles[SelectedIndex.GetValueOrDefault()];

            ListBox_Results.SelectedIndex = SelectedIndex.GetValueOrDefault();
            ListBox_Results.SelectedItem = ListBox_Results.SelectedIndex;
            ListBox_Results.ScrollIntoView(ListBox_Results.SelectedItem);

            // setup the selected item
            if (selectedGame != null)
            {
                TextBlock_CurrentTitle.Text = selectedGame.Title;

                if (selectedGame.FrontImagePath != null)
                {
                    Image_GameFront.Source = new BitmapImage(new Uri(selectedGame.FrontImagePath));
                }

                if(selectedGame.PlatformClearLogoImagePath != null)
                {
                    Image_PlatformClearLogo.Source = new BitmapImage(new Uri(selectedGame.PlatformClearLogoImagePath));
                }
                else
                {
                    // todo: set default missing platform clear logo image
                }
            }
            else
            {
                // todo: set default fall back image
                Image_GameFront.Source = null;
                Image_PlatformClearLogo.Source = null;
            }
        }

        // on enter - show the selected game
        public bool OnEnter()
        {
            if(!focused)
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
            if (!this.focused)
            {
                return (false);
            }

            this.Visibility = Visibility.Hidden;
            this.focused = false;

            return (true);
        }

        // shift results left
        public bool OnLeft(bool held)
        {
            if (!this.focused)
            {
                return false;
            }

            this.shiftLeft();

            return true;
        }

        // shift results right
        public bool OnRight(bool held)
        {
            if (!this.focused)
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
            this.focused = true;

            // make sure the plug-in UI is visible 
            this.Visibility = Visibility.Visible;

            // clear the result list            
            TextBlock_Prompt.Text = $"Please wait while voice recognition processes games {RecognizerSetupCurrentGame} of {RecognizerSetupTotalGames}";

            // clear the collection of words from the voice recognition
            SearchResults.Clear();

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
            Log("new search");

            // flag the plug-in UI as focused so we know it's active when moving around in other events
            this.focused = true;

            // make sure the plug-in UI is visible 
            this.Visibility = Visibility.Visible;

            // clear the result list            
            TextBlock_Prompt.Text = "Speak a game title";
            
            // clear the collection of words from the voice recognition
            SearchResults.Clear();

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

                if(!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.Title) && !TitleElements.Contains(gameTitleGrammarBuilder.Title))
                {
                    TitleElements.Add(gameTitleGrammarBuilder.Title);
                }

                if(!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.TitleClean) && !TitleElements.Contains(gameTitleGrammarBuilder.TitleClean))
                {
                    TitleElements.Add(gameTitleGrammarBuilder.TitleClean);
                }

                if(!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.MainTitle) && !TitleElements.Contains(gameTitleGrammarBuilder.MainTitle))
                {
                    TitleElements.Add(gameTitleGrammarBuilder.MainTitle);
                }

                if(!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.MainTitleClean) && !TitleElements.Contains(gameTitleGrammarBuilder.MainTitleClean))
                {
                    TitleElements.Add(gameTitleGrammarBuilder.MainTitleClean);
                }

                if (!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.Subtitle) && !TitleElements.Contains(gameTitleGrammarBuilder.Subtitle))
                {
                    TitleElements.Add(gameTitleGrammarBuilder.Subtitle);
                }

                if (!string.IsNullOrWhiteSpace(gameTitleGrammarBuilder.SubtitleClean) && !TitleElements.Contains(gameTitleGrammarBuilder.SubtitleClean))
                {
                    TitleElements.Add(gameTitleGrammarBuilder.SubtitleClean);
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

        // filter out noise words
        internal static bool IsNoiseWord(string wLower)
        {
            if (string.Equals(wLower, "the", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "a", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "of", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "at", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "as", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "and", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "ii", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "to", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "n'", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "'n", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "a", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "b", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "x", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "in", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "on", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        // if confidence is low, ignore it, if it's medium, save the word, if it's very high, eliminate everything and take the high confidence result
        void SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            Log($"speech hypothesized: {e.Result.Text} ({e.Result.Confidence})");

            if (!IsNoiseWord(e.Result.Text))
            {                
                if(!SearchResults.Any(r => r.RecognizedPhrase.Equals(e.Result.Text, StringComparison.InvariantCultureIgnoreCase)))
                {
                    // add if it doesn't exist already
                    SearchResults.Add(new VoiceSearchResult { RecognizedPhrase = e.Result.Text, Confidence = e.Result.Confidence });
                }
                else
                {
                    // update confidence if text already exists but confidence on new item is higher
                    var existingResult = SearchResults.First(r => r.RecognizedPhrase.Equals(e.Result.Text, StringComparison.InvariantCultureIgnoreCase));
                    if(existingResult.Confidence < e.Result.Confidence)
                    {
                        SearchResults.Remove(existingResult);
                        SearchResults.Add(new VoiceSearchResult { RecognizedPhrase = e.Result.Text, Confidence = e.Result.Confidence });
                    }
                }
            }
        }

        // once recognition is completed, match the voice recognition result against the games list
        void RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            RecognitionInProgress = false;

            Log("Recognize completed");
            if(SearchResults != null && SearchResults.Count > 0)
            {
                foreach(var res in SearchResults)
                {
                    Log($"Result: {res.RecognizedPhrase} ({res.Confidence})");
                }
            }
            else
            {
                Log("Search results are null or empty");
            }

            /*
             * Lowest - part of a word
             * Medium Low - A word
             * Medium - Multiple words - not together
             * Medium High - Multiple words together
             * Highest - complete title
             */

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

            // todo: perform search for each term - for now just search on highest confidence
            if(SearchResults?.Count() > 0)
            {
                var maxResult = SearchResults.OrderByDescending(p => p.Confidence)?.FirstOrDefault();

                if(maxResult != null)
                {
                    Log($"Max Result: {maxResult.RecognizedPhrase} ({maxResult.Confidence})");
                }

                var gameMatches = from game in AllGames
                                  where game.Title.Contains(maxResult.RecognizedPhrase)
                                  select game;

                foreach(var game in gameMatches)
                {
                    Log($"Game match: {game.Title}");
                    MatchingTitles.Add(game);
                }

                if(MatchingTitles?.Count() > 0)
                {
                    SelectedIndex = 0;
                    selectedGame = MatchingTitles[SelectedIndex.GetValueOrDefault()];
                    selectedGameChanged();
                    TextBlock_Prompt.Text = $"Found {MatchingTitles.Count()} matching games";                    
                    ListBox_Results.ItemsSource = MatchingTitles;
                    ListBox_RecognitionResults.ItemsSource = SearchResults;
                }
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