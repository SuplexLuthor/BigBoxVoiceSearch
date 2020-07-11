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

namespace BigBoxVoiceSearch
{
    /// <summary>
    /// Interaction logic for BigBoxVoiceSearch.xaml
    /// </summary>
    public partial class BigBoxVoiceSearch : UserControl, IBigBoxThemeElementPlugin
    {
        IGame selectedGame;
        private string _appPath;
        public static SpeechRecognitionEngine Recognizer = new SpeechRecognitionEngine();
        List<string> TitleElements = new List<string>();
        public static List<string> MatchingSearchWords = new List<string>();
        public static List<float> MatchingSearchWordConfidence= new List<float>();

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

            this.InitRecognizer();
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

        void shiftRight()
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
                if (selectedGame.FrontImagePath != null)
                {
                    Image_GameFront.Source = new BitmapImage(new Uri(selectedGame.FrontImagePath));
                }

                if(selectedGame.PlatformClearLogoImagePath != null)
                {
                    Image_PlatformClearLogo.Source = new BitmapImage(new Uri(selectedGame.PlatformClearLogoImagePath));
                }
                Log($"Clear logo path: {selectedGame.PlatformClearLogoImagePath}");

                // todo: why can't we display controller images? 
                /*
                string controllerImagePath = $@"{_appPath}\Plugins\BigBoxVoiceSearch\Media\Controllers\{selectedGame.Platform}.png";
                Log($"Controller path: {controllerImagePath}");
                Image_PlatformController.Source = new BitmapImage(new Uri(controllerImagePath));
                */


                // Text_Title.Text = selectedGame.Title;
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

        // don't do anything on left/right
        public bool OnLeft(bool held)
        {
            if (!this.focused)
            {
                return false;
            }

            this.shiftLeft();

            return true;
        }

        // don't do anything on left/right
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
            ResetForNewSearch();

            // fire off the voice recognition
            Recognizer.RecognizeAsync(RecognizeMode.Single); 
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
            MatchingSearchWords.Clear();
            MatchingSearchWordConfidence.Clear();

            // clear the collection of titles that were previously matched
            MatchingTitles.Clear();

            // clear the box image if there is one
            Image_GameFront.Source = null;
            Image_PlatformClearLogo.Source = null;
            Image_PlatformController = null;
            TextBlock_SearchedFor.Text = null;
            ListBox_Results.ItemsSource = null;
        }

        public void OnSelectionChanged(FilterType filterType, string filterValue, IPlatform platform, IPlatformCategory category, IPlaylist playlist, IGame game)
        {
        }

