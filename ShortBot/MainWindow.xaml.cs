using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using ShortBot.Url;

namespace ShortBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly Regex ProxyRegex = new Regex(@"(?<url>.*)(?<port>:\d+)");
        private readonly ProxyPool _proxies;
        private readonly List<string> _urls;
        private bool _running;
        private volatile bool _shouldStop;

        private List<IUrlShortener> _shorteners; 
          
        public MainWindow()
        {
            InitializeComponent();
            _proxies = new ProxyPool();
            _urls = new List<string>();
            _running = false;
            _shouldStop = false;
            _shorteners = new List<IUrlShortener>();
            _shorteners.Add(new McAfeeShortener());
            _shorteners.Add(new TinyCCShortener());
            _shorteners.Add(new TinyUrlShortener());
            // _shorteners.Add(new IsGdShortener());
        }

        private void sliderThreads_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lblThreadCount.Content = sliderThreads.Value.ToString(CultureInfo.CurrentCulture);
        }

        private async void btnLoadProxies_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Text Files (*.txt)|*.txt",
                CheckFileExists = true,
                CheckPathExists = true
            };
            if (!(bool)dialog.ShowDialog(this))
                return;
            var count = 0;
            using (var reader = new StreamReader(File.OpenRead(dialog.FileName)))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var matches = ProxyRegex.Matches(line);
                    foreach (Match m in matches)
                    {
                        _proxies.Add(new WebProxy(m.Result("${url}${port}")));
                        count++;
                    }

                    //Console.WriteLine(_proxies.Count);
                }
            }
            ListBoxLogs.Items.Add("Loaded " + count + " proxies.");
            lblProxyCount.Content = _proxies.Count.ToString(CultureInfo.CurrentCulture);
        }

        private async void btnLoadUrls_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Text Files (*.txt)|*.txt",
                CheckFileExists = true,
                CheckPathExists = true
            };
            if (!(bool)dialog.ShowDialog(this))
                return;
            var count = 0;
            using (var reader = new StreamReader(File.OpenRead(dialog.FileName)))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    _urls.Add(line);
                    count++;
                }
            }
            ListBoxLogs.Items.Add("Loaded " + count + " urls.");
            lblUrlCount.Content = _urls.Count.ToString(CultureInfo.CurrentCulture);
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            var maxThreads = new SemaphoreSlim((int) sliderThreads.Value);
            var threads = new List<Task>();
            _running = true;
            //var threads = new List<Task>();
            foreach (var url in _urls)
            {
                await maxThreads.WaitAsync();
                threads.Add(Task.Run(async () =>
                {
                    foreach (var shortener in _shorteners)
                    {
                        var count = 0;
                        while (count < 3)
                        {
                            try
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    ListBoxLogs.Items.Add("Attempting to create link on " + shortener.Name + "...");
                                });
                                var shortened = await shortener.ShortenUrl(_proxies.GetOldestProxy(), url);
                                Dispatcher.Invoke(() =>
                                {
                                    ListBoxLogs.Items.Add("Successfully created link: " + shortened);
                                    listUrlsCreated.Items.Add(shortened);
                                });
                                break;
                            }
                            catch (Exception)
                            {
                                count++;
                            }
                        }
                        if (count == 3)
                        {
                            Dispatcher.Invoke(
                                () => { ListBoxLogs.Items.Add("Failed to create link: " + shortener.Name); });
                        }
                    }
                }).ContinueWith(task =>
                {
                    maxThreads.Release();
                }));
            }

            await Task.WhenAll(threads);
            ListBoxLogs.Items.Add("Finished creating links for " + _urls.Count + " urls.");

            lblThreadCount.Content = sliderThreads.Value.ToString(CultureInfo.CurrentCulture);
            
            threads.Clear();

            // ReSharper disable once PossibleInvalidOperationException
            if ((bool)RadioIndexPages.IsChecked)
            {
                ListBoxLogs.Items.Add("Indexing " + listUrlsCreated.Items.Count + " links...");
                foreach (string name in listUrlsCreated.Items)
                {
                    await maxThreads.WaitAsync();
                    threads.Add(Task.Run(async () =>
                    {
                        using (var client = HttpHelper.CreateHttpClient(_proxies.GetOldestProxy()))
                        {
                            var count = 0;
                            Dispatcher.Invoke(() =>
                            {
                                ListBoxLogs.Items.Add("Attempting to index link: " + name);
                            });
                            while (count < 3)
                            {
                                
                                try
                                {
                                    await client.GetAsync("https://developers.google.com/speed/pagespeed/insights/?url=" +
                                                    name);
                                    Dispatcher.Invoke(() =>
                                    {
                                        ListBoxLogs.Items.Add("Indexed link: " + name);
                                    });
                                    break;
                                }
                                catch
                                {
                                    count++;
                                }
                            }
                            if (count == 3)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    ListBoxLogs.Items.Add("Failed to index link: " + name);
                                });
                            }
                        }
                    }).ContinueWith(task =>
                    {
                        maxThreads.Release();
                    }));

                }
            }

            await Task.WhenAll(threads);
            _running = false;
            _shouldStop = false;

            MessageBox.Show(this, "Done!", "ShortBot");
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (!_running)
                return;
            _shouldStop = true;
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_running)
            {
                MessageBox.Show(this, "Currently running. Press stop, then press save when urls are not being added.",
                    "Error");
                return;
            }
            var save = new SaveFileDialog()
            {
                CheckPathExists = true,
                Filter = "Text Files (*.txt)|*.txt"
            };
            if (!(bool) save.ShowDialog(this))
                return;
            using (var writer = new StreamWriter(File.OpenWrite(save.FileName)))
            {
                foreach (string item in listUrlsCreated.Items)
                {
                    await writer.WriteLineAsync(item);
                }
            }
        }

        private void Window_Initialized(object sender, EventArgs e)
        {

        }
    }
}
