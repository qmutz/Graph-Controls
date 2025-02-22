// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Net.Authentication;
using CommunityToolkit.Uwp.Authentication;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SampleTest
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        // Which provider should be used for authentication?
        private readonly ProviderType _providerType = ProviderType.Mock;

        // List of available authentication providers.
        private enum ProviderType
        {
            Mock,
            Msal,
            Windows
        }

        /// <summary>
        /// Initialize the global authentication provider.
        /// </summary>
        private async void InitializeGlobalProvider()
        {
            if (ProviderManager.Instance.GlobalProvider != null)
            {
                return;
            }

            await Window.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // Provider config
                string clientId = "YOUR_CLIENT_ID_HERE";
                string[] scopes = { "User.Read", "User.ReadBasic.All", "People.Read", "Calendars.Read", "Mail.Read", "Group.Read.All", "ChannelMessage.Read.All" };
                bool autoSignIn = true;

                switch (_providerType)
                {
                    // Mock provider
                    case ProviderType.Mock:
                        ProviderManager.Instance.GlobalProvider = new MockProvider(signedIn: autoSignIn);
                        break;

                    // Msal provider
                    case ProviderType.Msal:
                        ProviderManager.Instance.GlobalProvider = new MsalProvider(clientId: clientId, scopes: scopes, autoSignIn: autoSignIn);
                        break;

                    // Windows provider
                    case ProviderType.Windows:
                        var webAccountProviderConfig = new WebAccountProviderConfig(WebAccountProviderType.MSA, clientId);
                        ProviderManager.Instance.GlobalProvider = new WindowsProvider(scopes, webAccountProviderConfig: webAccountProviderConfig, autoSignIn: autoSignIn);
                        break;
                }
            });
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();

                InitializeGlobalProvider();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