        // setup the voice recognition with a grammar consisting of all of the titles in the user's installation, split by word
        private void InitRecognizer()
        {            
            // create the voice search grammar from installed games
            string[] splitter = new string[1];
            splitter[0] = " ";

            foreach (var game in AllGames)
            {
                string cleanTitle = game.Title;
                if (string.IsNullOrWhiteSpace(cleanTitle))
                {
                    continue;
                }

                cleanTitle = cleanTitle.Replace(":", " ");
                var splitTitle = cleanTitle.Split(splitter, System.StringSplitOptions.RemoveEmptyEntries);

                // add each word of each game title to the list of title elements
                foreach (string word in splitTitle)
                {
                    if (!IsNoiseWord(word))
                    {
                        if (!TitleElements.Contains(word, StringComparer.InvariantCultureIgnoreCase))
                        {
                            TitleElements.Add(word);
                        }
                    }

                    // clean up the game title
                    switch (word)
                    {
                        case "II":
                            cleanTitle = cleanTitle.Replace("II", "2");
                            break;
                        case "III":
                            cleanTitle = cleanTitle.Replace("III", "3");
                            break;
                        case "IV":
                            cleanTitle = cleanTitle.Replace("IV", "4");
                            break;
                        case "V":
                            cleanTitle = cleanTitle.Replace("V", "5");
                            break;
                        case "VI":
                            cleanTitle = cleanTitle.Replace("VI", "6");
                            break;
                        case "VII":
                            cleanTitle = cleanTitle.Replace("VII", "7");
                            break;
                        case "VIII":
                            cleanTitle = cleanTitle.Replace("VIII", "8");
                            break;
                        case "IX":
                            cleanTitle = cleanTitle.Replace("IX", "9");
                            break;
                        case "X":
                            cleanTitle = cleanTitle.Replace("X", "10");
                            break;
                        case "XI":
                            cleanTitle = cleanTitle.Replace("XI", "11");
                            break;
                        case "XII":
                            cleanTitle = cleanTitle.Replace("XII", "12");
                            break;
                        case "XIII":
                            cleanTitle = cleanTitle.Replace("XIII", "13");
                            break;
                        case "XIV":
                            cleanTitle = cleanTitle.Replace("XIV", "14");
                            break;
                        case "XV":
                            cleanTitle = cleanTitle.Replace("XV", "15");
                            break;
                        case "XVI":
                            cleanTitle = cleanTitle.Replace("XVI", "16");
                            break;
                        case "XVII":
                            cleanTitle = cleanTitle.Replace("XVII", "17");
                            break;
                        case "XVIII":
                            cleanTitle = cleanTitle.Replace("XVIII", "18");
                            break;
                        case "XIX":
                            cleanTitle = cleanTitle.Replace("XIX", "19");
                            break;
                    }

                }

                // add the game title
                if (!TitleElements.Contains(cleanTitle, StringComparer.InvariantCultureIgnoreCase))
                {
                    TitleElements.Add(cleanTitle);
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
                if (!MatchingSearchWords.Contains(e.Result.Text, StringComparer.InvariantCultureIgnoreCase))
                {
                    // need some standards; require a score of at least 0.5
                    if (e.Result.Confidence >= 0.5)
                    {
                        // if we're very confident, fuck the rest
                        // todo: flag convident results and change the way search is done to match full title instead of word by word?
                        if (e.Result.Confidence >= 0.9)
                        {
                            MatchingSearchWords.Clear();
                            MatchingSearchWordConfidence.Clear();
                        }

                        MatchingSearchWords.Add(e.Result.Text);
                        MatchingSearchWordConfidence.Add(e.Result.Confidence);
                    }
                }
            }
        }

        // once recognition is completed, match the voice recognition result against the games list
        void RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {

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

            if (MatchingSearchWords?.Count() > 0)
            {
                bool searchMatch = false;

                foreach (var game in AllGames)
                {
                    foreach (string word in MatchingSearchWords)
                    {
                        if (!string.IsNullOrWhiteSpace(game.Title))
                        {
                            if (game.Title.IndexOf(word, StringComparison.InvariantCultureIgnoreCase) >= 0)
                            {
                                if (!searchMatch)
                                {
                                    searchMatch = true;
                                }
                                if (!MatchingTitles.Contains(game))
                                {
                                    MatchingTitles.Add(game);
                                }
                            }
                        }

                    }
                }
            }

            // if we have matching titles, set up the games grid
            if(MatchingTitles?.Count() > 0)
            {                
                SelectedIndex = 0;
                selectedGame = MatchingTitles[SelectedIndex.GetValueOrDefault()];
                selectedGameChanged();
                TextBlock_Prompt.Text = $"Found {MatchingTitles.Count()} matching games";

                StringBuilder searchedFor = new StringBuilder();
                for(int i = 0; i < MatchingSearchWords.Count; i++)
                {
                    string word = MatchingSearchWords[i];
                    float conf = MatchingSearchWordConfidence[i];
                    searchedFor.Append($"{word} ({conf}) ");
                }

                /*
                foreach(var word in MatchingSearchWords)
                {
                    searchedFor.Append($"{word} ");
                }
                */
                TextBlock_SearchedFor.Text = searchedFor.ToString();                
                ListBox_Results.ItemsSource = MatchingTitles;               
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _appPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        }
    }
}