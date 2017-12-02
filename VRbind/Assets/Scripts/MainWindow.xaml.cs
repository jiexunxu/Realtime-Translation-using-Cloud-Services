// <copyright file="MainWindow.xaml.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// Microsoft Cognitive Services (formerly Project Oxford): https://www.microsoft.com/cognitive-services
//
// Microsoft Cognitive Services (formerly Project Oxford) GitHub:
// https://github.com/Microsoft/Cognitive-Speech-STT-Windows
//
// Copyright (c) Microsoft Corporation
// All rights reserved.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>

namespace Microsoft.CognitiveServices.SpeechRecognition
{
    using System;
    using System.ComponentModel;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Runtime.CompilerServices;
    using System.Windows;
  //  using Google.Cloud.Translation.V2;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// The isolated storage subscription key file name.
        /// </summary>
        private const string IsolatedStorageSubscriptionKeyFileName = "Subscription.txt";

        /// <summary>
        /// The default subscription key prompt message
        /// </summary>
        private const string DefaultSubscriptionKeyPromptMessage = "Paste your subscription key here to start";

        /// <summary>
        /// You can also put the primary key in app.config, instead of using UI.
        /// string subscriptionKey = ConfigurationManager.AppSettings["primaryKey"];
        /// </summary>
        private string subscriptionKey;

        /// <summary>
        /// The data recognition client
        /// </summary>
        private DataRecognitionClient dataClient;

        /// <summary>
        /// The microphone client
        /// </summary>
        private MicrophoneRecognitionClient micClient;

        private String inputLanguage;

// private TranslationClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {

            /*   Console.OutputEncoding = System.Text.Encoding.Unicode;
               TranslationClient client = TranslationClient.Create();
               var response = client.TranslateText("Hello World.", "ru");
               Console.WriteLine(response.TranslatedText);  */
            this.InitializeComponent();
            this.Initialize();          
        }

        #region Events

        /// <summary>
        /// Implement INotifyPropertyChanged interface
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        /// <summary>
        /// Gets or sets a value indicating whether this instance is microphone client short phrase.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is microphone client short phrase; otherwise, <c>false</c>.
        /// </value>
        public bool IsMicrophoneClientShortPhrase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is microphone client dictation.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is microphone client dictation; otherwise, <c>false</c>.
        /// </value>
        public bool IsMicrophoneClientDictation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is microphone client with intent.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is microphone client with intent; otherwise, <c>false</c>.
        /// </value>
        public bool IsMicrophoneClientWithIntent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is data client short phrase.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is data client short phrase; otherwise, <c>false</c>.
        /// </value>
        public bool IsDataClientShortPhrase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is data client with intent.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is data client with intent; otherwise, <c>false</c>.
        /// </value>
        public bool IsDataClientWithIntent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is data client dictation.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is data client dictation; otherwise, <c>false</c>.
        /// </value>
        public bool IsDataClientDictation { get; set; }

        /// <summary>
        /// Gets or sets subscription key
        /// </summary>
        public string SubscriptionKey
        {
            get
            {
                return this.subscriptionKey;
            }

            set
            {
                this.subscriptionKey = value;
                this.OnPropertyChanged<string>();
            }
        }

        /// <summary>
        /// Gets the LUIS endpoint URL.
        /// </summary>
        /// <value>
        /// The LUIS endpoint URL.
        /// </value>
        private string LuisEndpointUrl
        {
            get { return ConfigurationManager.AppSettings["LuisEndpointUrl"]; }
        }

        /// <summary>
        /// Gets a value indicating whether or not to use the microphone.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use microphone]; otherwise, <c>false</c>.
        /// </value>
        private bool UseMicrophone
        {
            get
            {
                return this.IsMicrophoneClientWithIntent ||
                    this.IsMicrophoneClientDictation ||
                    this.IsMicrophoneClientShortPhrase;
            }
        }

        /// <summary>
        /// Gets a value indicating whether LUIS results are desired.
        /// </summary>
        /// <value>
        ///   <c>true</c> if LUIS results are to be returned otherwise, <c>false</c>.
        /// </value>
        private bool WantIntent
        {
            get
            {
                return !string.IsNullOrEmpty(this.LuisEndpointUrl) &&
                    (this.IsMicrophoneClientWithIntent || this.IsDataClientWithIntent);
            }
        }

        /// <summary>
        /// Gets the current speech recognition mode.
        /// </summary>
        /// <value>
        /// The speech recognition mode.
        /// </value>
        private SpeechRecognitionMode Mode
        {
            get
            {
                if (this.IsMicrophoneClientDictation ||
                    this.IsDataClientDictation)
                {
                    return SpeechRecognitionMode.LongDictation;
                }

                return SpeechRecognitionMode.ShortPhrase;
            }
        }

        /// <summary>
        /// Gets the default locale.
        /// </summary>
        /// <value>
        /// The default locale.
        /// </value>
        private string DefaultLocale
        {
            get { return "en-US"; }
        }

        /// <summary>
        /// Gets the Cognitive Service Authentication Uri.
        /// </summary>
        /// <value>
        /// The Cognitive Service Authentication Uri.  Empty if the global default is to be used.
        /// </value>
        private string AuthenticationUri
        {
            get
            {
                return ConfigurationManager.AppSettings["AuthenticationUri"];
            }
        }

        /// <summary>
        /// Raises the System.Windows.Window.Closed event.
        /// </summary>
        /// <param name="e">An System.EventArgs that contains the event data.</param>
        protected override void OnClosed(EventArgs e)
        {
            if (null != this.dataClient)
            {
                this.dataClient.Dispose();
            }

            if (null != this.micClient)
            {
                this.micClient.Dispose();
            }

            base.OnClosed(e);
        }

        /// <summary>
        /// Initializes a fresh audio session.
        /// </summary>
        private void Initialize()
        {
            this.subscriptionKey = "77b4b6d16a5547d28575c12dd73352e7";

            this.IsMicrophoneClientShortPhrase = true;
            this.IsMicrophoneClientWithIntent = false;
            this.IsMicrophoneClientDictation = false;
            this.IsDataClientShortPhrase = false;
            this.IsDataClientWithIntent = false;
            this.IsDataClientDictation = false;

            // Set the default choice for the group of checkbox.
            this._micRadioButton.IsChecked = true;

            this.SubscriptionKey = this.GetSubscriptionKeyFromIsolatedStorage();
        }

        /// <summary>
        /// Handles the Click event of the _startButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            this._startButton.IsEnabled = false;
            this._radioGroup.IsEnabled = false;

            this.LogRecognitionStart();

            if (this.micClient == null)
            {
                if (this.WantIntent)
                {
                    this.CreateMicrophoneRecoClientWithIntent();
                }
                else
                {
                    this.CreateMicrophoneRecoClient();
                }
            }

            this.micClient.StartMicAndRecognition();
        }

        /// <summary>
        /// Logs the recognition start.
        /// </summary>
        private void LogRecognitionStart()
        {
            statusText.Text = "Start speech recognition with " + this.Mode + " mode in " + this.inputLanguage + " language, please start speaking.";
        }

        /// <summary>
        /// Creates a new microphone reco client without LUIS intent support.
        /// </summary>
        private void CreateMicrophoneRecoClient()
        {
            this.micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                this.Mode,
                this.DefaultLocale,
                this.SubscriptionKey);
            this.micClient.AuthenticationUri = this.AuthenticationUri;

            // Event handlers for speech recognition results
            this.micClient.OnMicrophoneStatus += this.OnMicrophoneStatus;
            this.micClient.OnPartialResponseReceived += this.OnPartialResponseReceivedHandler;
            if (this.Mode == SpeechRecognitionMode.ShortPhrase)
            {
                this.micClient.OnResponseReceived += this.OnMicShortPhraseResponseReceivedHandler;
            }
            else if (this.Mode == SpeechRecognitionMode.LongDictation)
            {
                this.micClient.OnResponseReceived += this.OnMicDictationResponseReceivedHandler;
            }

            this.micClient.OnConversationError += this.OnConversationErrorHandler;
        }

        /// <summary>
        /// Creates a new microphone reco client with LUIS intent support.
        /// </summary>
        private void CreateMicrophoneRecoClientWithIntent()
        {
            this.micClient =
                SpeechRecognitionServiceFactory.CreateMicrophoneClientWithIntentUsingEndpointUrl(
                    this.DefaultLocale,
                    this.SubscriptionKey,
                    this.LuisEndpointUrl);
            this.micClient.AuthenticationUri = this.AuthenticationUri;
            this.micClient.OnIntent += this.OnIntentHandler;

            // Event handlers for speech recognition results
            this.micClient.OnMicrophoneStatus += this.OnMicrophoneStatus;
            this.micClient.OnPartialResponseReceived += this.OnPartialResponseReceivedHandler;
            this.micClient.OnResponseReceived += this.OnMicShortPhraseResponseReceivedHandler;
            this.micClient.OnConversationError += this.OnConversationErrorHandler;
        }

        /// <summary>
        /// Handles the Click event of the HelpButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.projectoxford.ai/doc/general/subscription-key-mgmt");
        }

        /// <summary>
        /// Creates a data client without LUIS intent support.
        /// Speech recognition with data (for example from a file or audio source).  
        /// The data is broken up into buffers and each buffer is sent to the Speech Recognition Service.
        /// No modification is done to the buffers, so the user can apply their
        /// own Silence Detection if desired.
        /// </summary>
        private void CreateDataRecoClient()
        {
            this.dataClient = SpeechRecognitionServiceFactory.CreateDataClient(
                this.Mode,
                this.DefaultLocale,
                this.SubscriptionKey);
            this.dataClient.AuthenticationUri = this.AuthenticationUri;

            // Event handlers for speech recognition results
            if (this.Mode == SpeechRecognitionMode.ShortPhrase)
            {
                this.dataClient.OnResponseReceived += this.OnDataShortPhraseResponseReceivedHandler;
            }
            else
            {
                this.dataClient.OnResponseReceived += this.OnDataDictationResponseReceivedHandler;
            }

            this.dataClient.OnPartialResponseReceived += this.OnPartialResponseReceivedHandler;
            this.dataClient.OnConversationError += this.OnConversationErrorHandler;
        }

        /// <summary>
        /// Creates a data client with LUIS intent support.
        /// Speech recognition with data (for example from a file or audio source).  
        /// The data is broken up into buffers and each buffer is sent to the Speech Recognition Service.
        /// No modification is done to the buffers, so the user can apply their
        /// own Silence Detection if desired.
        /// </summary>
        private void CreateDataRecoClientWithIntent()
        {
            this.dataClient = SpeechRecognitionServiceFactory.CreateDataClientWithIntentUsingEndpointUrl(
                this.DefaultLocale,
                this.SubscriptionKey,
                this.LuisEndpointUrl);
            this.dataClient.AuthenticationUri = this.AuthenticationUri;

            // Event handlers for speech recognition results
            this.dataClient.OnResponseReceived += this.OnDataShortPhraseResponseReceivedHandler;
            this.dataClient.OnPartialResponseReceived += this.OnPartialResponseReceivedHandler;
            this.dataClient.OnConversationError += this.OnConversationErrorHandler;

            // Event handler for intent result
            this.dataClient.OnIntent += this.OnIntentHandler;
        }

        /// <summary>
        /// Sends the audio helper.
        /// </summary>
        /// <param name="wavFileName">Name of the wav file.</param>
        private void SendAudioHelper(string wavFileName)
        {
            using (FileStream fileStream = new FileStream(wavFileName, FileMode.Open, FileAccess.Read))
            {
                // Note for wave files, we can just send data from the file right to the server.
                // In the case you are not an audio file in wave format, and instead you have just
                // raw data (for example audio coming over bluetooth), then before sending up any 
                // audio data, you must first send up an SpeechAudioFormat descriptor to describe 
                // the layout and format of your raw audio data via DataRecognitionClient's sendAudioFormat() method.
                int bytesRead = 0;
                byte[] buffer = new byte[1024];

                try
                {
                    do
                    {
                        // Get more Audio data to send into byte buffer.
                        bytesRead = fileStream.Read(buffer, 0, buffer.Length);

                        // Send of audio data to service. 
                        this.dataClient.SendAudio(buffer, bytesRead);
                    }
                    while (bytesRead > 0);
                }
                finally
                {
                    // We are done sending audio.  Final recognition results will arrive in OnResponseReceived event call.
                    this.dataClient.EndAudio();
                }
            }
        }

        /// <summary>
        /// Called when a final response is received;
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private void OnMicShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {

                statusText.Clear();
                statusText.Text = "Finished short response. Displaying results above...";
                // we got the final result, so it we can end the mic reco.  No need to do this
                // for dataReco, since we already called endAudio() on it as soon as we were done
                // sending all the data.
                this.micClient.EndMicAndRecognition();

                this.WriteResponseResult(e);
                _startButton.IsEnabled = true;
                _radioGroup.IsEnabled = true;
            }));
        }

        /// <summary>
        /// Called when a final response is received;
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private void OnDataShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                this.WriteLine("--- OnDataShortPhraseResponseReceivedHandler ---");

                // we got the final result, so it we can end the mic reco.  No need to do this
                // for dataReco, since we already called endAudio() on it as soon as we were done
                // sending all the data.
                this.WriteResponseResult(e);

                _startButton.IsEnabled = true;
                _radioGroup.IsEnabled = true;
            }));
        }

        /// <summary>
        /// Writes the response result.
        /// </summary>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private void WriteResponseResult(SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.Results.Length == 0)
            {
                this.WriteLine("No phrase response is available.");
            }
            else
            {
                this.WriteLine(e.PhraseResponse.Results[0].DisplayText);
                this.WriteLine();
            }
        }

        /// <summary>
        /// Called when a final response is received;
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private void OnMicDictationResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            this.WriteLine("--- OnMicDictationResponseReceivedHandler ---");
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation ||
                e.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
            { 
                Dispatcher.Invoke(
                    (Action)(() => 
                    {
                        // we got the final result, so it we can end the mic reco.  No need to do this
                        // for dataReco, since we already called endAudio() on it as soon as we were done
                        // sending all the data.
                        this.micClient.EndMicAndRecognition();

                        this._startButton.IsEnabled = true;
                        this._radioGroup.IsEnabled = true;
                    }));                
            }

            this.WriteResponseResult(e);
        }

        /// <summary>
        /// Called when a final response is received;
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private void OnDataDictationResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            this.WriteLine("--- OnDataDictationResponseReceivedHandler ---");
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation ||
                e.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
            {
                Dispatcher.Invoke(
                    (Action)(() => 
                    {
                        _startButton.IsEnabled = true;
                        _radioGroup.IsEnabled = true;

                        // we got the final result, so it we can end the mic reco.  No need to do this
                        // for dataReco, since we already called endAudio() on it as soon as we were done
                        // sending all the data.
                    }));
            }

            this.WriteResponseResult(e);
        }

        /// <summary>
        /// Called when a final response is received and its intent is parsed
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechIntentEventArgs"/> instance containing the event data.</param>
        private void OnIntentHandler(object sender, SpeechIntentEventArgs e)
        {
            this.WriteLine("--- Intent received by OnIntentHandler() ---");
            this.WriteLine("{0}", e.Payload);
            this.WriteLine();
        }

        /// <summary>
        /// Called when a partial response is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="PartialSpeechResponseEventArgs"/> instance containing the event data.</param>
        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            this.WriteLine(e.PartialResult);
            this.WriteLine();
        }

        /// <summary>
        /// Called when an error is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechErrorEventArgs"/> instance containing the event data.</param>
        private void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e)
        {
           Dispatcher.Invoke(() =>
           {
               _startButton.IsEnabled = true;
               _radioGroup.IsEnabled = true;
           });

            this.WriteLine("--- Error received by OnConversationErrorHandler() ---");
            this.WriteLine("Error code: {0}", e.SpeechErrorCode.ToString());
            this.WriteLine("Error text: {0}", e.SpeechErrorText);
            this.WriteLine();
        }

        /// <summary>
        /// Called when the microphone status has changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="MicrophoneEventArgs"/> instance containing the event data.</param>
        private void OnMicrophoneStatus(object sender, MicrophoneEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
            });
        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        private void WriteLine()
        {
            this.WriteLine(string.Empty);
        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        private void WriteLine(string format, params object[] args)
        {
            var formattedStr = string.Format(format, args);
            Trace.WriteLine(formattedStr);
            Dispatcher.Invoke(() =>
            {
                _logText.Text += (formattedStr + "\n");
                _logText.ScrollToEnd();
            });
        }

        /// <summary>
        /// Gets the subscription key from isolated storage.
        /// </summary>
        /// <returns>The subscription key.</returns>
        private string GetSubscriptionKeyFromIsolatedStorage()
        {
            string subscriptionKey = null;

            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null))
            {
                try
                {
                    using (var iStream = new IsolatedStorageFileStream(IsolatedStorageSubscriptionKeyFileName, FileMode.Open, isoStore))
                    {
                        using (var reader = new StreamReader(iStream))
                        {
                            subscriptionKey = reader.ReadLine();
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    subscriptionKey = null;
                }
            }

            if (string.IsNullOrEmpty(subscriptionKey))
            {
                subscriptionKey = DefaultSubscriptionKeyPromptMessage;
            }

            return subscriptionKey;
        }

        /// <summary>
        /// Helper function for INotifyPropertyChanged interface 
        /// </summary>
        /// <typeparam name="T">Property type</typeparam>
        /// <param name="caller">Property name</param>
        private void OnPropertyChanged<T>([CallerMemberName]string caller = null)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(caller));
            }
        }

        /// <summary>
        /// Handles the Click event of the RadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset everything
            if (this.micClient != null)
            {
                this.micClient.EndMicAndRecognition();
                this.micClient.Dispose();
                this.micClient = null;
            }

            if (this.dataClient != null)
            {
                this.dataClient.Dispose();
                this.dataClient = null;
            }

            this._logText.Text = string.Empty;
            this._startButton.IsEnabled = true;
            this._radioGroup.IsEnabled = true;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            inputLanguage = "en-US";
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            inputLanguage = "zh-cn";
        }

        private void RadioButton_Checked_2(object sender, RoutedEventArgs e)
        {
            inputLanguage = "es-es";
        }

        private void _micRadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void _micDictationRadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void _micIntentRadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
