using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;
using System;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Linq;
using YoutubeExplode.Common;
using YoutubeExplode.Search;
using System.Windows.Media;
using MahApps.Metro.IconPacks;
using System.Windows.Controls;

namespace PlayerTube
{
    public partial class MainWindow : Window
    {
        private YoutubeClient _youtubeClient = new YoutubeClient();
        private string currentVideoId = string.Empty;
        private bool isPlaying = false;
        private DispatcherTimer timer;
        private MediaPlayer mediaPlayer = new MediaPlayer();

        public MainWindow()
        {
            InitializeComponent();

            // Configurar timer para atualizar o slider
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
        }

        private async void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxMode.SelectedIndex == 0) // Link mode
            {
                if (isPlaying)
                {
                    mediaPlayer.Pause();
                    btnPlayPause.Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                {
                    new PackIconMaterial { Kind = PackIconMaterialKind.Play, Width = 16, Height = 16 },
                    new TextBlock { Text = "Play", Margin = new Thickness(5, 0, 0, 0) }
                }
                    };
                    isPlaying = false;
                }
                else
                {
                    if (mediaPlayer.Source != null)
                    {
                        mediaPlayer.Play();
                        btnPlayPause.Content = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Children =
                    {
                        new PackIconMaterial { Kind = PackIconMaterialKind.Pause, Width = 16, Height = 16 },
                        new TextBlock { Text = "Pause", Margin = new Thickness(5, 0, 0, 0) }
                    }
                        };
                        isPlaying = true;
                    }
                    else
                    {
                        string url = txtUrl.Text;

                        if (string.IsNullOrWhiteSpace(url))
                        {
                            MessageBox.Show("Cola um link do YouTube aí, fera!");
                            return;
                        }

                        try
                        {
                            var videoId = ExtrairIdDoVideo(url);
                            if (string.IsNullOrEmpty(videoId))
                            {
                                MessageBox.Show("Não foi possível extrair o ID do vídeo.");
                                return;
                            }

                            var video = await _youtubeClient.Videos.GetAsync(videoId);
                            currentVideoId = video.Id;  // Salva o ID do vídeo atual
                            txtNomeMusica.Text = video.Title;

                            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id);
                            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                            if (audioStreamInfo != null)
                            {
                                // Toca a música inicial
                                mediaPlayer.Open(new Uri(audioStreamInfo.Url));
                                mediaPlayer.Play();

                                isPlaying = true;
                                btnPlayPause.Content = new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    Children =
                            {
                                new PackIconMaterial { Kind = PackIconMaterialKind.Pause, Width = 16, Height = 16 },
                                new TextBlock { Text = "Pause", Margin = new Thickness(5, 0, 0, 0) }
                            }
                                };
                                timer.Start();
                            }
                            else
                            {
                                MessageBox.Show("Não foi possível encontrar o áudio!");
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Erro ao carregar a música: {ex.Message}");
                        }
                    }
                }
            }
            else // Search mode
            {
                string query = txtUrl.Text;

                if (string.IsNullOrWhiteSpace(query))
                {
                    MessageBox.Show("Digite um termo de busca!");
                    return;
                }

                try
                {
                    var searchResults = await _youtubeClient.Search.GetVideosAsync(query);
                    if (dataGridResults != null)
                    {
                        dataGridResults.ItemsSource = searchResults.Select(video => new
                        {
                            Title = video.Title,
                            Author = video.Author.Title,
                            Duration = video.Duration?.ToString(@"hh\:mm\:ss")
                        }).ToList();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao realizar a busca: {ex.Message}");
                }
            }
        }

        private void ComboBoxMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataGridResults != null)
            {
                dataGridResults.ItemsSource = null; // Limpa os resultados de busca anteriores
            }
            txtUrl.Text = string.Empty; // Limpa o campo de entrada

            // Mostra ou oculta o botão de pesquisa com base na seleção do ComboBox
            if (comboBoxMode.SelectedIndex == 1) // Procurar
            {
                btnSearch.Visibility = Visibility.Visible;
            }
            else
            {
                btnSearch.Visibility = Visibility.Collapsed;
            }
        }


        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string query = txtUrl.Text;

            if (string.IsNullOrWhiteSpace(query))
            {
                MessageBox.Show("Digite um termo de busca!");
                return;
            }

            try
            {
                var searchResults = await _youtubeClient.Search.GetVideosAsync(query);
                dataGridResults.ItemsSource = searchResults.Select(video => new
                {
                    Title = video.Title,
                    Author = video.Author.Title,
                    Duration = video.Duration?.ToString(@"hh\:mm\:ss")
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao realizar a busca: {ex.Message}");
            }
        }





        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
            btnPlayPause.Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new PackIconMaterial { Kind = PackIconMaterialKind.Play, Width = 16, Height = 16 },
                    new TextBlock { Text = "Play", Margin = new Thickness(5, 0, 0, 0) }
                }
            };
            isPlaying = false;
            timer.Stop();
        }


        private void DataGridResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataGridResults != null && dataGridResults.SelectedItem != null)
            {
                var selectedItem = dataGridResults.SelectedItem;
                if (selectedItem != null)
                {
                    var selectedVideoTitle = (selectedItem.GetType().GetProperty("Title").GetValue(selectedItem, null) as string) ?? string.Empty;
                    txtUrl.Text = selectedVideoTitle;
                    txtNomeMusica.Text = selectedVideoTitle;
                    dataGridResults.ItemsSource = null;  // Clear search results after selection
                }
            }
            else
            {
                MessageBox.Show("O DataGrid não está inicializado corretamente.");
            }
        }



        private async void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            try
            {
                // Busca o próximo vídeo relacionado ao artista
                string nextVideoId = await ObterProximoVideo(currentVideoId);

                if (!string.IsNullOrEmpty(nextVideoId) && nextVideoId != currentVideoId)
                {
                    var video = await _youtubeClient.Videos.GetAsync(nextVideoId);
                    txtNomeMusica.Text = video.Title;
                    var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id);
                    var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                    if (audioStreamInfo != null)
                    {
                        // Reproduz o próximo vídeo
                        mediaPlayer.Open(new Uri(audioStreamInfo.Url));
                        mediaPlayer.Play();
                        currentVideoId = nextVideoId;  // Atualiza o ID do vídeo atual
                        btnPlayPause.Content = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Children =
                    {
                        new PackIconMaterial { Kind = PackIconMaterialKind.Pause, Width = 16, Height = 16 },
                        new TextBlock { Text = "Pause", Margin = new Thickness(5, 0, 0, 0) }
                    }
                        };
                    }
                    else
                    {
                        MessageBox.Show("Não foi possível encontrar o áudio da próxima música!");
                    }
                }
                else
                {
                    MessageBox.Show("Não há mais vídeos relacionados disponíveis ou o próximo vídeo é o mesmo.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar a próxima música: {ex.Message}");
            }
        }

        private async Task<string> ObterProximoVideo(string videoId)
        {
            try
            {
                // Obtém o vídeo atual usando o ID
                var video = await _youtubeClient.Videos.GetAsync(videoId);
                var videoTitle = video.Title;

                // Tenta extrair o nome do artista do título (assumindo formato "Artista - Música")
                var artista = videoTitle.Split('-').FirstOrDefault()?.Trim();

                if (string.IsNullOrEmpty(artista))
                {
                    MessageBox.Show("Não foi possível extrair o nome do artista.");
                    return null;
                }

                // Realiza uma busca por vídeos do mesmo artista
                var searchResults = await _youtubeClient.Search.GetVideosAsync(artista);

                // Obtém o primeiro vídeo que não seja o atual
                var nextVideoId = searchResults.FirstOrDefault(v => v.Id != videoId)?.Id.ToString();

                return nextVideoId;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao obter vídeos relacionados: {ex.Message}");
                return null;
            }
        }

        private void MediaPlayer_MediaOpened(object sender, EventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                sliderTempo.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                sliderTempo.SmallChange = 1;
                sliderTempo.LargeChange = Math.Min(10, mediaPlayer.NaturalDuration.TimeSpan.Seconds / 10);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (mediaPlayer.Source != null && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                sliderTempo.Value = mediaPlayer.Position.TotalSeconds;
            }
        }

        private string ExtrairIdDoVideo(string url)
        {
            var uri = new Uri(url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return query["v"];
        }

    }
}
